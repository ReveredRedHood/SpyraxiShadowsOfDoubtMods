using System;
using System.Linq;
using BepInEx.Logging;
using ThrottleDebounce;

namespace TestHelper;
internal static class LogUtils {
    private static ManualLogSource LogSource { get; set; }

    internal static void Load(ManualLogSource pluginLogSource) {
        LogSource = new ManualLogSource(pluginLogSource.SourceName);
    }

    internal static void Unload() {
    }

    internal static void QuickLog(this string msg) {
        LogSource.LogDebug(msg);
    }

    internal static void QuickLog<T>(this System.Collections.Generic.IEnumerable<T> msg) {
        if (msg.Count() == 0) {
            QuickLog($"{msg.ToString()} is empty");
            return;
        }
        QuickLog($"{string.Join(", ", msg)}");
    }

    internal static RateLimitedAction<string> GetThrottledLog(float seconds) {
        return Throttler.Throttle<string>(str => QuickLog(str), TimeSpan.FromSeconds(seconds));
    }
};