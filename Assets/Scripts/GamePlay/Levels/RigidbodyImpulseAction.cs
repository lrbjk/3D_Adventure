using UnityEngine;
namespace Astraparasitus
{
    public class RigidbodyImpulseAction : LevelAction
    {
        [SerializeField] private Rigidbody target;
        [SerializeField] private Vector3 localDirection = Vector3.right - Vector3.forward;
        [SerializeField, Min(0f)] private float force = 1000f;
        public override Object Target => target;
        public override void Execute(LevelContext context) { if (target == null) return; target.isKinematic = false; target.AddForce(target.transform.TransformDirection(localDirection) * force); }
        public override bool IsConfigured(out string message) { message = target == null ? "Rigidbody 未配置" : string.Empty; return target != null; }
    }
}
