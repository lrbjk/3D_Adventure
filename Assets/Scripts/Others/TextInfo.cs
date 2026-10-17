using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Text Info", menuName = "Text Info")]
public class TextInfo : ScriptableObject
{
    public string title;
    [TextArea]
    public string text;
    public GameObject prefab;
}
