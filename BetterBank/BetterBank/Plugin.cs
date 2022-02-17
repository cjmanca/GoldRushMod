using BepInEx;
using BepInEx.Logging;

namespace BetterBank
{

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("GoldRushTheGame.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Log;

        BetterBank betterBank;


        private void Awake()
        {
            Plugin.Log = base.Logger;

            betterBank = new BetterBank(this);



            // Plugin startup logic
            Plugin.Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

    }
}
