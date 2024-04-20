using System;
using System.Collections.Generic;
using System.Linq;
using SOD.Common.Extensions;
using UniverseLib;

namespace PresetEdit;
public static class Helpers {
    internal static string GetPresetKey(object obj) {
        return $"{obj.GetActualType().FullName}_{GetPresetName(obj)}";
    }
    internal static string GetPresetName(object obj) {
        var castObj = obj.TryCast<SoCustomComparison>();
        string presetName;
        try {
            presetName = castObj.name;
            return presetName;
        }
        catch {
            presetName = castObj.presetName;
            if (presetName != null && presetName != default) {
                return presetName;
            }
            presetName = castObj.GetPresetName();
        }
        return presetName;
    }
    public static IEnumerable<object> GetPresetInstances(Type type, string presetName, bool disallowDupes = true) {
        return GetPresetInstances(type, objName => objName == presetName, disallowDupes);
    }

    public static IEnumerable<object> GetPresetInstances(Type type = null, Func<string, bool> presetNamePredicate = null, bool disallowDupes = true) {
        IEnumerable<object> presetInstances = GetAllUnityObjectsOfType(type ?? typeof(SoCustomComparison));
        if (presetInstances.Count() == 0) {
            throw new NullReferenceException($"No presets found with type {type.FullName}.");
        }
        if (presetNamePredicate != null) {
            presetInstances = presetInstances.Where(obj => presetNamePredicate(GetPresetName(obj)));
        }
        if (presetInstances.Count() <= 0) {
            throw new NullReferenceException($"No presets of type {type.FullName} found matching predicate.");
        }
        if (disallowDupes) {
            presetInstances = presetInstances.DistinctBy(elem => Helpers.GetPresetKey(elem));
        }
        return presetInstances;
    }

    public static List<object> FormListIfEnumerable(this object enumerable) {
        if (!ReflectionUtility.TryGetEnumerator(enumerable, out var enumerator)) {
            throw new ArgumentException("Could not get enumerator.");
        }
        var result = new List<object>();
        while (enumerator.MoveNext()) {
            result.Add(enumerator.Current);
        }
        return result;
    }

    public static void OverwriteIl2CppCollectionContents(this object il2CppList, IEnumerable<object> list) {
        var genericType = il2CppList.GetActualType();
        genericType.GetMethod("Clear").Invoke(il2CppList, null);
        var methodInfo = genericType.GetMethod("Add");
        foreach (var element in list) {
            methodInfo.Invoke(il2CppList, [element]);
        }
    }

    public static bool IsTypeConsistent(this IEnumerable<object> enumerable) {
        return enumerable.DistinctBy(element => element.GetActualType()).Count() == 1;
    }

    public static bool IsExplicitlyIl2CppType(this Type type) {
        return type.FullName.Contains("Il2Cpp") || type.FullName.Contains("IL2CPP");
    }

    public static Type AttemptToConvertToManagedType(this Type typeToConvert) {
        var name = typeToConvert.FullName;
        if (name.Contains("Il2CppSystem")) {
            var nonIl2CppName = name.Replace("Il2Cpp", "").Replace("IL2CPP", "");
            return ReflectionUtility.GetTypeByName(nonIl2CppName);
        }
        throw new ArgumentException($"Type to convert ({typeToConvert}) must be a Il2Cpp type.");
    }

    public static Type GetIl2CppAssignableToTargetType(this object obj, Type targetType) {
        var type1 = obj.GetActualType();
        return type1;
    }

    public static bool AreAllAssignableToTargetType(this IEnumerable<object> enumerable, Type targetType) {
        return enumerable.All(obj => obj.GetIl2CppAssignableToTargetType(targetType) != null);
    }

    public static bool AreAllAssignableToTargetType<T>(this IEnumerable<object> enumerable) {
        return enumerable.All(obj => obj.GetIl2CppAssignableToTargetType(typeof(T)) != null);
    }

    public static IEnumerable<object> TryCastAll(this IEnumerable<object> enumerable, Type castToType) {
        if (!enumerable.AreAllAssignableToTargetType(castToType)) {
            throw new ArgumentException($"Cannot cast all elements to type {castToType}");
        }
        List<object> result = new();
        foreach (var element in enumerable) {
            result.Add(element.TryCast(castToType));
        }
        return result;
    }

    public static IEnumerable<T> TryCastAll<T>(this IEnumerable<object> enumerable) {
        return TryCastAll(enumerable, typeof(T)).Cast<T>();
    }

    public static IEnumerable<object> ToIEnumerable<T>(this List<T> list) {
        return list.AsEnumerable().Cast<object>();
    }

    public static IEnumerable<object> GetAllUnityObjectsOfType(Type type) {
        return RuntimeHelper.FindObjectsOfTypeAll(type);
    }

    public static IEnumerable<object> GetAllUnityObjectsOfType<T>() {
        return RuntimeHelper.FindObjectsOfTypeAll(typeof(T));
    }

    public static void WrapException(Action action, string message) {
        try {
            action();
        }
        catch (Exception original) {
            throw new InvalidOperationException($"PresetEdit: {message}", original);
        }
    }
}