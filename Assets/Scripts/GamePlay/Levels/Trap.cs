using UnityEngine;

namespace Astraparasitus
{
    public class Trap : MonoBehaviour
    {
        public Animator animator;
        public string openName = "BigDoorOpen";
        public string closeName = "BigDoorClose";
        public void OpenTrap()
        {
            animator.Play(openName);
        }
        public void CloseTrap()
        {
            animator.Play(closeName);
        }
    }
}
