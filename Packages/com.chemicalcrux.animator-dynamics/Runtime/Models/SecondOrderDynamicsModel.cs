using System;
using System.Collections.Generic;
using ChemicalCrux.ProceduralController.Runtime.Models;
using Pursuit.Code;
using UnityEngine;

namespace ChemicalCrux.AnimatorDynamics.Runtime.Models
{
    [Serializable]
    public struct InputItem
    {
        public string parameter;
        public Vector2 inputRange;
        public Vector2 outputRange;
    }
    
    [CreateAssetMenu]
    public class SecondOrderDynamicsModel : AssetModel
    {
        public List<InputItem> inputs;
        public float x0;

        public float f;
        public float z;
        public float r;
    
        public string outputParameter;

        public string deltaTimeParameter = "Shared/Time/Delta";
        public string deltaTimeInverseParameter = "Shared/Time/Delta Inverse";

        [Header ("Preview")]
        public AnimationCurve previewCurve = new();
        public int minimumFramerate;
        
        private void OnValidate()
        {
            SecondOrderDynamicsFloat simulator = new()
            {
                x0 = 0,
                f = f,
                z = z,
                r = r
            };

            previewCurve.ClearKeys();

            float t = 0;

            simulator.Setup();

            minimumFramerate = Mathf.RoundToInt(1 / simulator.tCrit);

            previewCurve.AddKey(0, simulator.y);

            while (t < 1)
            {
                simulator.Update(0.01f, 1);
                t += 0.01f;
                previewCurve.AddKey(t, simulator.y);
            }
        }
    }
}