using System;
using System.Linq;
using Crux.AnimatorDynamics.Runtime.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static Crux.AnimatorDynamics.Editor.ExtensionMethods.VisualElementExtensionMethods;

namespace Crux.AnimatorDynamics.Editor.Controls
{
    public class RemapModelPreview : VisualElement
    {
        private readonly VisualElement root;
        private readonly Label output;

        private SliderSet sliderSet;

        private RemapModel model;
        private RemapDataV1 data;
        private SerializedProperty property;

        public RemapModelPreview()
        {
            root = new VisualElement
            {
                name = "Root"
            };

            Add(root);

            output = new Label();
            Add(output);

            sliderSet = new()
            {
                root = root
            };
        }

        public void Connect(RemapModel model)
        {
            this.model = model;
            Rebuild();
        }

        void Recalculate()
        {
            float result = 0;
            
            foreach (var (input, slider) in data.inputs.Zip(sliderSet.sliders, ValueTuple.Create))
            {
                float inputFactor = Mathf.InverseLerp(input.inputRange.x, input.inputRange.y, slider.value);
                float outputFactor = Mathf.Lerp(input.outputRange.x, input.outputRange.y, inputFactor);
                result += outputFactor;
            }

            output.text = $"{result:N2}";
        }

        void Rebuild()
        {
            if (!model.data.TryUpgradeTo(out data))
                return;

            sliderSet.Rebuild(data.inputs.Select(input => input.parameter), Recalculate);

            Recalculate();
        }

        public new class UxmlFactory : UxmlFactory<RemapModelPreview, UxmlTraits>
        {
            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                var field = base.Create(bag, cc) as RemapModelPreview;

                field.TrackPropertyByName(field!.Binding, field.Rebuild);

                return field;
            }
        }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private readonly UxmlStringAttributeDescription binding = new()
            {
                name = "binding", defaultValue = ""
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                var ate = ve as RemapModelPreview;

                ate!.Binding = binding.GetValueFromBag(bag, cc);
            }
        }

        public string Binding { get; set; }
    }
}