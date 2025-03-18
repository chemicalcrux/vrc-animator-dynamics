using ChemicalCrux.AnimatorDynamics.Runtime;
using ChemicalCrux.AnimatorDynamics.Runtime.Sources;
using com.vrcfury.api;
using UnityEditor.Animations;
using UnityEngine;

namespace ChemicalCrux.AnimatorDynamics.Editor.Processors
{
    public class SecondOrderDynamicsProcessor
    {
        public static void Process(SecondOrderDynamicsSource source, GameObject avatarRoot)
        {
            AnimatorController controller = new();

            float k1, k2, k3, tCrit;

            k1 = source.z / (Mathf.PI * source.f);
            k2 = 1 / (2 * Mathf.PI * source.f * (2 * Mathf.PI * source.f));
            k3 = source.r * source.z / (2 * Mathf.PI * source.f);

            tCrit = 0.8f * (Mathf.Sqrt(4 * k2 + k1 * k1) - k1);

            controller.AddParameter("One", AnimatorControllerParameterType.Float);
            controller.AddParameter(source.outputParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(source.deltaTimeParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(source.deltaTimeParameter + "sqr", AnimatorControllerParameterType.Float);
            controller.AddParameter(source.deltaTimeInverseParameter, AnimatorControllerParameterType.Float);

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
                if (parameter.name == source.deltaTimeParameter)
                {
                    parameter.defaultFloat = 0.2f;
                }
                if (parameter.name == source.deltaTimeParameter + "sqr")
                {
                    parameter.defaultFloat = 0.2f * 0.2f;
                }

                if (parameter.name == source.deltaTimeInverseParameter)
                {
                    parameter.defaultFloat = 5f;
                }

                if (parameter.name == "One")
                {
                    parameter.defaultFloat = 1f;
                }

                if (parameter.name == "k1")
                {
                    parameter.defaultFloat = k1;
                }

                if (parameter.name == "k2_inv")
                {
                    parameter.defaultFloat = 1 / k2;
                }

                if (parameter.name == "k3")
                {
                    parameter.defaultFloat = k3;
                }

                if (parameter.name == "tCrit")
                {
                    parameter.defaultFloat = tCrit;
                }

                if (parameter.name == "x")
                {
                    parameter.defaultFloat = source.x0;
                }

                if (parameter.name == source.outputParameter)
                {
                    parameter.defaultFloat = source.x0;
                }
            }

            controller.parameters = parameters;

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

            var moveTree = CreateMoveTree(controller, source, tCrit);
            rootTree.AddChild(moveTree);

            var copyXTree = AnimatorMath.Copy("x", "xp");
            rootTree.AddChild(copyXTree);

            var copyYDTree = AnimatorMath.Copy("yd", "yd");
            rootTree.AddChild(copyYDTree);

            var inputTree = CreateInputTree(controller, source, "x", true);
            rootTree.AddChild(inputTree);

            var velocityTree = CreateVelocityTree(source);
            rootTree.AddChild(velocityTree);

            var positiveResult = AnimatorMath.Remap(source.deltaTimeParameter, "yd", new Vector2(0, tCrit),
                new Vector2(0, 1000 * tCrit));
            var negativeResult = AnimatorMath.Remap(source.deltaTimeParameter, "yd", new Vector2(0, tCrit),
                new Vector2(0, -1000 * tCrit));

            var positiveResultDoubleTime = AnimatorMath.Create1DProductTree(
                AnimatorMath.Constant("yd", 1000 * tCrit * tCrit),
                AnimatorMath.Constant("yd", -1000 * tCrit * tCrit),
                (source.deltaTimeParameter, new Vector2(-tCrit, tCrit)),
                (source.deltaTimeParameter, new Vector2(-tCrit, tCrit))
            );

            var negativeResultDoubleTime = AnimatorMath.Create1DProductTree(
                AnimatorMath.Constant("yd", -1000 * tCrit * tCrit),
                AnimatorMath.Constant("yd", 1000 * tCrit * tCrit),
                (source.deltaTimeParameter, new Vector2(-tCrit, tCrit)),
                (source.deltaTimeParameter, new Vector2(-tCrit, tCrit))
            );
            
            var range1000 = new Vector2(-1000, 1000);

            var part1 = AnimatorMath.Create1DProductTree(positiveResult, negativeResult, ("x", range1000));
            part1 = AnimatorMath.CreateProductTree(part1, "k2_inv");

            var part2 = AnimatorMath.Create1DProductTree(positiveResult, negativeResult, ("xd", range1000));
            part2 = AnimatorMath.CreateProductTree(part2, "k3", "k2_inv");

            var part3 = AnimatorMath.Create1DProductTree(negativeResult, positiveResult,
                (source.outputParameter, range1000));
            part3 = AnimatorMath.CreateProductTree(part3, "k2_inv");
            
            var part4 = AnimatorMath.Create1DProductTree(negativeResultDoubleTime, positiveResultDoubleTime, ("yd", range1000));
            part4 = AnimatorMath.CreateProductTree(part4, "k2_inv");
            
            var part5 = AnimatorMath.Create1DProductTree(negativeResult, positiveResult, ("yd", range1000));
            part5 = AnimatorMath.CreateProductTree(part5, "k1", "k2_inv");
            
            rootTree.AddChild(part1);
            rootTree.AddChild(part2);
            rootTree.AddChild(part3);
            rootTree.AddChild(part4);
            rootTree.AddChild(part5);
            //
            // this does not correspond to any part of the equation -- but we need to compensate for
            // how the addition to y doesn't happen until the next frame!

            // var part5 = CreateCorrectionTree(source, tCrit, "yd", -1, "k2_inv", "yd");
            // rootTree.AddChild(part5);
            // part5 = CreateCorrectionTree(source, tCrit, "yd_neg", 1, "k2_inv", "yd_neg");
            // rootTree.AddChild(part5);

            // var part5 = AnimatorMath.CreateProductTree(positiveResult, "yd",
            //     source.deltaTimeParameter + "sqr");
            //
            // rootTree.AddChild(part5);
            //
            // part5 = AnimatorMath.CreateProductTree(negativeResult, "yd_neg",
            //     source.deltaTimeParameter + "sqr");
            //
            // rootTree.AddChild(part5);

            var children = rootTree.children;

            for (int i = 0; i < children.Length; ++i)
            {
                children[i].directBlendParameter = "One";
            }

            rootTree.children = children;

            var state = machine.AddState("Tree");
            state.motion = rootTree;

            var fc = FuryComponents.CreateFullController(source.gameObject);

            fc.AddController(controller);

            foreach (var input in source.inputs)
            {
                fc.AddGlobalParam(input.parameter);
            }

            fc.AddGlobalParam(source.outputParameter);
            fc.AddGlobalParam(source.deltaTimeParameter);
            fc.AddGlobalParam(source.deltaTimeInverseParameter);
        }

        static BlendTree CreateInputTree(AnimatorController controller, SecondOrderDynamicsSource source, string param,
            bool positive)
        {
            var inputRoot = new BlendTree
            {
                name = "Input",
                blendType = BlendTreeType.Direct,
                hideFlags = HideFlags.HideInHierarchy
            };

            foreach (var input in source.inputs)
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

        static BlendTree CreateMoveTree(AnimatorController controller, SecondOrderDynamicsSource source, float tCrit)
        {
            var root = new BlendTree
            {
                name = "Move Root",
                blendType = BlendTreeType.Direct,
                hideFlags = HideFlags.HideInHierarchy
            };

            var keep = AnimatorMath.Copy(source.outputParameter, source.outputParameter);

            var change = new BlendTree
            {
                name = "Move",
                blendType = BlendTreeType.Direct,
                hideFlags = HideFlags.HideInHierarchy
            };

            var positive = AnimatorMath.Remap(source.deltaTimeParameter, source.outputParameter, new Vector2(0, tCrit),
                new Vector2(0, 100 * tCrit));
            var negative = AnimatorMath.Remap(source.deltaTimeParameter, source.outputParameter, new Vector2(0, tCrit),
                new Vector2(0, -100 * tCrit));

            var move = AnimatorMath.Create1DProductTree(positive, negative, ("yd", new Vector2(-100, 100)));

            change.AddChild(move);

            AnimatorMath.SetOneParams(change);

            root.AddChild(keep);
            root.AddChild(change);

            AnimatorMath.SetOneParams(root);

            return root;
        }

        static BlendTree CreateVelocityTree(SecondOrderDynamicsSource source)
        {
            var root = new BlendTree
            {
                name = "Velocity Root",
                blendType = BlendTreeType.Direct,
            };

            var subtract = AnimatorMath.Subtract("x", "xp", "xd");
            root.AddChild(subtract);

            var children = root.children;
            children[0].directBlendParameter = source.deltaTimeInverseParameter;
            root.children = children;

            return root;
        }
    }
}