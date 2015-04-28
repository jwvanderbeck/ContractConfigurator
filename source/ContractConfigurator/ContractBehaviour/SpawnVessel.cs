﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using ContractConfigurator;
using ContractConfigurator.ExpressionParser;

namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// Behaviour class for spawning a vessel.
    /// </summary>
    public class SpawnVessel : ContractBehaviour, IHasKerbalBehaviour
    {
        private class CrewData
        {
            public string name = null;
            public bool addToRoster = true;

            public CrewData() { }
            public CrewData(CrewData cd)
            {
                name = cd.name;
                addToRoster = cd.addToRoster;
            }
        }

        private class VesselData
        {
            public string name = null;
            public Guid? id = null;
            public string craftURL = null;
            public string flagURL = null;
            public VesselType vesselType = VesselType.Ship;
            public CelestialBody body = null;
            public Orbit orbit = null;
            public double latitude = 0.0;
            public double longitude = 0.0;
            public double? altitude = null;
            public bool landed = false;
            public bool owned = false;
            public List<CrewData> crew = new List<CrewData>();

            public VesselData() { }
            public VesselData(VesselData vd)
            {
                name = vd.name;
                id = vd.id;
                craftURL = vd.craftURL;
                flagURL = vd.flagURL;
                vesselType = vd.vesselType;
                body = vd.body;
                orbit = vd.orbit;
                latitude = vd.latitude;
                longitude = vd.longitude;
                altitude = vd.altitude;
                landed = vd.landed;
                owned = vd.owned;

                foreach (CrewData cd in vd.crew)
                {
                    crew.Add(new CrewData(cd));
                }
            }
        }
        private List<VesselData> vessels = new List<VesselData>();
        private bool vesselsCreated = false;

        public int KerbalCount
        {
            get
            {
                return vessels.Sum(vd => vd.crew.Count);
            }
        }

        public SpawnVessel() {}

        /// <summary>
        /// Copy Constructor.
        /// </summary>
        /// <param name="orig"></param>
        public SpawnVessel(SpawnVessel orig)
        {
            foreach (VesselData vd in orig.vessels)
            {
                vessels.Add(new VesselData(vd));
            }
        }

        public static SpawnVessel Create(ConfigNode configNode, CelestialBody defaultBody, SpawnVesselFactory factory)
        {
            SpawnVessel spawnVessel = new SpawnVessel();

            bool valid = true;
            int index = 0;
            foreach (ConfigNode child in ConfigNodeUtil.GetChildNodes(configNode, "VESSEL"))
            {
                DataNode dataNode = new DataNode("VESSEL_" + index++, factory.dataNode, factory);
                try
                {
                    ConfigNodeUtil.SetCurrentDataNode(dataNode);

                    VesselData vessel = new VesselData();

                    // Get name
                    if (child.HasValue("name"))
                    {
                        vessel.name = child.GetValue("name");
                    }

                    // Get paths
                    vessel.craftURL = ConfigNodeUtil.ParseValue<string>(child, "craftURL");
                    vessel.flagURL = ConfigNodeUtil.ParseValue<string>(child, "flagURL", (string)null);
                    vessel.vesselType = ConfigNodeUtil.ParseValue<VesselType>(child, "vesselType", VesselType.Ship);

                    // Get celestial body
                    valid &= ConfigNodeUtil.ParseValue<CelestialBody>(child, "targetBody", x => vessel.body = x, factory, defaultBody, Validation.NotNull);

                    // Get landed stuff
                    if (child.HasValue("lat") && child.HasValue("lon"))
                    {
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "lat", x => vessel.latitude = x, factory);
                        valid &= ConfigNodeUtil.ParseValue<double>(child, "lon", x => vessel.longitude = x, factory);
                        valid &= ConfigNodeUtil.ParseValue<double?>(child, "alt", x => vessel.altitude = x, factory, (double?)null);
                        vessel.landed = true;
                    }
                    // Get orbit
                    else
                    {
                        valid &= ConfigNodeUtil.ValidateMandatoryChild(child, "ORBIT", factory);
                        vessel.orbit = new OrbitSnapshot(ConfigNodeUtil.GetChildNode(child, "ORBIT")).Load();
                        vessel.orbit.referenceBody = vessel.body;
                    }

                    // Get additional flags
                    valid &= ConfigNodeUtil.ParseValue<bool>(child, "owned", x => vessel.owned = x, factory, false);

                    // Handle the CREW nodes
                    foreach (ConfigNode crewNode in ConfigNodeUtil.GetChildNodes(child, "CREW"))
                    {
                        int count = 1;
                        valid &= ConfigNodeUtil.ParseValue<int>(crewNode, "count", x => count = x, factory, 1);
                        for (int i = 0; i < count; i++)
                        {
                            CrewData cd = new CrewData();

                            // Read crew details
                            valid &= ConfigNodeUtil.ParseValue<string>(crewNode, "name", x => cd.name = x, factory, (string)null);
                            valid &= ConfigNodeUtil.ParseValue<bool>(crewNode, "addToRoster", x => cd.addToRoster = x, factory, true);

                            // Add the record
                            vessel.crew.Add(cd);
                        }
                    }

                    // Add to the list
                    spawnVessel.vessels.Add(vessel);
                }
                finally
                {
                    ConfigNodeUtil.SetCurrentDataNode(factory.dataNode);
                }
            }

            return valid ? spawnVessel : null;
        }

        protected override void OnUpdate()
        {
 	        base.OnUpdate();
            if (HighLogic.LoadedScene == GameScenes.FLIGHT || HighLogic.LoadedScene == GameScenes.TRACKSTATION)
            {
                CreateVessels();
            }
        }

        protected void CreateVessels()
        {
            if (vesselsCreated)
            {
                return;
            }

            String gameDataDir = KSPUtil.ApplicationRootPath;
            gameDataDir = gameDataDir.Replace("\\", "/");
            if (!gameDataDir.EndsWith("/"))
            {
                gameDataDir += "/";
            }
            gameDataDir += "GameData";

            // Spawn the vessel in the game world
            foreach (VesselData vesselData in vessels)
            {
                LoggingUtil.LogVerbose(this, "Spawning a vessel named '" + vesselData.name + "'");

                // Save the current ShipConstruction ship, otherwise the player will see the spawned ship next time they enter the VAB!
                ConfigNode currentShip = ShipConstruction.ShipConfig;

                ShipConstruct shipConstruct = ShipConstruction.LoadShip(gameDataDir + "/" + vesselData.craftURL);
                if (shipConstruct == null)
                {
                    LoggingUtil.LogError(this, "ShipConstruct was null when tried to load '" + vesselData.craftURL +
                        "' (usually this means the file could not be found).");
                    continue;
                }

                // Restore ShipConstruction ship
                ShipConstruction.ShipConfig = currentShip;

                // Set the name
                if (string.IsNullOrEmpty(vesselData.name))
                {
                    vesselData.name = shipConstruct.shipName;
                }

                // Set some parameters that need to be at the part level
                uint flightId = ShipConstruction.GetUniqueFlightID(HighLogic.CurrentGame.flightState);
                uint missionID = (uint)Guid.NewGuid().GetHashCode();
                uint launchID = HighLogic.CurrentGame.launchID++;
                foreach (Part p in shipConstruct.parts)
                {
                    p.flightID = flightId;
                    p.missionID = missionID;
                    p.launchID = launchID;
                    p.flagURL = vesselData.flagURL ?? HighLogic.CurrentGame.flagURL;
                }

                // Assign crew to the vessel
                foreach (CrewData cd in vesselData.crew)
                {
                    bool success = false;

                    // Find a seat for the crew
                    Part part = shipConstruct.parts.Find(p => p.protoModuleCrew.Count < p.CrewCapacity);

                    // Add the crew member
                    if (part != null)
                    {
                        // Create the ProtoCrewMember
                        ProtoCrewMember crewMember = HighLogic.CurrentGame.CrewRoster.GetNewKerbal(ProtoCrewMember.KerbalType.Unowned);
                        if (cd.name != null)
                        {
                            crewMember.name = cd.name;
                        }

                        // Add them to the part
                        success = part.AddCrewmemberAt(crewMember, part.protoModuleCrew.Count);
                    }

                    if (!success)
                    {
                        LoggingUtil.LogWarning(this, "Unable to add crew to vessel named '" + vesselData.name + "'.  Perhaps there's no room?");
                        break;
                    }
                }

                // Set additional info for landed kerbals
                if (vesselData.landed)
                {
                    if (vesselData.altitude == null)
                    {
                        vesselData.altitude = LocationUtil.TerrainHeight(vesselData.latitude, vesselData.longitude, vesselData.body);
                    }

                    Vector3d pos = vesselData.body.GetWorldSurfacePosition(vesselData.latitude, vesselData.longitude, vesselData.altitude.Value);

                    vesselData.orbit = new Orbit(0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, vesselData.body);
                    vesselData.orbit.UpdateFromStateVectors(pos, vesselData.body.getRFrmVel(pos), vesselData.body, Planetarium.GetUniversalTime());
                    LoggingUtil.LogDebug(this, "vesselData generated, orbit = " + vesselData.orbit);
                }
                
                // Create a dummy ProtoVessel, we will use this to dump the parts to a config node.
                // We can't use the config nodes from the .craft file, because they are in a
                // slightly different format than those required for a ProtoVessel (seriously
                // Squad?!?).
                ConfigNode empty = new ConfigNode();
                ProtoVessel dummyProto = new ProtoVessel(empty, null);
                Vessel dummyVessel = new Vessel();
                dummyVessel.parts = shipConstruct.parts;
                dummyProto.vesselRef = dummyVessel;

                // Create the ProtoPartSnapshot objects and then initialize them
                foreach (Part p in shipConstruct.parts)
                {
                    dummyProto.protoPartSnapshots.Add(new ProtoPartSnapshot(p, dummyProto));
                }
                foreach (ProtoPartSnapshot p in dummyProto.protoPartSnapshots)
                {
                    p.storePartRefs();
                }

                // Estimate an object class, numbers are based on the in game description of the
                // size classes.
                float size = shipConstruct.shipSize.magnitude / 2.0f;
                UntrackedObjectClass sizeClass;
                if (size < 4.0f)
                {
                    sizeClass = UntrackedObjectClass.A;
                }
                else if (size < 7.0f)
                {
                    sizeClass = UntrackedObjectClass.B;
                }
                else if (size < 12.0f)
                {
                    sizeClass = UntrackedObjectClass.C;
                }
                else if (size < 18.0f)
                {
                    sizeClass = UntrackedObjectClass.D;
                }
                else
                {
                    sizeClass = UntrackedObjectClass.E;
                }

                // Create the ship's parts
                ConfigNode[] partNodes = dummyProto.protoPartSnapshots.Select<ProtoPartSnapshot, ConfigNode>(GetNodeForPart).ToArray();

                // Create additional nodes
                ConfigNode[] additionalNodes = new ConfigNode[1];
                DiscoveryLevels discoveryLevel = vesselData.owned ? DiscoveryLevels.Owned : DiscoveryLevels.Unowned;
                additionalNodes[0] = ProtoVessel.CreateDiscoveryNode(discoveryLevel, sizeClass, contract.TimeDeadline, contract.TimeDeadline);

                // Create the config node representation of the ProtoVessel
                ConfigNode protoVesselNode = ProtoVessel.CreateVesselNode(vesselData.name, vesselData.vesselType, vesselData.orbit, 0, partNodes, additionalNodes);

                // Additional seetings for a landed vessel
                if (vesselData.landed)
                {
                    // Create the config node representation of the ProtoVessel
                    protoVesselNode.SetValue("sit", Vessel.Situations.LANDED.ToString());
                    protoVesselNode.SetValue("landed", true.ToString());
                    protoVesselNode.SetValue("lat", vesselData.latitude.ToString());
                    protoVesselNode.SetValue("lon", vesselData.longitude.ToString());
                    protoVesselNode.SetValue("alt", vesselData.altitude.ToString());
                    protoVesselNode.SetValue("landedAt", vesselData.body.name);

                    // Figure out the additional heigh to subtract
                    float lowest = float.MaxValue;
                    foreach (Part p in shipConstruct.parts)
                    {
                        foreach (Collider collider in p.GetComponentsInChildren<Collider>())
                        {
                            if (collider.gameObject.layer != 21 && collider.enabled)
                            {
                                lowest = Mathf.Min(lowest, collider.bounds.min.y);
                            }
                        }
                    }

                    // Figure out the surface height and rotation
                    Vector3d norm = vesselData.body.GetRelSurfaceNVector(vesselData.latitude, vesselData.longitude);
                    Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, Vector3.up) *
                        Quaternion.LookRotation(new Vector3((float)norm.x, (float)norm.y, (float)norm.z));
                    protoVesselNode.SetValue("hgt", (shipConstruct.parts[0].localRoot.attPos0.y - lowest).ToString());
                    protoVesselNode.SetValue("rot", KSPUtil.WriteQuaternion(rotation));
                }

                // Add vessel to the game
                ProtoVessel protoVessel = HighLogic.CurrentGame.AddVessel(protoVesselNode);

                // Store the id for later use
                vesselData.id = protoVessel.vesselRef.id;

                // Associate it so that it can be used in contract parameters
                ContractVesselTracker.Instance.AssociateVessel(vesselData.name, protoVessel.vesselRef);
            }

            vesselsCreated = true;
        }

        protected override void OnSave(ConfigNode configNode)
        {
            base.OnSave(configNode);
            configNode.AddValue("vesselsCreated", vesselsCreated);
            
            foreach (VesselData vd in vessels)
            {
                ConfigNode child = new ConfigNode("VESSEL_DETAIL");

                child.AddValue("name", vd.name);
                if (vd.id != null)
                {
                    child.AddValue("id", vd.id);
                }
                child.AddValue("craftURL", vd.craftURL);
                child.AddValue("flagURL", vd.flagURL);
                child.AddValue("vesselType", vd.vesselType);
                child.AddValue("body", vd.body.name);
                child.AddValue("lat", vd.latitude);
                child.AddValue("lon", vd.longitude);
                if (vd.altitude != null)
                {
                    child.AddValue("alt", vd.altitude);
                }
                child.AddValue("landed", vd.landed);
                child.AddValue("owned", vd.owned);

                if (vd.orbit != null)
                {
                    ConfigNode orbitNode = new ConfigNode("ORBIT");
                    new OrbitSnapshot(vd.orbit).Save(orbitNode);
                    child.AddNode(orbitNode);
                }

                // Add crew data
                foreach (CrewData cd in vd.crew)
                {
                    ConfigNode crewNode = new ConfigNode("CREW");

                    if (!string.IsNullOrEmpty(cd.name))
                    {
                        crewNode.AddValue("name", cd.name);
                    }
                    crewNode.AddValue("addToRoster", cd.addToRoster);

                    child.AddNode(crewNode);
                }

                configNode.AddNode(child);
            }
        }

        protected override void OnLoad(ConfigNode configNode)
        {
            base.OnLoad(configNode);
            vesselsCreated = ConfigNodeUtil.ParseValue<bool>(configNode, "vesselsCreated");

            foreach (ConfigNode child in configNode.GetNodes("VESSEL_DETAIL"))
            {
                // Read all the orbit data
                VesselData vd = new VesselData();
                vd.name = child.GetValue("name");
                vd.id = ConfigNodeUtil.ParseValue<Guid?>(child, "id", (Guid?)null);
                vd.craftURL = child.GetValue("craftURL");
                vd.flagURL = ConfigNodeUtil.ParseValue<string>(child, "flagURL", (string)null);
                vd.vesselType = ConfigNodeUtil.ParseValue<VesselType>(child, "vesselType");
                vd.body = ConfigNodeUtil.ParseValue<CelestialBody>(child, "body");
                vd.latitude = ConfigNodeUtil.ParseValue<double>(child, "lat");
                vd.longitude = ConfigNodeUtil.ParseValue<double>(child, "lon");
                vd.altitude = ConfigNodeUtil.ParseValue<double?>(child, "alt", (double?)null);
                vd.landed = ConfigNodeUtil.ParseValue<bool>(child, "landed");
                vd.owned = ConfigNodeUtil.ParseValue<bool>(child, "owned");

                if (child.HasNode("ORBIT"))
                {
                    vd.orbit = new OrbitSnapshot(child.GetNode("ORBIT")).Load();
                }

                // Load crew data
                foreach (ConfigNode crewNode in child.GetNodes("CREW"))
                {
                    CrewData cd = new CrewData();

                    cd.name = ConfigNodeUtil.ParseValue<string>(crewNode, "name", (string)null);
                    cd.addToRoster = ConfigNodeUtil.ParseValue<bool>(crewNode, "addToRoster");

                    vd.crew.Add(cd);
                }

                // Add to the global list
                vessels.Add(vd);
            }
        }

        protected override void OnRegister()
        {
            GameEvents.onVesselRecovered.Add(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
        }

        protected override void OnUnregister()
        {
            GameEvents.onVesselRecovered.Remove(new EventData<ProtoVessel>.OnEvent(OnVesselRecovered));
        }

        private void OnVesselRecovered(ProtoVessel v)
        {
            LoggingUtil.LogVerbose(this, "OnVesselRecovered: " + v);

            // EVA vessel
            if (v.vesselType == VesselType.EVA)
            {
                foreach (ProtoPartSnapshot p in v.protoPartSnapshots)
                {
                    {
                        LoggingUtil.LogVerbose(this, "    p: " + p);
                        foreach (string name in p.protoCrewNames)
                        {
                            // Find this crew member in our data
                            foreach (VesselData vd in vessels)
                            {
                                foreach (CrewData cd in vd.crew)
                                {
                                    if (cd.name == name && cd.addToRoster)
                                    {
                                        // Add them to the roster
                                        ProtoCrewMember pcm = HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(cm => cm.name == cd.name).First();
                                        pcm.type = ProtoCrewMember.KerbalType.Crew;
                                    }
                                }
                            }
                        }
                    }
                }

            }

            // Vessel with crew
            foreach (ProtoCrewMember crewMember in v.GetVesselCrew())
            {
                // Find this crew member in our data
                foreach (VesselData vd in vessels)
                {
                    foreach (CrewData cd in vd.crew)
                    {
                        if (cd.name == crewMember.name && cd.addToRoster)
                        {
                            // Add them to the roster
                            ProtoCrewMember pcm = HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(cm => cm.name == cd.name).First();
                            pcm.type = ProtoCrewMember.KerbalType.Crew;
                        }
                    }
                }
            }
        }

        protected override void OnCancelled()
        {
            RemoveVessels();
        }

        protected override void OnDeadlineExpired()
        {
            RemoveVessels();
        }

        protected override void OnDeclined()
        {
            RemoveVessels();
        }

        protected override void OnGenerateFailed()
        {
            RemoveVessels();
        }

        protected override void OnOfferExpired()
        {
            RemoveVessels();
        }

        protected override void OnWithdrawn()
        {
            RemoveVessels();
        }

        private void RemoveVessels()
        {
            foreach (VesselData vd in vessels)
            {
                Vessel vessel = FlightGlobals.Vessels.Find(v => v.id == vd.id);
                if (vessel != null)
                {
                    vessel.state = Vessel.State.DEAD;
                }
            }
        }

        private ConfigNode GetNodeForPart(ProtoPartSnapshot p)
        {
            ConfigNode node = new ConfigNode("PART");
            p.Save(node);
            return node;
        }

        public ProtoCrewMember GetKerbal(int index)
        {
            int current = index;
            foreach (VesselData vd in vessels)
            {
                if (current < vd.crew.Count)
                {
                    return HighLogic.CurrentGame.CrewRoster.AllKerbals().Where(cm => cm.name == vd.crew[current].name).First();
                }
                current -= vd.crew.Count;
            }

            throw new Exception("ContractConfigurator: index " + index +
                " is out of range for number of Kerbals spawned (" + KerbalCount + ").");
        }

    }
}
