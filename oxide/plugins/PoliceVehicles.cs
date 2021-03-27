using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("PoliceVehicles", "HexOptimal", "1.2.1")]
    [Description("Allow the spawning of police vehicles")]
    internal class PoliceVehicles : RustPlugin
    {
        #region Config
        [PluginReference]
        private Plugin SpawnModularCar;
        public string rhibprefab = "assets/content/vehicles/boats/rhib/rhib.prefab";
        public string bluelightprefab = "assets/prefabs/deployable/playerioents/lights/flasherlight/electric.flasherlight.deployed.prefab";
        public string buttonPrefab = "assets/prefabs/deployable/playerioents/button/button.prefab";
        public string strobelight = "assets/content/props/strobe light/strobelight.prefab";
        public string phoneprefab = "assets/prefabs/voiceaudio/telephone/telephone.deployed.prefab";
        public string minicopterPrefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        public string SpherePrefab = "assets/prefabs/visualization/sphere.prefab";
        public string SearchLightPrefab = "assets/prefabs/deployable/search light/searchlight.deployed.prefab";
        public string scrapheliPrefab = "assets/content/vehicles/scrap heli carrier/scraptransporthelicopter.prefab";
        public string searchlightprefab = "assets/prefabs/deployable/search light/searchlight.deployed.prefab";
        private static readonly int GlobalLayerMask = LayerMask.GetMask("Construction", "Default", "Deployed", "Resource", "Terrain", "Water", "World");

        private ConfigData configData;
        class ConfigData
        {
            [JsonProperty(PropertyName = "Police car fuel amount on spawn")]
            public int policeCarFuel = 500;
            [JsonProperty(PropertyName = "Police transport vehicle fuel amount on spawn")]
            public int policeTransportFuel = 500;
            [JsonProperty(PropertyName = "Police heli fuel amount on spawn")]
            public int policeHeliFuel = 500;
            [JsonProperty(PropertyName = "Police heli large fuel amount on spawn")]
            public int policeHeliLargeFuel = 500;
            [JsonProperty(PropertyName = "Police boat fuel amount on spawn")]
            public int policeBoatFuel = 500;
            [JsonProperty(PropertyName = "Lock police car engine parts")]
            public bool lockCar = true;
            [JsonProperty(PropertyName = "Lock police transport vehicle engine parts")]
            public bool lockTransport = true;
            [JsonProperty(PropertyName = "Police car engine parts tier")]
            public int carTier = 3;
            [JsonProperty(PropertyName = "Police Transport engine parts tier")]
            public int transportTier = 3;
        }

        private bool LoadConfigVariables()
        {
            try
            {
                configData = Config.ReadObject<ConfigData>();
            }
            catch
            {
                return false;
            }
            SaveConfig(configData);
            return true;
        }

        protected override void LoadDefaultConfig()
        {
            Puts("Creating new config file.");
            configData = new ConfigData();
            SaveConfig(configData);
        }

        void SaveConfig(ConfigData config)
        {
            Config.WriteObject(config, true);
        }


        #endregion
        #region Data

        StoredData storedData;
        class StoredData
        {
            public Dictionary<ulong, uint> currentVehicle = new Dictionary<ulong, uint>();
            public Dictionary<ulong, List<uint>> currentVehicleUnlimited = new Dictionary<ulong, List<uint>>();
            public HashSet<uint> LightsActivated = new HashSet<uint>();
            public Dictionary<uint, uint> heliSearchLights = new Dictionary<uint, uint>();
            public Dictionary<uint, uint> rhibSearchLights = new Dictionary<uint, uint>();
        }

        void Loaded()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("PoliceVehicles");
            Interface.Oxide.DataFileSystem.WriteObject("PoliceVehicles", storedData);
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("PoliceVehicles", storedData);
        }

        void Unload()
        {
            SaveData();
        }

        #endregion
        #region Commands
        [ChatCommand("policecar")]
        void policecar(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            var policecar = SpawnPoliceCar(player);
            if (policecar == null) return;
            spawnentity(policecar, buttonPrefab, new Vector3(0f, 0.05f, 0.15f), new Quaternion(0f, 90f, 0f, 0f));
            spawnentity(policecar, bluelightprefab, new Vector3(-0.6f, 2.05f, -0.3f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(0.6f, 2.05f, -0.3f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(-0.6f, 2.05f, -2.2f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(0.6f, 2.05f, -2.2f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(-0.4f, 0.55f, 2.3f), new Quaternion(0f, 90f, 90f, 0f));
            spawnentity(policecar, bluelightprefab, new Vector3(0.4f, 0.55f, 2.3f), new Quaternion(0f, 90f, 90f, 0f));
            spawnentity(policecar, bluelightprefab, new Vector3(0.8f, 0.52f, -2.4f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
            spawnentity(policecar, bluelightprefab, new Vector3(-0.8f, 0.52f, -2.4f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
            spawnentity(policecar, strobelight, new Vector3(-0.4f, 0.55f, 2.3f), new Quaternion());
            spawnentity(policecar, strobelight, new Vector3(0.4f, 0.55f, 2.3f), new Quaternion());

            if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                storedData.currentVehicle.Add(player.userID, policecar.net.ID);
            }
            else
            {
                if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                {
                    storedData.currentVehicleUnlimited[player.userID].Add(policecar.net.ID);
                }
                else
                {
                    List<uint> vehicles = new List<uint>();
                    vehicles.Add(policecar.net.ID);
                    storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                }
                
            }

                
        }
        [ChatCommand("policetransport")]
        void policetransport(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            var policecar = SpawnPoliceTransport(player);
            if (policecar == null) return;
            spawnentity(policecar, buttonPrefab, new Vector3(0f, 0.05f, 0.9f), new Quaternion(0f, 90f, 0f, 0f));
            spawnentity(policecar, bluelightprefab, new Vector3(-0.6f, 2.05f, 0.5f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(0.6f, 2.05f, 0.5f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(-0.6f, 2.05f, -3f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(0.6f, 2.05f, -3f), new Quaternion());
            spawnentity(policecar, bluelightprefab, new Vector3(-0.4f, 0.55f, 3f), new Quaternion(0f, 90f, 90f, 0f));
            spawnentity(policecar, bluelightprefab, new Vector3(0.4f, 0.55f, 3f), new Quaternion(0f, 90f, 90f, 0f));
            spawnentity(policecar, bluelightprefab, new Vector3(0.8f, 0.52f, -3.15f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
            spawnentity(policecar, bluelightprefab, new Vector3(-0.8f, 0.52f, -3.15f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
            spawnentity(policecar, strobelight, new Vector3(-0.4f, 0.55f, 2.3f), new Quaternion());
            spawnentity(policecar, strobelight, new Vector3(0.4f, 0.55f, 2.3f), new Quaternion());
            if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                storedData.currentVehicle.Add(player.userID, policecar.net.ID);
            }
            else
            {
                if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                {
                    storedData.currentVehicleUnlimited[player.userID].Add(policecar.net.ID);
                }
                else
                {
                    List<uint> vehicles = new List<uint>();
                    vehicles.Add(policecar.net.ID);
                    storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                }

            }
        }

        [ChatCommand("policeheli")]
        void policeheli(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity, GlobalLayerMask))
            {
                Vector3 position = hit.point + Vector3.up * 2f;
                BaseVehicle policeheli = (BaseVehicle)GameManager.server.CreateEntity(minicopterPrefab, position);
                if (policeheli == null) return;
                policeheli.OwnerID = player.userID;
                policeheli.Spawn();
                policeheli.GetFuelSystem().AddStartingFuel(configData.policeHeliFuel);
                spawnentity(policeheli, bluelightprefab, new Vector3(0, 1.2f, -2.0f), new Quaternion());
                spawnentity(policeheli, bluelightprefab, new Vector3(0, 2.2f, -0.02f), new Quaternion());
                spawnentity(policeheli, bluelightprefab, new Vector3(0f, 0.83f, -2.6f), new Quaternion(-0.5f, -0.5f, 0.5f, 0.5f));
                spawnentity(policeheli, bluelightprefab, new Vector3(0f, 0.3f, 2.1f), new Quaternion(0f, 90f, 90f, 0f));
                spawnentity(policeheli, bluelightprefab, new Vector3(0f, 0.25f, -0.75f), new Quaternion(90f, 0f, 0f, 0f));
                spawnentity(policeheli, bluelightprefab, new Vector3(0f, 0.25f, 1.1f), new Quaternion(90f, 0f, 0f, 0f));
                spawnentity(policeheli, buttonPrefab, new Vector3(0, -0.5f, 1f), new Quaternion(0f, -90f, 0f, 0f));
                SphereEntity helilightsphere = (SphereEntity)GameManager.server.CreateEntity(SpherePrefab, policeheli.transform.position, new Quaternion(0, 0, 0, 0), true);
                if (helilightsphere == null) return;
                RemoveColliderProtection(helilightsphere);
                helilightsphere.Spawn();
                helilightsphere.SetParent(policeheli);
                helilightsphere.transform.localPosition = new Vector3(0, -100, 0);
                SearchLight searchLight = GameManager.server.CreateEntity(SearchLightPrefab, helilightsphere.transform.position) as SearchLight;
                if (searchLight == null) return;
                RemoveColliderProtection(searchLight);
                searchLight.Spawn();
                searchLight.SetFlag(BaseEntity.Flags.On, true);
                searchLight.SetParent(helilightsphere);
                searchLight.transform.localPosition = new Vector3(0, 0, 0);
                searchLight.transform.localRotation = Quaternion.Euler(new Vector3(-20, 180, 0));
                helilightsphere.transform.localScale += new Vector3(0.9f, 0, 0);
                helilightsphere.LerpRadiusTo(0.1f, 10f);
                helilightsphere.transform.localPosition = new Vector3(0, 0.24f, 2.35f);
                storedData.heliSearchLights.Add(policeheli.net.ID, searchLight.net.ID);
                helilightsphere.SendNetworkUpdateImmediate();

                if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
                {
                    storedData.currentVehicle.Add(player.userID, policeheli.net.ID);
                }
                else
                {
                    if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                    {
                        storedData.currentVehicleUnlimited[player.userID].Add(policeheli.net.ID);
                    }
                    else
                    {
                        List<uint> vehicles = new List<uint>();
                        vehicles.Add(policeheli.net.ID);
                        storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                    }

                }
            }
        }

        [ChatCommand("policehelilarge")]
        void policehelilarge(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity, GlobalLayerMask))
            {
                Vector3 position = hit.point + Vector3.up * 2f;
                BaseVehicle policehelilarge = (BaseVehicle)GameManager.server.CreateEntity(scrapheliPrefab, position);
                if (policehelilarge == null) return;
                policehelilarge.OwnerID = player.userID;
                policehelilarge.Spawn();
                policehelilarge.GetFuelSystem().AddStartingFuel(configData.policeHeliLargeFuel);
                spawnentity(policehelilarge, bluelightprefab, new Vector3(0f, 3.24f, -7.65f), new Quaternion());
                spawnentity(policehelilarge, bluelightprefab, new Vector3(-0.2f, 3.24f, -7.65f), new Quaternion());
                spawnentity(policehelilarge, bluelightprefab, new Vector3(0.5f, 2.85f, 2.4f), new Quaternion());
                spawnentity(policehelilarge, bluelightprefab, new Vector3(-0.5f, 2.85f, 2.4f), new Quaternion());
                spawnentity(policehelilarge, bluelightprefab, new Vector3(-1.2f, 0.8f, -3.06f), new Quaternion());
                spawnentity(policehelilarge, bluelightprefab, new Vector3(1.18f, 0.8f, -3.06f), new Quaternion());
                spawnentity(policehelilarge, bluelightprefab, new Vector3(0f, 0.6f, 3.65f), new Quaternion(90f, 0f, 0f, 0f));
                spawnentity(policehelilarge, bluelightprefab, new Vector3(0f, 0.6f, -3f), new Quaternion(90f, 0f, 0f, 0f));
                spawnentity(policehelilarge, bluelightprefab, new Vector3(0f, 1f, 4.47f), new Quaternion(0f, 90f, 90f, 0f));
                spawnentity(policehelilarge, buttonPrefab, new Vector3(0.005f, -0.2f, 2.8f), new Quaternion(0f, 90f, 0f, 0f));
                spawnentity(policehelilarge, phoneprefab, new Vector3(0f, 0.8f, 2f), new Quaternion(0f, 90f, 0f, 0f));
                foreach (var children in policehelilarge.children)
                {
                    if (children.name == phoneprefab)
                    {
                        children.SetFlag(BaseEntity.Flags.Reserved8, true);
                    }
                }
                if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
                {
                    storedData.currentVehicle.Add(player.userID, policehelilarge.net.ID);
                }
                else
                {
                    if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                    {
                        storedData.currentVehicleUnlimited[player.userID].Add(policehelilarge.net.ID);
                    }
                    else
                    {
                        List<uint> vehicles = new List<uint>();
                        vehicles.Add(policehelilarge.net.ID);
                        storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                    }

                }
            }
        }

        [ChatCommand("policeboat")]
        void policeboat(BasePlayer player)
        {
            if (!permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not have permission to use that command");
                return;
            }
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                SendReply(player, "You can only use one vehicle at a time, use /removevehicle to remove your current vehicle");
                return;
            }
            RaycastHit hit;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, Mathf.Infinity, GlobalLayerMask))
            {
                Vector3 position = hit.point + Vector3.up * 2f;
                var policeboat = GameManager.server?.CreateEntity(rhibprefab, position) as RHIB;
                if (policeboat == null) return;
                policeboat.Spawn();
                policeboat.AddFuel(configData.policeBoatFuel);
                spawnentity(policeboat, bluelightprefab, new Vector3(-0.1f, 3.345f, 0.66f), new Quaternion());
                spawnentity(policeboat, bluelightprefab, new Vector3(0f, 1.40f, -3.745f), new Quaternion());
                spawnentity(policeboat, buttonPrefab, new Vector3(0.5f, 0.3f, 0.35f), new Quaternion(0f, 90f, 0f, 0f));
                spawnentity(policeboat, bluelightprefab, new Vector3(0f, 1.77f, 4.15f), new Quaternion(0f, 90f, 90f, 0f));
                spawnentity(policeboat, bluelightprefab, new Vector3(0f, 1.80f, 4.15f), new Quaternion());
                spawnentity(policeboat, bluelightprefab, new Vector3(-1.2f, 1.31f, 0.66f), new Quaternion());
                spawnentity(policeboat, bluelightprefab, new Vector3(1.2f, 1.31f, 0.66f), new Quaternion());
                spawnentity(policeboat, bluelightprefab, new Vector3(-1.2f, 1.31f, -3.745f), new Quaternion());
                spawnentity(policeboat, bluelightprefab, new Vector3(1.2f, 1.31f, -3.745f), new Quaternion());
                SphereEntity boatlightsphere = (SphereEntity)GameManager.server.CreateEntity(SpherePrefab, policeboat.transform.position, new Quaternion(0, 0, 0, 0), true);
                if (boatlightsphere == null) return;
                RemoveColliderProtection(boatlightsphere);
                boatlightsphere.Spawn();
                boatlightsphere.SetParent(policeboat);
                boatlightsphere.transform.localPosition = new Vector3(0, 1.76f, 4.2f);
                BaseEntity searchlight = GameManager.server.CreateEntity(SearchLightPrefab, boatlightsphere.transform.position) as BaseEntity;
                if (searchlight == null) return;
                RemoveColliderProtection(searchlight);
                searchlight.Spawn();
                searchlight.SetFlag(BaseEntity.Flags.On, true);
                searchlight.SetParent(boatlightsphere);
                searchlight.transform.localPosition = new Vector3(0, 0, 0);
                boatlightsphere.LerpRadiusTo(0.1f, 10f);
                boatlightsphere.transform.rotation = new Quaternion(0f, 90f, 0f, 0f);
                storedData.rhibSearchLights.Add(policeboat.net.ID, searchlight.net.ID);
                boatlightsphere.SendNetworkUpdateImmediate();
                if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.use") && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
                {
                    storedData.currentVehicle.Add(player.userID, policeboat.net.ID);
                }
                else
                {
                    if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
                    {
                        storedData.currentVehicleUnlimited[player.userID].Add(policeboat.net.ID);
                    }
                    else
                    {
                        List<uint> vehicles = new List<uint>();
                        vehicles.Add(policeboat.net.ID);
                        storedData.currentVehicleUnlimited.Add(player.userID, vehicles);
                    }

                }
            }
        }

        [ChatCommand("removevehicle")]
        void removevehicle(BasePlayer player)
        {
            if (!storedData.currentVehicle.ContainsKey(player.userID) && !permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                SendReply(player, "You do not currently have a vehicle");
                return;
            }
            if (permission.UserHasPermission(player.UserIDString.ToString(), "policevehicles.unlimited"))
            {
                RaycastHit hit;
                BaseVehicle car = null;
                if (Physics.Raycast(player.eyes.HeadRay(), out hit, 3))
                {
                    car = hit.GetEntity() as BaseVehicle;
                }

                if (car != null)
                {
                    KillVehicleCheck(player, car, car.net.ID);
                }
                else
                {
                    SendReply(player, "You are not looking at a police vehicle");
                }
            }
            else
            {
                var foundentities = BaseVehicle.FindObjectsOfType<BaseVehicle>();
                var netID = storedData.currentVehicle[player.userID];
                foreach (var entity in foundentities)
                {
                    if (entity.net.ID == netID)
                    {
                        KillVehicleCheck(player, entity, netID);  
                        return;
                    }
                    else
                    {
                        SendReply(player, "This is not your vehicle");
                    }
                }
            }
        }

        #endregion
        #region Calls
        void Init()
        {
            permission.RegisterPermission("PoliceVehicles.use", this);
            permission.RegisterPermission("PoliceVehicles.unlimited", this);
            if (!LoadConfigVariables())
            {
                Puts("Config file issue detected. Please delete file, or check syntax and fix.");
                return;
            }
        }
        private void OnServerInitialized()
        {
            if (SpawnModularCar == null)
            {
                Puts("SpawnModularCar is not loaded, get it at https://umod.org");
            }
        }
        void OnButtonPress(PressButton entity, BasePlayer player)
        {
            if (entity == null || player == null)
            {
                return;
            }
            var result = player.GetMounted();
            if (result == null)
            {
                return;
            }
            if (result.ShortPrefabName == "modularcardriverseat" || result.ShortPrefabName == "miniheliseat" || result.ShortPrefabName == "minihelipassenger" || result.ShortPrefabName == "transporthelipilot" || result.ShortPrefabName == "transporthelicopilot" || result.ShortPrefabName == "standingdriver" || result.ShortPrefabName == "smallboatpassenger")
            {
                var mountedveh = player.GetMountedVehicle();
                if (storedData.LightsActivated.Contains(mountedveh.net.ID))
                {
                    lightsOnOff(player, mountedveh, false);
                    storedData.LightsActivated.Remove(mountedveh.net.ID);
                    return;
                }
                else
                {
                    lightsOnOff(player, mountedveh, true);
                    storedData.LightsActivated.Add(mountedveh.net.ID);
                    return;
                }
            }
        }
        void OnEntityDeath(BaseCombatEntity vehicle, HitInfo info)
        {
            bool isInCurrentVehicle = false;
            foreach (var entity in storedData.currentVehicle.Values)
            {
                if (vehicle.net.ID == entity)
                {
                    var myKey = storedData.currentVehicle.FirstOrDefault(x => x.Value == entity).Key;
                    storedData.LightsActivated.Remove(entity);
                    storedData.heliSearchLights.Remove(entity);
                    storedData.rhibSearchLights.Remove(entity);
                    storedData.currentVehicle.Remove(myKey);
                    isInCurrentVehicle = true;
                    return;
                }
            }
            if (!isInCurrentVehicle)
            {
                foreach (var player in storedData.currentVehicleUnlimited.Keys)
                {
                    storedData.currentVehicleUnlimited[player].Remove(vehicle.net.ID);
                }
            }
        }
        void OnEntityKill(BaseNetworkable vehicle)
        {
            bool isInCurrentVehicle = false;
            foreach (var entity in storedData.currentVehicle.Values)
            {
                if (vehicle.net.ID == entity)
                {
                    var myKey = storedData.currentVehicle.FirstOrDefault(x => x.Value == entity).Key;
                    storedData.LightsActivated.Remove(entity);
                    storedData.heliSearchLights.Remove(entity);
                    storedData.rhibSearchLights.Remove(entity);
                    storedData.currentVehicle.Remove(myKey);
                    isInCurrentVehicle = true;
                    return;
                }
            }
            if (!isInCurrentVehicle)
            {
                foreach (var player in storedData.currentVehicleUnlimited.Keys)
                {
                    storedData.currentVehicleUnlimited[player].Remove(vehicle.net.ID);
                }
            }
        }
        #endregion
        #region Core
        void spawnentity(BaseVehicle vehicle, string spawnentity, Vector3 position, Quaternion rotation)
        {
            BaseEntity entity = GameManager.server.CreateEntity(spawnentity, vehicle.transform.position);
            if (entity == null) return;
            entity.transform.localPosition = position;
            UnityEngine.Object.DestroyImmediate(entity.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(entity.GetComponent<GroundWatch>());
            entity.SetParent(vehicle);
            entity.transform.localPosition = position;
            entity.transform.localRotation = rotation;
            entity.Spawn();
            vehicle.AddChild(entity);
            entity.SendNetworkUpdateImmediate();
            vehicle.SendNetworkUpdateImmediate();
        }

        void KillVehicleCheck(BasePlayer player, BaseVehicle entity, uint netID)
        {
            storedData.LightsActivated.Remove(netID);
            storedData.heliSearchLights.Remove(netID);
            storedData.rhibSearchLights.Remove(netID);
            if (storedData.currentVehicle.ContainsKey(player.userID))
            {
                storedData.currentVehicle.Remove(player.userID);
                KillVehicle(entity);
            }
            else if (storedData.currentVehicleUnlimited.ContainsKey(player.userID))
            {
                bool unlimitedvehicleremove = storedData.currentVehicleUnlimited[player.userID].Remove(netID);
                if (!unlimitedvehicleremove)
                {
                    SendReply(player, "This is not your vehicle");
                    return;
                }
                KillVehicle(entity);
            }
            else
            {
                SendReply(player, "This is not your vehicle");
                return;
            }
            SendReply(player, "Vehicle removed");
        }
        void KillVehicle(BaseVehicle entity)
        {
            Vector3 position = entity.transform.position;
            position.y = position.y - 50;
            entity.transform.position = position;
            entity.Kill(BaseVehicle.DestroyMode.None);
        }

        void lightsOnOff(BasePlayer player, BaseVehicle policecar, bool onOff)
        {
            int numlightson = 0;
            foreach (var children in policecar.children)
            {
                if (children.name == strobelight)
                {
                    children.SetFlag(BaseEntity.Flags.On, onOff);

                }
                else if (children.name == bluelightprefab)
                {
                    numlightson++;
                    if (numlightson >=5)
                    {
                        children.SetFlag(BaseEntity.Flags.Reserved8, onOff);
                    }
                    else
                    {
                        timer.Once(0.8f, () =>
                        {
                            children.SetFlag(BaseEntity.Flags.Reserved8, onOff);
                        });
                    }
                    
                }
            }
            if (storedData.heliSearchLights.ContainsKey(policecar.net.ID))
            {
                var searchlight = storedData.heliSearchLights[policecar.net.ID];
                var foundentities = BaseNetworkable.serverEntities.OfType<SearchLight>();
                foreach (var entity in foundentities)
                {
                    if (entity.net.ID == searchlight)
                    {
                        if (onOff)
                        {
                            entity.UpdateHasPower(10, 1);
                            entity.SendNetworkUpdateImmediate();
                        }
                        else
                        {
                            entity.UpdateHasPower(0, 1);
                            entity.SendNetworkUpdateImmediate();
                        }
                    }
                }
            }
            if (storedData.rhibSearchLights.ContainsKey(policecar.net.ID))
            {
                var searchlight = storedData.rhibSearchLights[policecar.net.ID];
                var foundentities = BaseNetworkable.serverEntities.OfType<SearchLight>();
                foreach (var entity in foundentities)
                {
                    if (entity.net.ID == searchlight)
                    {
                        if (onOff)
                        {
                            entity.UpdateHasPower(10, 1);
                            entity.SendNetworkUpdateImmediate();
                        }
                        else
                        {
                            entity.UpdateHasPower(0, 1);
                            entity.SendNetworkUpdateImmediate();
                        }

                    }
                }
            }
        }
        void RemoveColliderProtection(BaseEntity collider)
        {
            foreach (var meshCollider in collider.GetComponentsInChildren<MeshCollider>())
            {
                UnityEngine.Object.DestroyImmediate(meshCollider);
            }
            UnityEngine.Object.DestroyImmediate(collider.GetComponent<GroundWatch>());
        }
        #endregion
        #region API
        ModularCar SpawnPoliceCar(BasePlayer player)
        {
            var car = SpawnModularCar.Call("API_SpawnPresetCar", player,
                new Dictionary<string, object>
                {
                    ["CodeLock"] = true,
                    ["KeyLock"] = false,
                    ["EnginePartsTier"] = configData.carTier,
                    ["FuelAmount"] = configData.policeCarFuel,
                    ["Modules"] = new object[] {
                        "vehicle.1mod.engine",
                        "vehicle.1mod.cockpit.with.engine",
                        "vehicle.1mod.rear.seats"
                    },
                }, new Action<ModularCar>(readyCar => OnCarReady(readyCar, "car"))
            ) as ModularCar;
            return car;
        }
        ModularCar SpawnPoliceTransport(BasePlayer player)
        {
            var car = SpawnModularCar.Call("API_SpawnPresetCar", player,
                new Dictionary<string, object>
                {
                    ["CodeLock"] = true,
                    ["KeyLock"] = false,
                    ["EnginePartsTier"] = configData.transportTier,
                    ["FuelAmount"] = configData.policeTransportFuel,
                    ["Modules"] = new object[] {
                        "vehicle.1mod.engine",
                        "vehicle.1mod.cockpit.with.engine",
                        "vehicle.1mod.passengers.armored",
                        "vehicle.1mod.passengers.armored"
                    },
                }, new Action<ModularCar>(readyCar => OnCarReady(readyCar, "transport"))
            ) as ModularCar;
            return car;
        }



        private void OnCarReady(ModularCar car, string type)
        {
            if (configData.lockCar == true && type == "car")
            {
                foreach (var module in car.AttachedModuleEntities)
                {
                    var engineContainer = (module as VehicleModuleEngine)?.GetContainer();
                    if (engineContainer != null)
                        engineContainer.inventory.SetLocked(true);
                }
            }
            else if (configData.lockTransport == true && type == "transport")
            {
                foreach (var module in car.AttachedModuleEntities)
                {
                    var engineContainer = (module as VehicleModuleEngine)?.GetContainer();
                    if (engineContainer != null)
                        engineContainer.inventory.SetLocked(true);
                }
            }
        }
        #endregion
    }
}
