using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using SOD.Common.Extensions;
using UniverseLib;
using UniverseLib.Utility;

namespace PresetEdit;
public class JsonSoCustomComparisonConverter<T> : JsonConverter<T> where T : SoCustomComparison {
    public const string PRESET_PROPERTY_SEPARATOR = "__PRESET__";

    public override bool CanConvert(Type typeToConvert) {
        Plugin.Log.LogInfo("It's checking me!");
        var result = typeToConvert.IsAssignableTo(typeof(SoCustomComparison));
        Plugin.Log.LogInfo($"{typeToConvert} vs. SoCustomComparison? I will return {result}");
        return result;
    }

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        Plugin.Log.LogInfo("HELLO, we are trying to read.");
        var presetName = reader
            .GetString()
            .Split(PRESET_PROPERTY_SEPARATOR, StringSplitOptions.RemoveEmptyEntries)
            .Last();
        var preset = Helpers.GetAllUnityObjectsOfType(typeToConvert).WhereUnityOrPresetNameEquals(presetName).FirstOrDefault();
        if (preset == default || preset.IsNullOrDestroyed()) {
            throw new InvalidOperationException("Preset not found; ensure you are in an active savegame, and that any custom presets that are being loaded have their corresponding plugin(s) installed.");
        }
        return preset.TryCast<T>();
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) {
        writer.WriteStringValue(GetJsonString(value));
    }

    public static string GetJsonString(object value) {
        string presetName;
        try {
            presetName = ((SoCustomComparison)value).name;
        }
        catch {
            Plugin.Log.LogWarning($"{value}: Name was null, trying alternative method to get the preset name.");
            presetName = ((SoCustomComparison)value).presetName;
        }
        return $"{value.GetActualType().FullName}{PRESET_PROPERTY_SEPARATOR}{presetName}";
    }

}
