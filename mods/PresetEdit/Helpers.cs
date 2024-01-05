using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Il2CppInterop.Runtime;
using SOD.Common.Extensions;
using UniverseLib;

namespace PresetEdit;
public static class Helpers {
    public static bool IsTypeConsistent(this IEnumerable<object> enumerable) {
        return enumerable.DistinctBy(element => element.GetType()).Count() == 1;
    }

    public static bool IsExplicitlyIl2CppType(this Type type) {
        return type.FullName.Contains("Il2Cpp") || type.FullName.Contains("IL2CPP");
    }

    public static Type AttemptToConvertToManagedType(this Type typeToConvert) {
        var name = typeToConvert.FullName;
        if (name.Contains("Il2CppSystem")) {
            var nonIl2CppName = name.Replace("Il2Cpp", "").Replace("IL2CPP", "");
            return UniverseLib.Il2CppReflection.GetTypeByName(nonIl2CppName);
        }
        throw new System.ArgumentException($"Type to convert ({typeToConvert}) must be a Il2Cpp type.");
    }

    public static void ReplaceElements<T>(this Il2CppSystem.Collections.Generic.List<T> enumerable, in List<T> replacements) {
        enumerable.Clear();
        replacements.ForEach(replacement => enumerable.Add(replacement));
    }

    public static Type GetIl2CppAssignableToTargetType(this object obj, Type targetType) {
        var type1 = obj.GetType();
        if (type1.IsAssignableTo(targetType)) {
            return type1;
        }
        var type2 = obj.GetActualType();
        if (type2.IsAssignableTo(targetType)) {
            return type2;
        }
        return null;
    }

    public static bool AreAllAssignableToTargetType(this IEnumerable<object> enumerable, Type targetType) {
        return enumerable.All(obj => obj.GetIl2CppAssignableToTargetType(targetType) != null);
    }

    public static bool AreAllAssignableToTargetType<T>(this IEnumerable<object> enumerable) {
        return enumerable.All(obj => obj.GetIl2CppAssignableToTargetType(typeof(T)) != null);
    }

    public static IEnumerable<object> TryCastAll(this IEnumerable<object> enumerable, Type castToType) {
        if (!enumerable.AreAllAssignableToTargetType(castToType)) {
            throw new System.ArgumentException($"Cannot cast all elements to type {castToType}");
        }
        List<object> result = new();
        foreach (var element in enumerable) {
            result.Add(element.TryCast(castToType));
        }
        return result;
        // return enumerable.Select(element => element.TryCast(castToType));
    }

    public static IEnumerable<T> TryCastAll<T>(this IEnumerable<object> enumerable) {
        return TryCastAll(enumerable, typeof(T)).Cast<T>();
    }

    public static IEnumerable<object> WhereUnityOrPresetNameMatches(this IEnumerable<object> enumerable, Func<string, bool> predicate) {
        if (enumerable.AreAllAssignableToTargetType<SoCustomComparison>()) {
            return enumerable.TryCastAll<SoCustomComparison>().Where(preset => predicate(preset.presetName) || predicate(preset.name));
        }
        return enumerable.TryCastAll<UnityEngine.Object>().Where(preset => predicate(preset.name));
    }

    public static IEnumerable<object> WhereUnityOrPresetNameEquals(this IEnumerable<object> enumerable, string name) {
        return enumerable.WhereUnityOrPresetNameMatches(elementName => elementName == name);
    }

    public static IEnumerable<T> WhereUnityOrPresetNameEquals<T>(this IEnumerable<T> enumerable, string name) where T : UnityEngine.Object {
        return (IEnumerable<T>)enumerable.WhereUnityOrPresetNameMatches(elementName => elementName == name);
    }

    public static IEnumerable<object> GetAllUnityObjectsOfType(Type type) {
        return RuntimeHelper.FindObjectsOfTypeAll(type).TryCastAll(type);
    }

    public static IEnumerable<T> GetAllUnityObjectsOfType<T>() {
        return (IEnumerable<T>)GetAllUnityObjectsOfType(typeof(T));
    }
}