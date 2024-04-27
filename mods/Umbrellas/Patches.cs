using System;
using HarmonyLib;

namespace Umbrellas;
internal class Patches
{
    // [HarmonyPatch(typeof(Player), nameof(Player.OnGameLocationChange))]
    // internal class Player_OnGameLocationChange
    // {
    //     [HarmonyPostfix]
    //     internal static void Postfix(
    //         Player __instance
    //     )
    //     {
    //         var locationObj = __instance.currentGameLocation as UnityEngine.Object;
    //         if (locationObj == null)
    //         {
    //             return;
    //         }
    //         var name = locationObj.name;
    //         if (name == String.Empty)
    //         {
    //             return;
    //         }
    //         Plugin.LocationsVisited.Add(name);
    //     }
    // }
}