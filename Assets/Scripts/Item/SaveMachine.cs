using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    public class SaveMachine : ItemBase
    {
        public override void Interact()
        {
            GamePlay.Instance.saveMenu.SetActive(true);
        }
    }
}
