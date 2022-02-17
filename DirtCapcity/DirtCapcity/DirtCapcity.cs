using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using GoldDigger;
using System.Collections.Generic;
using System;

namespace DirtCapcity
{
    public class DirtCapcity
    {
        static Plugin plugin;
        private static Harmony patcher = null;

        public class DirtContainer
        {

            public DiggingController instance = null;


            private float _MaxVolume;
            public ConfigEntry<float> ConfigMaxVolume { get; set; } = null;
            public float MaxVolume
            {
                get
                {
                    return GetValue(ConfigMaxVolume, _MaxVolume);
                }
                set
                {
                    SetValue(ConfigMaxVolume, ref _MaxVolume, value);
                }
            }

            private float _BladeSizeX;
            public ConfigEntry<float> ConfigBladeSizeX { get; set; } = null;
            public float BladeSizeX
            {
                get
                {
                    return GetValue(ConfigBladeSizeX, _BladeSizeX);
                }
                set
                {
                    SetValue(ConfigBladeSizeX, ref _BladeSizeX, value);
                }
            }

            private float _BladeSizeZ;
            public ConfigEntry<float> ConfigBladeSizeZ { get; set; } = null;
            public float BladeSizeZ
            {
                get
                {
                    return GetValue(ConfigBladeSizeZ, _BladeSizeZ);
                }
                set
                {
                    SetValue(ConfigBladeSizeZ, ref _BladeSizeZ, value);
                }
            }

            private float _DigDepth;
            public ConfigEntry<float> ConfigDigDepth { get; set; } = null;
            public float DigDepth
            {
                get
                {
                    return GetValue(ConfigDigDepth, _DigDepth);
                }
                set
                {
                    SetValue(ConfigDigDepth, ref _DigDepth, value);
                }
            }



            public DirtContainer()
            {

            }
            public DirtContainer(float maxVolume, float bladeSizeX, float bladeSizeZ, float digDepth)
            {
                _MaxVolume = maxVolume;
                _BladeSizeX = bladeSizeX;
                _BladeSizeZ = bladeSizeZ;
                _DigDepth = digDepth;
            }
            public DirtContainer(float maxVolume)
            {
                _MaxVolume = maxVolume;
            }


            private static T GetValue<T>(ConfigEntry<T> ce, T cached)
            {
                if (ce != null)
                {
                    return ce.Value;
                }
                return cached;
            }

            private static void SetValue<T>(ConfigEntry<T> ce, ref T toSet, T newValue)
            {
                if (ce != null)
                {
                    if (!ce.Value.Equals(newValue))
                    {
                        ce.Value = newValue;
                    }
                }
                else
                {
                    toSet = newValue;
                }
            }

        }


        private static ConfigEntry<bool> enabled { get; set; }


        private static Shovel shovelInstance = null;
        private static DirtContainer shovel = new DirtContainer(0.01f, 0.4385498f, 0.5189149f, 0.2f);
        private static DirtContainer originalShovel = null;



        private static HogPanDirtBox hogPanInstance = null;
        private static DirtContainer hogPan = new DirtContainer(0.09f);
        private static DirtContainer originalHogPan = null;

        private static Bucket bucketInstance = null;
        private static DirtContainer bucket = new DirtContainer(0.03f);
        private static DirtContainer originalBucket = null;

        private static MagnetiteSeparator magnetiteSeparatorInstance = null;
        private static DirtContainer magnetiteSeparator = new DirtContainer(0.12f);
        private static DirtContainer originalMagnetiteSeparator = null;

        private static MagnetiteTrailer magnetiteTrailerInstance = null;
        private static DirtContainer magnetiteTrailer = new DirtContainer(0.3f);
        private static DirtContainer originalMagnetiteTrailer = null;


        private static MiniWashplant miniWashplantInstance = null;
        private static DirtContainer miniWashplant = new DirtContainer(1); // not sure what this is
        private static DirtContainer originalMiniWashplant = null;

