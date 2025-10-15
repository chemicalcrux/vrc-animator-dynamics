using System;
using System.Collections.Generic;
using Crux.Core.Runtime.Upgrades;
using Crux.ProceduralController.Runtime.Models;
using UnityEngine;

namespace Crux.AnimatorDynamics.Runtime.Models.ExponentialDamping
{
    [CreateAssetMenu(menuName = Consts.AssetRootPath + "Exponential Damping", order = Consts.AssetRootOrder)]
    public class ExponentialDampingModel : AssetModel
    {
        [SerializeReference] public ExponentialDampingData data = new ExponentialDampingDataV1();

        private void Reset()
        {
            data = new ExponentialDampingDataV1();
        }
    }

    [Serializable]
    [UpgradableLatestVersion(1)]
    public abstract class ExponentialDampingData : Upgradable<ExponentialDampingData>
    {
        
    }

    [Serializable]
    [UpgradableVersion(1)]
    public class ExponentialDampingDataV1 : ExponentialDampingData
    {
        [Serializable]
        public class InputItem
        {
            public string parameter;
            public Vector2 inputRange;
            public Vector2 outputRange;
        }

        public List<InputItem> inputs = new();
        public string outputParameter;
        public string deltaTimeParameter = "Shared/Time/Delta";

        public Vector2 outputRange = new Vector2(0, 1);

        public float dampingFactor;
        
        public override ExponentialDampingData Upgrade()
        {
            return this;
        }
    }
}