using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using BepInEx.Logging;
using SOD.Common;

namespace Guns;

internal sealed class LifeAndLivingIntegration {
    internal const string LIFE_AND_LIVING_GUID = "Venomaus.SOD.LifeAndLiving";
    private ManualLogSource Log => Plugin.Log;
    internal bool IsActive => Lib.PluginDetection.IsPluginLoaded(LIFE_AND_LIVING_GUID);

    internal void Setup() {
        if (!IsActive) {
            return;
        }
        Log.LogInfo($"Detected plugin {LIFE_AND_LIVING_GUID}");
        // Will need to adjust buy prices in response to config changes
        Lib.PluginDetection.AddPluginConfigEntryChangedListener(LIFE_AND_LIVING_GUID, OnSettingChanged);
    }

    internal void OnSettingChanged(SettingChangedEventArgs args) {
        var applicableSettingNames = new List<string> {
            "PercentageValueIncrease",
            "MinItemValue",
        };
        Log.LogInfo(args.ChangedSetting.Definition.Key);
        if (!applicableSettingNames.Contains(args.ChangedSetting.Definition.Key)) {
            return;
        }
        Log.LogInfo("Detected adjustment in LifeAndLiving setting, re-adjusting gun prices...");
        Plugin.Instance.GunInfoEntries.ForEach(Plugin.Instance.CalculateBuyPrice);
    }

    internal void OverwriteVars(ref int minItemValue, ref int percentageValueIncrease) {
        if (Lib.PluginDetection.IsPluginLoaded(LIFE_AND_LIVING_GUID)) {
            // General items = non-food items
            SetValueFromConfigEntry(LIFE_AND_LIVING_GUID, "LifeAndLiving.ItemPrice", "MinGeneralItemValue", ref minItemValue);
            SetValueFromConfigEntry(LIFE_AND_LIVING_GUID, "LifeAndLiving.ItemPrice", "PercentageValueIncreaseGeneral", ref percentageValueIncrease);
        }
    }

    internal void SetValueFromConfigEntry<T>(string pluginGuid, string section, string key, ref T field) {
        try {
            field = Lib.PluginDetection.GetPluginConfigEntryValue<T>(pluginGuid, section, key);
        }
        catch(InvalidOperationException) {
            Log.LogError($"Missing config entry detected for LifeAndLiving integration ({section}, {key}). Skipped config entry integration, was there an update to LifeAndLiving?");
        }
    }
}