using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class TutorialUI : MonoBehaviour
    {
        public void CloseUI()
        {
            gameObject.SetActive(false);
        }
    }
}
