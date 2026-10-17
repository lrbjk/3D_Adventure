using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
namespace Astraparasitus
{
    public class EnemyModel : Model
    {
        [Header("����")]
        [HideInInspector] public SkillConfig[] skillConfigs;
        public GameObject[] hitBox;
        private int currentSkillIndex = 0;
        [Header("��Ч")]
        public VisualEffect vfx;
        public SkinnedMeshRenderer skinnedMeshRenderer;
        public float dissolveRate = 0.0125f;
        public float refreshRate = 0.025f;
        private Material[] skinnedMaterails;
        private Coroutine dissolveCoroutine;
        [Header("Ͷ����")]
        public bool canThrow = true;
        public Transform throwPoint;
        public GameObject throwObj;
        [Header("����")]
        public GameObject weapon;
        // Start is called before the first frame update
        private void Awake()
        {
            if (skinnedMeshRenderer != null)
            {
                skinnedMaterails = skinnedMeshRenderer.materials;
            }
            if (vfx != null)
                vfx.Stop();
        }
        protected virtual void Start()
        {
            for (int i = 0; i < skillConfigs.Length; i++)
            {
                switch (skillConfigs[i].eventName)
                {
                    case EventName.Attack:
                        var clip = AnimationHelper.GetClipFromAnimator(animator, skillConfigs[i].clipName);
                        AnimationHelper.AddEventToClip(clip, skillConfigs[i].startframe, "StartAttack", true);
                        AnimationHelper.AddEventToClip(clip, skillConfigs[i].endframe, "EndAttack", true);
                        break;
                    case EventName.TC:
                        var clip1 = AnimationHelper.GetClipFromAnimator(animator, skillConfigs[i].clipName);
                        AnimationHelper.AddEventToClip(clip1, skillConfigs[i].startframe, "StartAttack", true);
                        break;
                    case EventName.FarAttack:
                        var clip2 = AnimationHelper.GetClipFromAnimator(animator, skillConfigs[i].clipName);
                        AnimationHelper.AddEventToClip(clip2, skillConfigs[i].startframe, "ShootObj", true);
                        AnimationHelper.AddEventToClip(clip2, skillConfigs[i].endframe, "CheckThrowObj", true);
                        break;
                }

            }
        }
        public void StartAttack()
        {
            SkillConfig skillConfig = skillConfigs[currentSkillIndex];
            hitBox[skillConfig.hitboxIndex].GetComponent<CapsuleCollider>().enabled = true;
        }
        public void EndAttack()
        {
            SkillConfig skillConfig = skillConfigs[currentSkillIndex];
            hitBox[skillConfig.hitboxIndex].GetComponent<CapsuleCollider>().enabled = false;
        }
        public void SetSkillIndex(int index)
        {
            currentSkillIndex = index;
        }
        // Update is called once per frame
        void Update()
        {

        }
        public void PlayDistroyEffect()
        {
            dissolveCoroutine = StartCoroutine(Dissolve());
        }
        public void ResetDistroyEffect()
        {
            if (vfx != null)
                vfx.Stop();
            if (skinnedMaterails.Length > 0)
            {
                for (int i = 0; i < skinnedMaterails.Length; i++)
                {
                    skinnedMaterails[i].SetFloat("_DissolveAmount", 0);
                }
            }
        }
        private IEnumerator Dissolve()
        {
            if (vfx != null)
            {
                vfx.Play();
            }
            if (skinnedMaterails.Length > 0)
            {
                float count = 0;
                while (skinnedMaterails[0].GetFloat("_DissolveAmount") < 1)
                {
                    count += dissolveRate;
                    for (int i = 0; i < skinnedMaterails.Length; i++)
                    {
                        skinnedMaterails[i].SetFloat("_DissolveAmount", count);
                    }
                    yield return new WaitForSeconds(refreshRate);
                }
                ObjectPool.Instance.PushObject(transform.parent.gameObject);
            }
        }
        public void ShootObj()
        {
            if (canThrow)
            {
                weapon.SetActive(false);
                GameObject obj = ObjectPool.Instance.GetObject(throwObj);
                obj.GetComponent<BezierCurve>().endTran = GamePlay.Instance.player.transform;
                obj.transform.position = throwPoint.position;
                obj.GetComponent<BezierCurve>().SetCenterPoint();
                obj.GetComponent<BezierCurve>().DoAnim();
            }
        }
        public void CheckThrowObj()
        {
            if (!weapon.activeSelf)
            {
                canThrow = false;
            }
            else
            {
                canThrow = true;
            }
        }
        public bool CheckWeapon()
        {
            return weapon.activeSelf;
        }
    }
}
