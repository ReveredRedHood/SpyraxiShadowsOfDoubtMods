namespace HoursOfOperation {
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using BepInEx;
    using SOD.Common;
    using SOD.Common.BepInEx;
    using SOD.Common.Extensions;
    using SOD.Common.Helpers;

    /// <summary>
    /// HoursOfOperation BepInEx BE plugin.
    /// </summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    [BepInDependency(SOD.Common.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : PluginController<Plugin, IConfigBindings> {

        public override void Load() {
            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            Harmony.PatchAll();
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is patched!");

            Lib.Interaction.OnAfterActionStarted += OnAfterActionStarted;
        }

        private const string BEAUTY_SYNC_DISK_NAME = "ElGen-Beauty";
        private const float SPEECH_SHOW_TIME = 0.25f;
        internal static HashSet<string> LocationsVisited = new();

        private static bool IsSyncDiskUpgradeInstalled(string upgradeName) {
            var upgrades = UpgradesController.Instance.upgrades.ToList();
            return upgrades.Exists(
                (UpgradesController.Upgrades item) => item.upgrade == upgradeName
            );
        }

        private void OnAfterActionStarted(object sender, Interaction.SimpleActionArgs args) {
            var interactable = args.InteractableInstanceData.Interactable;
            if (!interactable.locked || !interactable.node.isIndoorsEntrance || !interactable.p.Contains("Door")) {
                return;
            }

            var company = interactable.node?.gameLocation?.thisAsAddress?.company;
            if (company == null) {
                return;
            }
            var speechText = "Looks like they're closed...";
            Lib.GameMessage.ShowPlayerSpeech(speechText, SPEECH_SHOW_TIME * NumberOfWords(speechText));
            if (Config.OnlyShowIfPreviouslyVisited) {
                if (!Config.ShowAllIfBeautyInstalled || !IsSyncDiskUpgradeInstalled(BEAUTY_SYNC_DISK_NAME)) {
                    if (!LocationsVisited.Contains(company.address.name)) {
                        speechText = $"I've never visited {company.name}, so I'm not sure what their hours are.";
                        Lib.GameMessage.ShowPlayerSpeech(speechText, SPEECH_SHOW_TIME * NumberOfWords(speechText));
                        if (Config.ShowAllIfBeautyInstalled) {
                            speechText = $"Maybe I should get the {BEAUTY_SYNC_DISK_NAME} sync disk installed.";
                            Lib.GameMessage.ShowPlayerSpeech(speechText, SPEECH_SHOW_TIME * NumberOfWords(speechText));
                        }
                        return;
                    }
                }
            }
            Plugin.Log.LogInfo($"Have we visited? {LocationsVisited.Contains(company.address.name)}");

            var daysOpen = company.daysOpen.ToList();
            CultureInfo culture = CultureInfo.CurrentCulture;
            string daysOpenStr;
            switch (daysOpen.Count) {
                case 1: {
                    var day = daysOpen.First().ToString();
                    daysOpenStr = $"{culture.TextInfo.ToTitleCase(day)} only";
                    break;
                }
                case 7:
                    daysOpenStr = "every day";
                    break;
                default:
                    daysOpenStr = string.Join(", ", daysOpen);
                    daysOpenStr = culture.TextInfo.ToTitleCase(daysOpenStr);
                    break;
            }

            var hours = company.retailOpenHours;
            var hrOpensAt = hours.x;
            var hrClosesAt = hours.y;
            var hrOpensAtStr = TimeSpan.FromHours(hrOpensAt).ToString(@"hh\:mm");
            var hrClosesAtStr = TimeSpan.FromHours(hrClosesAt).ToString(@"hh\:mm");

            speechText =
                $"If I remember right, {company.name} is open from {string.Format(hrOpensAtStr)} - {hrClosesAtStr}, {daysOpenStr}.";
            Lib.GameMessage.ShowPlayerSpeech(speechText, SPEECH_SHOW_TIME * NumberOfWords(speechText));
        }

        private static int NumberOfWords(string speechText) {
            return speechText.Split(' ').Count();
        }

        public override bool Unload() {
            Harmony?.UnpatchSelf();

            return base.Unload();
        }
    }
}
