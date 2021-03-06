﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator
{
    /// <summary>
    /// ParameterFactory wrapper for EnterOrbit ContractParameter.
    /// </summary>
    [Obsolete("EnterOrbit is obsolete as of Contract Configurator 0.7.5 and will be removed in 1.0.0.  Please use Orbit or ReachState instead.")]
    public class EnterOrbitFactory : ParameterFactory
    {
        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            valid &= ValidateTargetBody(configNode);
            LoggingUtil.LogWarning(this, "EnterOrbit is obsolete as of Contract Configurator 0.7.5 and will be removed in 1.0.0.  Please use Orbit or ReachState instead.");

            return valid;
        }

        public override ContractParameter Generate(Contract contract)
        {
            return new EnterOrbit(targetBody);
        }
    }
}
