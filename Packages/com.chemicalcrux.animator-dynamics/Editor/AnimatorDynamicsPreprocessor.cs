using ChemicalCrux.AnimatorDynamics.Editor.Processors;
using ChemicalCrux.AnimatorDynamics.Runtime;
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

            foreach (var source in avatarGameObject.GetComponentsInChildren<SecondOrderDynamicsSource>(true))
                SecondOrderDynamicsProcessor.Process(source, avatarGameObject);

            foreach (var source in avatarGameObject.GetComponentsInChildren<ApproachSource>(true))
                ApproachProcessor.Process(source, avatarGameObject);

            return true;
        }
    }
}