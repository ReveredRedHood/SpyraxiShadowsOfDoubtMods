namespace Autosave
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Linq;
    using BepInEx;
    using BepInEx.Configuration;
    using SOD.Common;
    using SOD.Common.BepInEx;
    using SOD.Common.Helpers;
    using UnityEngine;
    using UniverseLib;

    /// <summary>
    /// Autosave BepInEx BE plugin.
    /// </summary>
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Shadows of Doubt.exe")]
    [BepInDependency(SOD.Common.Plugin.PLUGIN_GUID, BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : PluginController<Plugin, IConfigBindings>
    {
        private const string AUTOSAVE_FILE_NAME = "Autosave";
        private const string AUTOSAVE_SUFFIX = " - AUTO #";
        private const int PLAYER_AFK_TIME_THRESHOLD = 10;
        private const int AUTOSAVE_DIGITS = 3;
        private const int MAX_AUTOSAVES_PER_FILE_NAME = 500;
        private const int MIN_AUTOSAVE_DELAY = 15;
        private int timeSinceLastAutosave = 0;
        private bool isAutoSaving = false;
        private bool isGamePaused = false;
        private bool isGameTransitioning = false;
        private bool playerJustReturnedFromAFK = false;
        private bool wasPlayerAFKDuringLastAutosave = false;
        private bool sentAutosaveWarningImminent = false;
        private bool sentAutosaveWarningSoon = false;
        private bool warnedAboutAutosaveDelay = false;
        private string originalSaveGameFilePath;

        /// <summary>
        /// Gets a value indicating whether the player is AFK, which is
        /// determined by whether any input was detected in the last
        /// PLAYER_AFK_TIME_THRESHOLD seconds.
        /// </summary>
        public bool IsPlayerAfk { get; private set; } = false;
        private int AutosaveDelay
        {
            get
            {
                if (Config.AutosaveDelay < MIN_AUTOSAVE_DELAY)
                {
                    if (!warnedAboutAutosaveDelay)
                    {
                        Log.LogWarning(
                            $"The minimum autosave delay is {MIN_AUTOSAVE_DELAY} seconds, which will be used instead."
                        );
                        warnedAboutAutosaveDelay = true;
                    }
                    return MIN_AUTOSAVE_DELAY;
                }
                else
                {
                    return Config.AutosaveDelay;
                }
            }
        }
        private bool ShouldProgress
        {
            get
            {
                var result =
                    !isGamePaused
                    && !isGameTransitioning
                    && !(
                        Config.AvoidConsecutiveAfkAutosaves
                        && IsPlayerAfk
                        && wasPlayerAFKDuringLastAutosave
                    );
                return result;
            }
        }

        public Coroutine Coroutine { get; private set; }

        public override void Load()
        {
            // Plugin startup logic
            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            // Harmony.PatchAll();
            // Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is patched!");

            Lib.SaveGame.OnBeforeLoad += OnBeforeLoad;
            Lib.SaveGame.OnAfterLoad += OnAfterLoad;
            Lib.SaveGame.OnBeforeNewGame += OnBeforeNewGame;
            Lib.SaveGame.OnAfterNewGame += OnAfterNewGame;
            Lib.SaveGame.OnBeforeSave += OnBeforeSave;
            Lib.SaveGame.OnAfterSave += OnAfterSave;
            Lib.Time.OnGamePaused += OnGamePaused;
            Lib.Time.OnGameResumed += OnGameResumed;
            Lib.InputDetection.OnButtonStateChanged += OnButtonStateChanged;
            ConfigFile.SettingChanged += OnConfigSettingChanged;
        }

        private void OnConfigSettingChanged(object sender, SettingChangedEventArgs arg)
        {
            Log.LogInfo($"Config setting changed, restarting autosave timer...");
            OnPlayerReturnFromAfk();
            RestartCoroutine();
        }

        public override bool Unload()
        {
            // Harmony?.UnpatchSelf();
            Lib.SaveGame.OnBeforeLoad -= OnBeforeLoad;
            Lib.SaveGame.OnAfterLoad -= OnAfterLoad;
            Lib.SaveGame.OnBeforeNewGame -= OnBeforeNewGame;
            Lib.SaveGame.OnAfterNewGame -= OnAfterNewGame;
            Lib.SaveGame.OnBeforeSave -= OnBeforeSave;
            Lib.SaveGame.OnAfterSave -= OnAfterSave;
            Lib.Time.OnGamePaused -= OnGamePaused;
            Lib.Time.OnGameResumed -= OnGameResumed;
            Lib.InputDetection.OnButtonStateChanged -= OnButtonStateChanged;

            return base.Unload();
        }

        private void OnBeforeNewGame(object sender, EventArgs e)
        {
            isGameTransitioning = true;
        }

        private void OnGameResumed(object sender, EventArgs args)
        {
            isGamePaused = false;
        }

        private void OnGamePaused(object sender, EventArgs args)
        {
            isGamePaused = true;
        }

        private void OnAfterNewGame(object sender, EventArgs args)
        {
            isGameTransitioning = false;
            originalSaveGameFilePath = string.Empty;
            RestartCoroutine();
        }

        private void OnBeforeLoad(object sender, SaveGameArgs args)
        {
            isGameTransitioning = true;
        }

        private void OnButtonStateChanged(object sender, InputDetectionEventArgs args)
        {
            OnPlayerReturnFromAfk();
        }

        private void OnAfterSave(object sender, SaveGameArgs args)
        {
            if (!isAutoSaving)
            {
                // Since the filepath changes during a manual save, we need to
                // update the filepath as we restart the coroutine
                SetOriginalSaveGameFilePath(args.FilePath);
            }

            // Start the next Autosave timer
            RestartCoroutine();

            isAutoSaving = false;
            isGameTransitioning = false;
        }

        private void OnBeforeSave(object sender, SaveGameArgs args)
        {
            isGameTransitioning = true;
            if (!isAutoSaving)
            {
                return;
            }
            wasPlayerAFKDuringLastAutosave = IsPlayerAfk;
        }

        private void SetOriginalSaveGameFilePath(string filePath)
        {
            originalSaveGameFilePath = filePath;
            // Correct the filepath if necessary
            var fileName = Path.GetFileNameWithoutExtension(originalSaveGameFilePath);
            if (fileName.Length >= (AUTOSAVE_DIGITS + AUTOSAVE_SUFFIX.Length))
            {
                var minusEndDigits = fileName.Remove(
                    fileName.Length - AUTOSAVE_DIGITS,
                    AUTOSAVE_DIGITS
                );
                if (minusEndDigits.EndsWith(AUTOSAVE_SUFFIX))
                {
                    // This filepath is an autosave, so set the originalSaveGameFilePath to the name without the autosave part
                    var originalFileName = minusEndDigits.Remove(
                        minusEndDigits.Length - AUTOSAVE_SUFFIX.Length,
                        AUTOSAVE_SUFFIX.Length
                    );
                    originalSaveGameFilePath =
                        $"{Application.persistentDataPath}/Save/{originalFileName}.sodb";
                }
            }
        }

        private void OnAfterLoad(object sender, SaveGameArgs args)
        {
            isGameTransitioning = false;
            SetOriginalSaveGameFilePath(args.FilePath);
            OnPlayerReturnFromAfk();
            RestartCoroutine();
        }

        private void RestartCoroutine()
        {
            timeSinceLastAutosave = 0;
            if (Coroutine != null)
            {
                RuntimeHelper.StopCoroutine(Coroutine);
            }
            Coroutine = RuntimeHelper.StartCoroutine(UpdateAutosave());
        }

        private void OnPlayerReturnFromAfk()
        {
            IsPlayerAfk = false;
            wasPlayerAFKDuringLastAutosave = false;
            playerJustReturnedFromAFK = true;
        }

        private void UpdatePlayerAFKStatus(ref int afkEntryCounter)
        {
            if (IsPlayerAfk)
            {
                afkEntryCounter = 0;
            }
            else if (!playerJustReturnedFromAFK)
            {
                afkEntryCounter++;
            }
            else
            {
                afkEntryCounter = 0;
                playerJustReturnedFromAFK = false;
            }

            if (afkEntryCounter >= PLAYER_AFK_TIME_THRESHOLD)
            {
                IsPlayerAfk = true;
            }
        }

        private void BroadcastAutosaveWarnings(int timeSinceLastAutosave)
        {
            var timeLeft = AutosaveDelay - timeSinceLastAutosave;
            if (!Config.ShowWarnings)
            {
                return;
            }
            if (timeLeft == 30 && !sentAutosaveWarningSoon)
            {
                Lib.GameMessage.Broadcast(
                    "Warning: Autosaving in 30 seconds.",
                    InterfaceController.GameMessageType.notification,
                    InterfaceControls.Icon.time,
                    Color.cyan,
                    0.0f
                );
                sentAutosaveWarningSoon = true;
            }
            if (timeLeft == 5 && !sentAutosaveWarningImminent)
            {
                Lib.GameMessage.Broadcast(
                    "WARNING: Autosaving in 5 seconds!",
                    InterfaceController.GameMessageType.notification,
                    InterfaceControls.Icon.time,
                    Color.cyan,
                    0.0f
                );
                sentAutosaveWarningImminent = true;
            }
        }

        private IEnumerator UpdateAutosave()
        {
            var afkEntryCounter = 0;
            while (timeSinceLastAutosave < AutosaveDelay)
            {
                yield return new WaitForSecondsRealtime(1.0f);
                if (!ShouldProgress)
                {
                    continue;
                }
                UpdatePlayerAFKStatus(ref afkEntryCounter);
                timeSinceLastAutosave += 1;
                BroadcastAutosaveWarnings(timeSinceLastAutosave);
            }

            isAutoSaving = true;
            sentAutosaveWarningImminent = false;
            sentAutosaveWarningSoon = false;

            PruneOldAutosaves(GetAutosaveFilename(false));

            var path = $"{Application.persistentDataPath}/Save/{GetAutosaveFilename()}.sod";
            Log.LogInfo($"Autosaving, path: {path}");
            SaveStateController.Instance.CaptureSaveStateAsync(path);
        }

        private void PruneOldAutosaves(string fileName)
        {
            string dirPath = $"{Application.persistentDataPath}/Save/";
            string searchPattern = $"{fileName}*";
            var filePaths = Directory.GetFiles(dirPath, searchPattern).ToList();
            if (filePaths.Count == 1 && Config.NumberOfAutosavesToKeep == 1)
            {
                File.Delete(filePaths[0]);
                return;
            }
            if (filePaths.Count < Config.NumberOfAutosavesToKeep)
            {
                return;
            }
            filePaths.Sort();
            var count = filePaths.Count;
            for (int i = 0; i < count - 1; i++)
            {
                var current = filePaths[i];
                var next = filePaths[i + 1];
                File.Move(next, current, true);
            }
        }

        private string GetAutosaveFilename(bool includePaddedNumber = true)
        {
            string fileName;
            if (Config.UseLastSaveName && originalSaveGameFilePath != string.Empty)
            {
                fileName = Path.GetFileNameWithoutExtension(originalSaveGameFilePath);
                if (fileName == string.Empty)
                {
                    fileName = AUTOSAVE_FILE_NAME;
                }
                fileName = $"{fileName}{AUTOSAVE_SUFFIX}";
            }
            else
            {
                fileName = $"{AUTOSAVE_FILE_NAME}{AUTOSAVE_SUFFIX}";
            }

            string filePrefix = $"{Application.persistentDataPath}/Save/";

            int counter = 1;
            string paddedNumber = $"{counter}".PadLeft(AUTOSAVE_DIGITS, '0');
            string nextFilePath = $"{filePrefix}{fileName}{paddedNumber}.sod";
            while (File.Exists($"{nextFilePath}") || File.Exists($"{nextFilePath}b"))
            {
                counter++;
                paddedNumber = $"{counter}".PadLeft(AUTOSAVE_DIGITS, '0');
                nextFilePath = $"{filePrefix}{fileName}{paddedNumber}.sod";
                if (counter > MAX_AUTOSAVES_PER_FILE_NAME)
                {
                    throw new MaxAutosavesReachedException();
                }
            }
            return includePaddedNumber ? $"{fileName}{paddedNumber}" : fileName;
        }

        /// <summary>
        /// Exception thrown when number of autosaves exceeds threshold.
        /// </summary>
        public sealed class MaxAutosavesReachedException : System.Exception
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="MaxAutosavesReachedException"/> class.
            /// </summary>
            public MaxAutosavesReachedException()
                : base($"Cannot exceed {MAX_AUTOSAVES_PER_FILE_NAME} autosaves per file name.") { }
        }
    }
}
