﻿using ContractConfigurator.Parameters;
using Contracts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using UnityEngine;
using RemoteTech;
using RemoteTech.API;
using ContractConfigurator;

namespace ContractConfigurator.RemoteTech
{
    public class KSCConnectivityParameter : RemoteTechParameter
    {
        public bool hasConnectivity { get; set; }

        public KSCConnectivityParameter()
            : this(true, "")
        {
        }

        public KSCConnectivityParameter(bool hasConnectivity, string title)
            : base(title)
        {
            this.title = string.IsNullOrEmpty(title) ? (hasConnectivity ? "Connected to KSC" : " Not connected to KSC") : title;
            this.hasConnectivity = hasConnectivity;
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            base.OnParameterSave(node);

            node.AddValue("hasConnectivity", hasConnectivity);
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            base.OnParameterLoad(node);

            hasConnectivity = ConfigNodeUtil.ParseValue<bool>(node, "hasConnectivity");
        }

        /// <summary>
        /// Whether this vessel meets the parameter condition.
        /// </summary>
        /// <param name="vessel">The vessel to check</param>
        /// <returns>Whether the vessel meets the condition</returns>
        protected override bool VesselMeetsCondition(Vessel vessel)
        {
            LoggingUtil.LogVerbose(this, "Checking VesselMeetsCondition: " + vessel.id);
            var satellite = RTCore.Instance.Satellites[vessel.id];
            foreach (var v in RTCore.Instance.Network[satellite])
            {
                LoggingUtil.LogVerbose(this, "    Goal = " + v.Goal.Name);
                LoggingUtil.LogVerbose(this, "    Links.Count = " + v.Links.Count);
            }
            return API.HasConnectionToKSC(vessel.id) ^ !hasConnectivity;
        }
    }
}
