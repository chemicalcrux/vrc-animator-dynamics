using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Crux.AnimatorDynamics.Editor
{
    public class SliderSet
    {
        public VisualElement root;
        public readonly List<Slider> sliders = new();
        private readonly List<float> oldValues = new();

        public void Rebuild(IEnumerable<string> sliderData, Action callback)
        {
            root.Clear();
            sliders.Clear();

            int idx = 0;
            foreach (var label in sliderData)
            {
                var slider = new Slider(label, 0, 1);

                if (idx < oldValues.Count)
                    slider.value = oldValues[idx];
                else
                {
                    oldValues.Add(0);
                    slider.value = 0;
                }

                int capturedIndex = idx;
                
                slider.RegisterValueChangedCallback(_ => oldValues[capturedIndex] = slider.value);
                slider.RegisterValueChangedCallback(_ => callback());
                
                sliders.Add(slider);
                root.Add(slider);

                ++idx;
            }
        }
    }
}