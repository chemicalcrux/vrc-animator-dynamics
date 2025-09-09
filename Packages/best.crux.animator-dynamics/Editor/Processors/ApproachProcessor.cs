using System.Collections.Generic;
using System.Linq;
using Crux.AnimatorDynamics.Runtime.Models;
using Crux.ProceduralController.Editor;
using Crux.ProceduralController.Editor.Processors;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;
using static ChemicalCrux.AnimatorDynamics.Editor.AnimatorMath;

namespace ChemicalCrux.AnimatorDynamics.Editor.Processors
{
    [UsedImplicitly]
    public class ApproachProcessor : Processor<ApproachModel>
    {
        private AnimatorController controller;
        
        private int inputIndex;
        private readonly Dictionary<string, string> inputMap = new();

        public override void Process(Context context)
        {
            inputMap.Clear();
            inputIndex = 0;

            controller = new AnimatorController();

            AddParameters();
            AddRemapLayer();
            AddProductLayer();

            context.receiver.AddController(controller);
        }

        void AddParameters()
        {
            foreach (var input in model.inputs)
            {
                inputMap[input.parameter] = $"Internal/Remap {inputIndex}";
                ++inputIndex;
                controller.AddParameter(input.parameter, AnimatorControllerParameterType.Float);
                controller.AddParameter(inputMap[input.parameter], AnimatorControllerParameterType.Float);
            }

            controller.AddParameter(model.outputParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter("One", AnimatorControllerParameterType.Float);

            var parameters = controller.parameters;

            foreach (var parameter in parameters)
            {
                if (parameter.name == "One")
                {
                    parameter.defaultFloat = 1;
                }
            }

            controller.parameters = parameters;
        }

        void AddRemapLayer()
        {
            var machine = new AnimatorStateMachine
            {
                name = "Remap",
                hideFlags = HideFlags.HideInHierarchy
            };

            var layer = new AnimatorControllerLayer
            {
                name = "Remap",
                defaultWeight = 1f,
                stateMachine = machine
            };

            controller.AddLayer(layer);

            List<Motion> motions = new();

            foreach (var input in model.inputs)
            {
                Vector2 outputRange;

                outputRange.x = 1 - input.approachRange.x;
                outputRange.y = 1 - input.approachRange.y;
                var motion = Remap(input.parameter, inputMap[input.parameter], input.inputRange, outputRange);
                motions.Add(motion);
            }

            var combine = Combine(motions.ToArray());

            var state = machine.AddState("Tree");
            state.motion = combine;
        }

        void AddProductLayer()
        {
            var machine = new AnimatorStateMachine
            {
                name = "Product",
                hideFlags = HideFlags.HideInHierarchy
            };

            var layer = new AnimatorControllerLayer
            {
                name = "Product",
                defaultWeight = 1f,
                stateMachine = machine
            };

            controller.AddLayer(layer);

            var root = new BlendTree
            {
                name = "Root",
                blendType = BlendTreeType.Direct,
                hideFlags = HideFlags.HideInHierarchy
            };

            root.AddChild(GetClip(model.outputParameter, model.outputRange.y));

            var product = CreateProductTree(
                GetClip(model.outputParameter, model.outputRange.x - model.outputRange.y),
                model.inputs.Select(input => inputMap[input.parameter]).ToArray());

            root.AddChild(product);

            SetOneParams(root);

            var state = machine.AddState("Tree");
            state.motion = root;
        }
    }
}