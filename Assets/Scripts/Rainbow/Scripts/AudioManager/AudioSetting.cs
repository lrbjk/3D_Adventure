using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AudioSetting : MonoBehaviour
{
    [SerializeField] Slider Master;
    [SerializeField] Slider Music;
    [SerializeField] Slider SFX;

    //SldOnClick：传递参数以触发AudioManager的SldOnClick
    public void SetMaster(GameObject image)
    {
        AudioManager.instance.SetMaster(image, Master);
    }
    public void SetMusic(GameObject image)
    {
        AudioManager.instance.SetMusic(image, Music);
    }
    public void SetSfx(GameObject image)
    {
        AudioManager.instance.SetSfx(image, SFX);
    }

    //BtnOnClick：传递参数以触发AudioManager的BtnOnClick
    public void MasterBtnOnClick(GameObject image)
    {
        AudioManager.instance.MasterBtnOnClick(image, Master);
    }
    public void SfxBtnOnClick(GameObject image)
    {
        AudioManager.instance.SfxBtnOnClick(image, SFX);
    }
    public void MusicBtnOnClick(GameObject image)
    {
        AudioManager.instance.MusicBtnOnClick(image, Music);
    }

}
