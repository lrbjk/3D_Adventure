using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public class TextShow : ItemBase
    {
        public TextInfo textInfo;
        public override void Interact()
        {
            if (textInfo.prefab != null)
                GamePlay.Instance.ShowItem(textInfo.prefab, textInfo.title, interactSound);
            GamePlay.Instance.ShowText(textInfo.text);
            ObjectPool.Instance.PushObject(gameObject);
        }
    }
}