using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor.Animations;
using UnityEngine;

namespace ChemicalCrux.AnimatorDynamics.Editor
{
    [PublicAPI]
    public static class AnimatorMath
    {
        public static void Reset()
        {
            Caches.Clear();
        }
        public static readonly Dictionary<string, Dictionary<float, AnimationClip>> Caches = new();
        
        public static AnimationClip GetClip(string param, float value)
        {
            if (!Caches.TryGetValue(param, out var cache))
            {
                cache = new();
                Caches[param] = cache;
            }
    
            if (cache.TryGetValue(value, out var clip))
            {
                return clip;
            }

            clip = new AnimationClip
            {
                name = $"{param}: {value:N3}",
                hideFlags = HideFlags.HideInHierarchy
            };

            clip.SetCurve("", typeof(Animator), param, AnimationCurve.Constant(0f, 1f, value));
    
            cache[value] = clip;

            return clip;
        }

        public static Motion Combine(params Motion[] motions)
        {
            var tree = new BlendTree
            {
                name = "Combine",
                blendType = BlendTreeType.Direct,
                hideFlags = HideFlags.HideInHierarchy
            };

            foreach (var motion in motions)
            {
                tree.AddChild(motion);
            }

            var children = tree.children;

            for (int i = 0; i < children.Length; ++i)
            {
                children[i].directBlendParameter = "One";
            }

            tree.children = children;

            return tree;
        }

        public static BlendTree Constant(string outParam, float value)
        {
            var tree = new BlendTree
            {
                name = "Constant",
                blendType = BlendTreeType.Direct
            };

            tree.AddChild(GetClip(outParam, value));

            SetOneParams(tree);

            return tree;
        }
        
        public static BlendTree Subtract(string lhsParam, string rhsParam, string outParam)
        {
            var subtractTree = new BlendTree
            {
                name = "Subtraction",
                blendType = BlendTreeType.Direct,
                hideFlags = HideFlags.HideInHierarchy
            };

            var subtractLeft = new BlendTree
            {
                name = "LHS",
                blendType = BlendTreeType.Simple1D,
                blendParameter = lhsParam,
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy
            };

            var subtractRight = new BlendTree
            {
                name = "RHS",
                blendType = BlendTreeType.Simple1D,
                blendParameter = rhsParam,
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy
            };

            subtractLeft.AddChild(GetClip(outParam, -100));
            subtractLeft.AddChild(GetClip(outParam, 100));

            var children = subtractLeft.children;

            children[0].threshold = -100;
            children[1].threshold = 100;

            subtractLeft.children = children;

            subtractRight.AddChild(GetClip(outParam, 100));
            subtractRight.AddChild(GetClip(outParam, -100));

            children = subtractRight.children;

            children[0].threshold = -100;
            children[1].threshold = 100;

            subtractRight.children = children;

            subtractTree.AddChild(subtractLeft);
            subtractTree.AddChild(subtractRight);

            children = subtractTree.children;
            children[0].directBlendParameter = "One";
            children[1].directBlendParameter = "One";
            subtractTree.children = children;

            return subtractTree;
        }

        public static BlendTree Remap(string inputParam, string outputParam, Vector2 inputRange, Vector2 outputRange)
        {
            ChildMotion[] children;

            var root = new BlendTree
            {
                name = "Remap",
                blendType = BlendTreeType.Simple1D,
                blendParameter = inputParam,
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy
            };

            root.AddChild(GetClip(outputParam, outputRange.x));
            root.AddChild(GetClip(outputParam, outputRange.y));

            children = root.children;
            children[0].threshold = inputRange.x;
            children[1].threshold = inputRange.y;
            root.children = children;

            return root;
        }

        public static BlendTree CreateProductTree(Motion tip, params string[] parameters)
        {
            BlendTree leaf = default;

            for (int i = parameters.Length - 1; i >= 0; --i)
            {
                BlendTree tree = new BlendTree
                {
                    name = "Product",
                    blendType = BlendTreeType.Direct,
                    hideFlags = HideFlags.HideInHierarchy
                };

                if (i == parameters.Length - 1)
                {
                    tree.AddChild(tip);
                }
                else
                {
                    tree.AddChild(leaf);
                }

                var children = tree.children;
                children[0].directBlendParameter = parameters[i];
                tree.children = children;

                leaf = tree;
            }

            return leaf;
        }

        public static BlendTree Create1DProductTree(Motion positiveTip, Motion negativeTip,
            params (string param, Vector2 range)[] parts)
        {
            var tree = new BlendTree
            {
                name = "1D Product: " + parts[0].param,
                blendType = BlendTreeType.Simple1D,
                blendParameter = parts[0].param,
                useAutomaticThresholds = false
            };

            if (parts.Length == 1)
            {
                tree.AddChild(negativeTip);
                tree.AddChild(positiveTip);
            }
            else
            {
                var childArray = parts.Skip(1).ToArray();

                tree.AddChild(Create1DProductTree(negativeTip, positiveTip, childArray));
                tree.AddChild(Create1DProductTree(positiveTip, negativeTip, childArray));
            }

            var children = tree.children;

            children[0].threshold = parts[0].range.x;
            children[1].threshold = parts[0].range.y;

            tree.children = children;

            return tree;
        }

        public static void SetOneParams(BlendTree tree)
        {
            var children = tree.children;

            for (int i = 0; i < children.Length; ++i)
                children[i].directBlendParameter = "One";

            tree.children = children;
        }

        public static BlendTree Copy(string from, string to)
        {
            var copyTree = new BlendTree
            {
                name = $"Copy {from} to {to}",
                blendType = BlendTreeType.Simple1D,
                blendParameter = from,
                useAutomaticThresholds = false,
                hideFlags = HideFlags.HideInHierarchy
            };
            
            copyTree.AddChild(GetClip(to, -100));
            copyTree.AddChild(GetClip(to, 100));

            var children = copyTree.children;
            children[0].threshold = -100;
            children[1].threshold = 100;
            copyTree.children = children;

            return copyTree;
        }
    }
}