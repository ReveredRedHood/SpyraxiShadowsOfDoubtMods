using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CLSS;
using UnityEngine;
using UniverseLib;

namespace DeTESTive
{
    /// <summary>
    /// Used for running DeTESTive tests.
    /// </summary>
    public static class TestRunner
    {
        private const string INDENT = "  ";
        private const string DIVIDER =
            "------------------------------------------------------------------";
        private const float HIGH_TIME_SCALE = 10.0f;
        private const float DELAY_SHORT = 0.2f;

        public static bool ApplicationStarted { get; internal set; }
        public static bool MainMenuLoaded { get; internal set; }
        private static readonly List<DeTest> tests = new();
        private static bool s_runnerStarted { get; set; } = false;
        private static bool s_gameNotStarted { get; set; }
        private static bool s_isFirstLog { get; set; } = true;

        /// <summary>
        /// Adds and queues a DeTest to the Test Runner, running it after a short delay.
        /// </summary>
        /// <param name="test"></param>
        /// <param name="source"></param>
        public static void AddTest(DeTest test, string source)
        {
            tests.Remove(test);
            test.Source = source;
            tests.Add(test);
        }

        /// <summary>
        /// Runs all added tests.
        /// </summary>
        public static void RunTests(bool exitGameWhenFinished = false)
        {
            if (s_runnerStarted)
            {
                return;
            }
            s_runnerStarted = true;
            RuntimeHelper.StartCoroutine(RunTestsCoroutine(exitGameWhenFinished));
        }

        private static System.Collections.IEnumerator RunTestsCoroutine(bool exitGameWhenFinished)
        {
            Time.timeScale = HIGH_TIME_SCALE;
            yield return new WaitForSecondsRealtime(1.0f);
            while (!ApplicationStarted)
            {
                yield return new WaitForSecondsRealtime(DELAY_SHORT);
            }
            Plugin.Logger.LogMessage(DIVIDER);
            Plugin.Logger.LogWarning("DeTESTive is running. For optimal results, do not interact");
            Plugin.Logger.LogWarning("with the game while tests are running.");
            Plugin.Logger.LogMessage(DIVIDER);
            // Force the game to proceed to the main menu without us having to do anything
            ControlDetectController.Instance.loadSceneTriggered = true;
            while (!MainMenuLoaded)
            {
                yield return new WaitForSecondsRealtime(DELAY_SHORT);
            }
            Time.timeScale = 1.0f;
            foreach (
                var sourceGroup in (IEnumerable<IGrouping<string, DeTest>>)
                    tests.GroupBy(test => test.Source)
            )
            {
                foreach (
                    var saveGroup in (IEnumerable<IGrouping<string, DeTest>>)
                        sourceGroup.GroupBy(test => test.SaveGameNameWithExt)
                )
                {
                    IList<DeTest> currentGroupOfTests = saveGroup.ToList();
                    // Shuffle the order so that issues related to the order
                    // of tests are visible to devs
                    currentGroupOfTests = currentGroupOfTests.Shuffle();
                    yield return RuntimeHelper.StartCoroutine(
                        RunTestsForGroup(currentGroupOfTests)
                    );
                }
            }
            Plugin.Logger.LogMessage(
                $"DeTESTive run finished, report available at: \"{REPORT_FILE_PATH}\"."
            );
            File.AppendAllText(REPORT_FILE_PATH, "</code></body></html>");
            if (exitGameWhenFinished)
            {
                Plugin.Logger.LogMessage("Quitting the game...");
                yield return new WaitForSecondsRealtime(1.0f);
                MainMenuController.Instance.ExitGame();
            }
        }

        private static System.Collections.IEnumerator RunTestsForGroup(IEnumerable<DeTest> tests)
        {
            var allPassing = true;
            foreach (var test in tests)
            {
                yield return RuntimeHelper.StartCoroutine(RunTest(test));
                // This is probably redundant, but I'll keep it
                while (!test.IsComplete)
                {
                    yield return new WaitForSecondsRealtime(DELAY_SHORT);
                }
                if (!test.IsPassing)
                {
                    allPassing = false;
                }
            }

            var source = tests.First().Source;
            var saveName = tests.First().SaveGameNameWithExt;

            PrintTestSuiteSummaryHeader(allPassing, source, saveName);
            var index = 0;
            foreach (var test in tests)
            {
                PrintTestResult(test, index);
                index++;
            }
            PrintTestSuiteSummaryFooter(tests);
        }

        private enum LogLevel
        {
            ERROR,
            MESSAGE,
            HEADER,
        }

        private static readonly string REPORTS_DIRECTORY_PATH =
            $@"{Directory.GetCurrentDirectory()}\DeTESTive";

        private static string REPORT_FILE_PATH = $@"{REPORTS_DIRECTORY_PATH}\report.html";

