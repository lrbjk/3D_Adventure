#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astraparasitus.Editor
{
    [CustomEditor(typeof(LevelEventTrigger))]
    public class LevelEventTriggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            LevelEventTrigger trigger = (LevelEventTrigger)target;
            EditorGUILayout.Space();
            if (trigger.ValidateConfiguration(out string message))
                EditorGUILayout.HelpBox("配置完整", MessageType.Info);
            else
                EditorGUILayout.HelpBox(message, MessageType.Error);
        }
    }

    public static class LevelSceneContractValidator
    {
        private static readonly string[] RequiredRoots =
            { "__Level", "__Gameplay", "__Presentation", "__Environment", "__Lighting" };

        [MenuItem("Tools/3D Adventure/Validate Level Scene Contract")]
        public static void ValidateCurrentSceneMenu()
        {
            int errors = 0;
            int warnings = 0;
            ValidateOpenScenes(ref errors, ref warnings);
            Debug.Log($"Level scene validation finished. Errors: {errors}, Warnings: {warnings}");
        }

        public static void ValidateOpenScenes(ref int errors, ref int warnings)
        {
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded) continue;
                GameObject[] roots = scene.GetRootGameObjects();
                bool playable = roots.Any(root => root.GetComponentInChildren<PlayerControl>(true) != null) &&
                                roots.Any(root => root.GetComponentInChildren<GamePlay>(true) != null);
                if (!playable) continue;

                ValidateSingleScene(scene, roots, ref errors, ref warnings);
            }
        }

        private static void ValidateSingleScene(Scene scene, GameObject[] roots, ref int errors, ref int warnings)
        {
            foreach (string rootName in RequiredRoots)
            {
                int count = roots.Count(root => root.name == rootName);
                if (count != 1)
                {
                    Debug.LogError($"{scene.name}: root '{rootName}' count must be 1, actual {count}.");
                    errors++;
                }
            }

            LevelInstaller[] installers = roots.SelectMany(root => root.GetComponentsInChildren<LevelInstaller>(true)).ToArray();
            LevelContext[] contexts = roots.SelectMany(root => root.GetComponentsInChildren<LevelContext>(true)).ToArray();
            if (installers.Length != 1) { Debug.LogError($"{scene.name}: LevelInstaller count must be 1."); errors++; }
            if (contexts.Length != 1) { Debug.LogError($"{scene.name}: LevelContext count must be 1."); errors++; }
            if (contexts.Length == 1 && !contexts[0].ValidateReferences(out string contextMessage))
            {
                Debug.LogError($"{scene.name}: {contextMessage}", contexts[0]);
                errors++;
            }

            HashSet<string> eventIds = new HashSet<string>();
            foreach (LevelEventTrigger trigger in roots.SelectMany(root => root.GetComponentsInChildren<LevelEventTrigger>(true)))
            {
                if (!trigger.ValidateConfiguration(out string message))
                {
                    Debug.LogError($"{scene.name}/{trigger.name}: {message}", trigger);
                    errors++;
                }
                if (!eventIds.Add(trigger.EventId))
                {
                    Debug.LogError($"{scene.name}: duplicate event ID {trigger.EventId}", trigger);
                    errors++;
                }
            }

            foreach (EnemyWave wave in roots.SelectMany(root => root.GetComponentsInChildren<EnemyWave>(true)))
            {
                if (wave.Entries.Count == 0)
                {
                    Debug.LogWarning($"{scene.name}/{wave.name}: EnemyWave has no entries.", wave);
                    warnings++;
                    continue;
                }
                foreach (EnemyWaveEntry entry in wave.Entries)
                {
                    if (entry == null || entry.spawnPoint == null)
                    {
                        Debug.LogError($"{scene.name}/{wave.name}: EnemyWave contains an empty spawn point.", wave);
                        errors++;
                    }
                }
            }
        }
    }

    public static class LevelAuthoringTemplates
    {
        private const string TemplateFolder = "Assets/Prefab/Templates";

        [MenuItem("Tools/3D Adventure/Create Level Authoring Templates")]
        public static void CreateTemplates()
        {
            EnsureFolder("Assets", "Prefab");
            EnsureFolder("Assets/Prefab", "Templates");
            CreateEventTemplate();
            CreateSimpleTemplate<EnemyWave>("EnemyWave_Template");
            CreateSimpleTemplate<SaveableId>("SaveableObject_Template");
            AssetDatabase.SaveAssets();
        }

        private static void CreateEventTemplate()
        {
            string path = $"{TemplateFolder}/LevelEventNode_Template.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject root = new GameObject("LevelEventNode_Template");
            root.AddComponent<BoxCollider>().isTrigger = true;
            LevelEventTrigger trigger = root.AddComponent<LevelEventTrigger>();
            ShowTipAction tip = root.AddComponent<ShowTipAction>();
            SetString(tip, "message", "配置事件提示");
            SerializedObject serialized = new SerializedObject(trigger);
            SerializedProperty actions = serialized.FindProperty("actions");
            actions.arraySize = 1;
            actions.GetArrayElementAtIndex(0).objectReferenceValue = tip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void CreateSimpleTemplate<T>(string name) where T : Component
        {
            string path = $"{TemplateFolder}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject root = new GameObject(name);
            root.AddComponent<T>();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void EnsureFolder(string parent, string child)
        {
            string path = Path.Combine(parent, child).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
#endif
