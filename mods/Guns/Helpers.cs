using System;
using System.Collections.Generic;
using System.Linq;
using SOD.Common.Extensions;
using UniverseLib;

namespace Guns;

internal static class Helpers {
    // TODO: move these (or equivalent functionality) to SOD.Common
    internal static bool IsPlayerHeardByOthers() {
        return InterfaceController.Instance.awarenessIcons.Where(x => x.alertProgress > 0.0f).Any();
    }

    internal static bool IsPlayerBeingPursuedByMurderer() {
        return IsPlayerBeingPursuedByActor(MurderController.Instance.currentMurderer);
    }

    internal static bool IsPlayerBeingPursuedByActor(Actor actor) {
        return Player.Instance.persuedProgressLag > 0.0f && actor.ai != null && actor.ai.persuitTarget == Player.Instance;
    }

    internal static IEnumerable<T> GetPresetInstances<T>(Func<string, bool> presetNamePredicate = null, bool disallowDupes = true) where T : SoCustomComparison {
        var type = typeof(T);
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
            presetInstances = presetInstances.DistinctBy(GetPresetKey);
        }
        return presetInstances.Select(TypeExtensions.TryCast<T>);
    }

    internal static string GetPresetKey(object obj) {
        return $"{obj.GetActualType().FullName}_{GetPresetName(obj)}";
    }

    internal static string GetPresetName(object obj) {
        var castObj = TypeExtensions.TryCast<SoCustomComparison>(obj);
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

    internal static IEnumerable<object> GetAllUnityObjectsOfType(Type type) {
        return RuntimeHelper.FindObjectsOfTypeAll(type);
    }

    internal static IEnumerable<object> GetAllUnityObjectsOfType<T>() {
        return RuntimeHelper.FindObjectsOfTypeAll(typeof(T));
    }
}