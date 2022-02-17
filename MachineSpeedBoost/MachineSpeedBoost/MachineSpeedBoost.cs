using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using GoldDigger;
using System.Collections.Generic;

namespace MachineSpeedBoost
{
    public class MachineSpeedBoost
    {
        static Plugin plugin;
        private static Harmony patcher = null;

        private static ConfigEntry<bool> enabled { get; set; }


        static AccessTools.FieldRef<MachineController, WorkerBuffBase> myBuffRef = AccessTools.FieldRefAccess<MachineController, WorkerBuffBase>("_myBuff");
        static AccessTools.FieldRef<MachineController, float> speedBuffBonusRef = AccessTools.FieldRefAccess<MachineController, float>("_speedBuffBonus");



        class SpeedData
        {
            public SpeedData()
            {
            }

            public SpeedData(float mass, float scaleFactor, float rpmIdle, float rpmMax, float rpmPeak)
            {
                _mass = mass;
                _scaleFactor = scaleFactor;
                _rpmIdle = rpmIdle;
                _rpmMax = rpmMax;
                _rpmPeak = rpmPeak;
            }
            public MachineController instance = null;
            private float _mass { get; set; } = -1;
            private float _scaleFactor { get; set; } = -1;
            private float _rpmIdle { get; set; } = -1;
            private float _rpmMax { get; set; } = -1;
            private float _rpmPeak { get; set; } = -1;

            public float Mass
            {
                get
                {
                    if (ConfigMass != null)
                    {
                        return ConfigMass.Value;
                    }
                    return _mass;
                }
                set
                {
                    if (ConfigMass != null && ConfigMass.Value != value)
                    {
                        ConfigMass.Value = value;
                    }
                    else
                    {
                        _mass = value;
                    }
                }
            }
            public float ScaleFactor
            {
                get
                {
                    if (ConfigScaleFactor != null)
                    {
                        return ConfigScaleFactor.Value;
                    }
                    return _scaleFactor;
                }
                set
                {
                    if (ConfigScaleFactor != null && ConfigScaleFactor.Value != value)
                    {
                        ConfigScaleFactor.Value = value;
                    }
                    else
                    {
                        _scaleFactor = value;
                    }
                }
            }
            public float RpmIdle 
            {
                get
                {
                    if (ConfigRpmIdle != null)
                    {
                        return ConfigRpmIdle.Value;
                    }
                    return _rpmIdle;
                }
                set
                {
                    if (ConfigRpmIdle != null && ConfigRpmIdle.Value != value)
                    {
                        ConfigRpmIdle.Value = value;
                    }
                    else
                    {
                        _rpmIdle = value;
                    }
                }
            }
            public float RpmMax
            {
                get
                {
                    if (ConfigRpmMax != null)
                    {
                        return ConfigRpmMax.Value;
                    }
                    return _rpmMax;
                }
                set
                {
                    if (ConfigRpmMax != null && ConfigRpmMax.Value != value)
                    {
                        ConfigRpmMax.Value = value;
                    }
                    else
                    {
                        _rpmMax = value;
                    }
                }
            }
            public float RpmPeak
            {
                get
                {
                    if (ConfigRpmPeak != null)
                    {
                        return ConfigRpmPeak.Value;
                    }
                    return _rpmPeak;
                }
                set
                {
                    if (ConfigRpmPeak != null && ConfigRpmPeak.Value != value)
                    {
                        ConfigRpmPeak.Value = value;
                    }
                    else
                    {
                        _rpmPeak = value;
                    }
                }
            }

            public ConfigEntry<float> ConfigMass { get; set; } = null;
            public ConfigEntry<float> ConfigScaleFactor { get; set; } = null;
            public ConfigEntry<float> ConfigRpmIdle { get; set; } = null;
            public ConfigEntry<float> ConfigRpmMax { get; set; } = null;
            public ConfigEntry<float> ConfigRpmPeak { get; set; } = null;
        }

        static Dictionary<string, SpeedData> originalSpeedData = new Dictionary<string, SpeedData>();

        static Dictionary<string, SpeedData> speedData = new Dictionary<string, SpeedData>() // init with defaults from vanilla game
        {
            { "Chevrolet_CK_Pickup", new SpeedData(2300, 1, 850, 5000, 4000) },
            { "Moxy6x6", new SpeedData(15000, 1, 500, 2300, 1200) },
            { "Hitachi_EX400LC", new SpeedData(42000, 1, 800, 2200, 1600) },
            { "Hitachi_EX270LC", new SpeedData(26703, 1, 800, 2788, 1600) },
            { "Caterpillar_980B", new SpeedData(20000, 1, 500, 2000, 1200) },
            { "Caterpillar_D6H", new SpeedData(42000, 1, 800, 2400, 1800) },
            { "Hitachi_EX400LC_Frankenstein", new SpeedData(40000, 1, 800, 2200, 1600) },
            { "Caterpillar_D6H_Drill", new SpeedData(42000, 1, 800, 2400, 1800) }
        };

