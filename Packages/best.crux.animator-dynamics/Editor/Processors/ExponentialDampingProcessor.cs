using System.Collections.Generic;
using System.Linq;
using Crux.AnimatorDynamics.Runtime.Models;
using Crux.AnimatorDynamics.Runtime.Models.ExponentialDamping;
using Crux.ProceduralController.Editor;
using Crux.ProceduralController.Editor.Processors;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Crux.AnimatorDynamics.Editor.Processors
{
    [UsedImplicitly]
    public class ExponentialDampingProcessor : Processor<ExponentialDampingModel>
    {
        private ExponentialDampingDataV1 data;

        private AnimatorController controller;

        private readonly Dictionary<string, string> inputMap = new();
        private int inputIndex;

        public override void Process(Context context)
        {
            if (!model.data.TryUpgradeTo(out data))
                return;

            RemapDataV1 remapData = new()
            {
                inputs = data.inputs.Select(input => new RemapDataV1.InputItem
                {
                    parameter = input.parameter,
                    inputRange = input.inputRange,
                    outputRange = input.outputRange
                }).ToList(),
                output = context.MakeParam("Target")
            };

            RemapModel remapModel = ScriptableObject.CreateInstance<RemapModel>();
            remapModel.data = remapData;

            foreach (var processor in ProcessorLocator.GetProcessors(remapModel))
                processor.Process(context);

            inputMap.Clear();
            inputIndex = 0;

            controller = new AnimatorController();

            AddParameters(context);
            CreateCalcLayer(context);
            CreateBlendLayer(context);

            context.receiver.AddController(controller);
            context.receiver.AddGlobalParameter(data.deltaTimeParameter);
        }

        void AddParameters(Context context)
        {
            foreach (var input in data.inputs)
            {
                controller.AddParameter(input.parameter, AnimatorControllerParameterType.Float);
            }

            controller.AddParameter("One", AnimatorControllerParameterType.Float);
            controller.AddParameter(context.MakeParam("Factor"), AnimatorControllerParameterType.Float);
            controller.AddParameter(context.MakeParam("Target"), AnimatorControllerParameterType.Float);
            controller.AddParameter(data.outputParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(data.deltaTimeParameter, AnimatorControllerParameterType.Float);
        }

        void CreateCalcLayer(Context context)
        {
            var machine = new AnimatorStateMachine
            {
                name = "Exponential Calculation Layer"
            };

            var layer = new AnimatorControllerLayer
            {
                name = machine.name,
                defaultWeight = 1f,
                stateMachine = machine
            };

            controller.AddLayer(layer);

            var state = machine.AddState("Calculate Factor");

            var clip = new AnimationClip();
            var curve = new AnimationCurve();

            for (int step = 0; step < 16; ++step)
            {
                float time = Mathf.Pow(0.5f, step);
                float value = 1 - Mathf.Exp(-data.dampingFactor * time);

                curve.AddKey(time, value);
            }

            clip.SetCurve("", typeof(Animator), context.MakeParam("Factor"), curve);
            state.motion = clip;
            state.timeParameterActive = true;
            state.timeParameter = data.deltaTimeParameter;
        }

        void CreateBlendLayer(Context context)
        {
            var machine = new AnimatorStateMachine
            {
                name = "Exponential Damping Layer"
            };

            var layer = new AnimatorControllerLayer
            {
                name = "Exponential Damping Layer",
                defaultWeight = 1f,
                stateMachine = machine
            };

            controller.AddLayer(layer);

            var state = machine.AddState("Blend");

            var root = new BlendTree
            {
                name = "Root Blend Tree",
                blendType = BlendTreeType.Simple1D,
                blendParameter = context.MakeParam("Factor")
            };

            state.motion = root;

            var currentTree = new BlendTree
            {
                name = "Current Tree",
                blendType = BlendTreeType.Simple1D,
                blendParameter = data.outputParameter,
                useAutomaticThresholds = false
            };

            root.AddChild(currentTree);

            var targetTree = new BlendTree
            {
                name = "Target Tree",
                blendType = BlendTreeType.Simple1D,
                blendParameter = context.MakeParam("Target"),
                useAutomaticThresholds = false
            };

            root.AddChild(targetTree);

            currentTree.AddChild(AnimatorMath.GetClip(data.outputParameter, data.outputRange.x));
            currentTree.AddChild(AnimatorMath.GetClip(data.outputParameter, data.outputRange.y));

            targetTree.AddChild(AnimatorMath.GetClip(data.outputParameter, data.outputRange.x));
            targetTree.AddChild(AnimatorMath.GetClip(data.outputParameter, data.outputRange.y));

            var children = currentTree.children;

            children[0].threshold = data.outputRange.x;
            children[1].threshold = data.outputRange.y;

            currentTree.children = children;
            children = targetTree.children;

            children[0].threshold = data.outputRange.x;
            children[1].threshold = data.outputRange.y;

            targetTree.children = children;
        }
    }
}