        private static void LogToTestReport(LogLevel level, string message)
        {
            if (!Directory.Exists(REPORTS_DIRECTORY_PATH))
            {
                Directory.CreateDirectory(REPORTS_DIRECTORY_PATH);
            }

            if (s_isFirstLog)
            {
                // Using a very simple setup for now, but could use Scriban if it becomes more advanced.
                var html =
                    @"
                    <!DOCTYPE html>
                    <html lang='en'>
                      <head>
                        <meta charset='UTF-8' />
                        <meta name='viewport' content='width=device-width, initial-scale=1.0' />
                        <title>DeTESTive Report</title>
                      </head>
                      <body>
                        <h1 style='font-size: x-large;'>DeTESTive Report</h1>
                        <h2>
                          <a
                            style='font-size: large;'
                            href='https://bitbucket.org/shadows-of-doubt-mods/game-balance-options-mod-thunderstore-bepinex-bleeding-edge/src/main/dist/GameBalanceOptions/'
                            >See the Bitbucket repo for documentation.</a
                          >
                        </h2>
                        <code>";
                File.WriteAllText(REPORT_FILE_PATH, html);
                s_isFirstLog = false;
            }
            if (message == "")
            {
                File.AppendAllText(REPORT_FILE_PATH, "<br />");
                return;
            }
            if (message == DIVIDER)
            {
                File.AppendAllText(REPORT_FILE_PATH, "<hr />");
                return;
            }
            switch (level)
            {
                case LogLevel.ERROR:
                    Plugin.Logger.LogError(message);
                    File.AppendAllText(REPORT_FILE_PATH, $"<p style='color: red;'>{message}</p>\n");
                    break;
                case LogLevel.HEADER:
                    Plugin.Logger.LogMessage(message);
                    File.AppendAllText(REPORT_FILE_PATH, $"<strong>{message}</strong>\n");
                    break;
                default:
                    Plugin.Logger.LogMessage(message);
                    File.AppendAllText(REPORT_FILE_PATH, $"<p>{message}</p>\n");
                    break;
            }
        }

        private static void PrintTestResult(DeTest test, int index)
        {
            var resultHeading =
                $"{INDENT}{index + 1}: {(test.IsPassing ? "PASSED" : "FAILED")} - \"{test.Description}\" (took {test.TimeTaken} ms)";
            if (test.IsPassing)
            {
                LogToTestReport(LogLevel.MESSAGE, resultHeading);
            }
            else
            {
                LogToTestReport(LogLevel.ERROR, resultHeading);
                LogToTestReport(
                    LogLevel.MESSAGE,
                    $"{INDENT}{INDENT}Message: {test.FailureMessage}"
                );
                LogToTestReport(LogLevel.MESSAGE, $"{INDENT}{INDENT}Hint: {test.FailureHint}");
            }
            LogToTestReport(LogLevel.MESSAGE, "");
        }

        private static void PrintTestSuiteSummaryHeader(
            bool allPassing,
            string source,
            string saveName = "No Save Game"
        )
        {
            LogToTestReport(LogLevel.MESSAGE, DIVIDER);
            LogToTestReport(LogLevel.HEADER, $"Test summary for \"{source}\" ({saveName}):");
            if (allPassing)
            {
                LogToTestReport(LogLevel.HEADER, $"{INDENT}All tests PASSED");
            }
            else
            {
                LogToTestReport(LogLevel.HEADER, $"{INDENT}One or more tests FAILED");
            }
            LogToTestReport(LogLevel.MESSAGE, DIVIDER);
            LogToTestReport(LogLevel.MESSAGE, "");
        }

        private static void PrintTestSuiteSummaryFooter(IEnumerable<DeTest> tests)
        {
            LogToTestReport(LogLevel.MESSAGE, DIVIDER);
            LogToTestReport(
                LogLevel.MESSAGE,
                $"{tests.Count(test => test.IsPassing)} passing / {tests.Count(test => !test.IsPassing)} failing / {tests.Count()} total"
            );
            LogToTestReport(LogLevel.MESSAGE, DIVIDER);
        }

