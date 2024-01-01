using System;
using HarmonyLib;

namespace HoursOfOperation
{
    internal class Patches
    {
        // TODO: Consider adding to SOD.Common
        [HarmonyPatch(typeof(Actor), nameof(Actor.OnGameLocationChange))]
        internal class Actor_OnGameLocationChange
        {
            [HarmonyPostfix]
            internal static void Postfix(
                Actor __instance,
                bool enableSocialSightings,
                bool forceDisableLocationMemory
            )
            {
                if (!__instance.isPlayer)
                {
                    return;
                }
                if (forceDisableLocationMemory)
                {
                    Plugin.Log.LogInfo($"forceDisableLocationMemory is true");
                    return;
                }
                var name =
                    __instance.currentGameLocation?.thisAsAddress?.company?.name ?? String.Empty;
                if (name != String.Empty)
                {
                    Plugin.VisitedCompanies.Add(name);
                    Plugin.Log.LogInfo($"Visited {name}");
                }
            }
        }
    }
}
