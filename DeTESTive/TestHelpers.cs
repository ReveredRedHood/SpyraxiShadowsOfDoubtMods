using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using UnityEngine;
using UniverseLib;

namespace DeTESTive
{
    public static class TestHelpers
    {
        public static readonly float defaultCameraNearClip = 0.015f;
        private static bool cameraEnabled;

        /// <summary>
        /// Set to false to turn the player's camera off during in-game tests,
        /// vastly improving framerate. The camera's near clip plane is
        /// modified to turn it off, to prevent errors that might be thrown
        /// when disabling the camera directly from being thrown.
        /// </summary>
        public static bool CameraEnabled
        {
            get { return cameraEnabled; }
            set
            {
                cameraEnabled = value;
                CameraController.Instance.cam.nearClipPlane = cameraEnabled
                    ? defaultCameraNearClip
                    : float.MaxValue;
            }
        }

        private static readonly string THUNDERSTORE_MM_CACHE_PATH =
            $@"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\Thunderstore Mod Manager\DataFolder\ShadowsofDoubt\cache";

        private static readonly string BEPINEX_PATH =
            $@"{Directory.GetCurrentDirectory()}\BepInEx\plugins";

        public static readonly string SAVE_FILES_PATH = $@"{Application.persistentDataPath}/Save";

        /// <summary>
        /// Searches possible directories where mod files are installed for a
        /// file and returns the full path to that file.
        /// </summary>
        /// <param name="modFolderName">The name of the mod folder, used to
        /// verify that the file found is for the correct mod.</param>
        /// <param name="nameWithExt">The name of the file, e.g. "list.txt".</param>
        /// <returns>The full path to the mod's file.</returns>
        public static string GetFilePathInModInstallDirectory(
            string modFolderName,
            string nameWithExt
        )
        {
            var dirs = new List<string>(2) { THUNDERSTORE_MM_CACHE_PATH, BEPINEX_PATH };
            string potentialPath;
            foreach (var dir in dirs)
            {
                potentialPath = nameWithExt;
                Plugin.Logger.LogInfo($"Path: {potentialPath}");
                var path = GetFilePathFromPattern(dir, potentialPath, modFolderName);
                if (path?.Length == 0)
                {
                    Plugin.Logger.LogInfo($"File not found at: {dir}");
                    continue;
                }
                Plugin.Logger.LogInfo($"File found at: {path}");
                return path;
            }
            // Note: This one uses forward-slashes
            potentialPath = $"{SAVE_FILES_PATH}/{nameWithExt}";
            Plugin.Logger.LogInfo($"File assumed to be at: {potentialPath}");
            return potentialPath;
        }

        private static string GetFilePathFromPattern(
            string directory,
            string checkPath,
            string modFolderName
        )
        {
            IEnumerable<string> results;
            try
            {
                results = Directory.GetFiles(directory, checkPath, SearchOption.AllDirectories);
            }
            catch (System.Exception e)
                when (e is System.IO.FileNotFoundException
                    || e is System.IO.DirectoryNotFoundException
                )
            {
                return "";
            }
            // Filter out results that aren't specific to the particular plugin
            results = results.Where(result => result.Contains(modFolderName));
            return results.Any() ? results.First() : "";
        }

        /// <summary>
        /// Get the game's version.
        /// </summary>
        /// <returns>A string representing the game's version, e.g. "34.10".</returns>
        public static string GetGameVersion()
        {
            return Game.Instance.buildID;
        }

        /// <summary>
        /// The default setup used for each test. Simply turns off the player camera.
        /// </summary>
        /// <returns></returns>
        public static System.Collections.IEnumerator DefaultSetup()
        {
            CameraEnabled = false;
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// The default teardown used for each test. Simply turns the player camera back on.
        /// </summary>
        /// <returns></returns>
        public static System.Collections.IEnumerator DefaultTeardown()
        {
            CameraEnabled = true;
            yield return new WaitForEndOfFrame();
        }

        /// <summary>
        /// End current in-game session and return to main menu
        /// </summary>
        public static void EndGameAndReturnToMainMenu()
        {
            SessionData.Instance.EndDemo();
            TestRunner.s_currentlyLoadedSave = "";
        }

        /// <summary>
        /// Resumes the game.
        /// </summary>
        public static void ResumeGame() => SessionData.Instance.ResumeGame();

        /// <summary>
        /// Pauses the game.
        /// </summary>
        /// <param name="showPauseText">Whether to show "Paused" text.</param>
        /// <param name="delayOverride"></param>
        /// <param name="openDesktopMode">Whether to open the pause menu including the map and notes screen.</param>
        public static void PauseGame(
            bool showPauseText,
            bool delayOverride,
            bool openDesktopMode
        ) => SessionData.Instance.PauseGame(showPauseText, delayOverride, openDesktopMode);
    }
}
