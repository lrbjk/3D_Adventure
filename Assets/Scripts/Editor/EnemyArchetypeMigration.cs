#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Astraparasitus.Editor
{
    public static class EnemyArchetypeMigration
    {
        private const string EnemyPrefabFolder = "Assets/Prefab/Enemy";
        private const string ArchetypeFolder = "Assets/GameData/Enemies";

        [MenuItem("Tools/3D Adventure/Migrate Enemy Archetypes")]
        public static void Run()
        {
            EnsureFolder("Assets", "GameData");
            EnsureFolder("Assets/GameData", "Enemies");

            List<EnemyArchetype> migrated = new List<EnemyArchetype>();
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { EnemyPrefabFolder });
            System.Array.Sort(prefabGuids, (left, right) =>
                string.CompareOrdinal(AssetDatabase.GUIDToAssetPath(left), AssetDatabase.GUIDToAssetPath(right)));

            foreach (string prefabGuid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    EnemyBase enemy = root.GetComponentInChildren<EnemyBase>(true);
                    if (enemy == null)
                    {
                        continue;
                    }

                    string safeName = Path.GetFileNameWithoutExtension(prefabPath).Replace(' ', '_');
                    string archetypePath = $"{ArchetypeFolder}/{safeName}.asset";
                    EnemyArchetype archetype = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(archetypePath);
                    bool created = archetype == null;
                    if (created)
                    {
                        archetype = ScriptableObject.CreateInstance<EnemyArchetype>();
                        AssetDatabase.CreateAsset(archetype, archetypePath);
                    }

                    CopyLegacyData(enemy, archetype, prefabAsset);
                    if (created)
                    {
                        SerializedObject archetypeObject = new SerializedObject(archetype);
                        SerializedProperty stableId = archetypeObject.FindProperty("stableId");
                        if (stableId != null)
                        {
                            stableId.stringValue = GameContentId.Create();
                            archetypeObject.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }

                    EditorUtility.SetDirty(archetype);
                    SerializedObject enemyObject = new SerializedObject(enemy);
                    enemyObject.FindProperty("archetype").objectReferenceValue = archetype;
                    enemyObject.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    migrated.Add(archetype);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            UpdateCatalogs(migrated);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Migrated {migrated.Count} enemy prefabs to EnemyArchetype assets in {ArchetypeFolder}.");
        }

        private static void CopyLegacyData(EnemyBase enemy, EnemyArchetype target, GameObject prefabAsset)
        {
            EnemyArchetype source = enemy.Archetype;
            if (source != null)
            {
                target.ID = source.ID;
                target.Name = source.Name;
                target.health = source.health;
                target.endurance = source.endurance;
                target.speedRate = source.speedRate;
                target.size = source.size;
                target.damage = source.damage;
                target.hitStunValue = source.hitStunValue;
                target.stunTime = source.stunTime;
                target.leftLegValue = source.leftLegValue;
                target.rightLegValue = source.rightLegValue;
                target.leftArmValue = source.leftArmValue;
                target.rightArmValue = source.rightArmValue;
                target.idle = source.idle;
                target.chase = source.chase;
                target.toNoLeg = source.toNoLeg;
                target.noLegIdle = source.noLegIdle;
                target.noLegChase = source.noLegChase;
                target.Break = source.Break;
                target.other = source.other;
            }

            target.enemyPrefab = prefabAsset;
            target.recoverTime = enemy.recoverTime;
            target.gravity = enemy.gravity;
            target.navMeshAreaName = enemy.NavMeshAreaName;
            target.idleSound = enemy.idleSound;
            target.chaseSound = enemy.chaseSound;
            target.breakSound = enemy.breakSound;
            target.attackSound = enemy.attackSound;
            target.hitSound = enemy.hitSound;
            target.dieSound = enemy.dieSound;
            target.fallbackLootPrefab = enemy.prefab;
            target.useLegacyRandomLoot = enemy.isRandom;

            if (enemy.aiDetect != null)
            {
                target.viewRadius = enemy.aiDetect.viewRadius;
                target.viewAngleStep = enemy.aiDetect.viewAngleStep;
                target.viewAngle = enemy.aiDetect.viewAngle;
                target.viewOffset = enemy.aiDetect.offset;
                target.attackDistance = enemy.aiDetect.attackDistence;
                target.farAttackDistance = enemy.aiDetect.farAttackDistence;
                target.executionDistance = enemy.aiDetect.tcDistence;
                target.forgetDelay = enemy.aiDetect.forgetDelay;
                target.useDistanceCheck = enemy.aiDetect.useDistanceCheck;
                target.ignoreLayer = enemy.aiDetect.ignoreLayer;
            }

            target.skills = enemy.enemyModel != null && enemy.enemyModel.skillConfigs != null
                ? (SkillConfig[])enemy.enemyModel.skillConfigs.Clone()
                : System.Array.Empty<SkillConfig>();
        }

        private static void UpdateCatalogs(List<EnemyArchetype> archetypes)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:GameContentCatalog"))
            {
                GameContentCatalog catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(AssetDatabase.GUIDToAssetPath(guid));
                if (catalog == null)
                {
                    continue;
                }

                SerializedObject catalogObject = new SerializedObject(catalog);
                SerializedProperty enemies = catalogObject.FindProperty("enemies");
                enemies.arraySize = archetypes.Count;
                for (int i = 0; i < archetypes.Count; i++)
                {
                    enemies.GetArrayElementAtIndex(i).objectReferenceValue = archetypes[i];
                }
                catalogObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
#endif
