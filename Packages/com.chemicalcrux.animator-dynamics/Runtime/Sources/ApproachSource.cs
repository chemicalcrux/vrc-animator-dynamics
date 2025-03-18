using System;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace ChemicalCrux.AnimatorDynamics.Runtime.Sources
{
    public class ApproachSource : MonoBehaviour, IEditorOnly
    {
        [Serializable]
        public struct InputItem
        {
            public string parameter;
            public Vector2 inputRange;
            public Vector2 approachRange;
        }

        public string outputParameter;
        public Vector2 outputRange;

        public List<InputItem> inputs;
    }
}