        private static MobileWashplant mobileWashplantInstance = null;
        private static DirtContainer mobileWashplant = new DirtContainer(10);
        private static DirtContainer originalMobileWashplant = null;

        private static WashplantShakerBase washplantShakerInstance = null;
        private static DirtContainer washplantShaker = new DirtContainer(40);
        private static DirtContainer originalWashplantShaker = null;

        private static WaveTable waveTableInstance = null;
        private static DirtContainer waveTable = new DirtContainer(0.06f);
        private static DirtContainer originalWaveTable = null;

        private static MinersMoss minersMossInstance = null;
        private static DirtContainer minersMoss = new DirtContainer(0.2488f);
        private static DirtContainer originalMinersMoss = null;

        private static ConveyorGround conveyorGroundInstance = null;
        private static DirtContainer conveyorGround = new DirtContainer(80);
        private static DirtContainer originalConveyorGround = null;


        static Dictionary<string, DirtContainer> originalContainerData = new Dictionary<string, DirtContainer>();

        static Dictionary<string, DirtContainer> containerData = new Dictionary<string, DirtContainer>()
        {
            { "Moxy6x6", new DirtContainer(80, 3.128363f, 0.5457281f, 0.3f) },
            { "Hitachi_EX400LC", new DirtContainer(5, 2.002002f, 0.5278873f, 0.5f) },
            { "Hitachi_EX270LC", new DirtContainer(2.5f, 1.601601f, 0.4224002f, 0.5f) },
            //{ "Caterpillar_D6H", new DirtContainer(0, 2.662001f, 0.3428272f, 0.18f) }, // just pushes dirt around, no capacity to change
            { "Caterpillar_980B", new DirtContainer(10, 4.207835f, 0.5245685f, 0.3f) } // Front End Loader
        };


        static AccessTools.FieldRef<Shovel, float> Shovel_bladeSizex = AccessTools.FieldRefAccess<Shovel, float>("_bladeSizex");
        static AccessTools.FieldRef<Shovel, float> Shovel_bladeSizez = AccessTools.FieldRefAccess<Shovel, float>("_bladeSizez");

        static AccessTools.FieldRef<DiggingController, float> DiggingController_bladeSizex = AccessTools.FieldRefAccess<DiggingController, float>("_bladeSizex");
        static AccessTools.FieldRef<DiggingController, float> DiggingController_bladeSizez = AccessTools.FieldRefAccess<DiggingController, float>("_bladeSizey");

        static AccessTools.FieldRef<Controller, MachineController> MyMachineController = AccessTools.FieldRefAccess<Controller, MachineController>("MyMachineController");

        



