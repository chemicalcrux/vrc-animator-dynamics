using Crux.AnimatorDynamics.Editor.Controls;
using Crux.AnimatorDynamics.Runtime.Models.SecondOrderDynamics;
using Crux.Core.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Crux.AnimatorDynamics.Editor.Editors
{
    [CustomEditor(typeof(SecondOrderDynamicsModel))]
    public class SecondOrderDynamicsModelEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            if (!AssetReference.TryParse("540b22466867149a2adc3dd1f63e4957,9197481963319205126", out var assetRef))
                return null;

            if (!assetRef.TryLoad(out VisualTreeAsset uxml))
                return null;

            var root = uxml.Instantiate();
            
            SecondOrderDynamicsModel model = target as SecondOrderDynamicsModel;
            root.Q<SecondOrderDynamicsPreview>().Connect(model);

            return root;
        }
    }
}