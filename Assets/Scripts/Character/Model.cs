using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class Model : MonoBehaviour
    {
        public Animator animator;
        // Start is called before the first frame update
        #region ¸ùÔË¶¯
        private Action<Vector3, Quaternion> rootMotionAction;

        public void SetRootMotionAction(Action<Vector3, Quaternion> rootMotionAction)
        {
            this.rootMotionAction = rootMotionAction;
        }

        public void ClearRootMotionAction()
        {
            rootMotionAction = null;
        }

        private void OnAnimatorMove()
        {
            rootMotionAction?.Invoke(animator.deltaPosition, animator.deltaRotation);
        }
        #endregion
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}