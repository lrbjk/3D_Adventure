#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astraparasitus.Editor
{
    [InitializeOnLoad]
    public static class Level01EventRegression
    {
        private const string PendingKey = "3DAdventure.Level01EventRegression.Pending";
        private static int waitFrames;

        static Level01EventRegression()
        {
            EditorApplication.update += Tick;
        }

        [MenuItem("Tools/3D Adventure/Run Level01 Event Regression")]
        public static void Run()
        {
            if (SceneManager.GetActiveScene().path != "Assets/Scenes/Level01.unity")
            {
                Debug.LogError("Open Level01 before running its event regression.");
                return;
            }
            waitFrames = 0;
            SessionState.SetBool(PendingKey, true);
            EditorApplication.isPlaying = true;
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(PendingKey, false) || !EditorApplication.isPlaying) return;
            if (LevelContext.Current == null || GamePlay.Instance == null || GamePlay.Instance.player == null)
            {
                if (++waitFrames < 300) return;
                Finish("Level01 did not initialize LevelContext, GamePlay, and Player.");
                return;
            }
            if (++waitFrames < 5) return;

            try
            {
                Check();
                Finish(null);
            }
            catch (Exception exception)
            {
                Finish(exception.ToString());
            }
        }

        private static void Check()
        {
            LevelContext context = LevelContext.Current;
            Require(context.QuestDoor1 != null && context.QuestDoor2 != null, "Quest doors are not bound to LevelContext.");
            LevelEventTrigger[] events = UnityEngine.Object.FindObjectsByType<LevelEventTrigger>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Require(events.Length == 6, $"Expected six Level01 events, found {events.Length}.");
            Require(events.Select(node => node.EventId).Distinct().Count() == events.Length, "Event IDs are duplicated.");
            foreach (LevelEventTrigger node in events)
                Require(node.ValidateConfiguration(out string message), $"{node.name}: {message}");

            LevelEventTrigger music = Find(events, "level01.music.start");
            Require(music.HasTriggered, "The opening music event did not run on Start.");
            Require(Find(events, "level01.machine_room.key1") != null, "Machine-room event is missing.");

            CheckConditional(Find(events, "level01.document.first_wave"), 6, true, false);
            CheckConditional(Find(events, "level01.back_door.wave"), 2, false, false);
            CheckConditional(Find(events, "level01.danger_sign.butcher"), 1, false, true);
            CheckConditional(Find(events, "level01.tube.alpha"), 1, false, false);
        }

        private static void CheckConditional(LevelEventTrigger node, int spawnCount, bool activatesTarget, bool opensTrap)
        {
            Require(node.Conditions.Count == 1, $"{node.name} must have one condition.");
            LevelCondition condition = node.Conditions[0];
            Require(condition.type == LevelConditionType.GameObjectActive && condition.invert && condition.targetObject != null,
                $"{node.name} must trigger when its target becomes inactive.");
            SpawnWaveAction spawn = node.Actions.OfType<SpawnWaveAction>().SingleOrDefault();
            EnemyWave wave = spawn != null ? spawn.Target as EnemyWave : null;
            Require(wave != null && wave.Entries.Count == spawnCount, $"{node.name} has the wrong wave entries.");
            Require(node.Actions.OfType<SetActiveAction>().Any() == activatesTarget,
                $"{node.name} has an unexpected SetActive action.");
            Require(node.Actions.OfType<OpenTrapAction>().Any() == opensTrap,
                $"{node.name} has an unexpected OpenTrap action.");

            if (node.HasTriggered)
            {
                Require(wave.HasSpawned, $"{node.name} triggered without spawning its wave.");
                return;
            }

            GameObject target = condition.targetObject;
            target.SetActive(true);
            Require(!node.TryTrigger(), $"{node.name} fired while its condition target was active.");
            target.SetActive(false);
            Require(node.TryTrigger(), $"{node.name} did not fire after its condition target was disabled.");
            Require(wave.HasSpawned, $"{node.name} did not spawn its wave.");
            Require(!node.TryTrigger(), $"{node.name} triggered twice.");
            if (activatesTarget)
            {
                SetActiveAction action = node.Actions.OfType<SetActiveAction>().Single();
                Require(((GameObject)action.Target).activeSelf, "The first-wave event did not enable Trigger1.");
            }
        }

        private static LevelEventTrigger Find(LevelEventTrigger[] events, string id)
        {
            LevelEventTrigger node = Array.Find(events, candidate => candidate.EventId == id);
            Require(node != null, $"Event {id} is missing.");
            return node;
        }

        private static void Require(bool result, string message)
        {
            if (!result) throw new InvalidOperationException(message);
        }

        private static void Finish(string error)
        {
            SessionState.SetBool(PendingKey, false);
            if (error == null) Debug.Log("Level01 Event Regression passed: six events, four waves, gates, actions, and once-only triggers.");
            else Debug.LogError($"Level01 Event Regression failed: {error}");
            EditorApplication.isPlaying = false;
        }
    }
}
#endif
