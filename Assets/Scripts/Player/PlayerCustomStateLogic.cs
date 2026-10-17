using UnityEngine;

namespace Astraparasitus
{
    // 在子类中实现玩法逻辑，资产实例只保存配置；运行时每个状态会克隆一份。
    public abstract class PlayerCustomStateLogic : ScriptableObject
    {
        public virtual void OnEnter(PlayerControl player) { }
        public virtual void OnUpdate(PlayerControl player) { }
        public virtual void OnLateUpdate(PlayerControl player) { }
        public virtual void OnFixedUpdate(PlayerControl player) { }
        public virtual void OnExit(PlayerControl player) { }
    }

    public class PlayerState_Custom : PlayerBaseState
    {
        public readonly PlayerCustomStateDefinition definition;
        private PlayerCustomStateLogic runtimeLogic;
        protected override PlayerMotionMode MotionMode => definition.motionMode;
        protected override bool ApplyAnimationRotation => definition.applyAnimationRotation;

        public PlayerState_Custom(PlayerCustomStateDefinition definition)
        {
            this.definition = definition;
        }

        public override void Init(IStateMachineOwner owner)
        {
            base.Init(owner);
            if (definition.logic != null)
            {
                runtimeLogic = Object.Instantiate(definition.logic);
                runtimeLogic.hideFlags = HideFlags.DontSave;
            }
        }

        public override void Enter()
        {
            player.model.SetRootMotionAction(OnRootMotion);
            if (!string.IsNullOrWhiteSpace(definition.animationState))
                player.PlayAnimaton(definition.animationState, definition.crossFadeTime);
            runtimeLogic?.OnEnter(player);
        }

        public override void Update()
        {
            runtimeLogic?.OnUpdate(player);
            if (player.stateMachine.CurrentState != this || !definition.returnToIdleOnAnimationEnd ||
                string.IsNullOrWhiteSpace(definition.animationState)) return;
            if (CheckAnimationName(definition.animationState, 0, out float time) && time >= 1f)
                player.ChangeState(PlayerState.Idle);
        }

        public override void LateUpdate() => runtimeLogic?.OnLateUpdate(player);
        public override void FixedUpdate() => runtimeLogic?.OnFixedUpdate(player);

        public override void Exit()
        {
            runtimeLogic?.OnExit(player);
            player.model.ClearRootMotionAction();
        }

        protected override void OnRootMotion(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            player.Motion.ApplyRootMotion(deltaPosition * definition.rootMotionScale, deltaRotation,
                MotionMode, ApplyAnimationRotation);
        }

        public override void Unint()
        {
            if (runtimeLogic != null) Object.Destroy(runtimeLogic);
            runtimeLogic = null;
        }
    }
}