        private static System.Collections.IEnumerator RunTest(DeTest test)
        {
            yield return new WaitForEndOfFrame();
            var startTime = Time.fixedUnscaledTime;
            if (test.TimeScale.HasValue)
            {
                Time.timeScale = test.TimeScale.Value;
            }
            // Game Init
            if (test.StartsNewGame)
            {
                yield return RuntimeHelper.StartCoroutine(StartNewGame(test));
                if (s_gameNotStarted)
                {
                    var message =
                        $"Save file \"{test.SaveGameNameWithExt}\" ({test.SaveGameModFolderName}) could not be loaded for \"{test.Description}\" of \"{test.Source}\"!";
                    Plugin.Logger.LogError($"{message} Skipping test...");
                    test.IsPassing = false;
                    test.FailureMessage = message;
                    test.TimeTaken = 0.0f;
                    s_gameNotStarted = false;
                    test.IsComplete = true;
                    yield break;
                }
            }
            // Post-Game-Init Setup
            if (test.Setup != null)
            {
                yield return RuntimeHelper.StartCoroutine(test.Setup());
            }
            // Assert
            try
            {
                test.IsPassing = test.AssertFunc();
            }
            catch (System.Exception error)
            {
                test.IsPassing = false;
                test.FailureMessage = error.Message;
            }
            // Teardown
            test.TimeTaken = (Time.fixedUnscaledTime - startTime) * 1000.0f;
            Time.timeScale = 1.0f;
            if (test.Teardown != null)
            {
                yield return RuntimeHelper.StartCoroutine(test.Teardown());
            }

            test.IsComplete = true;
        }

        private static System.Collections.IEnumerator PopulateLoadGameSubMenu()
        {
            // Open the Load Game submenu
            MainMenuController.Instance.SetMenuComponent(MainMenuController.Component.loadGame);
            yield return new WaitForEndOfFrame();
            // Populate it
            MainMenuController.Instance.LoadGame();
            yield return new WaitForEndOfFrame();
        }

        private static System.Collections.IEnumerator SelectSaveEntry(string savePath)
        {
            var loadEntryContainer = MainMenuController.Instance.loadGameContentRect;
            for (int i = 0; i < loadEntryContainer.childCount; i++)
            {
                var entry = loadEntryContainer.GetChild(i).GetComponent<SaveGameEntryController>();
                if (entry.info == null)
                {
                    continue;
                }
                // Both paths are converted from POSIX for this comparison
                if (entry.info.FullPath.Replace("/", "\\") != savePath.Replace("/", "\\"))
                {
                    continue;
                }
                entry.OnLeftClick();
                yield return new WaitForSecondsRealtime(DELAY_SHORT);
                yield break;
            }
            throw new System.Exception($"Did not find match for savePath: {savePath}");
        }

        internal static string s_currentlyLoadedSave = "";

        private static System.Collections.IEnumerator StartNewGame(DeTest test)
        {
            var isLoaded = false;
            System.Action loadDetectAction = () => isLoaded = true;
            Hooks.OnGameStart.AddListener(loadDetectAction);

            string hashStr = "";
            string savePath(string _hashStr) =>
                $"{TestHelpers.SAVE_FILES_PATH}/DeTESTive-{_hashStr}.sodb";
            var resolvedPath = TestHelpers.GetFilePathInModInstallDirectory(
                test.SaveGameModFolderName,
                test.SaveGameNameWithExt
            );
            if (s_currentlyLoadedSave == resolvedPath)
            {
                Hooks.OnGameStart.RemoveListener(loadDetectAction);
                yield break;
            }
            else if (SessionData.Instance.startedGame)
            {
                TestHelpers.EndGameAndReturnToMainMenu();
                yield return new WaitForEndOfFrame();
            }
            if (!File.Exists(resolvedPath))
            {
                Hooks.OnGameStart.RemoveListener(loadDetectAction);
                s_gameNotStarted = true;
                yield break;
            }
            var hash = new Hash128();
            hash.Append(resolvedPath);
            hash.Append(File.GetLastWriteTime(resolvedPath).ToLongTimeString());
            hashStr = hash.ToString();
            // Copy the save file to the persistent data path
            try
            {
                File.Copy(resolvedPath, savePath(hashStr));
            }
            catch (System.IO.IOException e)
            {
                if (!e.Message.Contains("already exists"))
                {
                    Hooks.OnGameStart.RemoveListener(loadDetectAction);
                    s_gameNotStarted = true;
                    yield break;
                }
            }
            yield return RuntimeHelper.StartCoroutine(PopulateLoadGameSubMenu());
            yield return RuntimeHelper.StartCoroutine(SelectSaveEntry(savePath(hashStr)));
            // Start the loading process
            MainMenuController.Instance.SetMenuComponent(MainMenuController.Component.loadingCity);
            yield return new WaitForEndOfFrame();
            while (!isLoaded)
            {
                yield return new WaitForSecondsRealtime(DELAY_SHORT);
            }
            s_currentlyLoadedSave = resolvedPath;
            Hooks.OnGameStart.RemoveListener(loadDetectAction);
            if (hashStr != "")
            {
                yield return RuntimeHelper.StartCoroutine(PopulateLoadGameSubMenu());
                yield return RuntimeHelper.StartCoroutine(SelectSaveEntry(savePath(hashStr)));
                MainMenuController.Instance.DeleteSave();
            }
        }
    }
}
