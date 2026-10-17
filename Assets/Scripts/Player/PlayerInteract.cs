using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class PlayerInteract : MonoBehaviour
    {
        public string aimTag;
        //public GameObject[] InteractUI;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(aimTag))
            {
                if (other.GetComponent<ItemBase>() != null)
                {
                    switch (other.GetComponent<ItemBase>().interactionType)
                    {
                        case InteractionType.Door:
                            GamePlay.Instance.ShowInteractUI(0);
                            GamePlay.Instance.HideInteractUI(1);
                            GamePlay.Instance.HideInteractUI(2);
                            break;
                        case InteractionType.Box:
                            GamePlay.Instance.ShowInteractUI(1);
                            GamePlay.Instance.HideInteractUI(0);
                            GamePlay.Instance.HideInteractUI(2);
                            break;
                        case InteractionType.Item:
                            GamePlay.Instance.ShowInteractUI(2);
                            GamePlay.Instance.HideInteractUI(0);
                            GamePlay.Instance.HideInteractUI(1);
                            break;
                    }
                }
                if (other.TryGetComponent(out Iinteractable interactable))
                {
                    interactable.ShowInfo();
                }

            }

        }
        private void OnTriggerStay(Collider other)
        {
            if (other.CompareTag(aimTag))
            {
                if (other.GetComponent<ItemBase>() != null)
                {
                    switch (other.GetComponent<ItemBase>().interactionType)
                    {
                        case InteractionType.Door:
                            GamePlay.Instance.ShowInteractUI(0);
                            GamePlay.Instance.HideInteractUI(1);
                            GamePlay.Instance.HideInteractUI(2);
                            break;
                        case InteractionType.Box:
                            GamePlay.Instance.ShowInteractUI(1);
                            GamePlay.Instance.HideInteractUI(0);
                            GamePlay.Instance.HideInteractUI(2);
                            break;
                        case InteractionType.Item:
                            GamePlay.Instance.ShowInteractUI(2);
                            GamePlay.Instance.HideInteractUI(0);
                            GamePlay.Instance.HideInteractUI(1);
                            break;
                    }
                }
                if (other.GetComponent<EnemyBase>() != null && other.TryGetComponent(out Iinteractable interactable))
                {
                    interactable.ShowInfo();
                }
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(aimTag))
            {
                if (other.GetComponent<ItemBase>() != null)
                {
                    switch (other.GetComponent<ItemBase>().interactionType)
                    {
                        case InteractionType.Door:
                            GamePlay.Instance.HideInteractUI(0);
                            break;
                        case InteractionType.Box:
                            GamePlay.Instance.HideInteractUI(1);
                            break;
                        case InteractionType.Item:
                            GamePlay.Instance.HideInteractUI(2);
                            break;
                    }
                }
                if (other.TryGetComponent(out Iinteractable interactable))
                {
                    interactable.HideInfo();
                }
            }

        }
    }
}
