#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Astraparasitus.Editor
{
    [CustomEditor(typeof(PlayerDefinition), true)]
    public class PlayerDefinitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawGroup("基础信息", "displayName", "description");
            DrawGroup("生存属性", "health", "defense", "reductionRate");
            DrawGroup("Root Motion 运动", "gravity", "maxFallSpeed", "groundedDownSpeed", "turnSpeed", "moveInputThreshold", "idleRootMotionScale", "walkRootMotionScale", "runRootMotionScale", "aimRootMotionScale", "actionRootMotionScale", "airborneRootMotionScale", "cinematicRootMotionScale", "moveBlendDampTime", "runTurnAngle", "runSteerTriggerAngle", "walkFacingMode", "runFacingMode", "idleTurnStartAngle", "idleTurnStopAngle", "footstepDistance", "aimNearObstacleDistance", "aimFarObstacleDistance");
            DrawGroup("运动动画", "idleAnimation", "idleTurnAnimation", "walkAnimation", "runAnimation", "runTurnLeftAnimation", "runTurnRightAnimation", "aimMoveAnimation", "reloadAnimation", "fallAnimation", "landAnimation");
            DrawGroup("Animator 参数", "verticalBlendParameter", "horizontalBlendParameter", "leftTurnTrigger", "rightTurnTrigger", "aimCloseParameter");
            DrawGroup("相机", "cameraSensitivity", "cameraPitchMin", "cameraPitchMax");
            DrawGroup("交互检测", "interactRadius", "enemyCheckRadius", "groundCheckRadius", "tacticalMoveRadius");
            DrawGroup("初始物品与武器", "startingItems", "startingWeapons", "defaultRangedWeapon", "meleeWeapon");

            serializedObject.ApplyModifiedProperties();
            DrawValidation((PlayerDefinition)target);

            EditorGUILayout.Space();
            if (GUILayout.Button("打开运动与状态表"))
                EditorApplication.ExecuteMenuItem("Tools/3D Adventure/Player Movement & State Table");
            if (GUILayout.Button("重新绑定玩家 Prefab"))
            {
                PlayerDefinitionMigration.Run();
            }
        }

        private void DrawGroup(string title, params string[] properties)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            foreach (string propertyName in properties)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyName);
                if (property != null)
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }
            EditorGUILayout.Space();
        }

        private static void DrawValidation(PlayerDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.displayName))
            {
                EditorGUILayout.HelpBox("显示名称不能为空。", MessageType.Error);
            }
            if (definition.meleeWeapon == null)
            {
                EditorGUILayout.HelpBox("未配置近战武器定义，玩家拾取刀后将无法建立统一的近战规则。", MessageType.Warning);
            }
            if (string.IsNullOrWhiteSpace(definition.walkAnimation) ||
                string.IsNullOrWhiteSpace(definition.runAnimation) ||
                string.IsNullOrWhiteSpace(definition.verticalBlendParameter) ||
                string.IsNullOrWhiteSpace(definition.horizontalBlendParameter))
            {
                EditorGUILayout.HelpBox("运动动画或 Animator 参数名为空。请与角色 Animator Controller 中的名称保持一致。", MessageType.Warning);
            }
        }
    }

    [CustomEditor(typeof(PlayerControl))]
    public class PlayerControlEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawGroup("配置入口", "definition");
            DrawGroup("角色组件", "model", "controller");
            DrawGroup("相机连接", "mainCamera", "virtualCamera", "aimCamera", "normalTarget");
            DrawGroup("武器表现连接", "weapon", "weaponModels", "parryRange", "spotlight");
            DrawGroup("战斗挂点", "aimTarget", "lhand", "knifeTrans", "knifeHitSpark");
            DrawGroup("场景 UI 连接", "aimUI", "qteUI", "bulletCount");
            serializedObject.ApplyModifiedProperties();

            PlayerControl player = (PlayerControl)target;
            if (player.Definition == null)
            {
                EditorGUILayout.HelpBox("必须绑定 PlayerDefinition。", MessageType.Error);
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("运行状态（只读）", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.EnumPopup("当前状态", player.currentState);
                    EditorGUILayout.FloatField("生命", player.GetCurrentHp());
                    EditorGUILayout.FloatField("防御", player.GetCurrentDefence());
                    EditorGUILayout.ObjectField("当前武器", player.currentWp, typeof(WpConfig), false);
                    EditorGUILayout.FloatField("近战耐久", player.currentUse);
                }
            }
        }

        private void DrawGroup(string title, params string[] propertyNames)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            foreach (string propertyName in propertyNames)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyName);
                if (property != null)
                {
                    EditorGUILayout.PropertyField(property, true);
                }
            }
            EditorGUILayout.Space();
        }
    }

    public static class PlayerDefinitionMigration
    {
        private const string DefinitionFolder = "Assets/GameData/Player";
        private const string DefinitionPath = DefinitionFolder + "/Player.asset";

        [MenuItem("Tools/3D Adventure/Rebind Player Definition")]
        public static void Run()
        {
            EnsureFolder("Assets", "GameData");
            EnsureFolder("Assets/GameData", "Player");

            PlayerDefinition target = AssetDatabase.LoadAssetAtPath<PlayerDefinition>(DefinitionPath);
            if (target == null)
            {
                target = ScriptableObject.CreateInstance<PlayerDefinition>();
                AssetDatabase.CreateAsset(target, DefinitionPath);
            }

            int playerPrefabs = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab/Player" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    PlayerControl player = root.GetComponentInChildren<PlayerControl>(true);
                    if (player == null)
                    {
                        continue;
                    }

                    CopyDefinition(player, target);
                    SerializedObject playerObject = new SerializedObject(player);
                    playerObject.FindProperty("definition").objectReferenceValue = target;
                    playerObject.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    playerPrefabs++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            UpdateBackPackPrefabs(target);
            UpdateCatalogs(target);
            EnsureStableId(target);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = target;
            Debug.Log($"Bound {playerPrefabs} player prefab(s) to {DefinitionPath}.", target);
        }

        private static void CopyDefinition(PlayerControl player, PlayerDefinition target)
        {
            PlayerDefinition source = player.Definition;
            if (source != null && source != target)
            {
                target.displayName = string.IsNullOrWhiteSpace(source.displayName) ? "Player" : source.displayName;
                target.description = source.description;
                target.health = source.health;
                target.defense = source.defense;
                target.reductionRate = source.reductionRate;
                target.cameraSensitivity = source.cameraSensitivity;
                target.cameraPitchMin = source.cameraPitchMin;
                target.cameraPitchMax = source.cameraPitchMax;
                target.startingWeapons = new List<WpConfig>(source.startingWeapons);
                target.startingItems = new List<StartingItemEntry>(source.startingItems);
                target.defaultRangedWeapon = source.defaultRangedWeapon;
                target.moveInputThreshold = source.moveInputThreshold;
                target.idleRootMotionScale = source.idleRootMotionScale;
                target.walkRootMotionScale = source.walkRootMotionScale;
                target.runRootMotionScale = source.runRootMotionScale;
                target.aimRootMotionScale = source.aimRootMotionScale;
                target.actionRootMotionScale = source.actionRootMotionScale;
                target.airborneRootMotionScale = source.airborneRootMotionScale;
                target.cinematicRootMotionScale = source.cinematicRootMotionScale;
                target.moveBlendDampTime = source.moveBlendDampTime;
                target.runTurnAngle = source.runTurnAngle;
                target.runSteerTriggerAngle = source.runSteerTriggerAngle;
                target.walkFacingMode = source.walkFacingMode;
                target.runFacingMode = source.runFacingMode;
                target.idleTurnStartAngle = source.idleTurnStartAngle;
                target.idleTurnStopAngle = source.idleTurnStopAngle;
                target.groundedDownSpeed = source.groundedDownSpeed;
                target.maxFallSpeed = source.maxFallSpeed;
                target.footstepDistance = source.footstepDistance;
                target.aimNearObstacleDistance = source.aimNearObstacleDistance;
                target.aimFarObstacleDistance = source.aimFarObstacleDistance;
                target.idleAnimation = source.idleAnimation;
                target.idleTurnAnimation = source.idleTurnAnimation;
                target.walkAnimation = source.walkAnimation;
                target.runAnimation = source.runAnimation;
                target.runTurnLeftAnimation = source.runTurnLeftAnimation;
                target.runTurnRightAnimation = source.runTurnRightAnimation;
                target.aimMoveAnimation = source.aimMoveAnimation;
                target.reloadAnimation = source.reloadAnimation;
                target.fallAnimation = source.fallAnimation;
                target.landAnimation = source.landAnimation;
                target.verticalBlendParameter = source.verticalBlendParameter;
                target.horizontalBlendParameter = source.horizontalBlendParameter;
                target.leftTurnTrigger = source.leftTurnTrigger;
                target.rightTurnTrigger = source.rightTurnTrigger;
                target.aimCloseParameter = source.aimCloseParameter;
                target.transitionOverrides = new List<PlayerTransitionOverride>();
                if (source.transitionOverrides != null)
                {
                    foreach (PlayerTransitionOverride rule in source.transitionOverrides)
                    {
                        if (rule == null) continue;
                        target.transitionOverrides.Add(new PlayerTransitionOverride
                        {
                            fromState = rule.fromState,
                            requestedState = rule.requestedState,
                            enabled = rule.enabled,
                            destinationState = rule.destinationState,
                            useCustomDestination = rule.useCustomDestination,
                            customDestinationId = rule.customDestinationId
                        });
                    }
                }

                target.customStates = new List<PlayerCustomStateDefinition>();
                if (source.customStates != null)
                    foreach (PlayerCustomStateDefinition state in source.customStates)
                    {
                        if (state == null) continue;
                        target.customStates.Add(new PlayerCustomStateDefinition
                        {
                            id = state.id,
                            displayName = state.displayName,
                            animationState = state.animationState,
                            motionMode = state.motionMode,
                            applyAnimationRotation = state.applyAnimationRotation,
                            rootMotionScale = state.rootMotionScale,
                            crossFadeTime = state.crossFadeTime,
                            returnToIdleOnAnimationEnd = state.returnToIdleOnAnimationEnd,
                            logic = state.logic
                        });
                    }

            }

            target.gravity = player.gravity;
            target.turnSpeed = player.turnSpeed;
            if (target.defaultRangedWeapon == null)
            {
                target.defaultRangedWeapon = FindWeapon(player.currentWeaponIndex);
            }
            target.meleeWeapon = player.knifeConfig;
            if (player.model != null)
            {
                target.interactRadius = player.model.checkRadius;
                target.enemyCheckRadius = player.model.enemyCheckRadius;
                target.groundCheckRadius = player.model.groundRadius;
                target.tacticalMoveRadius = player.model.radius;
            }
        }

        private static WpConfig FindWeapon(int legacyId)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:WpConfig"))
            {
                WpConfig weapon = AssetDatabase.LoadAssetAtPath<WpConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (weapon != null && weapon.ID == legacyId)
                {
                    return weapon;
                }
            }
            return null;
        }

        private static void UpdateBackPackPrefabs(PlayerDefinition definition)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab/GamePlay" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    BackPack[] backPacks = root.GetComponentsInChildren<BackPack>(true);
                    if (backPacks.Length == 0)
                    {
                        continue;
                    }

                    foreach (BackPack backPack in backPacks)
                    {
                        SerializedObject serialized = new SerializedObject(backPack);
                        serialized.FindProperty("playerDefinition").objectReferenceValue = definition;
                        serialized.ApplyModifiedPropertiesWithoutUndo();
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
        }

        private static void UpdateCatalogs(PlayerDefinition definition)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:GameContentCatalog"))
            {
                GameContentCatalog catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(AssetDatabase.GUIDToAssetPath(guid));
                SerializedObject serialized = new SerializedObject(catalog);
                serialized.FindProperty("player").objectReferenceValue = definition;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(catalog);
            }
        }

        private static void EnsureStableId(PlayerDefinition definition)
        {
            SerializedObject serialized = new SerializedObject(definition);
            SerializedProperty stableId = serialized.FindProperty("stableId");
            if (stableId != null && string.IsNullOrWhiteSpace(stableId.stringValue))
            {
                stableId.stringValue = GameContentId.Create();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = Path.Combine(parent, child).Replace('\\', '/');
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
#endif
