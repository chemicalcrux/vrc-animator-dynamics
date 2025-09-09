using System;
using System.Collections.Generic;
using Crux.ProceduralController.Runtime.Models;
using UnityEngine;

namespace Crux.AnimatorDynamics.Runtime.Models
{
    [CreateAssetMenu]
    public class ApproachModel : AssetModel
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