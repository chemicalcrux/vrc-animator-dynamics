using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Crux.AnimatorDynamics.Runtime.Models.Approach;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Crux.AnimatorDynamics.Editor.Controls
{
    public class ApproachModelPreview : VisualElement
    {
        private VisualElement root;
        private Label output;

        private ApproachModel model;
        private ApproachDataV1 data;

        private List<Slider> sliders = new();
        private SerializedProperty property;

        public ApproachModelPreview()
        {
            root = new VisualElement
            {
                name = "Root"
            };

            Add(root);

            output = new Label();
            Add(output);
        }

        public void Connect(ApproachModel model)
        {
            this.model = model;
            Rebuild();
        }

        void Recalculate()
        {
            float result = 0;

            foreach (var (input, slider) in data.inputs.Zip(sliders, ValueTuple.Create))
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
            root.Clear();
            sliders.Clear();

            if (!model.data.TryUpgradeTo(out data))
                return;

            foreach (var input in data.inputs)
            {
                var slider = new Slider(input.parameter, 0, 1);

                slider.RegisterValueChangedCallback(_ => Recalculate());
                sliders.Add(slider);
                root.Add(slider);
            }

            Recalculate();
        }

        public new class UxmlFactory : UxmlFactory<ApproachModelPreview, UxmlTraits>
        {
            public override VisualElement Create(IUxmlAttributes bag, CreationContext cc)
            {
                var field = base.Create(bag, cc) as ApproachModelPreview;
                
// this allows us to watch for changes in a binding
                var propertyField = new PropertyField();
                propertyField.bindingPath = field.Binding;
                propertyField.style.display = DisplayStyle.None;

                propertyField.RegisterValueChangeCallback(_ => { field.Rebuild(); });

                propertyField.schedule.Execute(() =>
                {
                    FieldInfo fieldInfo = propertyField.GetType().GetField("m_SerializedProperty",
                        BindingFlags.NonPublic | BindingFlags.Instance);

                    if (fieldInfo == null)
                    {
                        Debug.LogWarning("This shouldn't happen...");
                        return;
                    }

                    field.property = (SerializedProperty)fieldInfo.GetValue(propertyField);

                    Debug.Log("Property: " + field.property);
                    field.Rebuild();
                });

                field.Add(propertyField);

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