        public MachineSpeedBoost(Plugin pPlugin)
        {
            plugin = pPlugin;

            enabled = plugin.Config.Bind<bool>("Machine.Speed", "Enabled", true, new ConfigDescription("Enable this mod", new AcceptableValueList<bool>(false, true)));

            foreach (var item in speedData)
            {
                item.Value.ConfigMass = plugin.Config.Bind<float>("Machine.Speed", item.Key + ".Mass", item.Value.Mass, new ConfigDescription("Weight of Vehicle", new AcceptableValueRange<float>(item.Value.Mass/5, 100000)));
                item.Value.ConfigScaleFactor = plugin.Config.Bind<float>("Machine.Speed", item.Key + ".ScaleFactor", item.Value.ScaleFactor, new ConfigDescription("Higher will give wheels more lift", new AcceptableValueRange<float>(0, 10)));
                item.Value.ConfigRpmIdle = plugin.Config.Bind<float>("Machine.Speed", item.Key + ".RpmIdle", item.Value.RpmIdle, new ConfigDescription("RPM when idle", new AcceptableValueRange<float>(0, 100000)));
                item.Value.ConfigRpmMax = plugin.Config.Bind<float>("Machine.Speed", item.Key + ".RpmMax", item.Value.RpmMax, new ConfigDescription("Maximum RPM", new AcceptableValueRange<float>(0, 100000)));
                item.Value.ConfigRpmPeak = plugin.Config.Bind<float>("Machine.Speed", item.Key + ".RpmPeak", item.Value.RpmPeak, new ConfigDescription("Peak RPM", new AcceptableValueRange<float>(0, 100000)));
            }



            if (enabled.Value && patcher == null)
            {
                Plugin.Log.LogInfo("Patching MachineSpeedBoost");
                patcher = Harmony.CreateAndPatchAll(typeof(MachineSpeedBoost));
            }

            //Plugin.Log.LogMessage("DAYILY_STATS_CREDIT_PART: " + new LocalizationKey("DAYILY_STATS_CREDIT_PART").GetLocalized());

            enabled.SettingChanged += Enabled_SettingChanged;
        }

        private void Enabled_SettingChanged(object sender, System.EventArgs e)
        {
            if (enabled.Value && patcher == null)
            {
                patcher = Harmony.CreateAndPatchAll(typeof(MachineSpeedBoost));
            }
            else
            {
                patcher.UnpatchAll();
                patcher = null;

                foreach (var data in originalSpeedData)
                {
                    ApplyData(data.Value);
                }
            }
        }



        static void ApplyData(SpeedData data)
        {

            if (data != null)
            {
                MachineController self = data.instance;

                data.RpmIdle = Mathf.Max(10f, data.RpmIdle);
                data.RpmPeak = Mathf.Max(data.RpmIdle + 10f, data.RpmPeak);
                data.RpmMax = Mathf.Max(data.RpmPeak + 10f, data.RpmMax);

                self._defaultidleRpm = data.RpmIdle;
                self._defaultpeakRpm = data.RpmPeak;
                self._defaultmaxRpm = data.RpmMax;

                WorkerBuffBase worker = myBuffRef(self);

                speedBuffBonusRef(self) = 1f;

                if (worker != null && worker._IsWorkerAttached && worker._Worker.IsWorkinig)
                {
                    if (worker._IsWorkerAttached && worker._Worker.IsWorkinig)
                    {
                        speedBuffBonusRef(self) = 1f + global::Singleton<WorkersManager>.Instance.MovementSpeed / 100f * worker._Worker.Preformance;
                    }
                }

                self.MyVehicleController.engine.maxRpm = self._defaultmaxRpm * speedBuffBonusRef(self);
                self.MyVehicleController.engine.idleRpm = self._defaultidleRpm * speedBuffBonusRef(self);
                self.MyVehicleController.engine.peakRpm = self._defaultpeakRpm * speedBuffBonusRef(self);

                self.MyVehicleController.scaleFactor = data.ScaleFactor;

                Rigidbody rb = self.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    rb.mass = data.Mass;
                }

            }
        }


