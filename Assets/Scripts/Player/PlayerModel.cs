using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
namespace Astraparasitus
{
    public class PlayerModel : Model
    {
        public Rig rig;
        public string[] attackAnimations;
        public bool canAttack = false;
        public bool openCheck = false;
        public GameObject[] attackObjects;
        private Coroutine freezeFrameCoroutine;
        public AudioSource audioSource;
        [Header("�������")]
        public Transform checkPoint;
        public Transform enemyCheckPoint;
        [HideInInspector] public float checkRadius;
        [HideInInspector] public float enemyCheckRadius;
        public LayerMask enemyLayer;
        public LayerMask interactLayer;
        public Transform enemyTransform;
        public ItemType currentItem;
        [Header("�������")]
        public Transform middleCheckPoint;
        public Transform[] tcPositions;
        private bool isUp;
        private Collider currentCollider;
        [Header("������")]
        public Transform groundCheck;
        [HideInInspector] public float groundRadius;
        public LayerMask groundLayer;
        [Header("��λ���λ��")]
        [HideInInspector] public float radius;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
        public void SetRig()
        {
            rig.weight = 1;
        }
        public void CanAttack()
        {
            canAttack = true;
        }
        public void Attack()
        {
            openCheck = true;
        }
        public void EndAttack()
        {
            openCheck = false;
        }
        public void StartAttack(int hitBoxIndex)
        {
            attackObjects[hitBoxIndex].SetActive(true);
            AudioManager.instance.PlaySound("Kick", audioSource);
        }
        public void EndfAttack(int hitBoxIndex)
        {
            attackObjects[hitBoxIndex].SetActive(false);
        }
        public float GetShortestDistance()
        {
            float shortestDistance = 1000f;
            int numberOfRays = 10;
            float angleStep = 20f / numberOfRays; // ��-45�ȵ�+45��

            for (int i = -numberOfRays / 2; i <= numberOfRays / 2; i++)
            {
                Vector3 direction = Quaternion.Euler(0, angleStep * i, 0) * transform.forward;
                RaycastHit hit;
                Debug.DrawRay(middleCheckPoint.position, direction * 100, Color.red); // �������ߣ�����Ϊ100��λ����ɫΪ��ɫ������ʱ��Ϊ5��

                if (Physics.Raycast(middleCheckPoint.position, direction, out hit))
                {
                    Debug.DrawRay(middleCheckPoint.position, direction * hit.distance, Color.green); // ���Ƽ�⵽�����ߣ���ɫΪ��ɫ
                    if (hit.distance < shortestDistance)
                    {
                        shortestDistance = hit.distance;
                    }
                }
            }

            return shortestDistance;
        }

        public void IsUpOrDown(Collider collider)
        {
            if (collider.transform.position.y > middleCheckPoint.position.y)
            {
                isUp = true;
            }
            else
            {
                isUp = false;
            }
        }
        public bool GetIsUp()
        {
            return isUp;
        }
        public bool CheckInteract()
        {
            Collider[] colliders = new Collider[1];
            int count = Physics.OverlapSphereNonAlloc(checkPoint.position, checkRadius, colliders, interactLayer);
            for (int i = 0; i < count; i++)
            {
                if (colliders[i].GetComponent<ItemBase>() != null)
                {
                    currentItem = colliders[i].GetComponent<ItemBase>().itemType;
                    IsUpOrDown(colliders[i]);
                    currentCollider = colliders[i];
                    return true;
                }
            }
            return false;
        }
        public void AddInteract()
        {
            if (currentCollider != null && currentCollider.TryGetComponent(out Iinteractable interactable))
            {
                interactable.Interact();
            }
        }
        public bool CheckEnemy()
        {
            Collider[] colliders = new Collider[10];
            int count = Physics.OverlapSphereNonAlloc(enemyCheckPoint.position, enemyCheckRadius, colliders, enemyLayer);
            for (int i = 0; i < count; i++)
            {
                if (colliders[i].GetComponent<HitPart>() != null && colliders[i].GetComponent<HitPart>().enemy.GetBreakState())
                {
                    enemyTransform = colliders[i].transform;
                    return true;
                }
            }
            return false;
        }
        public void FreezeFrame(float time)
        {
            freezeFrameCoroutine = StartCoroutine(FreezeFrameCoroutine(time));
        }
        private IEnumerator FreezeFrameCoroutine(float time)
        {
            animator.speed = 0;
            yield return new WaitForSecondsRealtime(time);
            animator.speed = 1;
        }
        public void FootStepSound()
        {
            AudioManager.instance.PlaySound("FootstepDefault", audioSource);
        }
        public Vector3 FindPlayerPosition()
        {
            // ��ȡ��ҵ�λ��
            Vector3 playerPosition = Vector3.zero;

            // �����λ������ radius Ϊ�뾶��һ��Բ���������Բ��ѡȡһ������
            // ����һ������ĽǶȣ�0��360�ȣ�
            float angle = Random.Range(0, 360);

            // ���Ƕ�ת��Ϊ����
            float radians = angle * Mathf.Deg2Rad;

            // ��������������
            Vector3 randomPointOnCircle = new Vector3(
                playerPosition.x + radius * Mathf.Cos(radians),
                playerPosition.y, // ���������y���ϵĸ߶Ȳ���
                playerPosition.z + radius * Mathf.Sin(radians)
            );

            return randomPointOnCircle;
        }
        public bool CheckGround()
        {
            Collider[] colliders = new Collider[10];
            int count = Physics.OverlapSphereNonAlloc(groundCheck.position, groundRadius, colliders, groundLayer);
            if(count > 0)
            {
                return true;
            }
            return false;
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(checkPoint.position, checkRadius);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(enemyCheckPoint.position, enemyCheckRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }
}
