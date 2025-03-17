using ChemicalCrux.AnimatorDynamics.Runtime;
using com.vrcfury.api;
using UnityEditor.Animations;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace ChemicalCrux.AnimatorDynamics.Editor
{
    public class SecondOrderDynamicsProcessor : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => -10001;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            AnimatorMath.Reset();
            
            foreach (var source in avatarGameObject.GetComponentsInChildren<SecondOrderDynamicsSource>(true))
                Process(source, avatarGameObject);

            return true;
        }

        private static void Process(SecondOrderDynamicsSource source, GameObject avatarRoot)
        {
            AnimatorController controller = new();

            float k1, k2, k3, tCrit;

            k1 = source.z / (Mathf.PI * source.f);
            k2 = 1 / (2 * Mathf.PI * source.f * (2 * Mathf.PI * source.f));
            k3 = source.r * source.z / (2 * Mathf.PI * source.f);

            tCrit = 0.8f * (Mathf.Sqrt(4 * k2 + k1 * k1) - k1);

            controller.AddParameter("One", AnimatorControllerParameterType.Float);
            controller.AddParameter("Delta", AnimatorControllerParameterType.Float);
            controller.AddParameter(source.outputParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(source.deltaTimeParameter, AnimatorControllerParameterType.Float);
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

            var parameters = controller.parameters;

            foreach (AnimatorControllerParameter parameter in parameters)
            {
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

                if (parameter.name == "x0")
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

            var moveTree = CreateMoveTree(controller, source);
            rootTree.AddChild(moveTree);

            var copyXTree = AnimatorMath.Copy("x", "xp");
            rootTree.AddChild(copyXTree);

            var inputTree = CreateInputTree(controller, source, "x", true);
            rootTree.AddChild(inputTree);

            var velocityTree = CreateVelocityTree(controller, source);
            rootTree.AddChild(velocityTree);

            var copyYDTree = AnimatorMath.Copy("yd", "yd");
            var copyYDNegTree = AnimatorMath.Copy("yd_neg", "yd_neg");

            rootTree.AddChild(copyYDTree);
            rootTree.AddChild(copyYDNegTree);

            var positiveYD = AnimatorMath.Remap(source.deltaTimeParameter, "yd", new Vector2(0, tCrit),
                new Vector2(0, tCrit));
            var positiveYDNeg = AnimatorMath.Remap(source.deltaTimeParameter, "yd_neg", new Vector2(0, tCrit),
                new Vector2(0, -tCrit));

            var negativeYD = AnimatorMath.Remap(source.deltaTimeParameter, "yd", new Vector2(0, tCrit),
                new Vector2(0, -tCrit));
            var negativeYDNeg = AnimatorMath.Remap(source.deltaTimeParameter, "yd_neg", new Vector2(0, tCrit),
                new Vector2(0, tCrit));

            var positiveResult = AnimatorMath.Combine(positiveYD, positiveYDNeg);
            var negativeResult = AnimatorMath.Combine(negativeYD, negativeYDNeg);

            var part1 = AnimatorMath.CreateProductTree(positiveResult, "k2_inv", "x");
            var part2 = AnimatorMath.CreateProductTree(positiveResult, "k3",
                "xd", "k2_inv");
            var part3 = AnimatorMath.CreateProductTree(negativeResult, source.outputParameter, "k2_inv");
            var part4 = AnimatorMath.CreateProductTree(negativeResult, "k1",
                "yd", "k2_inv");

            rootTree.AddChild(part1);
            rootTree.AddChild(part2);
            rootTree.AddChild(part3);
            rootTree.AddChild(part4);

            part4 = AnimatorMath.CreateProductTree(positiveResult, "k1",
                "yd_neg", "k2_inv");

            rootTree.AddChild(part4);

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

        static BlendTree CreateMoveTree(AnimatorController controller, SecondOrderDynamicsSource source)
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

            var negative = AnimatorMath.CreateProductTree(AnimatorMath.GetClip(source.outputParameter, -1),
                source.deltaTimeParameter, "yd_neg");
            var positive = AnimatorMath.CreateProductTree(AnimatorMath.GetClip(source.outputParameter, 1),
                source.deltaTimeParameter, "yd");

            change.AddChild(negative);
            change.AddChild(positive);

            var children = change.children;

            children[0].directBlendParameter = "One";
            children[1].directBlendParameter = "One";

            change.children = children;

            root.AddChild(keep);
            root.AddChild(change);

            children = root.children;

            children[0].directBlendParameter = "One";
            children[1].directBlendParameter = "One";

            root.children = children;

            return root;
        }

        static BlendTree CreateVelocityTree(AnimatorController controller, SecondOrderDynamicsSource source)
        {
            var root = new BlendTree
            {
                name = "Velocity Root",
                blendType = BlendTreeType.Direct,
                hideFlags = HideFlags.HideInHierarchy
            };

            var subtract = AnimatorMath.Subtract("x", "xp", "xd");
            root.AddChild(subtract);

            var children = root.children;
            children[0].directBlendParameter = source.deltaTimeParameter;
            root.children = children;

            return root;
        }
    }
}