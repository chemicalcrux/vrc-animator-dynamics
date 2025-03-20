using System;
using System.Collections.Generic;
using ChemicalCrux.ProceduralController.Runtime.Models;
using UnityEngine;

namespace ChemicalCrux.AnimatorDynamics.Runtime.Sources
{
    [CreateAssetMenu]
    public class LinearMoveTowardsSource : AssetModel
    {
        [Serializable]
        public struct InputItem
        {
            public string parameter;
            public Vector2 inputRange;
            public Vector2 outputRange;
        }
    
        public List<InputItem> inputs;
        public string outputParameter;
        public string deltaTimeParameter = "Shared/Time/Delta";

        public Vector2 outputRange = new Vector2(0, 1);
    
        public float decreaseRate = 0.05f;
        public float increaseRate = 0.05f;
    }
}