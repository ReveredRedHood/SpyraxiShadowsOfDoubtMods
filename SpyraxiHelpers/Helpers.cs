using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SpyraxiHelpers
{
    public static class Helpers
    {
        public const string SAVE_FOLDER_NAME = "Save";
        public static void StartNewGame(string cityName, string seed, int cityX, int cityY)
        {
            RestartSafeController.Instance.cityName = cityName;
            RestartSafeController.Instance.seed = seed;
            RestartSafeController.Instance.cityX = cityX;
            RestartSafeController.Instance.cityY = cityY;
            CityConstructor.Instance.GenerateNewCity();
        }

        public static void LoadGame(string fileName)
        {
            var fi = new Il2CppSystem.IO.FileInfo($"{Application.persistentDataPath}/{SAVE_FOLDER_NAME}/{Path.GetFileNameWithoutExtension(fileName)}.sodb");
            if(!fi.Exists) {
                fi = new Il2CppSystem.IO.FileInfo($"{Application.persistentDataPath}/{SAVE_FOLDER_NAME}/{Path.GetFileNameWithoutExtension(fileName)}.sod");
            }
            if(!fi.Exists) {
                throw new System.Exception($"Could not load game, no file at path: {fi.OriginalPath}");
            }
            RestartSafeController.Instance.saveStateFileInfo = fi;
            CityConstructor.Instance.LoadSaveGame();
        }

        public static void SaveGame(string fileName)
        {
            SaveStateController.Instance.CaptureSaveStateAsync($"{Application.persistentDataPath}/{SAVE_FOLDER_NAME}/{Path.GetFileNameWithoutExtension(fileName)}.sod");
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
