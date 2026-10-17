#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Astraparasitus.Editor
{
    [InitializeOnLoad]
    public static class StableIdMigration
    {
        private const string SessionKey = "3DAdventure.StableIdMigration";

        static StableIdMigration()
        {
            if (!SessionState.GetBool(SessionKey, false))
            {
                EditorApplication.delayCall += Run;
            }
        }

        [MenuItem("Tools/3D Adventure/Assign Missing Stable IDs")]
        public static void Run()
        {
            SessionState.SetBool(SessionKey, true);
            int changed = 0;
            changed += Assign<ItemConfig>();
            changed += Assign<WpConfig>();
            changed += Assign<PlayerDefinition>();
            changed += Assign<EnemyArchetype>();
            changed += Assign<SkillConfig>();
            changed += Assign<QuestInfo>();

            if (changed > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"Assigned stable IDs to {changed} game content assets.");
            }
        }

        private static int Assign<T>() where T : Object
        {
            int changed = 0;
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                {
                    continue;
                }

                SerializedObject serialized = new SerializedObject(asset);
                SerializedProperty id = serialized.FindProperty("stableId");
                if (id == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(id.stringValue))
                {
                    id.stringValue = GameContentId.Create();
                    changed++;
                }

                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }

            return changed;
        }
    }

    [CustomPropertyDrawer(typeof(LevelCondition))]
    public class LevelConditionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;
            Rect row = new Rect(position.x, position.y, position.width, line);

            SerializedProperty type = property.FindPropertyRelative("type");
            EditorGUI.PropertyField(row, type, label);
            row.y += line + gap;
            EditorGUI.indentLevel++;
            EditorGUI.PropertyField(row, property.FindPropertyRelative("invert"));
            row.y += line + gap;

            switch ((LevelConditionType)type.enumValueIndex)
            {
                case LevelConditionType.GameObjectActive:
                    EditorGUI.PropertyField(row, property.FindPropertyRelative("targetObject"));
                    break;
                case LevelConditionType.DoorOpen:
                    EditorGUI.PropertyField(row, property.FindPropertyRelative("door"));
                    break;
                case LevelConditionType.HasItem:
                    EditorGUI.PropertyField(row, property.FindPropertyRelative("item"));
                    break;
                case LevelConditionType.QuestState:
                    EditorGUI.PropertyField(row, property.FindPropertyRelative("quest"));
                    row.y += line + gap;
                    EditorGUI.PropertyField(row, property.FindPropertyRelative("questState"));
                    break;
            }

            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            LevelConditionType type = (LevelConditionType)property.FindPropertyRelative("type").enumValueIndex;
            int lines = type == LevelConditionType.QuestState ? 4 :
                type == LevelConditionType.Always || type == LevelConditionType.NoActiveEnemies ? 2 : 3;
            return lines * EditorGUIUtility.singleLineHeight + (lines - 1) * EditorGUIUtility.standardVerticalSpacing;
        }
    }

    public class GameContentValidator : EditorWindow
    {
        [MenuItem("Tools/3D Adventure/Validate Game Content")]
        public static void ValidateContent()
        {
            int errors = 0;
            int warnings = 0;
            Dictionary<string, Object> stableIds = new Dictionary<string, Object>();

            errors += ValidateIds<ItemConfig>(stableIds);
            errors += ValidateIds<WpConfig>(stableIds);
            errors += ValidateIds<PlayerDefinition>(stableIds);
            errors += ValidateIds<EnemyArchetype>(stableIds);
            errors += ValidateIds<SkillConfig>(stableIds);
            errors += ValidateIds<QuestInfo>(stableIds);
            ValidateItemAuthoring(ref errors, ref warnings);
            ValidateWeaponAuthoring(ref errors, ref warnings);
            ValidatePlayerAuthoring(ref errors, ref warnings);
            ValidateDynamicResources(ref errors);
            LevelSceneContractValidator.ValidateOpenScenes(ref errors, ref warnings);

            Dictionary<string, SaveableId> saveableIds = new Dictionary<string, SaveableId>();
            SaveableId[] saveables = Object.FindObjectsByType<SaveableId>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (SaveableId saveable in saveables)
            {
                if (string.IsNullOrWhiteSpace(saveable.Id))
                {
                    Debug.LogError("Saveable object has no stable ID.", saveable);
                    errors++;
                    continue;
                }

                if (saveableIds.TryGetValue(saveable.Id, out SaveableId duplicate))
                {
                    Debug.LogError($"Duplicate SaveableId: {saveable.Id}. Also used by {duplicate.name}.", saveable);
                    errors++;
                    continue;
                }

                saveableIds.Add(saveable.Id, saveable);
            }

            EnemySpawn[] spawns = Object.FindObjectsByType<EnemySpawn>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (EnemySpawn spawn in spawns)
            {
                if (spawn.enemyPrefab == null)
                {
                    Debug.LogWarning("EnemySpawn has no enemy prefab.", spawn);
                    warnings++;
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:QuestInfo"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                QuestInfo quest = AssetDatabase.LoadAssetAtPath<QuestInfo>(path);
                if (quest == null)
                {
                    continue;
                }

                if (quest.questStepPrefab == null || quest.questStepPrefab.Length == 0)
                {
                    Debug.LogWarning($"Quest has no steps: {quest.name}", quest);
                    warnings++;
                }
            }

            if (errors == 0 && warnings == 0)
            {
                Debug.Log("Game content validation passed.");
            }
            else
            {
                Debug.Log($"Game content validation finished. Errors: {errors}, Warnings: {warnings}");
            }
        }

        private static void ValidateDynamicResources(ref int errors)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ResourceBalanceProfile"))
            {
                ResourceBalanceProfile profile = AssetDatabase.LoadAssetAtPath<ResourceBalanceProfile>(AssetDatabase.GUIDToAssetPath(guid));
                if (profile == null) continue;
                if (profile.defaultLootTable == null)
                {
                    Debug.LogError($"Resource balance has no default LootTable: {profile.name}", profile);
                    errors++;
                    continue;
                }
                if (profile.defaultLootTable.Entries.Count == 0)
                {
                    Debug.LogError($"Resource LootTable is empty: {profile.defaultLootTable.name}", profile.defaultLootTable);
                    errors++;
                }
                foreach (LootEntry entry in profile.defaultLootTable.Entries)
                {
                    ItemBase pickup = entry?.prefab != null ? entry.prefab.GetComponentInChildren<ItemBase>(true) : null;
                    if (entry == null || entry.item == null || pickup == null || pickup.itemConfig != entry.item ||
                        entry.weight <= 0 || entry.minAmount <= 0 || entry.maxAmount < entry.minAmount)
                    {
                        Debug.LogError($"Resource LootTable has an invalid entry: {profile.defaultLootTable.name}", profile.defaultLootTable);
                        errors++;
                    }
                }
            }

            foreach (GamePlay gamePlay in Object.FindObjectsByType<GamePlay>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!gamePlay.gameObject.scene.IsValid()) continue;
                if (gamePlay.Resources == null || gamePlay.Resources.Profile == null)
                {
                    Debug.LogError($"GamePlay has no configured ResourceDirector: {gamePlay.gameObject.scene.name}", gamePlay);
                    errors++;
                }
            }
        }

        private static int ValidateIds<T>(Dictionary<string, Object> stableIds)
            where T : Object, IGameContent
        {
            int errors = 0;
            foreach (string guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T content = AssetDatabase.LoadAssetAtPath<T>(path);
                if (content == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(content.StableId))
                {
                    Debug.LogError($"Content has no stable ID: {path}", content);
                    errors++;
                    continue;
                }

                if (stableIds.TryGetValue(content.StableId, out Object duplicate))
                {
                    Debug.LogError($"Duplicate content ID: {content.StableId}. Also used by {duplicate.name}.", content);
                    errors++;
                    continue;
                }

                stableIds.Add(content.StableId, content);
            }

            return errors;
        }

        private static void ValidateItemAuthoring(ref int errors, ref int warnings)
        {
            Dictionary<int, ItemConfig> legacyIds = new Dictionary<int, ItemConfig>();
            foreach (string guid in AssetDatabase.FindAssets("t:ItemConfig"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemConfig item = AssetDatabase.LoadAssetAtPath<ItemConfig>(path);
                if (item == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.itemName))
                {
                    Debug.LogError($"Item has no display name: {path}", item);
                    errors++;
                }

                if (item.itemType == ItemType.Pickup && item.icon == null)
                {
                    Debug.LogWarning($"Pickup item has no icon: {path}", item);
                    warnings++;
                }

                if (item.isCombinable && (item.combineWith == null || item.combineResult == null))
                {
                    Debug.LogError($"Combinable item requires Combine With and Combine Result: {path}", item);
                    errors++;
                }

                if (legacyIds.TryGetValue(item.id, out ItemConfig duplicate))
                {
                    Debug.LogError($"Duplicate item ID {item.id}: {path}. Also used by {duplicate.name}.", item);
                    errors++;
                }
                else
                {
                    legacyIds.Add(item.id, item);
                }
            }
        }

        private static void ValidateWeaponAuthoring(ref int errors, ref int warnings)
        {
            Dictionary<int, WpConfig> legacyIds = new Dictionary<int, WpConfig>();
            foreach (string guid in AssetDatabase.FindAssets("t:WpConfig"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WpConfig weapon = AssetDatabase.LoadAssetAtPath<WpConfig>(path);
                if (weapon == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(weapon.Name))
                {
                    Debug.LogError($"Weapon has no display name: {path}", weapon);
                    errors++;
                }
                if (weapon.canDrop && weapon.pickupPrefab == null)
                {
                    Debug.LogWarning($"Weapon has no pickup prefab: {path}", weapon);
                    warnings++;
                }
                if (weapon.weaponPrefab == null || weapon.wpIcon == null)
                {
                    Debug.LogWarning($"Weapon presentation assets are incomplete: {path}", weapon);
                    warnings++;
                }
                if (weapon.UsesAmmo && (weapon.ammoItem == null || weapon.bulletPrefab == null))
                {
                    Debug.LogError($"Ranged weapon requires Ammo Item and Bullet Prefab: {path}", weapon);
                    errors++;
                }
                if (legacyIds.TryGetValue(weapon.ID, out WpConfig duplicate))
                {
                    Debug.LogError($"Duplicate legacy weapon ID {weapon.ID}: {path}. Also used by {duplicate.name}.", weapon);
                    errors++;
                }
                else
                {
                    legacyIds.Add(weapon.ID, weapon);
                }
            }
        }

        private static void ValidatePlayerAuthoring(ref int errors, ref int warnings)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:PlayerDefinition"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PlayerDefinition definition = AssetDatabase.LoadAssetAtPath<PlayerDefinition>(path);
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.displayName))
                {
                    Debug.LogError($"Player definition has no display name: {path}", definition);
                    errors++;
                }
                if (definition.meleeWeapon == null)
                {
                    Debug.LogWarning($"Player definition has no melee weapon: {path}", definition);
                    warnings++;
                }
                string[] motionNames =
                {
                    definition.idleAnimation, definition.idleTurnAnimation, definition.walkAnimation,
                    definition.runAnimation, definition.runTurnLeftAnimation, definition.runTurnRightAnimation,
                    definition.aimMoveAnimation, definition.reloadAnimation, definition.fallAnimation,
                    definition.landAnimation, definition.verticalBlendParameter, definition.horizontalBlendParameter,
                    definition.leftTurnTrigger, definition.rightTurnTrigger, definition.aimCloseParameter
                };
                foreach (string name in motionNames)
                {
                    if (!string.IsNullOrWhiteSpace(name)) continue;
                    Debug.LogWarning($"Player movement animation or Animator parameter is empty: {path}", definition);
                    warnings++;
                    break;
                }
                if (definition.idleTurnStopAngle > definition.idleTurnStartAngle ||
                    definition.aimFarObstacleDistance < definition.aimNearObstacleDistance ||
                    definition.runSteerTriggerAngle > definition.runTurnAngle)
                {
                    Debug.LogError($"Player movement thresholds have an invalid order: {path}", definition);
                    errors++;
                }
                HashSet<string> transitionKeys = new HashSet<string>();
                HashSet<string> customStateIds = new HashSet<string>();
                if (definition.customStates != null)
                    foreach (PlayerCustomStateDefinition state in definition.customStates)
                    {
                        if (state == null || string.IsNullOrWhiteSpace(state.id) ||
                            string.IsNullOrWhiteSpace(state.displayName) || !customStateIds.Add(state.id))
                        {
                            Debug.LogError($"Player custom state has an empty or duplicated ID/name: {path}", definition);
                            errors++;
                        }
                        else if (state.returnToIdleOnAnimationEnd && string.IsNullOrWhiteSpace(state.animationState))
                        {
                            Debug.LogWarning($"Player custom state auto return needs an animation: {state.displayName} ({path})", definition);
                            warnings++;
                        }
                    }
                if (definition.transitionOverrides != null)
                {
                    foreach (PlayerTransitionOverride rule in definition.transitionOverrides)
                    {
                        if (rule == null) continue;
                        string key = rule.fromState + ":" + rule.requestedState;
                        if (!transitionKeys.Add(key) || rule.fromState == PlayerState.AimMove ||
                            rule.fromState == PlayerState.Custom || rule.requestedState == PlayerState.AimMove ||
                            rule.requestedState == PlayerState.Custom ||
                            (!rule.useCustomDestination && (rule.destinationState == PlayerState.AimMove ||
                                rule.destinationState == PlayerState.Custom)) ||
                            (rule.useCustomDestination && !customStateIds.Contains(rule.customDestinationId)))
                        {
                            Debug.LogError($"Player transition override has an invalid source or target: {key} ({path})", definition);
                            errors++;
                        }
                        if (rule.enabled && !rule.useCustomDestination && rule.fromState == rule.destinationState)
                        {
                            Debug.LogWarning($"Player transition override loops back to its own state: {key} ({path})", definition);
                            warnings++;
                        }
                        if (rule.requestedState == PlayerState.Dead &&
                            (!rule.enabled || rule.useCustomDestination || rule.destinationState != PlayerState.Dead))
                        {
                            Debug.LogWarning($"Player death transition was disabled or redirected: {key} ({path})", definition);
                            warnings++;
                        }
                    }
                }
                foreach (StartingItemEntry entry in definition.startingItems)
                {
                    if (entry.item == null)
                    {
                        Debug.LogError($"Player definition contains an empty starting item: {path}", definition);
                        errors++;
                    }
                }
            }
        }
    }

    [CustomEditor(typeof(ItemConfig))]
    public class ItemConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("id"), new GUIContent("旧版数字 ID"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemName"), new GUIContent("显示名称"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"), new GUIContent("策划说明"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("itemType"), new GUIContent("交互类型"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultAmount"), new GUIContent("默认数量"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("表现资源", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("icon"), new GUIContent("背包图标"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("worldPrefab"), new GUIContent("世界拾取 Prefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inspectPrefab"), new GUIContent("查看模型 Prefab"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("使用效果", EditorStyles.boldLabel);
            SerializedProperty useEffect = serializedObject.FindProperty("useEffect");
            EditorGUILayout.PropertyField(useEffect, new GUIContent("效果类型"));
            if ((ItemUseEffect)useEffect.enumValueIndex != ItemUseEffect.None)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("effectAmount"), new GUIContent("效果数值"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("组合规则", EditorStyles.boldLabel);
            SerializedProperty combinable = serializedObject.FindProperty("isCombinable");
            EditorGUILayout.PropertyField(combinable, new GUIContent("允许组合"));
            if (combinable.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("combineWith"), new GUIContent("组合材料"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("combineResult"), new GUIContent("组合结果"));
            }

            serializedObject.ApplyModifiedProperties();
            DrawValidation((ItemConfig)target);

            EditorGUILayout.Space();
            if (GUILayout.Button("自动迁移旧物品引用"))
            {
                ItemConfigMigration.MigrateOne((ItemConfig)target);
            }
            if (GUILayout.Button("打开世界拾取 Prefab"))
            {
                ItemConfig config = (ItemConfig)target;
                if (config.worldPrefab != null)
                {
                    AssetDatabase.OpenAsset(config.worldPrefab);
                }
            }
        }

        private static void DrawValidation(ItemConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.itemName))
            {
                EditorGUILayout.HelpBox("显示名称不能为空。", MessageType.Error);
            }
            if (config.itemType == ItemType.Pickup && config.icon == null)
            {
                EditorGUILayout.HelpBox("拾取物品建议配置背包图标。", MessageType.Warning);
            }
            if (config.isCombinable && (config.combineWith == null || config.combineResult == null))
            {
                EditorGUILayout.HelpBox("组合物品必须同时配置组合材料和组合结果。", MessageType.Error);
            }
        }
    }

    [InitializeOnLoad]
    public static class ItemConfigMigration
    {
        private const string SessionKey = "3DAdventure.ItemConfigMigrationV3";

        static ItemConfigMigration()
        {
            if (!SessionState.GetBool(SessionKey, false))
            {
                EditorApplication.delayCall += MigrateAll;
            }
        }

        [MenuItem("Tools/3D Adventure/Migrate Item Configs")]
        public static void MigrateAll()
        {
            SessionState.SetBool(SessionKey, true);
            foreach (string guid in AssetDatabase.FindAssets("t:ItemConfig"))
            {
                ItemConfig config = AssetDatabase.LoadAssetAtPath<ItemConfig>(AssetDatabase.GUIDToAssetPath(guid));
                MigrateOne(config, false);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Item configuration migration completed.");
        }

        public static void MigrateOne(ItemConfig config, bool saveAssets = true)
        {
            if (config == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty legacyItem = serialized.FindProperty("item");
            SerializedProperty combineWith = serialized.FindProperty("combineWith");
            int legacyCombineId = legacyItem.FindPropertyRelative("combineId").intValue;
            if (combineWith.objectReferenceValue == null && legacyCombineId > 0)
            {
                combineWith.objectReferenceValue = FindItemByLegacyId(legacyCombineId, config);
            }

            SerializedProperty worldPrefab = serialized.FindProperty("worldPrefab");
            if (worldPrefab.objectReferenceValue == null)
            {
                worldPrefab.objectReferenceValue = FindWorldPrefab(config);
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
            if (saveAssets)
            {
                AssetDatabase.SaveAssets();
            }
        }

        private static ItemConfig FindItemByLegacyId(int id, ItemConfig exclude)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ItemConfig"))
            {
                ItemConfig candidate = AssetDatabase.LoadAssetAtPath<ItemConfig>(AssetDatabase.GUIDToAssetPath(guid));
                if (candidate != null && candidate != exclude && candidate.id == id)
                {
                    return candidate;
                }
            }
            return null;
        }

        private static GameObject FindWorldPrefab(ItemConfig config)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab/Item" }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null)
                {
                    continue;
                }

                foreach (ItemBase itemBase in prefab.GetComponentsInChildren<ItemBase>(true))
                {
                    if (itemBase.itemConfig == config)
                    {
                        return prefab;
                    }
                }
            }
            return null;
        }
    }

    [CustomEditor(typeof(WpConfig))]
    public class WeaponConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ID"), new GUIContent("旧版数字 ID"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("Name"), new GUIContent("显示名称"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("description"), new GUIContent("策划说明"));
            SerializedProperty weaponType = serializedObject.FindProperty("weaponType");
            EditorGUILayout.PropertyField(weaponType, new GUIContent("武器分类"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("表现资源", EditorStyles.boldLabel);
            SerializedProperty canDrop = serializedObject.FindProperty("canDrop");
            EditorGUILayout.PropertyField(canDrop, new GUIContent("允许丢弃"));
            if (canDrop.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pickupPrefab"), new GUIContent("世界拾取 Prefab"));
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("weaponPrefab"), new GUIContent("查看模型 Prefab"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wpIcon"), new GUIContent("背包图标"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wpSimpleIcon"), new GUIContent("HUD 图标"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("战斗参数", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("damage"), new GUIContent("伤害"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stunRate"), new GUIContent("硬直倍率"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("range"), new GUIContent("有效距离"));

            bool usesAmmo = (WeaponType)weaponType.enumValueIndex != WeaponType.Knife;
            if (usesAmmo)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("fireRate"), new GUIContent("射击间隔"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("reloadTime"), new GUIContent("换弹时间"));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("弹药与弹道", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("ammoItem"), new GUIContent("弹药物品"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bulletSize"), new GUIContent("弹匣容量"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bulletTotalNum"), new GUIContent("初始弹量"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bulletSpeed"), new GUIContent("弹丸速度"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bulletSpread"), new GUIContent("散布"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("bulletPrefab"), new GUIContent("弹丸 Prefab"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("muzzleFirePrefab"), new GUIContent("枪口火焰"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("impactEffectPrefab"), new GUIContent("枪口反馈特效"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hitEffectPrefab"), new GUIContent("命中特效组"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("hitImpacts"), new GUIContent("命中冲击组"), true);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("shakeData"), new GUIContent("镜头震动"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("动画状态", EditorStyles.boldLabel);
            DrawAnimationField("toIdle", "拿出武器");
            DrawAnimationField("IdleAnim", "持有待机");
            DrawAnimationField("onHandAnim", "持有覆盖层");
            if (usesAmmo)
            {
                DrawAnimationField("AimAnim", "瞄准");
                DrawAnimationField("AimCloseAnim", "退出瞄准");
                DrawAnimationField("reloadAnim", "换弹");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("音效事件", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("shootSound"), new GUIContent(usesAmmo ? "射击" : "挥击"));
            if (usesAmmo)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("reloadSound"), new GUIContent("换弹"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("emptySound"), new GUIContent("空仓"));
            }

            serializedObject.ApplyModifiedProperties();
            DrawValidation((WpConfig)target);

            EditorGUILayout.Space();
            if (GUILayout.Button("自动补齐武器资产引用"))
            {
                WeaponConfigMigration.MigrateOne((WpConfig)target);
            }
            DrawOpenAssetButton("打开世界拾取 Prefab", ((WpConfig)target).pickupPrefab);
            DrawOpenAssetButton("打开查看模型 Prefab", ((WpConfig)target).weaponPrefab);
        }

        private void DrawAnimationField(string propertyName, string label)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(propertyName), new GUIContent(label));
        }

        private static void DrawValidation(WpConfig weapon)
        {
            if (string.IsNullOrWhiteSpace(weapon.Name))
            {
                EditorGUILayout.HelpBox("显示名称不能为空。", MessageType.Error);
            }
            if (weapon.canDrop && weapon.pickupPrefab == null)
            {
                EditorGUILayout.HelpBox("建议配置世界拾取 Prefab，武器丢弃时将直接使用它。", MessageType.Warning);
            }
            if (weapon.UsesAmmo && weapon.ammoItem == null)
            {
                EditorGUILayout.HelpBox("远程武器必须关联弹药物品。", MessageType.Error);
            }
            if (weapon.UsesAmmo && weapon.bulletPrefab == null)
            {
                EditorGUILayout.HelpBox("远程武器必须配置弹丸 Prefab。", MessageType.Error);
            }
        }

        private static void DrawOpenAssetButton(string label, Object asset)
        {
            using (new EditorGUI.DisabledScope(asset == null))
            {
                if (GUILayout.Button(label))
                {
                    AssetDatabase.OpenAsset(asset);
                }
            }
        }
    }

    [InitializeOnLoad]
    public static class WeaponConfigMigration
    {
        private const string SessionKey = "3DAdventure.WeaponConfigMigrationV1";

        static WeaponConfigMigration()
        {
            if (!SessionState.GetBool(SessionKey, false))
            {
                EditorApplication.delayCall += MigrateAll;
            }
        }

        [MenuItem("Tools/3D Adventure/Migrate Weapon Configs")]
        public static void MigrateAll()
        {
            SessionState.SetBool(SessionKey, true);
            foreach (string guid in AssetDatabase.FindAssets("t:WpConfig"))
            {
                WpConfig config = AssetDatabase.LoadAssetAtPath<WpConfig>(AssetDatabase.GUIDToAssetPath(guid));
                MigrateOne(config, false);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Weapon configuration migration completed.");
        }

        public static void MigrateOne(WpConfig config, bool saveAssets = true)
        {
            if (config == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(config);
            SerializedProperty pickupPrefab = serialized.FindProperty("pickupPrefab");
            if (config.canDrop && pickupPrefab.objectReferenceValue == null)
            {
                pickupPrefab.objectReferenceValue = FindPickupPrefab(config);
            }

            SerializedProperty ammoItem = serialized.FindProperty("ammoItem");
            if (config.UsesAmmo && ammoItem.objectReferenceValue == null)
            {
                ammoItem.objectReferenceValue = FindAmmoItem(config);
            }

            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
            if (saveAssets)
            {
                AssetDatabase.SaveAssets();
            }
        }

        private static GameObject FindPickupPrefab(WpConfig config)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefab/Weapons" }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null)
                {
                    continue;
                }

                foreach (PickWeapon pickup in prefab.GetComponentsInChildren<PickWeapon>(true))
                {
                    if (pickup.weapon == config)
                    {
                        return prefab;
                    }
                }
            }
            return null;
        }

        private static ItemConfig FindAmmoItem(WpConfig config)
        {
            string preferredName = config.Name + "Bullet";
            ItemConfig fallback = null;
            foreach (string guid in AssetDatabase.FindAssets("t:ItemConfig"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ItemConfig item = AssetDatabase.LoadAssetAtPath<ItemConfig>(path);
                if (item == null || item.id != config.bulletId)
                {
                    continue;
                }

                if (System.IO.Path.GetFileNameWithoutExtension(path) == preferredName)
                {
                    return item;
                }

                fallback ??= item;
            }
            return fallback;
        }
    }

    [CustomEditor(typeof(GameContentCatalog))]
    public class GameContentCatalogEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            if (GUILayout.Button("Collect All Game Content"))
            {
                CollectPlayer();
                Collect("items", "t:ItemConfig", typeof(ItemConfig));
                Collect("weapons", "t:WpConfig", typeof(WpConfig));
                Collect("enemies", "t:EnemyArchetype", typeof(EnemyArchetype));
                Collect("quests", "t:QuestInfo", typeof(QuestInfo));
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                AssetDatabase.SaveAssets();
            }
        }

        private void CollectPlayer()
        {
            SerializedProperty property = serializedObject.FindProperty("player");
            foreach (string guid in AssetDatabase.FindAssets("t:PlayerDefinition", new[] { "Assets/GameData/Player" }))
            {
                PlayerDefinition definition = AssetDatabase.LoadAssetAtPath<PlayerDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (definition != null)
                {
                    property.objectReferenceValue = definition;
                    return;
                }
            }
        }

        private void Collect(string propertyName, string filter, System.Type type)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            string[] guids = AssetDatabase.FindAssets(filter);
            property.arraySize = guids.Length;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                property.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath(path, type);
            }
        }
    }
}
#endif
