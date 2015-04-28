﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using Contracts;
using Contracts.Parameters;

namespace ContractConfigurator.Parameters
{
    /// <summary>
    /// Parameter for checking whether a target vessel is destroyed or not.
    /// </summary>
    public class TargetDestroyed : ContractConfiguratorParameter, ParameterDelegateContainer
    {
        protected List<string> vessels { get; set; }

        private List<string> destroyedTargets = new List<string>();
        public bool ChildChanged { get; set; }

        public TargetDestroyed()
            : base(null)
        {
        }

        public TargetDestroyed(IEnumerable<string> vessels, string title)
            : base(title)
        {
            this.vessels = vessels.ToList();

            CreateDelegates();
        }

        protected override string GetTitle()
        {
            string output = "";
            if (string.IsNullOrEmpty(title))
            {
                output = "Target" + (vessels.Count > 1 ? "s" : "") + " destroyed";
            }
            else
            {
                output = title;
            }
            return output;
        }

        protected void CreateDelegates()
        {
            foreach (string vessel in vessels)
            {
                AddParameter(new ParameterDelegate<string>("Target: " + ContractVesselTracker.Instance.GetDisplayName(vessel),
                    ignored => CheckTargetDestroyed(vessel), ParameterDelegateMatchType.VALIDATE_ALL, true));
            }
        }

        private bool CheckTargetDestroyed(string vessel)
        {
            LoggingUtil.LogVerbose(this, "TargetDestroyed(" + vessel + ")");
            return destroyedTargets.Contains(vessel);
        }

        protected override void OnParameterSave(ConfigNode node)
        {
            foreach (string vessel in vessels)
            {
                node.AddValue("vessel", vessel);
            }
            foreach (string destroyedTarget in destroyedTargets)
            {
                node.AddValue("destroyedTarget", destroyedTarget);
            }
        }

        protected override void OnParameterLoad(ConfigNode node)
        {
            vessels = ConfigNodeUtil.ParseValue<List<string>>(node, "vessel", new List<string>());
            destroyedTargets = ConfigNodeUtil.ParseValue<List<string>>(node, "destroyedTarget", new List<string>());

            ParameterDelegate<string>.OnDelegateContainerLoad(node);
            CreateDelegates();
        }

        protected override void OnRegister()
        {
            base.OnRegister();
            GameEvents.onVesselWillDestroy.Add(new EventData<Vessel>.OnEvent(OnVesselWillDestroy));
        }

        protected override void OnUnregister()
        {
            base.OnUnregister();
            GameEvents.onVesselWillDestroy.Remove(new EventData<Vessel>.OnEvent(OnVesselWillDestroy));
        }

        protected virtual void OnVesselWillDestroy(Vessel v)
        {
            LoggingUtil.LogVerbose(this, "OnVesselWillDestroy: " + v.id);
            foreach (string key in ContractVesselTracker.Instance.GetAssociatedKeys(v))
            {
                LoggingUtil.LogVerbose(this, "adding to destroyedTargets: " + key);
                destroyedTargets.AddUnique(key);
            }

            bool success = ParameterDelegate<string>.CheckChildConditions(this, "");
            if (ChildChanged || success)
            {
                ChildChanged = false;
                if (success)
                {
                    SetState(ParameterState.Complete);
                }
                else
                {
                    ContractConfigurator.OnParameterChange.Fire(Root, this);
                }
            }
            
            ContractConfigurator.OnParameterChange.Fire(Root, this);
        }
    }
}
