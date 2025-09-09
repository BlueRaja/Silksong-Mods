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
using GlobalEnums;

namespace rosaries_never_permanently_lost
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInProcess("Hollow Knight Silksong.exe")]
    public class Plugin : BaseUnityPlugin
    {
        private static float _onDeathCocoonMultiplier = 1.0f;
        private static bool _cocoonShouldStayInPlace = true;
        private static string _lastHeroCorpseScene;
        private static int _lastHeroCorpseMoneyPool;
        private static byte[] _lastHeroCorpseMarkerGuid;
        private static HeroDeathCocoonTypes _lastHeroCorpseType;
        private static ManualLogSource Logger;
        
        private void Awake()
        {
            Harmony.CreateAndPatchAll(typeof(Plugin));
            _onDeathCocoonMultiplier = Config.Bind("Cheats", "OnDeathCocoonMultiplier", 1.0f, "What percentage of rosaries should be kept in the cocoon each time you die. For instance, if this is set to 0.75 and the cocoon from your previous death(s) has 1000 rosaries, then if you die another time, the cocoon will have 750 rosaries + whatever you were holding").Value;
            _cocoonShouldStayInPlace = Config.Bind("Cheats", "CocoonShouldStayInPlace", true, "If set to true, the cocoon will stay in the same place until it's collected, no matter how many times you die. If false, the cocoon moves to your last death location each time you die.").Value;
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        // This happens before the HeroCorpse* fields are set
        [HarmonyPatch(typeof(HeroController), "Die")]
        [HarmonyPrefix]
        private static void DiePrefix(HeroController __instance)
        {
            _lastHeroCorpseScene = __instance.playerData.HeroCorpseScene;
            _lastHeroCorpseMoneyPool = __instance.playerData.HeroCorpseMoneyPool;
            _lastHeroCorpseMarkerGuid = __instance.playerData.HeroCorpseMarkerGuid;
            _lastHeroCorpseType = __instance.playerData.HeroCorpseType;
        }

        // This happens after the HeroCorpse* fields are set
        [HarmonyPatch(typeof(GameManager), "PlayerDead")]
        [HarmonyPrefix]
        private static void PlayerDeadPrefix(GameManager __instance)
        {
            if (_cocoonShouldStayInPlace && _lastHeroCorpseMarkerGuid != null && !String.IsNullOrEmpty(_lastHeroCorpseScene))
            {
                __instance.playerData.HeroCorpseScene = _lastHeroCorpseScene;
                __instance.playerData.HeroCorpseMarkerGuid = _lastHeroCorpseMarkerGuid;
                __instance.playerData.HeroCorpseType |= _lastHeroCorpseType;
            }

            __instance.playerData.HeroCorpseMoneyPool += (int)Math.Round(_lastHeroCorpseMoneyPool * _onDeathCocoonMultiplier);
            Logger.LogInfo($"Death cocoon now holds {__instance.playerData.HeroCorpseMoneyPool} rosaries");
        }
    }
}
