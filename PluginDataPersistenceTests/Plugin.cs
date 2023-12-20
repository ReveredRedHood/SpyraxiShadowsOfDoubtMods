using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using CLSS;
using SpyraxiHelpers;
using DeTESTive;
using System.Collections;
using System;
using FluentAssertions;

namespace PluginDataPersistenceTests
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    [BepInDependency("DeTESTive", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("PluginDataPersistence", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BasePlugin
    {
        internal const string KEY_A = "A";
        internal const string VALUE_A = "Apples";
        internal const string SAVE_GAME_NAME_A = "Test";
        internal const string SAVE_GAME_NAME_B = "TestAfter";

        internal static ManualLogSource Logger;

        public override void Load()
        {
            Logger = Log;

            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            // Tests
            TestRunner.AddTest(
                CreateDeTest
                    .WithDescription("No plugin data should be available on first load.")
                    .WithTimescale(5.0f)
                    .WhichLoadsSaveFileByPath(MyPluginInfo.PLUGIN_NAME, $"{SAVE_GAME_NAME_A}.sodb")
                    .DefaultSetup()
                    .WhichAsserts(AssertNoPersistenceOnFirstLoad)
                    .NoFailureHint()
                    .DefaultTeardown(),
                MyPluginInfo.PLUGIN_NAME
            );

            TestRunner.AddTest(
                CreateDeTest
                    .WithDescription("The plugin data should show on second load.")
                    .WithTimescale(5.0f)
                    .WhichLoadsSaveFileByPath(MyPluginInfo.PLUGIN_NAME, $"{SAVE_GAME_NAME_B}.sodb")
                    .DefaultSetup()
                    .WhichAsserts(AssertPersistenceOnSecondLoad)
                    .NoFailureHint()
                    .DefaultTeardown(),
                MyPluginInfo.PLUGIN_NAME
            );

            TestRunner.RunTests(true);
        }

        internal bool AssertNoPersistenceOnFirstLoad()
        {
            var data = PluginDataPersistence.Plugin.LoadOrGetSaveGameData(this);
            data.Keys.Count.Should().Be(0, "because no data has been saved to the file yet.");
            
            data.ContainsKey(KEY_A).Should().BeFalse("because no data has been saved to the file yet.");
            data.Add(KEY_A, VALUE_A);

            Helpers.SaveGame(SAVE_GAME_NAME_B);
            return true;
        }

        internal bool AssertPersistenceOnSecondLoad()
        {
            var data = PluginDataPersistence.Plugin.LoadOrGetSaveGameData(this);
            data.Keys.Count.Should().Be(1, "because we added exactly one data field to be saved.");
            
            data.ContainsKey(KEY_A).Should().BeTrue("because we saved data to the file under that key.");
            data[KEY_A].Should().Be(VALUE_A, "because that is the value we assigned to the key.");

            return true;
        }
    }
}