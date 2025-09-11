using Crux.AnimatorDynamics.Editor.Controls;
using Crux.AnimatorDynamics.Runtime.Models.Approach;
using Crux.Core.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Crux.AnimatorDynamics.Editor.Editors
{
    [CustomEditor(typeof(ApproachModel))]
    public class ApproachModelEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            if (!AssetReference.TryParse("345b0ed756e7844c282c2ac871385601,9197481963319205126", out var assetRef))
                return null;

            if (!assetRef.TryLoad(out VisualTreeAsset uxml))
                return null;

            var root = uxml.Instantiate();
            
            ApproachModel model = target as ApproachModel;
            root.Q<ApproachModelPreview>().Connect(model);

            return root;
        }
    }
}