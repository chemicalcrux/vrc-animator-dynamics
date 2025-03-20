using ChemicalCrux.AnimatorDynamics.Editor.Processors;
using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace ChemicalCrux.AnimatorDynamics.Editor
{
    public class AnimatorDynamicsPreprocessor : IVRCSDKPreprocessAvatarCallback
    {
        public int callbackOrder => -10001;

        public bool OnPreprocessAvatar(GameObject avatarGameObject)
        {
            AnimatorMath.Reset();

            return true;
        }
    }
}