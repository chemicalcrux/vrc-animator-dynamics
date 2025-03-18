using System.Collections.Generic;
using System.Linq;
using ChemicalCrux.AnimatorDynamics.Runtime;
using ChemicalCrux.AnimatorDynamics.Runtime.Sources;
using com.vrcfury.api;
using UnityEditor.Animations;
using UnityEngine;
using static ChemicalCrux.AnimatorDynamics.Editor.AnimatorMath;

namespace ChemicalCrux.AnimatorDynamics.Editor.Processors
{
    public static class ApproachProcessor
    {
        private static int _inputIndex;
        private static readonly Dictionary<string, string> InputMap = new();

        public static void Process(ApproachSource source, GameObject avatarRoot)
        {
            InputMap.Clear();
            _inputIndex = 0;

            var controller = new AnimatorController();

            AddParameters(source, controller);
            AddRemapLayer(source, controller);
            AddProductLayer(source, controller);

            var fc = FuryComponents.CreateFullController(source.gameObject);
            fc.AddController(controller);

            foreach (var input in source.inputs)
                fc.AddGlobalParam(input.parameter);

            fc.AddGlobalParam(source.outputParameter);
        }

        static void AddParameters(ApproachSource source, AnimatorController controller)
        {
            foreach (var input in source.inputs)
            {
                InputMap[input.parameter] = $"Internal/Remap {_inputIndex}";
                ++_inputIndex;
                controller.AddParameter(input.parameter, AnimatorControllerParameterType.Float);
                controller.AddParameter(InputMap[input.parameter], AnimatorControllerParameterType.Float);
            }

            controller.AddParameter(source.outputParameter, AnimatorControllerParameterType.Float);
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

        static void AddRemapLayer(ApproachSource source, AnimatorController controller)
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

            foreach (var input in source.inputs)
            {
                Vector2 outputRange;

                outputRange.x = 1 - input.approachRange.x;
                outputRange.y = 1 - input.approachRange.y;
                var motion = Remap(input.parameter, InputMap[input.parameter], input.inputRange, outputRange);
                motions.Add(motion);
            }

            var combine = Combine(motions.ToArray());

            var state = machine.AddState("Tree");
            state.motion = combine;
        }

        static void AddProductLayer(ApproachSource source, AnimatorController controller)
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

            root.AddChild(GetClip(source.outputParameter, source.outputRange.y));

            var product = CreateProductTree(
                GetClip(source.outputParameter, source.outputRange.x - source.outputRange.y),
                source.inputs.Select(input => InputMap[input.parameter]).ToArray());

            root.AddChild(product);

            SetOneParams(root);

            var state = machine.AddState("Tree");
            state.motion = root;
        }
    }
}