        public DirtCapcity(Plugin pPlugin)
        {
            plugin = pPlugin;

            enabled = plugin.Config.Bind<bool>("DirtCapcity", "Enabled", true, new ConfigDescription("Enable this mod", new AcceptableValueList<bool>(false, true)));

            shovel.ConfigMaxVolume = plugin.Config.Bind<float>("DirtCapcity", "Shovel.MaxVolume", shovel.MaxVolume, new ConfigDescription("Maximum dirt shovel can hold", new AcceptableValueRange<float>(0, 0.09f)));
            shovel.ConfigBladeSizeX = plugin.Config.Bind<float>("DirtCapcity", "Shovel.BladeSizeX", shovel.BladeSizeX, new ConfigDescription("Width of Shovel", new AcceptableValueRange<float>(0, 10)));
            shovel.ConfigBladeSizeZ = plugin.Config.Bind<float>("DirtCapcity", "Shovel.BladeSizeZ", shovel.BladeSizeZ, new ConfigDescription("Length of Shovel", new AcceptableValueRange<float>(0, 1)));
            shovel.ConfigDigDepth = plugin.Config.Bind<float>("DirtCapcity", "Shovel.DigDepth", shovel.DigDepth, new ConfigDescription("Extra Depth to dig?", new AcceptableValueRange<float>(0, 1)));

            foreach (var item in containerData)
            {
                item.Value.ConfigMaxVolume = plugin.Config.Bind<float>("DirtCapcity", item.Key + ".MaxVolume", item.Value.MaxVolume, new ConfigDescription("Maximum dirt bucket can hold", new AcceptableValueRange<float>(0, 1000)));
                item.Value.ConfigBladeSizeX = plugin.Config.Bind<float>("DirtCapcity", item.Key + ".BladeSizeX", item.Value.BladeSizeX, new ConfigDescription("Width of Dig Blades", new AcceptableValueRange<float>(0, 10)));
                item.Value.ConfigBladeSizeZ = plugin.Config.Bind<float>("DirtCapcity", item.Key + ".BladeSizeZ", item.Value.BladeSizeZ, new ConfigDescription("Length of Dig Blades", new AcceptableValueRange<float>(0, 1)));
                item.Value.ConfigDigDepth = plugin.Config.Bind<float>("DirtCapcity", item.Key + ".DigDepth", item.Value.DigDepth, new ConfigDescription("Extra Depth to dig?", new AcceptableValueRange<float>(0, 1)));

            }



            hogPan.ConfigMaxVolume = plugin.Config.Bind<float>("DirtCapcity", "HogPan.MaxVolume", hogPan.MaxVolume, new ConfigDescription("Maximum dirt HogPan can hold", new AcceptableValueRange<float>(0, 0.09f)));

            bucket.ConfigMaxVolume = plugin.Config.Bind<float>("DirtCapcity", "Bucket.MaxVolume", bucket.MaxVolume, new ConfigDescription("Maximum dirt bucket can hold", new AcceptableValueRange<float>(0, 0.03f)));

            magnetiteSeparator.ConfigMaxVolume = plugin.Config.Bind<float>("DirtCapcity", "MagnetiteSeparator.MaxVolume", magnetiteSeparator.MaxVolume, new ConfigDescription("Maximum dirt MagnetiteSeparator can hold", new AcceptableValueRange<float>(0, 0.09f)));

            magnetiteTrailer.ConfigMaxVolume = plugin.Config.Bind<float>("DirtCapcity", "MagnetiteTrailer.MaxVolume", magnetiteTrailer.MaxVolume, new ConfigDescription("Maximum dirt MagnetiteTrailer can hold", new AcceptableValueRange<float>(0, 0.09f)));

            //miniWashplant.ConfigMaxVolume = plugin.Config.Bind<float>("DirtCapcity", "MiniWashplant.MaxVolume", miniWashplant.MaxVolume, new ConfigDescription("Maximum dirt MiniWashplant can hold", new AcceptableValueRange<float>(0, 0.09f)));

            mobileWashplant.ConfigMaxVolume = plugin.Config.Bind<float>("DirtCapcity", "MobileWashplant.MaxVolume", mobileWashplant.MaxVolume, new ConfigDescription("Maximum dirt MobileWashplant can hold", new AcceptableValueRange<float>(0, 0.09f)));

            washplantShaker.ConfigMaxVolume = plugin.Config.Bind<float>("DirtCapcity", "WashplantShaker.MaxVolume", washplantShaker.MaxVolume, new ConfigDescription("Maximum dirt WashplantShaker can hold", new AcceptableValueRange<float>(0, 0.09f)));

            waveTable.ConfigMaxVolume = plugin.Config.Bind<float>("DirtCapcity", "WaveTable.MaxVolume", waveTable.MaxVolume, new ConfigDescription("Maximum dirt WaveTable can hold", new AcceptableValueRange<float>(0, 0.09f)));

            minersMoss.ConfigMaxVolume = plugin.Config.Bind<float>("DirtCapcity", "MinersMoss.MaxVolume", minersMoss.MaxVolume, new ConfigDescription("Maximum dirt MinersMoss can hold", new AcceptableValueRange<float>(0, 0.09f)));

            conveyorGround.ConfigMaxVolume = plugin.Config.Bind<float>("DirtCapcity", "ConveyorHopper.MaxVolume", conveyorGround.MaxVolume, new ConfigDescription("Maximum dirt Conveyor Hopper can hold", new AcceptableValueRange<float>(0, 0.09f)));




            if (enabled.Value && patcher == null)
            {
                patcher = Harmony.CreateAndPatchAll(typeof(DirtCapcity));
            }

            //Plugin.Log.LogMessage("DAYILY_STATS_CREDIT_PART: " + new LocalizationKey("DAYILY_STATS_CREDIT_PART").GetLocalized());

            enabled.SettingChanged += Enabled_SettingChanged;
        }

