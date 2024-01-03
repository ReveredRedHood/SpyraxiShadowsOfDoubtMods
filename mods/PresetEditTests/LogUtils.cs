using System;
using System.Linq;
using BepInEx.Logging;
using ThrottleDebounce;

namespace PresetEditTests;
internal static class LogUtils {
    private static ManualLogSource LogSource { get; set; }

    internal static void Load(ManualLogSource pluginLogSource) {
        LogSource = new ManualLogSource(pluginLogSource.SourceName + "/LogUtils");
        // Register the source
        BepInEx.Logging.Logger.Sources.Add(LogSource);
    }

    internal static void Unload() {
        // Remove the source to free resources
        BepInEx.Logging.Logger.Sources.Remove(LogSource);
    }

    internal static void QuickLog(this string msg) {
        LogSource.LogInfo($"{msg}");
    }

    internal static void QuickLog<T>(this System.Collections.Generic.IEnumerable<T> msg) {
        if (msg.Count() == 0) {
            LogSource.LogInfo($"{msg.ToString()} is empty");
            return;
        }
        LogSource.LogInfo($"{string.Join(", ", msg)}");
    }

    internal static RateLimitedAction<string> GetThrottledLog(float seconds) {
        return Throttler.Throttle<string>(str => QuickLog(str), TimeSpan.FromSeconds(seconds));
    }
};