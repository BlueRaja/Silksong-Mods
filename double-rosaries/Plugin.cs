using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using GlobalSettings;
using HarmonyLib;
using HutongGames.PlayMaker;
using InControl;
using System;
using System.Linq;
using GenericVariableExtension;

namespace double_rosaries
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Hollow Knight Silksong.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private static float _rosaryMultiplier = 2;
        private static ManualLogSource Logger;
        private const int DEFAULT_ROSARY_STRING_MACHINE_COST = 80;
        
        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Plugin));
            _rosaryMultiplier = Config.Bind("Cheats", "RosaryMultiplier", 2.0f, "The multiplier for collecting rosaries. Note that most fractional values won't work.").Value;
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        [HarmonyPatch(typeof(PlayerData), "AddGeo")]
        [HarmonyPrefix]
        private static void AddGeoPrefix(PlayerData __instance, ref int amount)
        {
            amount = (int)Math.Round(amount*_rosaryMultiplier);
        }

        [HarmonyPatch(typeof(HeroController), "CocoonBroken", new[] { typeof(bool), typeof(bool) })]
        [HarmonyPrefix]
        private static void CocoonBrokenPrefix(ref bool doAirPause, ref bool forceCanBind, HeroController __instance)
        {
            // Prevent player from getting double rosaries when picking up their dead body
            __instance.playerData.HeroCorpseMoneyPool = (int)Math.Round(__instance.playerData.HeroCorpseMoneyPool/_rosaryMultiplier);
        }

        [HarmonyPatch(typeof(ShopItem), "Cost", MethodType.Getter)]
        [HarmonyPostfix]
        private static void CostPostfix(ShopItem __instance, ref int __result)
        {
            // Prevent player from getting double rosaries when buying rosary strings from vendors
            string key = Traverse.Create(__instance).Field("displayName").Field<string>("Key").Value;
            if (key.StartsWith("INV_NAME_COIN_SET"))
            {
                __result = (int)Math.Round(__result * _rosaryMultiplier);
                Logger.LogInfo($"Set cost of {key} to {__result}");
            }
        }

        [HarmonyPatch(typeof(PlayMakerNPC), "SendEvent")]
        [HarmonyPrefix]
        private static void OnStartDialoguePrefix(PlayMakerNPC __instance, ref string eventName)
        {
            // Prevent player from getting double rosaries from the rosary string machines
            if (__instance.name == "rosary_string_machine" && eventName == "INTERACT")
            {
                var fsm = Traverse.Create(__instance).Field<PlayMakerFSM>("dialogueFsm").Value;
                var costReferenceRef = fsm.FsmVariables.ObjectVariables.FirstOrDefault(o => o.Value is CostReference);
                if (costReferenceRef != null)
                {
                    CostReference costReference = (CostReference)costReferenceRef.Value;
                    int newCost = (int)Math.Round(DEFAULT_ROSARY_STRING_MACHINE_COST * _rosaryMultiplier);
                    Traverse.Create(costReference).Field<int>("value").Value = newCost;
                    Logger.LogInfo($"Set cost of rosary string machine to {newCost}"); 
                }
                else
                {
                    Logger.LogError("Could not find cost reference for rosary_string_machine");
                }
            }
        }
    }
}
