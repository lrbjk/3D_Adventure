using UnityEngine;
namespace Astraparasitus
{
    public class PlayAudioAction : LevelAction
    {
        [SerializeField] private string soundName;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool playAsMusic;
        public override Object Target => audioSource;
        public override void Execute(LevelContext context) { if (AudioManager.instance == null || audioSource == null || string.IsNullOrWhiteSpace(soundName)) return; if (playAsMusic) AudioManager.instance.PlayMusic(soundName, audioSource); else AudioManager.instance.PlaySound(soundName, audioSource); }
        public override bool IsConfigured(out string message) { bool valid = audioSource != null && !string.IsNullOrWhiteSpace(soundName); message = valid ? string.Empty : "声音名称或 AudioSource 未配置"; return valid; }
    }
}
