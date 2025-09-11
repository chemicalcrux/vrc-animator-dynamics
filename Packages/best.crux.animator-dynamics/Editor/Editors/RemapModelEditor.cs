using Crux.AnimatorDynamics.Editor.Controls;
using Crux.AnimatorDynamics.Runtime.Models;
using Crux.Core.Editor;
using UnityEditor;
using UnityEngine.UIElements;

namespace Crux.AnimatorDynamics.Editor.Editors
{
    [CustomEditor(typeof(RemapModel))]
    public class RemapModelEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            if (!AssetReference.TryParse("26a81a4fee06b48df9ddfda70517552f,9197481963319205126", out var assetRef))
                return null;

            if (!assetRef.TryLoad(out VisualTreeAsset uxml))
                return null;

            var root = uxml.Instantiate();
            
            RemapModel model = target as RemapModel;
            root.Q<RemapModelPreview>().Connect(model);

            return root;
        }
    }
}