using ChemicalCrux.AnimatorDynamics.Runtime.Sources;
using com.vrcfury.api;
using UnityEditor.Animations;
using UnityEngine;

using static ChemicalCrux.AnimatorDynamics.Editor.AnimatorMath;

namespace ChemicalCrux.AnimatorDynamics.Editor.Processors
{
    public class LinearMoveTowardsProcessor
    {
        public void Process(LinearMoveTowardsSource source, GameObject avatarRoot)
        {
            var controller = new AnimatorController();
            
            ChildMotion[] children = default;

            controller.AddParameter("One", AnimatorControllerParameterType.Float);
            controller.AddParameter("Delta", AnimatorControllerParameterType.Float);
            controller.AddParameter("Step Size", AnimatorControllerParameterType.Float);
            controller.AddParameter(source.outputParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(source.deltaTimeParameter, AnimatorControllerParameterType.Float);

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

                foreach (var input in source.inputs)
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
                        blendParameter = source.outputParameter,
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
                        blendParameter = source.outputParameter,
                        useAutomaticThresholds = false,
                        hideFlags = HideFlags.HideInHierarchy
                    };

                    var lowerClip = GetClip(source.outputParameter, source.outputRange.x);
                    var upperClip = GetClip(source.outputParameter, source.outputRange.y);

                    outputCopyTree.AddChild(lowerClip, source.outputRange.x);
                    outputCopyTree.AddChild(upperClip, source.outputRange.y);

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

                    var lowerClip = GetClip("Step Size", source.decreaseRate);
                    var middleClip = GetClip("Step Size", 0);
                    var upperClip = GetClip("Step Size", source.increaseRate);

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

                    var lowerClip = GetClip(source.outputParameter, -1);
                    var middleClip = GetClip(source.outputParameter, 0);
                    var upperClip = GetClip(source.outputParameter, 1);

                    linearBlendTree.AddChild(lowerClip, -0.1f);
                    linearBlendTree.AddChild(middleClip, 0);
                    linearBlendTree.AddChild(upperClip, 0.1f);

                    deltaTimeTree.AddChild(linearBlendTree);

                    children = deltaTimeTree.children;
                    children[0].directBlendParameter = source.deltaTimeParameter;
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

            var fc = FuryComponents.CreateFullController(source.gameObject);

            fc.AddController(controller);

            foreach (var input in source.inputs)
                fc.AddGlobalParam(input.parameter);

            fc.AddGlobalParam(source.outputParameter);
            
            fc.AddGlobalParam(source.deltaTimeParameter);
        }
    }
}