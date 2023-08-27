using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using DeTESTive;
using UnityEngine;
using FluentAssertions;
using UniverseLib;

namespace DeTESTiveExample
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    // [BepInDependency(ProductionPluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("DeTESTive", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BasePlugin
    {
        internal static ManualLogSource Logger;

        public override void Load()
        {
            Logger = Log;

            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            // Define/add example tests...

            // Simple test that always fails
            var test = CreateDeTest
                .WithDescription("Always Fails")
                .WithoutChangingTimescale()
                .WithoutStartingGame()
                .NoSetup()
                .AlwaysFails()
                .WithFailureHint("Intentional failure")
                .NoTeardown();
            TestRunner.AddTest(test, MyPluginInfo.PLUGIN_NAME);

            // Simple test that always passes
            test = CreateDeTest
                .WithDescription("Always Passes")
                .WithoutChangingTimescale()
                .WithoutStartingGame()
                .NoSetup()
                .AlwaysPasses()
                .WithFailureHint("Example hint")
                .NoTeardown();
            TestRunner.AddTest(test, MyPluginInfo.PLUGIN_NAME);

            // Shows simple use of a save file that is NOT packaged with the
            // mod. It is required that you create a save with this name in
            // your game before running the test. Otherwise, you will see how
            // tests with invalid save files are skipped.
            test = CreateDeTest
                .WithDescription("Failed Assertion")
                .WithTimescale(2.0f)
                .WhichLoadsSaveFileByPath(MyPluginInfo.PLUGIN_NAME, "Example Save.sodb")
                .WithSetup(ExampleASetup)
                .WhichAsserts(ExampleAAssertionFails)
                .WithFailureHint("Intentional failure")
                .DefaultTeardown();
            TestRunner.AddTest(test, "Multiple sources work, and they get grouped together");

            // Shows simple use of a save file that is packaged with the mod
            test = CreateDeTest
                .WithDescription("Simple value change on Singleton")
                .WithTimescale(5.0f)
                .WhichLoadsSaveFileByPath(MyPluginInfo.PLUGIN_NAME, "TestSave.sodb")
                .WithSetup(ExampleASetup)
                .WhichAsserts(ExampleAAssertion)
                .NoFailureHint()
                .WithTeardown(ExampleATeardown);
            TestRunner.AddTest(test, MyPluginInfo.PLUGIN_NAME);

            // Self contained test
            test = SelfContainedTest.Test;
            TestRunner.AddTest(test, "Multiple sources work, and they get grouped together");

            // Without assigning created DeTest to a var
            TestRunner.AddTest(
                CreateDeTest
                    .WithDescription("Longer view of gameplay, pausing, turning camera off and on")
                    .WithTimescale(1.0f)
                    .WhichLoadsSaveFileByPath(MyPluginInfo.PLUGIN_NAME, "TestSave.sodb")
                    .WithSetup(ExampleBSetup)
                    .WhichAsserts(ExampleBAssertion)
                    .WithFailureHint("May be due to a problem with CameraEnabled")
                    .DefaultTeardown(),
                MyPluginInfo.PLUGIN_NAME
            );

            TestRunner.RunTests(true);
        }

        private System.Collections.IEnumerator ExampleATeardown()
        {
            Game.Instance.bloomIntensity = 1.0f;
            yield return new WaitForSecondsRealtime(0.5f);
            yield return RuntimeHelper.StartCoroutine(TestHelpers.DefaultTeardown());
        }

        private bool ExampleAAssertion()
        {
            Game.Instance.bloomIntensity
                .Should()
                .Be(3.21f, "it was set to that value during Setup");
            return true;
        }

        private bool ExampleAAssertionFails()
        {
            Game.Instance.bloomIntensity
                .Should()
                .Be(-2.0f, "it was set to that value during Setup");
            return true;
        }

        private bool ExampleBAssertion()
        {
            CameraController.Instance.cam.nearClipPlane
                .Should()
                .Be(
                    TestHelpers.defaultCameraNearClip,
                    "it was set to that value last during Setup"
                );
            Plugin.Logger.LogInfo($"Player name: {Player.Instance.casualName}.");
            return true;
        }

        private System.Collections.IEnumerator ExampleASetup()
        {
            Game.Instance.bloomIntensity = 3.21f;
            yield return new WaitForSecondsRealtime(0.5f);
        }

        private System.Collections.IEnumerator ExampleBSetup()
        {
            // Turn off camera after 5 seconds
            yield return new WaitForSecondsRealtime(5.0f);
            TestHelpers.CameraEnabled = false;
            // Wait 2 seconds, then back on
            yield return new WaitForSecondsRealtime(2.0f);
            TestHelpers.CameraEnabled = true;
            // Wait 1 second, then pause the game
            yield return new WaitForSecondsRealtime(1.0f);
            TestHelpers.PauseGame(true, true, false);
            // Wait 2 seconds, then resume the game, then wait 2 seconds and we are done
            yield return new WaitForSecondsRealtime(2.0f);
            TestHelpers.ResumeGame();
            yield return new WaitForSecondsRealtime(2.0f);
        }
    }
}