        private void Enabled_SettingChanged(object sender, System.EventArgs e)
        {
            if (enabled.Value && patcher == null)
            {
                patcher = Harmony.CreateAndPatchAll(typeof(DirtCapcity));
            }
            else
            {
                patcher.UnpatchAll();
                patcher = null;



                if (hogPanInstance != null)
                {
                    hogPanInstance.PlaneVolumeMax = originalHogPan.MaxVolume;
                }

                if (bucketInstance != null)
                {
                    bucketInstance.MaxVolume = originalBucket.MaxVolume;
                }

                if (magnetiteSeparatorInstance != null)
                {
                    magnetiteSeparatorInstance.MaxFill = originalMagnetiteSeparator.MaxVolume;
                }

                if (magnetiteTrailerInstance != null)
                {
                    magnetiteTrailerInstance.MaxMagnetiteTrailerVolume = originalMagnetiteTrailer.MaxVolume;
                }

                if (miniWashplantInstance != null)
                {
                    miniWashplantInstance.MaxFill = originalMiniWashplant.MaxVolume;
                }

                if (mobileWashplantInstance != null)
                {
                    mobileWashplantInstance.MaxFill = originalMobileWashplant.MaxVolume;
                }

                if (washplantShakerInstance != null)
                {
                    washplantShakerInstance.MaxFill = originalWashplantShaker.MaxVolume;
                }

                if (waveTableInstance != null)
                {
                    waveTableInstance.MaxGroundVolume = originalWaveTable.MaxVolume;
                }

                if (minersMossInstance != null)
                {
                    minersMossInstance.MaxGroundVolume = originalMinersMoss.MaxVolume;
                }

                if (conveyorGroundInstance != null)
                {
                    conveyorGroundInstance.MaxDirt = originalConveyorGround.MaxVolume;
                }

                if (shovelInstance != null)
                {
                    shovelInstance.MaxVolume = originalShovel.MaxVolume;
                    shovelInstance.DigDepth = originalShovel.DigDepth;

                    Shovel_bladeSizex(shovelInstance) = originalShovel.BladeSizeX;
                    Shovel_bladeSizez(shovelInstance) = originalShovel.BladeSizeZ;
                }

                foreach (var data in originalContainerData)
                {
                    ApplyData(data.Value);
                }
            }
        }


        static void ApplyData(DirtContainer data)
        {

            if (data != null && data.instance != null)
            {
                DiggingController self = data.instance;


                self._maxShovelVolume = shovel.MaxVolume;
                self.DigDepth = shovel.DigDepth;

                DiggingController_bladeSizex(self) = shovel.BladeSizeX;
                DiggingController_bladeSizez(self) = shovel.BladeSizeZ;

            }
        }


        [HarmonyPatch(typeof(HogPanDirtBox), nameof(HogPanDirtBox.AddToPlane))]
        [HarmonyPrefix]
        static bool Pre_HogPanDirtBox_AddToPlane(HogPanDirtBox __instance)
        {
            HogPanDirtBox self = __instance;

            hogPanInstance = self;

            if (originalHogPan == null)
            {
                originalHogPan = new DirtContainer();

                originalHogPan.MaxVolume = self.PlaneVolumeMax;


                Plugin.Log.LogInfo("HogPan Dirt MaxVolume: " + originalHogPan.MaxVolume);

                Plugin.Log.LogInfo("private static DirtContainer hogPan = new DirtContainer(" + originalHogPan.MaxVolume + ", 0, 0, 0)");

            }

            self.PlaneVolumeMax = hogPan.MaxVolume;
            self.MaxGoldVolume = float.MaxValue;

            return true; // false to skip original
        }


