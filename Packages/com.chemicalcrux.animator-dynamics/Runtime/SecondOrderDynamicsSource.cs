using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace ChemicalCrux.AnimatorDynamics.Runtime
{
    [Serializable]
    public struct InputItem
    {
        public string parameter;
        public Vector2 inputRange;
        public Vector2 outputRange;
    }
    
    public class SecondOrderDynamicsSource : MonoBehaviour, IEditorOnly
    {
        public List<InputItem> inputs;
        public float x0;

        public float f;
        public float z;
        public float r;
    
        public string outputParameter;

        public string deltaTimeParameter = "Shared/Time/Delta";
        public string deltaTimeInverseParameter = "Shared/Time/Delta Inverse";
    }
}