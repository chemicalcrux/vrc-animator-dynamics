using System;
using System.Linq;
using Crux.AnimatorDynamics.Editor.ExtensionMethods;
using Crux.AnimatorDynamics.Runtime.Models.Approach;
using UnityEngine;
using UnityEngine.UIElements;

namespace Crux.AnimatorDynamics.Editor.Controls
{
    public class ApproachModelPreview : VisualElement
    {
        private readonly Label output;
        private readonly SliderSet sliderSet;

        private ApproachModel model;
        private ApproachDataV1 data;

        public ApproachModelPreview()
        {
            var root = new VisualElement
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

        public void Connect(ApproachModel newModel)
        {
            model = newModel;
            Rebuild();
        }

        void Recalculate()
        {
            float result = 0;

            foreach (var (input, slider) in data.inputs.Zip(sliderSet.sliders, ValueTuple.Create))
            {
                float inputFactor = Mathf.InverseLerp(input.inputRange.x, input.inputRange.y, slider.value);
                float outputFactor = Mathf.Lerp(input.approachRange.x, input.approachRange.y, inputFactor);
                result = Mathf.Lerp(result, 1, outputFactor);
            }

            output.text = $"{result:N2}";
        }

        void Rebuild()
        {
            Debug.Log("Rebuilding");

            if (!model.data.TryUpgradeTo(out data))
                return;

            sliderSet.Rebuild(data.inputs.Select(input => input.parameter), Recalculate);

            Recalculate();
        }

        public new class UxmlFactory : UxmlFactory<ApproachModelPreview, UxmlTraits>
        {
            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                var field = base.Create(bag, cc) as ApproachModelPreview;

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
                var ate = ve as ApproachModelPreview;

                ate!.Binding = binding.GetValueFromBag(bag, cc);
            }
        }

        public string Binding { get; set; }
    }
}