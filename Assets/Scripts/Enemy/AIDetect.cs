using UnityEngine;

namespace Astraparasitus
{
    public class AIDetect : MonoBehaviour
    {
        [HideInInspector] public float viewRadius;
        [HideInInspector] public int viewAngleStep = 8;
        [HideInInspector] public float viewAngle;
        [HideInInspector] public Vector3 offset;
        public bool isDetected;
        [HideInInspector] public float attackDistence;
        [HideInInspector] public float farAttackDistence;
        [HideInInspector] public float tcDistence;
        [HideInInspector] public float forgetDelay = 10f;
        [SerializeField] private bool canAttack;
        [SerializeField] private bool canFarAttack;
        [HideInInspector] public bool useDistanceCheck;

        [HideInInspector] public LayerMask ignoreLayer;

        private float lastSeenTime;

        private void OnEnable()
        {
            if (isDetected)
            {
                lastSeenTime = Time.time;
            }
        }

        private void FixedUpdate()
        {
            DrawFieldOfView();
        }

        public void DrawFieldOfView()
        {
            int steps = Mathf.Max(1, viewAngleStep);
            Vector3 origin = transform.position + transform.forward * offset.x + transform.up * offset.y + transform.right * offset.z;
            Vector3 leftDirection = Quaternion.Euler(0f, -(viewAngle * 0.5f), 0f) * transform.forward;

            bool foundPlayer = false;
            bool nextCanAttack = false;
            bool nextCanFarAttack = false;

            for (int i = 0; i <= steps; i++)
            {
                Vector3 direction = Quaternion.Euler(0f, (viewAngle / steps) * i, 0f) * leftDirection;
                Debug.DrawRay(origin, direction * viewRadius, Color.red);

                if (!Physics.Raycast(origin, direction, out RaycastHit hitInfo, viewRadius, ~ignoreLayer))
                {
                    continue;
                }

                if (!hitInfo.collider.CompareTag("Player"))
                {
                    continue;
                }

                foundPlayer = true;
                float distance = Vector3.Distance(transform.position, hitInfo.collider.transform.position);
                if (useDistanceCheck)
                {
                    nextCanAttack = distance < attackDistence;
                    nextCanFarAttack = !nextCanAttack && distance >= farAttackDistence - 1f && distance <= farAttackDistence;
                }
                else
                {
                    nextCanAttack = distance < tcDistence;
                }
                break;
            }

            canAttack = nextCanAttack;
            canFarAttack = nextCanFarAttack;

            if (foundPlayer)
            {
                isDetected = true;
                lastSeenTime = Time.time;
            }
            else if (Time.time - lastSeenTime >= forgetDelay)
            {
                isDetected = false;
            }
        }

        public void SetCanAttack(bool value)
        {
            canAttack = value;
        }

        public void SetDetected(bool value)
        {
            isDetected = value;
            if (value)
            {
                lastSeenTime = Time.time;
            }
        }

        public bool GetCanAttack()
        {
            return canAttack;
        }

        public void SetCanFarAttack(bool value)
        {
            canFarAttack = value;
        }

        public bool GetCanFarAttack()
        {
            return canFarAttack;
        }

        private void OnValidate()
        {
            viewAngleStep = Mathf.Max(1, viewAngleStep);
            viewRadius = Mathf.Max(0f, viewRadius);
            attackDistence = Mathf.Max(0f, attackDistence);
            farAttackDistence = Mathf.Max(0f, farAttackDistence);
            tcDistence = Mathf.Max(0f, tcDistence);
            forgetDelay = Mathf.Max(0f, forgetDelay);
        }
    }
}
