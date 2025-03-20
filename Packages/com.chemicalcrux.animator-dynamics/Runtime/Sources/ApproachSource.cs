using System;
using System.Collections.Generic;
using ChemicalCrux.ProceduralController.Runtime.Models;
using UnityEngine;

namespace ChemicalCrux.AnimatorDynamics.Runtime.Sources
{
    [CreateAssetMenu]
    public class ApproachSource : AssetModel
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