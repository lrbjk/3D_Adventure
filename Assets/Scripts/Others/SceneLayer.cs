using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Astraparasitus
{
    [CreateAssetMenu(fileName = "Scene Layer", menuName = "Astraparasitus/Layer")]
    public class SceneLayer : ScriptableObject
    {
        public string Name;
        public Color SceneFolderColor = new Color(.15f, .15f, .15f, 1f);
    }
}
