using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class PlayerBaseState : StateBase
    {
        public PlayerControl player;
        protected virtual PlayerMotionMode MotionMode => PlayerMotionMode.Action;
        protected virtual bool ApplyAnimationRotation => false;
        public override void Init(IStateMachineOwner owner)
        {
            base.Init(owner);
            player = (PlayerControl)owner;
        }
        protected virtual bool CheckAnimationName(string stateName, int layerIndex, out float currentTime)
        {
            AnimatorStateInfo info = player.model.animator.GetCurrentAnimatorStateInfo(layerIndex);
            currentTime = info.normalizedTime;
            return info.IsName(stateName);
        }
        protected virtual void OnRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            player.Motion.ApplyRootMotion(deltaPosition, deltaRotation, MotionMode, ApplyAnimationRotation);
        }
    }
}
