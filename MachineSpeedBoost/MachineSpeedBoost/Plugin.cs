using BepInEx;
using BepInEx.Logging;

namespace MachineSpeedBoost
{

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInProcess("GoldRushTheGame.exe")]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Log;

        MachineSpeedBoost machineSpeedBoost;


        private void Awake()
        {
            Plugin.Log = base.Logger;

            machineSpeedBoost = new MachineSpeedBoost(this);



            // Plugin startup logic
            Plugin.Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

    }
}
