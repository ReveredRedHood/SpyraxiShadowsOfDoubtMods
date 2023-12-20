using M31.FluentApi.Attributes;
using UnityEngine;

namespace DeTESTive
{
    [FluentApi]
    public class DeTest
    {
        public string Source { get; internal set; }
        public bool IsSetupComplete { get; internal set; }
        public bool IsComplete { get; internal set; }
        public bool IsPassing { get; internal set; }
        public float TimeTaken { get; internal set; }
        public string FailureMessage { get; internal set; }

        [FluentMember(0, "WithDescription")]
        public string Description { get; private set; }

        [FluentMember(1, "WithTimescale")]
        [FluentDefault("WithoutChangingTimescale")]
        public float? TimeScale { get; private set; } = null;

        public bool StartsNewGame { get; private set; }

        public string SaveGameModFolderName { get; private set; }

        public string SaveGameNameWithExt { get; private set; }

        // public string CitySeed { get; private set; }
        // public bool IsMainStory { get; private set; }
        // public string SaveGameName { get; private set; }

        [FluentMethod(2)]
        public void WithoutStartingGame()
        {
            StartsNewGame = false;
        }

        // [FluentMethod(2)]
        // public void WhichStartsNewGame(string citySeed, bool isMainStory)
        // {
        //     StartsNewGame = true;
        //     CitySeed = citySeed;
        //     IsMainStory = isMainStory;
        // }

        // [FluentMethod(2)]
        // public void WhichLoadsSaveFileByName(string saveGameName)
        // {
        //     StartsNewGame = true;
        //     SaveGameName = saveGameName;
        // }

        [FluentMethod(2)]
        public void WhichLoadsSaveFileByPath(string modFolderName, string nameWithExt)
        {
            StartsNewGame = true;
            SaveGameModFolderName = modFolderName;
            SaveGameNameWithExt = nameWithExt;
        }

        [FluentMember(3, "WithSetup")]
        [FluentDefault("DefaultSetup")]
        [FluentNullable("NoSetup")]
        public System.Func<System.Collections.IEnumerator> Setup { get; private set; } =
            DeTest.DefaultSetup;

        /// <summary>
        /// The default setup used for each test. Simply turns off the player camera.
        /// </summary>
        /// <returns></returns>
        public static System.Collections.IEnumerator DefaultSetup()
        {
            Plugin.Logger.LogInfo("Turn off the camera plz");
            yield return new WaitForEndOfFrame();
        }

        [FluentMember(4, "WhichAsserts")]
        [FluentDefault("AlwaysPasses")]
        public System.Func<bool> AssertFunc { get; private set; } = () => true;

        [FluentMethod(4)]
        private void AlwaysFails()
        {
            AssertFunc = () => false;
        }

        // [FluentMethod(4)]
        // private void WhichChecksAScreenshot()
        // {
        //     AssertFunc = () =>
        //     {
        //         TestHelpers.CameraEnabled = true;
        //         Application.CaptureScreenshot(
        //             $"DeTESTiveScreenshots/{Source.Replace(" ", "-")}_{Description.Replace(" ", "-")}.png"
        //         );
        //         TestHelpers.CameraEnabled = false;
        //         return true;
        //     };
        // }

        [FluentMember(5, "WithFailureHint")]
        [FluentDefault("NoFailureHint")]
        public string FailureHint { get; private set; } = "Not provided.";

        [FluentMember(6, "WithTeardown")]
        [FluentDefault("DefaultTeardown")]
        [FluentNullable("NoTeardown")]
        public System.Func<System.Collections.IEnumerator> Teardown { get; private set; } =
            DeTest.DefaultTeardown;

        /// <summary>
        /// The default teardown used for each test. Simply turns on the player camera.
        /// </summary>
        /// <returns></returns>
        public static System.Collections.IEnumerator DefaultTeardown()
        {
            Plugin.Logger.LogInfo("Turn on the camera plz");
            yield return new WaitForEndOfFrame();
        }
    }
}
