﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;
using ContractConfigurator.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for ReachState ContractParameter.
    /// </summary>
    public class ReachStateFactory : ParameterFactory
    {
        protected string biome;
        protected Vessel.Situations? situation;
        protected float minAltitude;
        protected float maxAltitude;
        protected float minTerrainAltitude;
        protected float maxTerrainAltitude;
        protected double minSpeed;
        protected double maxSpeed;

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ConfigNodeUtil.ParseValue<string>(configNode, "biome", x => biome = x, this, "");
            valid &= ConfigNodeUtil.ParseValue<Vessel.Situations?>(configNode, "situation", x => situation = x, this, (Vessel.Situations?)null);
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "minAltitude", x => minAltitude = x, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "maxAltitude", x => maxAltitude = x, this, float.MaxValue, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "minTerrainAltitude", x => minTerrainAltitude = x, this, 0.0f, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<float>(configNode, "maxTerrainAltitude", x => maxTerrainAltitude = x, this, float.MaxValue, x => Validation.GE(x, 0.0f));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "minSpeed", x => minSpeed = x, this, 0.0, x => Validation.GE(x, 0.0));
            valid &= ConfigNodeUtil.ParseValue<double>(configNode, "maxSpeed", x => maxSpeed = x, this, double.MaxValue, x => Validation.GE(x, 0.0));

            // Validate target body
            valid &= ValidateTargetBody(configNode);

            // Validation minimum set
            valid &= ConfigNodeUtil.AtLeastOne(configNode, new string[] { "targetBody", "biome", "situation", "minAltitude", "maxAltitude",
                "minTerrainAltitude", "maxTerrainAltitude", "minSpeed", "maxSpeed" }, this);

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new ReachState(targetBody, biome, situation, minAltitude, maxAltitude, minTerrainAltitude, maxTerrainAltitude, minSpeed, maxSpeed, title);
        }
    }
}
