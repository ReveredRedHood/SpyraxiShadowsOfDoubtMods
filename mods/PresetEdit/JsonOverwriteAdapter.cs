using System;
using System.Collections.Generic;
using System.Linq;
using UniverseLib;

namespace PresetEdit;

public class JsonOverwriteAdapter {
    public static readonly string[] DEFAULT_CUSTOM_INSTRUCTIONS = [
        // "remove these elements from the target list if they are present"
        "difference-from-current",
        // "remove the target list's elements from these elements, set the target list to what's left"
        "difference-from-incoming", 
        // "remove list elements that don't match these"
        "intersection",
        // "add these elements, then afterwards remove anything that is a duplicate including the original"
        "symmetric-difference",
        // "add these elements, ensure there is a maximum of one of each kind afterwards (no duplicates)"
        "add-union-no-dupes",
        // "add these elements, allow duplicates"
        "add-union",
        // "keep the target list how it was"
        "ignore",
    ];

    /// <summary>
    /// Given the property name, the type of the property (managed/non-il2cpp),
    /// current value of the property (managed), incoming value as read from
    /// json (managed), and the custom instruction; return the value (managed)
    /// to write to the property on the preset instance. Override in a child
    /// class of JsonOverwriteAdapter to implement custom instructions or
    /// additional functionality.
    /// 
    /// Example: 
    /// The json is...
    /// {
    ///     "itemsSold__CUSTOM": "difference-from-current",
    ///     "itemsSold": [
    ///         "InteractablePreset__PRESET__Donut",
    ///         "InteractablePreset__PRESET__Splint",
    ///         "InteractablePreset__PRESET__Tracker",
    ///     ]
    /// }
    /// This method will be called with the signature...
    /// GetResultValue(
    ///     "itemsSold", 
    ///     typeof List_InteractablePreset, 
    ///     the value of itemsSold on the preset instance as a List_InteractablePreset, 
    ///     List_InteractablePreset(Donut, Splint, Tracker), 
    ///     "difference-from-current"
    /// )
    /// For DefaultJsonOverwriteAdapter, this would overwrite the .itemsSold
    /// List on the preset instance so that it includes its original elements
    /// minus the three written in the json file. (because of the
    /// "difference-from-current" customInstruction). Without a
    /// customInstruction, DefaultJsonOverwriteAdapter will replace the list
    /// to match what is written in the json file.
    /// </summary>
    /// <param name="propertyName">The name of the property to be overwritten</param>
    /// <param name="typeManaged">The non-il2cpp System.Type of both currentValueManaged and incomingValue</param>
    /// <param name="currentValueManaged">The value of the property on the preset instance</param>
    /// <param name="incomingValue">The deserialized value from json</param>
    /// <param name="customInstruction">The custom instruction, if provided with the json data</param>
    /// <param name="gameBuildId">The game build ID from when the json file was exported</param>
    /// <param name="presetEditPluginVersion">The version of the preset edit plugin that the json file was exported using</param>
    /// <returns>The value to write to the object (managed type)</returns>
    public virtual object GetResultValue(string propertyName, Type typeManaged, object currentValueManaged, object incomingValue, string customInstruction = null, string gameBuildId = default, string presetEditPluginVersion = default) {
        if (!typeManaged.IsGenericType
        || !typeManaged.GetGenericTypeDefinition().FullName.Contains("System.Collections.Generic")) {
            // No need to change incoming value, it's already deserialized
            LogReturnValue(propertyName, customInstruction, incomingValue);
            return incomingValue;
        }
        if (customInstruction == "ignore" || propertyName == "presetName") {
            LogReturnValue(propertyName, customInstruction, currentValueManaged);
            return currentValueManaged;
        }
        if (customInstruction == null || customInstruction == default) {
            // Default behavior for a list is to replace
            LogReturnValue(propertyName, customInstruction, incomingValue);
            return incomingValue;
        }
        var currList = currentValueManaged.FormListIfEnumerable();
        var incList = incomingValue.FormListIfEnumerable();
        var isPresetList = typeManaged.GetGenericArguments()[0].IsAssignableTo(typeof(SoCustomComparison));
        switch (customInstruction) {
            case "difference-from-current":
                incomingValue = isPresetList ? incList.Except(currList, new PresetComparer()) : incList.Except(currList);
                break;
            case "difference-from-incoming":
                incomingValue = isPresetList ? incList.Except(currList, new PresetComparer()) : incList.Except(currList);
                break;
            case "intersection":
                incomingValue = isPresetList ? currList.Intersect(incList, new PresetComparer()) : currList.Intersect(incList);
                break;
            case "symmetric-difference":
                incomingValue = isPresetList ? currList.Except(incList, new PresetComparer()).Union(incList.Except(currList, new PresetComparer())) : currList.Except(incList).Union(incList.Except(currList));
                break;
            case "add-union-no-dupes":
                incomingValue = isPresetList ? currList.Union(incList, new PresetComparer()) : currList.Union(incList);
                break;
            case "add-union":
                incomingValue = currList.Union(incList);
                break;
            default:
                Plugin.Log.LogWarning($"Custom instruction not recognized: \"{customInstruction}\"");
                break;
        }
        LogReturnValue(propertyName, customInstruction, incomingValue);
        return incomingValue;

        static void LogReturnValue(string propertyName, string customInstruction, object returnValue) {
            if (!UniverseLib.ReflectionUtility.IsEnumerable(returnValue.GetActualType()) || returnValue.GetActualType() == typeof(string)) {
                Plugin.Log.LogDebug($"-- Result for ({customInstruction ?? "default"}, {propertyName}) is {returnValue}.");
                return;
            }
            var asEnumerable = returnValue.FormListIfEnumerable();
            IEnumerable<object> outputFrom = asEnumerable;
            if (asEnumerable.Where(obj => obj.GetActualType().IsAssignableTo(typeof(SoCustomComparison))).Count() > 0) {
                outputFrom = asEnumerable.TryCastAll<SoCustomComparison>().Select(preset => Helpers.GetPresetKey(preset));
            }
            Plugin.Log.LogDebug($"-- Result for ({customInstruction ?? "default"}, {propertyName}) is {string.Join(", ", outputFrom) ?? "null"}.");
        }
    }
}