using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace double_shards
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Hollow Knight Silksong.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private static float _shardsMultiplier = 2;
        private static readonly Random Random = new();

        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Plugin));
            _shardsMultiplier = Config.Bind("Cheats", "ShardsMultiplier", 2.0f, "The multiplier for collecting shards. Fractional values work by randomly rounding up/down to get the desired multiplier on average.").Value;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(PlayerData), "AddShards")]
        [HarmonyPrefix]
        private static void AddShardsPrefix(PlayerData __instance, ref int amount)
        {
            amount = RoundRandomly(amount* _shardsMultiplier);
        }

        // Round up/down in a way that gives roundMe as an average value
        // For instance, if roundMe = 1.25, then it will round down to "1" 75% of the time, and round up to "2" 25% of the time
        private static int RoundRandomly(float roundMe)
        {
            int floor = (int)Math.Floor(roundMe);
            float fractionalPart = roundMe - floor;
            return fractionalPart > 0 && fractionalPart > Random.NextDouble() ? floor + 1 : floor;
        }
    }
}
