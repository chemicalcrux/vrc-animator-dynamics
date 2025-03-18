using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace ChemicalCrux.AnimatorDynamics.Runtime.Sources
{
    public class LinearMoveTowardsSource : MonoBehaviour, IEditorOnly
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