        [HarmonyPatch(typeof(Bucket), "Update")]
        [HarmonyPrefix]
        static bool Pre_Bucket_Update(Bucket __instance)
        {
            Bucket self = __instance;

            bucketInstance = self;

            if (originalBucket == null)
            {
                originalBucket = new DirtContainer();

                originalBucket.MaxVolume = self.MaxVolume;


                Plugin.Log.LogInfo("Bucket Dirt MaxVolume: " + originalBucket.MaxVolume);

                Plugin.Log.LogInfo("private static DirtContainer bucket = new DirtContainer(" + originalBucket.MaxVolume + ", 0, 0, 0)");

            }

            self.MaxVolume = bucket.MaxVolume;


            return true; // false to skip original
        }



        [HarmonyPatch(typeof(MagnetiteSeparator), "Update")]
        [HarmonyPrefix]
        static bool Pre_MagnetiteSeparator_Update(MagnetiteSeparator __instance)
        {
            MagnetiteSeparator self = __instance;

            magnetiteSeparatorInstance = self;

            if (originalMagnetiteSeparator == null)
            {
                originalMagnetiteSeparator = new DirtContainer();

                originalMagnetiteSeparator.MaxVolume = self.MaxFill;


                Plugin.Log.LogInfo("MagnetiteSeparator Dirt MaxVolume: " + originalMagnetiteSeparator.MaxVolume);

                Plugin.Log.LogInfo("private static DirtContainer magnetiteSeparator = new DirtContainer(" + originalMagnetiteSeparator.MaxVolume + ", 0, 0, 0)");

            }

            self.MaxFill = magnetiteSeparator.MaxVolume;
            self.MaxMagnetiteVolume = float.MaxValue;

            return true; // false to skip original
        }



        [HarmonyPatch(typeof(MagnetiteTrailer), "Update")]
        [HarmonyPrefix]
        static bool Pre_MagnetiteTrailer_Update(MagnetiteTrailer __instance)
        {
            MagnetiteTrailer self = __instance;

            magnetiteTrailerInstance = self;

            if (originalMagnetiteTrailer == null)
            {
                originalMagnetiteTrailer = new DirtContainer();

                originalMagnetiteTrailer.MaxVolume = self.MaxMagnetiteTrailerVolume;


                Plugin.Log.LogInfo("MagnetiteTrailer Dirt MaxVolume: " + originalMagnetiteTrailer.MaxVolume);

                Plugin.Log.LogInfo("private static DirtContainer magnetiteTrailer = new DirtContainer(" + originalMagnetiteTrailer.MaxVolume + ", 0, 0, 0)");

            }

            self.MaxMagnetiteTrailerVolume = magnetiteTrailer.MaxVolume;

            return true; // false to skip original
        }

        [HarmonyPatch(typeof(MiniWashplant), "Update")]
        [HarmonyPrefix]
        static bool Pre_MiniWashplant_Update(MiniWashplant __instance)
        {
            MiniWashplant self = __instance;

            miniWashplantInstance = self;

            if (originalMiniWashplant == null)
            {
                originalMiniWashplant = new DirtContainer();

                originalMiniWashplant.MaxVolume = self.MaxFill;


                Plugin.Log.LogInfo("MiniWashplant Dirt MaxVolume: " + originalMiniWashplant.MaxVolume);

                Plugin.Log.LogInfo("private static DirtContainer miniWashplant = new DirtContainer(" + originalMiniWashplant.MaxVolume + ", 0, 0, 0)");

            }

            self.MaxFill = miniWashplant.MaxVolume;

            return true; // false to skip original
        }


