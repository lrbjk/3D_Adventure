using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class EnemyButcherModel : EnemyModel
    {
        [Header(" ÷≤ø∞Ûµ„")]
        public Transform handRight;
        public Transform handLeft;
        public Vector3 weaponPosition;
        public Quaternion weaponRotation;
        protected override void Start()
        {
            base.Start();
            weaponPosition = weapon.transform.localPosition;
            weaponRotation = weapon.transform.localRotation;
        }
        public void ChangeLeftHand()
        {
            weapon.transform.SetParent(handLeft);
            weapon.transform.localPosition = weaponPosition;
            //weapon.transform.localRotation = weaponRotation;
        }
        public void ChangeRightHand()
        {
            weapon.transform.SetParent(handRight);
            weapon.transform.localPosition = weaponPosition;
            weapon.transform.localRotation = weaponRotation;
        }
    }
}
