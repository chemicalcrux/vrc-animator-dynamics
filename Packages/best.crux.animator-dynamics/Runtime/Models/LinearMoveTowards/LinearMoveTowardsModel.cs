using System;
using System.Collections.Generic;
using Crux.Core.Runtime.Upgrades;
using Crux.ProceduralController.Runtime.Models;
using UnityEngine;

namespace Crux.AnimatorDynamics.Runtime.Models.LinearMoveTowards
{
    [CreateAssetMenu(menuName = Consts.AssetRootPath + "Linear Move Towards", order = Consts.AssetRootOrder)]
    public class LinearMoveTowardsModel : AssetModel
    {
        [SerializeReference] public LinearMoveTowardsData data = new LinearMoveTowardsDataV1();

        void Reset()
        {
            data = new LinearMoveTowardsDataV1();
        }
    }

    [Serializable]
    [UpgradableLatestVersion(1)]
    public abstract class LinearMoveTowardsData : Upgradable<LinearMoveTowardsData>
    {
        
    }

    [Serializable]
    [UpgradableVersion(1)]
    public class LinearMoveTowardsDataV1 : LinearMoveTowardsData
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
    
        public float decreaseRate = 0.05f;
        public float increaseRate = 0.05f;
        
        public override LinearMoveTowardsData Upgrade()
        {
            return this;
        }
    }
}