        [HarmonyPatch(typeof(MobileWashplant), "Update")]
        [HarmonyPrefix]
        static bool Pre_MobileWashplant_Update(MobileWashplant __instance)
        {
            MobileWashplant self = __instance;

            mobileWashplantInstance = self;

            if (originalMobileWashplant == null)
            {
                originalMobileWashplant = new DirtContainer();

                originalMobileWashplant.MaxVolume = self.MaxFill;


                Plugin.Log.LogInfo("MobileWashplant Dirt MaxVolume: " + originalMobileWashplant.MaxVolume);

                Plugin.Log.LogInfo("private static DirtContainer mobileWashplant = new DirtContainer(" + originalMobileWashplant.MaxVolume + ", 0, 0, 0)");

            }

            self.MaxFill = mobileWashplant.MaxVolume;

            return true; // false to skip original
        }


        [HarmonyPatch(typeof(WashplantShakerBase), "Update")]
        [HarmonyPrefix]
        static bool Pre_WashplantShakerBase_Update(WashplantShakerBase __instance)
        {
            WashplantShakerBase self = __instance;

            washplantShakerInstance = self;

            if (originalWashplantShaker == null)
            {
                originalWashplantShaker = new DirtContainer();

                originalWashplantShaker.MaxVolume = self.MaxFill;


                Plugin.Log.LogInfo("WashplantShaker Dirt MaxVolume: " + originalWashplantShaker.MaxVolume);

                Plugin.Log.LogInfo("private static DirtContainer washplantShaker = new DirtContainer(" + originalWashplantShaker.MaxVolume + ", 0, 0, 0)");

            }

            self.MaxFill = washplantShaker.MaxVolume;

            return true; // false to skip original
        }


        [HarmonyPatch(typeof(WaveTable), "Update")]
        [HarmonyPrefix]
        static bool Pre_WaveTable_Update(WaveTable __instance)
        {
            WaveTable self = __instance;
            
            waveTableInstance = self;

            if (originalWaveTable == null)
            {
                originalWaveTable = new DirtContainer();

                originalWaveTable.MaxVolume = self.MaxGroundVolume;


                Plugin.Log.LogInfo("WaveTable Dirt MaxVolume: " + originalWaveTable.MaxVolume);

                Plugin.Log.LogInfo("private static DirtContainer waveTable = new DirtContainer(" + originalWaveTable.MaxVolume + ", 0, 0, 0)");

            }

            self.MaxGroundVolume = waveTable.MaxVolume;

            return true; // false to skip original
        }


        [HarmonyPatch(typeof(MinersMoss), "Update")]
        [HarmonyPrefix]
        static bool Pre_MinersMoss_Update(MinersMoss __instance)
        {
            MinersMoss self = __instance;

            minersMossInstance = self;

            if (originalMinersMoss == null)
            {
                originalMinersMoss = new DirtContainer();

                originalMinersMoss.MaxVolume = self.MaxGroundVolume;


                Plugin.Log.LogInfo("MinersMoss Dirt MaxVolume: " + originalMinersMoss.MaxVolume);

                Plugin.Log.LogInfo("private static DirtContainer minersMoss = new DirtContainer(" + originalMinersMoss.MaxVolume + ", 0, 0, 0)");

            }

            self.MaxGroundVolume = minersMoss.MaxVolume;

            return true; // false to skip original
        }


        [HarmonyPatch(typeof(ConveyorGround), "Update")]
        [HarmonyPrefix]
        static bool Pre_ConveyorGround_Update(ConveyorGround __instance)
        {
            ConveyorGround self = __instance;

            conveyorGroundInstance = self;

            if (originalConveyorGround == null)
            {
                originalConveyorGround = new DirtContainer();

                originalConveyorGround.MaxVolume = self.MaxDirt;


                Plugin.Log.LogInfo("ConveyorGround Dirt MaxVolume: " + originalConveyorGround.MaxVolume);

                Plugin.Log.LogInfo("private static DirtContainer conveyorGround = new DirtContainer(" + originalConveyorGround.MaxVolume + ", 0, 0, 0)");

            }

            self.MaxDirt = conveyorGround.MaxVolume;

            return true; // false to skip original
        }



