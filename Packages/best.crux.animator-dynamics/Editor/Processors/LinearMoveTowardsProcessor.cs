using Crux.ProceduralController.Editor;
using Crux.ProceduralController.Editor.Processors;
using com.vrcfury.api;
using Crux.AnimatorDynamics.Runtime.Models;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;

using static ChemicalCrux.AnimatorDynamics.Editor.AnimatorMath;

namespace ChemicalCrux.AnimatorDynamics.Editor.Processors
{
    [UsedImplicitly]
    public class LinearMoveTowardsProcessor : Processor<LinearMoveTowardsModel>
    {
        public override void Process(Context context)
        {
            var controller = new AnimatorController();
            
            ChildMotion[] children = default;

            controller.AddParameter("One", AnimatorControllerParameterType.Float);
            controller.AddParameter("Delta", AnimatorControllerParameterType.Float);
            controller.AddParameter("Step Size", AnimatorControllerParameterType.Float);
            controller.AddParameter(model.outputParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(model.deltaTimeParameter, AnimatorControllerParameterType.Float);

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
                    name = "Linear Blend",
                    hideFlags = HideFlags.HideInHierarchy
                };

                var layer = new AnimatorControllerLayer
                {
                    name = "Linear Blend",
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

                var deltaInputTree = new BlendTree
                {
                    name = "Add Input to Delta",
                    blendType = BlendTreeType.Direct,
                    hideFlags = HideFlags.HideInHierarchy
                };

                foreach (var input in model.inputs)
                {
                    var inputTree = new BlendTree()
                    {
                        name = "Add Input: " + input.parameter,
                        blendType = BlendTreeType.Simple1D,
                        blendParameter = input.parameter,
                        useAutomaticThresholds = false,
                        hideFlags = HideFlags.HideInHierarchy
                    };

                    controller.AddParameter(input.parameter, AnimatorControllerParameterType.Float);

                    var lowerClip = GetClip("Delta", input.outputRange.x);
                    var upperClip = GetClip("Delta", input.outputRange.y);

                    inputTree.AddChild(lowerClip, input.inputRange.x);
                    inputTree.AddChild(upperClip, input.inputRange.y);

                    deltaInputTree.AddChild(inputTree);
                }

                root.AddChild(deltaInputTree);

                {
                    children = deltaInputTree.children;

                    for (int i = 0; i < children.Length; ++i)
                    {
                        children[i].directBlendParameter = "One";
                    }

                    deltaInputTree.children = children;
                }

                {
                    var deltaOutputTree = new BlendTree
                    {
                        name = "Subtract Output",
                        blendType = BlendTreeType.Simple1D,
                        blendParameter = model.outputParameter,
                        useAutomaticThresholds = false,
                        hideFlags = HideFlags.HideInHierarchy
                    };

                    var lowerClip = GetClip("Delta", 100);
                    var upperClip = GetClip("Delta", -100);

                    deltaOutputTree.AddChild(lowerClip, -100);
                    deltaOutputTree.AddChild(upperClip, 100);

                    root.AddChild(deltaOutputTree);
                }

                {
                    var outputCopyTree = new BlendTree
                    {
                        name = "Copy Output",
                        blendType = BlendTreeType.Simple1D,
                        blendParameter = model.outputParameter,
                        useAutomaticThresholds = false,
                        hideFlags = HideFlags.HideInHierarchy
                    };

                    var lowerClip = GetClip(model.outputParameter, model.outputRange.x);
                    var upperClip = GetClip(model.outputParameter, model.outputRange.y);

                    outputCopyTree.AddChild(lowerClip, model.outputRange.x);
                    outputCopyTree.AddChild(upperClip, model.outputRange.y);

                    root.AddChild(outputCopyTree);
                }
                {
                    var stepSizeTree = new BlendTree
                    {
                        name = "Step Size",
                        blendType = BlendTreeType.Simple1D,
                        blendParameter = "Delta",
                        useAutomaticThresholds = false,
                        hideFlags = HideFlags.HideInHierarchy
                    };

                    var lowerClip = GetClip("Step Size", model.decreaseRate);
                    var middleClip = GetClip("Step Size", 0);
                    var upperClip = GetClip("Step Size", model.increaseRate);

                    stepSizeTree.AddChild(lowerClip, -0.01f);
                    stepSizeTree.AddChild(middleClip, 0f);
                    stepSizeTree.AddChild(upperClip, 0.01f);

                    root.AddChild(stepSizeTree);
                }

                {
                    var deltaTimeTree = new BlendTree
                    {
                        name = "Apply Delta Time",
                        blendType = BlendTreeType.Direct,
                        hideFlags = HideFlags.HideInHierarchy
                    };

                    var linearBlendTree = new BlendTree
                    {
                        name = "Linear Blend",
                        blendType = BlendTreeType.Simple1D,
                        blendParameter = "Delta",
                        useAutomaticThresholds = false,
                        hideFlags = HideFlags.HideInHierarchy
                    };

                    var lowerClip = GetClip(model.outputParameter, -1);
                    var middleClip = GetClip(model.outputParameter, 0);
                    var upperClip = GetClip(model.outputParameter, 1);

                    linearBlendTree.AddChild(lowerClip, -0.1f);
                    linearBlendTree.AddChild(middleClip, 0);
                    linearBlendTree.AddChild(upperClip, 0.1f);

                    deltaTimeTree.AddChild(linearBlendTree);

                    children = deltaTimeTree.children;
                    children[0].directBlendParameter = model.deltaTimeParameter;
                    deltaTimeTree.children = children;

                    root.AddChild(deltaTimeTree);
                }

                children = root.children;

                children[0].directBlendParameter = "One";
                children[1].directBlendParameter = "One";
                children[2].directBlendParameter = "One";
                children[3].directBlendParameter = "One";
                children[4].directBlendParameter = "Step Size";

                root.children = children;
            }

            context.receiver.AddController(controller);

            context.receiver.AddGlobalParameter(model.deltaTimeParameter);
        }
    }
}