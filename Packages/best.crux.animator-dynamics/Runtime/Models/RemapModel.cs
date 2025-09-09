using System;
using System.Collections.Generic;
using Crux.ProceduralController.Runtime.Models;
using UnityEngine;

namespace ChemicalCrux.AnimatorDynamics.Runtime.Models
{
    [CreateAssetMenu]
    public class RemapModel : AssetModel
    {
        [Serializable]
        public struct InputItem
        {
            public string parameter;
            public Vector2 inputRange;
            public Vector2 outputRange;
        }
    
        public List<InputItem> inputs;
        public string output;
    }
}