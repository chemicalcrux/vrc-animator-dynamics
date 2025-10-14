using System;
using System.Collections.Generic;
using Crux.Core.Runtime.Upgrades;
using Crux.ProceduralController.Runtime.Models;
using UnityEngine;

namespace Crux.AnimatorDynamics.Runtime.Models.Approach
{
    [CreateAssetMenu(menuName = Consts.AssetRootPath + "Approach", order = Consts.AssetRootOrder)]
    public class ApproachModel : AssetModel
    {
        [SerializeReference] public ApproachData data = new ApproachDataV1();

        private void Reset()
        {
            data = new ApproachDataV1();
        }
    }

    [Serializable]
    [UpgradableLatestVersion(1)]
    public abstract class ApproachData : Upgradable<ApproachData>
    {
        
    }

    [Serializable]
    [UpgradableVersion(1)]
    public class ApproachDataV1 : ApproachData
    {
        [Serializable]
        public class InputItem
        {
            public string parameter;
            public Vector2 inputRange = new(0, 1);
            public Vector2 approachRange = new(0, 1);
        }

        public string outputParameter;
        public Vector2 outputRange = new(0, 1);

        public List<InputItem> inputs = new();
        
        public override ApproachData Upgrade()
        {
            return this;
        }
    }
}