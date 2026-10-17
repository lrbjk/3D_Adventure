using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;
    [System.Serializable]
    public class Sound
    {
        public string name;
        public AudioClip audioClip;
    }
    public Sound[] sounds;
    public static AudioManager instance;

   // const float pitchMin = 0.9f;
    //const float pitchMax = 1.1f;

    //用于判断静音与否
    private bool IsMuteMaster = false;
    private bool IsMuteMusic = false;
    private bool IsMuteSound = false;
    //用于储存静音前的音量
    private float LastMaster;
    private float LastMusic;
    private float LastSound;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);
    }
    /// <summary>
    /// 调节总音量
    /// </summary>
    /// <param name="image"></param>
    /// <param name="slider"></param>
    public void SetMaster(GameObject image, Slider slider)
    {
        audioMixer.SetFloat("Master", slider.value);
        if (IsMuteMaster == false) return;
        else
        {
            image.SetActive(false);
            IsMuteMaster = false;
        }
    }
    /// <summary>
    /// 调节音乐大小
    /// </summary>
    /// <param name="image"></param>
    /// <param name="slider"></param>
    public void SetMusic(GameObject image, Slider slider)
    {
        audioMixer.SetFloat("Music", slider.value);
        if (IsMuteMusic == false) return;
        else
        {
            image.SetActive(false);
            IsMuteMusic = false;
        }
    }
    /// <summary>
    /// 调节音效大小
    /// </summary>
    /// <param name="image"></param>
    /// <param name="slider"></param>
    public void SetSfx(GameObject image, Slider slider)
    {
        audioMixer.SetFloat("Sfx", slider.value);
        if (IsMuteSound == false) return;
        else
        {
            image.SetActive(false);
            IsMuteSound = false;
        };
    }

    /// <summary>
    /// 是否全部静音
    /// </summary>
    /// <param name="image"></param>
    /// <param name="Master"></param>
    public void MasterBtnOnClick(GameObject image, Slider Master)
    {
        if (IsMuteMaster)
        {
            image.SetActive(false);
            IsMuteMaster = false;
            Master.value = LastMaster;
        }

        else
        {
            image.SetActive(true);
            LastMaster = Master.value;
            Master.value = Master.minValue;
            IsMuteMaster = true;
        }
    }
    /// <summary>
    /// 是否静音音效
    /// </summary>
    /// <param name="image"></param>
    /// <param name="Sound"></param>
    public void SfxBtnOnClick(GameObject image, Slider Sound)
    {
        if (IsMuteSound)
        {
            image.SetActive(false);
            IsMuteSound = false;
            Sound.value = instance.LastSound;
        }

        else
        {
            image.SetActive(true);
            LastSound = Sound.value;
            Sound.value = Sound.minValue;
            IsMuteSound = true;
        }
    }
    /// <summary>
    /// 是否静音bgm
    /// </summary>
    /// <param name="image"></param>
    /// <param name="Music"></param>
    public void MusicBtnOnClick(GameObject image, Slider Music)
    {
        if (IsMuteMusic)
        {
            image.SetActive(false);
            IsMuteMusic = false;
            Music.value = LastMusic;
        }

        else
        {
            image.SetActive(true);
            LastMusic = Music.value;
            Music.value = Music.minValue;
            IsMuteMusic = true;
        }
    }
    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="audioname"></param>
    /// <param name="audioSource"></param>
    public void PlaySound(string audioname, AudioSource audioSource)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == audioname);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + audioname + " not found!");
            return;
        }
        audioSource.pitch = 1f;
        audioSource.PlayOneShot(s.audioClip);
    }
    /// <summary>
    /// 播放音乐
    /// </summary>
    /// <param name="audioname"></param>
    /// <param name="audioSource"></param>
    public void PlayMusic(string audioname, AudioSource audioSource)
    {
        Sound s = System.Array.Find(sounds, sound => sound.name == audioname);
        if (s == null)
        {
            Debug.LogWarning("Sound: " + audioname + " not found!");
            return;
        }
        audioSource.clip = s.audioClip;
        audioSource.pitch = 1f;
        audioSource.Play();
    }
}
