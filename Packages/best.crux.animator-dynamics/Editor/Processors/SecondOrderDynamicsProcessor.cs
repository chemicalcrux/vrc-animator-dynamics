using Crux.AnimatorDynamics.Runtime.Models;
using Crux.AnimatorDynamics.Runtime.Models.SecondOrderDynamics;
using Crux.ProceduralController.Editor;
using Crux.ProceduralController.Editor.Processors;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;

namespace Crux.AnimatorDynamics.Editor.Processors
{
    [UsedImplicitly]
    public class SecondOrderDynamicsProcessor : Processor<SecondOrderDynamicsModel>
    {
        private SecondOrderDynamicsDataV1 data;
        
        private AnimatorController controller;
        private float k1, k2, k3, tCrit;

        public override void Process(Context context)
        {
            if (!model.data.TryUpgradeTo(out data))
                return;
            
            controller = new();

            CalculateConstants();
            AddParameters();
            BuildTree();

            context.receiver.AddController(controller);
            
            context.receiver.AddGlobalParameter(data.deltaTimeParameter);
            context.receiver.AddGlobalParameter(data.deltaTimeInverseParameter);
        }

        private void AddParameters()
        {
            controller.AddParameter("One", AnimatorControllerParameterType.Float);
            controller.AddParameter(data.outputParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(data.deltaTimeParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(data.deltaTimeParameter + "sqr", AnimatorControllerParameterType.Float);
            controller.AddParameter(data.deltaTimeInverseParameter, AnimatorControllerParameterType.Float);

            controller.AddParameter("k1", AnimatorControllerParameterType.Float);
            controller.AddParameter("k2_inv", AnimatorControllerParameterType.Float);
            controller.AddParameter("k3", AnimatorControllerParameterType.Float);

            controller.AddParameter("tCrit", AnimatorControllerParameterType.Float);

            controller.AddParameter("x", AnimatorControllerParameterType.Float);
            controller.AddParameter("xd", AnimatorControllerParameterType.Float);
            controller.AddParameter("xp", AnimatorControllerParameterType.Float);

            controller.AddParameter("yd", AnimatorControllerParameterType.Float);
            controller.AddParameter("yd_neg", AnimatorControllerParameterType.Float);

            controller.AddParameter("Fix yd", AnimatorControllerParameterType.Float);
            controller.AddParameter("Fix yd_neg", AnimatorControllerParameterType.Float);

            var parameters = controller.parameters;

            foreach (AnimatorControllerParameter parameter in parameters)
            {
                if (parameter.name == data.deltaTimeParameter)
                {
                    parameter.defaultFloat = 0.2f;
                }

                if (parameter.name == data.deltaTimeParameter + "sqr")
                    parameter.defaultFloat = 0.2f * 0.2f;

                if (parameter.name == data.deltaTimeInverseParameter)
                    parameter.defaultFloat = 5f;

                parameter.defaultFloat = parameter.name switch
                {
                    "One" => 1f,
                    "k1" => k1,
                    "k2_inv" => 1 / k2,
                    "k3" => k3,
                    "tCrit" => tCrit,
                    "x" => data.x0,
                    _ => parameter.defaultFloat
                };

                if (parameter.name == data.outputParameter)
                    parameter.defaultFloat = data.x0;
            }

            controller.parameters = parameters;
        }

        private void CalculateConstants()
        {
            k1 = data.z / (Mathf.PI * data.f);
            k2 = 1 / (2 * Mathf.PI * data.f * (2 * Mathf.PI * data.f));
            k3 = data.r * data.z / (2 * Mathf.PI * data.f);

            tCrit = 0.8f * (Mathf.Sqrt(4 * k2 + k1 * k1) - k1);
        }

        private void BuildTree()
        {
            var machine = new AnimatorStateMachine
            {
                name = "Integrator",
                hideFlags = HideFlags.HideInHierarchy
            };

            var layer = new AnimatorControllerLayer
            {
                name = "Integrator",
                defaultWeight = 1f,
                stateMachine = machine
            };

            controller.AddLayer(layer);

            var rootTree = new BlendTree
            {
                name = "Root",
                blendType = BlendTreeType.Direct,
                hideFlags = HideFlags.HideInHierarchy
            };

            var moveTree = CreateMoveTree();
            rootTree.AddChild(moveTree);

            var copyXTree = AnimatorMath.Copy("x", "xp");
            rootTree.AddChild(copyXTree);

            var copyYDTree = AnimatorMath.Copy("yd", "yd");
            rootTree.AddChild(copyYDTree);

            var inputTree = CreateInputTree("x", true);
            rootTree.AddChild(inputTree);

            var velocityTree = CreateVelocityTree();
            rootTree.AddChild(velocityTree);

            var positiveResult = AnimatorMath.Remap(data.deltaTimeParameter, "yd", new Vector2(0, tCrit),
                new Vector2(0, 1000 * tCrit));
            var negativeResult = AnimatorMath.Remap(data.deltaTimeParameter, "yd", new Vector2(0, tCrit),
                new Vector2(0, -1000 * tCrit));

            var positiveResultDoubleTime = AnimatorMath.Create1DProductTree(
                AnimatorMath.Constant("yd", 1000 * tCrit * tCrit),
                AnimatorMath.Constant("yd", -1000 * tCrit * tCrit),
                (data.deltaTimeParameter, new Vector2(-tCrit, tCrit)),
                (data.deltaTimeParameter, new Vector2(-tCrit, tCrit))
            );

            var negativeResultDoubleTime = AnimatorMath.Create1DProductTree(
                AnimatorMath.Constant("yd", -1000 * tCrit * tCrit),
                AnimatorMath.Constant("yd", 1000 * tCrit * tCrit),
                (data.deltaTimeParameter, new Vector2(-tCrit, tCrit)),
                (data.deltaTimeParameter, new Vector2(-tCrit, tCrit))
            );

            var range1000 = new Vector2(-1000, 1000);

            var part1 = AnimatorMath.Create1DProductTree(positiveResult, negativeResult, ("x", range1000));
            part1 = AnimatorMath.CreateProductTree(part1, "k2_inv");

            var part2 = AnimatorMath.Create1DProductTree(positiveResult, negativeResult, ("xd", range1000));
            part2 = AnimatorMath.CreateProductTree(part2, "k3", "k2_inv");

            var part3 = AnimatorMath.Create1DProductTree(negativeResult, positiveResult,
                (data.outputParameter, range1000));
            part3 = AnimatorMath.CreateProductTree(part3, "k2_inv");

            var part4 = AnimatorMath.Create1DProductTree(negativeResultDoubleTime, positiveResultDoubleTime,
                ("yd", range1000));
            part4 = AnimatorMath.CreateProductTree(part4, "k2_inv");

            var part5 = AnimatorMath.Create1DProductTree(negativeResult, positiveResult, ("yd", range1000));
            part5 = AnimatorMath.CreateProductTree(part5, "k1", "k2_inv");

            rootTree.AddChild(part1);
            rootTree.AddChild(part2);
            rootTree.AddChild(part3);
            rootTree.AddChild(part4);
            rootTree.AddChild(part5);

            var children = rootTree.children;

            for (int i = 0; i < children.Length; ++i)
            {
                children[i].directBlendParameter = "One";
            }

            rootTree.children = children;

            var state = machine.AddState("Tree");
            state.motion = rootTree;
        }

        BlendTree CreateInputTree(string param, bool positive)
        {
            var inputRoot = new BlendTree
            {
                name = "Input",
                blendType = BlendTreeType.Direct,
                hideFlags = HideFlags.HideInHierarchy
            };

            foreach (var input in data.inputs)
            {
                var inputTree = new BlendTree
                {
                    name = "Add Input: " + input.parameter,
                    blendType = BlendTreeType.Simple1D,
                    blendParameter = input.parameter,
                    useAutomaticThresholds = false,
                    hideFlags = HideFlags.HideInHierarchy
                };

                controller.AddParameter(input.parameter, AnimatorControllerParameterType.Float);

                var lowerClip = AnimatorMath.GetClip(param, (positive ? 1 : -1) * input.outputRange.x);
                var upperClip = AnimatorMath.GetClip(param, (positive ? 1 : -1) * input.outputRange.y);

                inputTree.AddChild(lowerClip, input.inputRange.x);
                inputTree.AddChild(upperClip, input.inputRange.y);

                inputRoot.AddChild(inputTree);
            }

            var children = inputRoot.children;

            for (int i = 0; i < children.Length; ++i)
            {
                children[i].directBlendParameter = "One";
            }

            inputRoot.children = children;

            return inputRoot;
        }

        BlendTree CreateMoveTree()
        {
            var root = new BlendTree
            {
                name = "Move Root",
                blendType = BlendTreeType.Direct,
                hideFlags = HideFlags.HideInHierarchy
            };

            var keep = AnimatorMath.Copy(data.outputParameter, data.outputParameter);

            var change = new BlendTree
            {
                name = "Move",
                blendType = BlendTreeType.Direct,
                hideFlags = HideFlags.HideInHierarchy
            };

            var positive = AnimatorMath.Remap(data.deltaTimeParameter, data.outputParameter, new Vector2(0, tCrit),
                new Vector2(0, 100 * tCrit));
            var negative = AnimatorMath.Remap(data.deltaTimeParameter, data.outputParameter, new Vector2(0, tCrit),
                new Vector2(0, -100 * tCrit));

            var move = AnimatorMath.Create1DProductTree(positive, negative, ("yd", new Vector2(-100, 100)));

            change.AddChild(move);

            AnimatorMath.SetOneParams(change);

            root.AddChild(keep);
            root.AddChild(change);

            AnimatorMath.SetOneParams(root);

            return root;
        }

        BlendTree CreateVelocityTree()
        {
            var root = new BlendTree
            {
                name = "Velocity Root",
                blendType = BlendTreeType.Direct,
            };

            var subtract = AnimatorMath.Subtract("x", "xp", "xd");
            root.AddChild(subtract);

            var children = root.children;
            children[0].directBlendParameter = data.deltaTimeInverseParameter;
            root.children = children;

            return root;
        }
    }
}