using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace Astraparasitus
{
    public class Door : ItemBase
    {
        [Header("��")]
        public ItemConfig key;
        public Animator animator;
        private Coroutine closeCoroutine;
        public bool isLocked = false;
        public string openAnimaName = "OpenDoor";
        public string closeAnimaName = "CloseDoor";
        private bool isOpen = false;
        public Transform spwanPoint;
        public GameObject prefab;
        private bool hasBorn = false;
        public bool isRandomSpwan = false;

        public override void Interact()
        {
            if (key == null)
            {
                UnLock(false);
            }
            else
            {
                if (BackPack.Instance.CheckItem(key.id))
                {
                    UnLock(false);
                    BackPack.Instance.RemoveAllItem(key.id);
                }
            }
            if (!isLocked)
            {
                isOpen = true;
                if (!hasBorn && (isRandomSpwan || prefab != null))
                {
                    Transform dropPoint = spwanPoint != null ? spwanPoint : transform;
                    if (!isRandomSpwan)
                    {
                        GameObject obj = ObjectPool.Instance.GetObject(prefab, dropPoint.position);
                        obj.transform.position = dropPoint.position;
                        obj.transform.SetParent(dropPoint);
                    }
                    else
                    {
                        GamePlay.Instance?.SpawnResourceDrop(ResourceDropSource.Door, null, dropPoint.position, dropPoint);
                    }
                    // 空掉落也是已结算结果，避免重复开门刷资源。
                    hasBorn = true;
                }
                animator.Play(openAnimaName);
                gameObject.layer = LayerMask.NameToLayer("Default");
                gameObject.tag = "Untagged";
                switch (interactionType)
                {
                    case InteractionType.Door:
                        AudioManager.instance.PlaySound("OpenDoor", audioSource);
                        GamePlay.Instance.HideInteractUI(0);
                        break;
                    case InteractionType.Box:
                        AudioManager.instance.PlaySound("OpenBox", audioSource);
                        GamePlay.Instance.HideInteractUI(1);
                        break;
                }
                
                closeCoroutine = StartCoroutine(CloseDoor());
            }
            else
            {
                AudioManager.instance.PlaySound("Locked", audioSource);
                GamePlay.Instance.ShowTips("Is Locked", 1.5f);
            }
        }

        public void UnLock(bool state)
        {
            isLocked = state;
        }
        public bool GetLockState()
        {
            return isLocked;
        }
        public bool GetOpenState()
        {
            return isOpen;
        }
        private IEnumerator CloseDoor()
        {
            yield return new WaitForSeconds(5f);
            gameObject.layer = LayerMask.NameToLayer("Interact");
            gameObject.tag = "Interact";

            isOpen = false;
            animator.Play(closeAnimaName);
        }
    }
}
