using System;
using System.Collections.Generic;
using Crux.Core.Runtime.Upgrades;
using Crux.ProceduralController.Runtime.Models;
using UnityEngine;

namespace Crux.AnimatorDynamics.Runtime.Models
{
    [CreateAssetMenu(menuName = Consts.AssetRootPath + "Remap", order = Consts.AssetRootOrder)]
    public class RemapModel : AssetModel
    {
        [SerializeReference] public RemapData data = new RemapDataV1();

        private void Reset()
        {
            data = new RemapDataV1();
        }
    }

    [Serializable]
    [UpgradableLatestVersion(1)]
    public abstract class RemapData : Upgradable<RemapData>
    {
        
    }

    [Serializable]
    [UpgradableVersion(1)]
    public class RemapDataV1 : RemapData
    {
        [Serializable]
        public class InputItem
        {
            public string parameter;
            public Vector2 inputRange;
            public Vector2 outputRange;
        }
    
        public List<InputItem> inputs;
        public string output;
        
        public override RemapData Upgrade()
        {
            return this;
        }
    }
}