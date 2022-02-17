using BepInEx;
using BepInEx.Logging;

namespace DirtCapcity
{

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("GoldRushTheGame.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Log;

        DirtCapcity dirtCapcity;


        private void Awake()
        {
            Plugin.Log = base.Logger;

            dirtCapcity = new DirtCapcity(this);



            // Plugin startup logic
            Plugin.Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

    }
}