        [HarmonyPatch(typeof(MachineController), "Update")]
        [HarmonyPostfix]
        static void Post_MachineControllerUpdate(MachineController __instance)
        {
            MachineController self = __instance;

            string name = self.name.Replace("(Clone)", "");
            SpeedData data = null;

            Rigidbody[] rbs = self.GetComponentsInChildren<Rigidbody>();
            
            if (!originalSpeedData.ContainsKey(name))
            {
                SpeedData sd = new SpeedData();

                sd.instance = self;

                sd.RpmIdle = self._defaultidleRpm;
                sd.RpmPeak = self._defaultpeakRpm;
                sd.RpmMax = self._defaultmaxRpm;

                sd.ScaleFactor = self.MyVehicleController.scaleFactor;

                Rigidbody rb1 = self.GetComponent<Rigidbody>();

                if (rb1 != null)
                {
                    sd.Mass = rb1.mass;
                }



                originalSpeedData.Add(name, sd);
                Plugin.Log.LogInfo("name: " + name);

                foreach (Rigidbody rb in rbs)
                {
                    if (rb != null)
                    {
                        //rb.mass /= 2;
                    }
                }

                foreach (var wheel in self.MyVehicleController.wheels)
                {
                    wheel.grip *= 10;
                }

                /*
                self._defaultidleRpm *= 2;
                self._defaultmaxRpm *= 2;
                self._defaultpeakRpm *= 2;

                self.MyVehicleController.engine.maxRpm *= 2;
                self.MyVehicleController.engine.idleRpm *= 2;
                self.MyVehicleController.engine.peakRpm *= 2;
                */

                //self.MyVehicleController.scaleFactor *= 5;
                //self.MyVehicleController.scaleFactor = 1;

                //self.MyVehicleController.scaleFactor = self.MyVehicleController.scaleFactor / self.MyVehicleController.wheels.Count() * 20f;
                //self.MyVehicleController.scaleFactor = 2.5f;


                self.MyVehicleController.contactAngleAffectsTireForce = true;

                self.MyVehicleController.speedControl.speedLimit *= 2;
                self.MyVehicleController.speedControl.minSpeed *= 2;
                //self.MyVehicleController.speedControl.throttleSlope *= 2;

                self.MyVehicleController.engine.canStall = false;
                self.MyVehicleController.engine.frictionTorque *= 10;
                self.MyVehicleController.engine.idleRpmTorque *= 10;
                self.MyVehicleController.engine.peakRpmTorque *= 10;
                //self.MyVehicleController.engine.stalledFrictionTorque *= 10;
                //self.MyVehicleController.engine.viscousFriction *= 2;
                //self.MyVehicleController.engine.rotationalFriction *= 2;
                self.MyVehicleController.engine.inertia /= 2;
                self.MyVehicleController.engine.maxIdleThrottle *= 2;
                self.MyVehicleController.engine.rpmLimiter = false;
                self.MyVehicleController.engine.rpmLimiterCutoffTime *= 2;
                self.MyVehicleController.engine.rpmLimiterMax *= 2;
            }


            /** /
            if (rb != null)
            {
                if (originalSpeedData[name].Mass != rb.mass)
                {
                    originalSpeedData[name].Mass = rb.mass;
                    Plugin.Log.LogInfo(name + " Mass: " + originalSpeedData[name].Mass);
                }
            }
            if (originalSpeedData[name].RpmIdle != self._defaultidleRpm)
            {
                originalSpeedData[name].RpmIdle = self._defaultidleRpm;
                Plugin.Log.LogInfo(name + " RpmIdle: " + originalSpeedData[name].RpmIdle);
            }
            if (originalSpeedData[name].RpmMax != self._defaultmaxRpm)
            {
                originalSpeedData[name].RpmMax = self._defaultmaxRpm;
                Plugin.Log.LogInfo(name + " RpmMax: " + originalSpeedData[name].RpmMax);
            }
            if (originalSpeedData[name].RpmPeak != self._defaultpeakRpm)
            {
                originalSpeedData[name].RpmPeak = self._defaultpeakRpm;
                Plugin.Log.LogInfo(name + " RpmPeak: " + originalSpeedData[name].RpmPeak);
            }

            /**/

            if (speedData.ContainsKey(name))
            {
                data = speedData[name];
            }

            
            if (data != null)
            {
                data.instance = self;

                if (originalSpeedData.ContainsKey(name))
                {
                    originalSpeedData[name].instance = self;
                }

                if (typeof(TrackMachineController).IsAssignableFrom(self.GetType()))
                {
                    ((TrackMachineController)self).MaxSpeed = 9999;
                }

                ApplyData(data);

            }


            /**/
        }




    }





}
