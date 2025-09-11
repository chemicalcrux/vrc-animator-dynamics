using UnityEngine;
using VRC.SDKBase.Editor.BuildPipeline;

namespace Crux.AnimatorDynamics.Editor
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