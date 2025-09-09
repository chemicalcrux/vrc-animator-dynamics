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
    public class RemapProcessor : Processor<RemapModel>
    {
        public override void Process(Context context)
        {
            var controller = new AnimatorController();
            
            ChildMotion[] children = default;

            controller.AddParameter("One", AnimatorControllerParameterType.Float);
            controller.AddParameter(model.output, AnimatorControllerParameterType.Float);

            var parameters = controller.parameters;

            foreach (AnimatorControllerParameter parameter in parameters)
            {
                if (parameter.name == "One")
                {
                    parameter.defaultFloat = 1f;
                }
            }

            controller.parameters = parameters;
            {
                var stateMachine = new AnimatorStateMachine
                {
                    name = "Remap",
                    hideFlags = HideFlags.HideInHierarchy
                };

                var layer = new AnimatorControllerLayer
                {
                    name = "Remap",
                    stateMachine = stateMachine,
                    defaultWeight = 1f,
                };

                controller.AddLayer(layer);

                var state = stateMachine.AddState("Blend");

                var root = new BlendTree
                {
                    name = "Root",
                    blendType = BlendTreeType.Direct,
                    hideFlags = HideFlags.HideInHierarchy
                };

                state.motion = root;

                foreach (var input in model.inputs)
                {
                    var inputTree = new BlendTree()
                    {
                        name = "Remap: " + input.parameter,
                        blendType = BlendTreeType.Simple1D,
                        blendParameter = input.parameter,
                        useAutomaticThresholds = false,
                        hideFlags = HideFlags.HideInHierarchy
                    };

                    controller.AddParameter(input.parameter, AnimatorControllerParameterType.Float);

                    // Motions must be added in threshold order. How annoying.
                    var motions = Enumerable.Empty<(Motion Motion, float threshold)>()
                        .Append((GetClip(model.output, input.outputRange.x), input.inputRange.x))
                        .Append((GetClip(model.output, input.outputRange.y), input.inputRange.y))
                        .OrderBy(item => item.threshold);

                    foreach (var (motion, threshold) in motions)
                    {
                        inputTree.AddChild(motion, threshold);
                    }

                    root.AddChild(inputTree);
                }

                SetOneParams(root);
            }

            context.receiver.AddController(controller);
        }
    }
}