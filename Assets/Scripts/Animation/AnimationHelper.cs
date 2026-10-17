using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Astraparasitus
{
    public static class AnimationHelper
    {
        public static AnimationClip GetClipFromAnimator(Animator animator, string stateName)
        {
            if (animator == null)
            {
                Debug.LogError("Animator is null!");
                return null;
            }

            var animatorController = animator.runtimeAnimatorController;

            if (animatorController == null)
            {
                Debug.LogError("Animator Controller is null!");
                return null;
            }

            foreach (var clip in animatorController.animationClips)
            {
                //Debug.Log(clip.name);
                if (clip.name == stateName)
                {
                    return clip;
                }
            }

            return null;
        }
        public static void AddEventToClip(AnimationClip clip, float time, string functionName, bool useFrames = false)
        {
            if (clip == null)
            {
                Debug.LogError("AnimationClip is null!");
                return;
            }

            float fps = clip.frameRate > 0 ? clip.frameRate : 60f; // Ĭ�� 60fps

            if (useFrames)
            {
                time = time / fps; // ��֡��ת��Ϊ����
            }

            if (time < 0f)
            {
                time = 0f;
            }

            if (time > clip.length)
            {
                time = clip.length;
            }

            AnimationEvent animationEvent = new AnimationEvent
            {
                time = time,
                functionName = functionName
            };

            clip.AddEvent(animationEvent);
        }

        public static void ClearAllEventsFromClip(AnimationClip clip)
        {
            if (clip == null)
            {
                Debug.LogError("ClearAllEventsFromClip: AnimationClip is null!");
                return;
            }

            clip.events = new AnimationEvent[0];
        }

        public static void RemoveEventFromClip(AnimationClip clip, string functionName)
        {
            if (clip == null)
            {
                Debug.LogError("RemoveEventFromClip: AnimationClip is null!");
                return;
            }

            if (string.IsNullOrEmpty(functionName))
            {
                Debug.LogError("RemoveEventFromClip: Function name is null or empty!");
                return;
            }

            AnimationEvent[] events = clip.events;
            var filteredEvents = new System.Collections.Generic.List<AnimationEvent>();
            foreach (var evt in events)
            {
                if (evt.functionName != functionName)
                {
                    filteredEvents.Add(evt);
                }
            }

            clip.events = filteredEvents.ToArray();
        }

    }
}

