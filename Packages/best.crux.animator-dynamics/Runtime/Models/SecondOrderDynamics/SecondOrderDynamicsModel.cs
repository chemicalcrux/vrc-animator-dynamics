using System;
using System.Collections.Generic;
using Crux.Core.Runtime.Upgrades;
using Crux.ProceduralController.Runtime.Models;
using UnityEngine;

namespace Crux.AnimatorDynamics.Runtime.Models.SecondOrderDynamics
{
    [Serializable]
    public struct InputItem
    {
        public string parameter;
        public Vector2 inputRange;
        public Vector2 outputRange;
    }
    
    [CreateAssetMenu]
    public class SecondOrderDynamicsModel : AssetModel
    {
        [SerializeReference] public SecondOrderDynamicsData data = new SecondOrderDynamicsDataV1();

        void Reset()
        {
            data = new SecondOrderDynamicsDataV1();
        }

        void OnValidate()
        {
            data.OnValidate();
            
        }
    }

    [Serializable]
    [UpgradableLatestVersion(1)]
    public abstract class SecondOrderDynamicsData : Upgradable<SecondOrderDynamicsData>
    {
        public virtual void OnValidate()
        {
            
        }
    }

    [Serializable]
    [UpgradableVersion(1)]
    public class SecondOrderDynamicsDataV1 : SecondOrderDynamicsData
    {
        public List<InputItem> inputs;
        public float x0;

        public float f;
        public float z;
        public float r;
    
        public string outputParameter;

        public string deltaTimeParameter = "Shared/Time/Delta";
        public string deltaTimeInverseParameter = "Shared/Time/Delta Inverse";

        public override SecondOrderDynamicsData Upgrade()
        {
            return this;
        }

        public override void OnValidate()
        {
            base.OnValidate();

            f = Mathf.Max(0, f);
            z = Mathf.Max(0, z);
        }
    }
}