        [HarmonyPatch(typeof(Shovel), nameof(Shovel.FixedUpdate))]
        [HarmonyPrefix]
        static bool Pre_Shovel_FixedUpdate(Shovel __instance)
        {
            Shovel self = __instance;

            shovelInstance = self;


            if (originalShovel == null)
            {
                originalShovel = new DirtContainer();

                originalShovel.MaxVolume = self.MaxVolume;
                originalShovel.DigDepth = self.DigDepth;

                originalShovel.BladeSizeX = Shovel_bladeSizex(self);
                originalShovel.BladeSizeZ = Shovel_bladeSizez(self);


                Plugin.Log.LogInfo("Shovel Dirt MaxVolume: " + originalShovel.MaxVolume);
                Plugin.Log.LogInfo("Shovel Dirt DigDepth: " + originalShovel.DigDepth);
                Plugin.Log.LogInfo("Shovel Dirt BladeSizeX: " + originalShovel.BladeSizeX);
                Plugin.Log.LogInfo("Shovel Dirt BladeSizeZ: " + originalShovel.BladeSizeZ);

                Plugin.Log.LogInfo("private static DirtContainer shovel = new DirtContainer(" + originalShovel.MaxVolume + ", " + originalShovel.BladeSizeX + ", " + originalShovel.BladeSizeZ + ", " + originalShovel.DigDepth + ")");

            }


            
            self.MaxVolume = shovel.MaxVolume;
            self.DigDepth = shovel.DigDepth;

            Shovel_bladeSizex(self) = shovel.BladeSizeX;
            Shovel_bladeSizez(self) = shovel.BladeSizeZ;
            


            return true; // false to skip original
        }


        [HarmonyPatch(typeof(DiggingController), nameof(DiggingController.Update))]
        [HarmonyPrefix]
        static bool Pre_DiggingController_FixedUpdate(DiggingController __instance)
        {
            DiggingController self = __instance;

            MachineController mmc = MyMachineController(self);

            if (mmc == null)
            {
                return true;
            }

            string name = mmc.name.Replace("(Clone)", "");

            if (!originalContainerData.ContainsKey(name))
            {
                DirtContainer sd = new DirtContainer();

                sd.instance = self;

                sd.MaxVolume = self._maxShovelVolume;
                sd.DigDepth = self.DigDepth;

                sd.BladeSizeX = DiggingController_bladeSizex(self);
                sd.BladeSizeZ = DiggingController_bladeSizez(self);

                originalContainerData.Add(name, sd);

                Plugin.Log.LogInfo(name + " Dirt MaxVolume: " + sd.MaxVolume);
                Plugin.Log.LogInfo(name + " Dirt DigDepth: " + sd.DigDepth);
                Plugin.Log.LogInfo(name + " Dirt BladeSizeX: " + sd.BladeSizeX);
                Plugin.Log.LogInfo(name + " Dirt BladeSizeZ: " + sd.BladeSizeZ);

                Plugin.Log.LogInfo("{ \"" + name + "\", new DirtContainer(" + sd.MaxVolume + ", " + sd.BladeSizeX + ", " + sd.BladeSizeZ + ", " + sd.DigDepth + ") },");
            }


            DirtContainer data = null;

            if (containerData.ContainsKey(name))
            {
                data = containerData[name];
            }

            if (data != null)
            {
                data.instance = self;

                if (originalContainerData.ContainsKey(name) && originalContainerData[name] != null)
                {
                    originalContainerData[name].instance = self;
                }

                ApplyData(data);
            }

            return true; // false to skip original
        }


    }





}
