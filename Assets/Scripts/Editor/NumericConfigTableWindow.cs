#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Astraparasitus.Editor
{
    // 只提供现有配置资产的另一种编辑视图，不保存第二份数值。
    public class NumericConfigTableWindow : EditorWindow
    {
        private sealed class Column
        {
            public readonly string title;
            public readonly string path;
            public readonly float width;
            public readonly string hint;

            public Column(string title, string path, float width = 95f, string hint = "")
            {
                this.title = title;
                this.path = path;
                this.width = width;
                this.hint = hint;
            }
        }

        private sealed class Page
        {
            public readonly string title;
            public readonly Type assetType;
            public readonly Column[] columns;
            public readonly string arrayPath;

            public Page(string title, Type assetType, Column[] columns, string arrayPath = null)
            {
                this.title = title;
                this.assetType = assetType;
                this.columns = columns;
                this.arrayPath = arrayPath;
            }
        }

        private static readonly Page[] Pages =
        {
            new Page("玩家", typeof(PlayerDefinition), new[]
            {
                new Column("名称", "displayName", 130),
                new Column("说明", "description", 180),
                new Column("生命上限", "health", 90, "玩家最大生命值"),
                new Column("防御", "defense", 80, "玩家防御值"),
                new Column("受伤比例", "reductionRate", 90, "存在防御值时，实际扣除生命的比例"),
                new Column("镜头灵敏度", "cameraSensitivity", 110),
                new Column("镜头最低角", "cameraPitchMin", 110), new Column("镜头最高角", "cameraPitchMax", 110),
                new Column("交互半径", "interactRadius"), new Column("敌人检测半径", "enemyCheckRadius", 120),
                new Column("地面检测半径", "groundCheckRadius", 120),
                new Column("战术移动半径", "tacticalMoveRadius", 120),
                new Column("默认远程武器", "defaultRangedWeapon", 170, "瞄准时默认选择，不代表开局拥有"),
                new Column("近战武器", "meleeWeapon", 170, "拥有后才能近战攻击")
            }),
            new Page("武器", typeof(WpConfig), new[]
            {
                new Column("类型", "weaponType", 95), new Column("伤害", "damage"),
                new Column("硬直", "stunRate"), new Column("射速", "fireRate"),
                new Column("射程", "range"), new Column("换弹时间", "reloadTime", 100),
                new Column("弹药物品", "ammoItem", 180), new Column("弹速", "bulletSpeed"),
                new Column("弹丸数量", "bulletSize", 100), new Column("弹匣容量", "bulletTotalNum", 100),
                new Column("散射", "bulletSpread")
            }),
            new Page("敌人", typeof(EnemyArchetype), new[]
            {
                new Column("生命", "health"), new Column("耐力", "endurance"),
                new Column("移动倍率", "speedRate", 100), new Column("体型", "size", 80),
                new Column("攻击伤害", "damage", 100), new Column("重力", "gravity"),
                new Column("受击硬直", "hitStunValue", 100), new Column("硬直时间", "stunTime", 100),
                new Column("恢复时间", "recoverTime", 100), new Column("左腿耐久", "leftLegValue", 100),
                new Column("右腿耐久", "rightLegValue", 100), new Column("左臂耐久", "leftArmValue", 100),
                new Column("右臂耐久", "rightArmValue", 100),
                new Column("视野半径", "viewRadius", 100), new Column("视角步数", "viewAngleStep", 100),
                new Column("视角", "viewAngle"), new Column("视野偏移", "viewOffset", 220),
                new Column("攻击距离", "attackDistance", 100), new Column("远程攻击距离", "farAttackDistance", 120),
                new Column("处决距离", "executionDistance", 100), new Column("遗忘延迟", "forgetDelay", 100)
            }),
            new Page("敌人技能", typeof(SkillConfig), new[]
            {
                new Column("技能名", "skillName", 150), new Column("事件", "eventName", 100),
                new Column("耐力消耗", "endurance", 100), new Column("伤害倍率", "damageRate", 100),
                new Column("起始帧", "startframe"), new Column("结束帧", "endframe"),
                new Column("命中盒索引", "hitboxIndex", 110)
            }),
            new Page("物品", typeof(ItemConfig), new[]
            {
                new Column("旧版数字 ID", "id", 110, "兼容旧存档；请勿随意修改"),
                new Column("类型", "itemType", 100), new Column("默认数量", "defaultAmount", 100),
                new Column("使用效果", "useEffect", 115), new Column("效果量", "effectAmount", 100)
            }),
            new Page("资源平衡", typeof(ResourceBalanceProfile), new[]
            {
                new Column("敌人掉率", "enemyChance", 100), new Column("容器掉率", "containerChance", 100),
                new Column("柜门掉率", "doorChance", 100), new Column("最多空掉落", "maxEmptyRolls", 110,
                    "真正短缺时，连续空结果达到该次数后触发救急"),
                new Column("低弹药", "lowAmmo"), new Column("充足弹药", "comfortableAmmo", 100),
                new Column("短缺权重", "shortageWeight", 100), new Column("充足权重", "surplusWeight", 100),
                new Column("低血量比例", "lowHealthRatio", 110),
                new Column("充足治疗品数", "comfortableHealingItems", 120),
                new Column("默认掉落池", "defaultLootTable", 170)
            }),
            new Page("资源·掉落池", typeof(LootTable), new[]
            {
                new Column("物品", "item", 160), new Column("世界 Prefab", "prefab", 170),
                new Column("权重", "weight", 80), new Column("最少数量", "minAmount", 100),
                new Column("最多数量", "maxAmount", 100)
            }, "entries"),
            new Page("任务奖励", typeof(QuestInfo), new[]
            {
                new Column("任务名", "displayName", 190), new Column("类型", "type", 110),
                new Column("奖励值", "reward", 100)
            }),
            new Page("场景·事件", typeof(LevelEventTrigger), new[]
            {
                new Column("触发方式", "mode", 120), new Column("仅触发一次", "triggerOnce", 105),
                new Column("检查间隔", "checkInterval", 105, "条件检查的间隔，秒")
            }),
            new Page("场景·敌人波次", typeof(EnemyWave), new[]
            {
                new Column("生成点", "spawnPoint", 180),
                new Column("延迟秒数", "delay", 105, "在生成该敌人前等待的秒数")
            }, "entries"),
            new Page("场景·提示", typeof(ShowTipAction), new[]
            {
                new Column("提示文本", "message", 260), new Column("持续秒数", "duration", 105)
            }),
            new Page("场景·物理冲量", typeof(RigidbodyImpulseAction), new[]
            {
                new Column("目标", "target", 170), new Column("局部方向", "localDirection", 220),
                new Column("冲量", "force", 100)
            })
        };

        private readonly List<UnityEngine.Object> assets = new List<UnityEngine.Object>();
        private int pageIndex;
        private string search = "";
        private string newName = "";
        private Vector2 scroll;

        [MenuItem("Tools/3D Adventure/Numeric Config Tables")]
        private static void Open()
        {
            NumericConfigTableWindow window = GetWindow<NumericConfigTableWindow>("数值配置表");
            window.minSize = new Vector2(650f, 300f);
            window.Reload();
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += Reload;
            EditorApplication.hierarchyChanged += Reload;
            Reload();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= Reload;
            EditorApplication.hierarchyChanged -= Reload;
        }

        private void Reload()
        {
            assets.Clear();
            Page page = Pages[Mathf.Clamp(pageIndex, 0, Pages.Length - 1)];
            if (typeof(Component).IsAssignableFrom(page.assetType))
            {
                AddSceneObjects(page.assetType);
                assets.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
                Repaint();
                return;
            }
            foreach (string guid in AssetDatabase.FindAssets("t:" + page.assetType.Name))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset != null && page.assetType.IsInstanceOfType(asset)) assets.Add(asset);
            }
            assets.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            Repaint();
        }

        private void AddSceneObjects(Type type)
        {
            if (type == typeof(LevelEventTrigger))
                assets.AddRange(UnityEngine.Object.FindObjectsByType<LevelEventTrigger>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            else if (type == typeof(EnemyWave))
                assets.AddRange(UnityEngine.Object.FindObjectsByType<EnemyWave>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            else if (type == typeof(ShowTipAction))
                assets.AddRange(UnityEngine.Object.FindObjectsByType<ShowTipAction>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            else if (type == typeof(RigidbodyImpulseAction))
                assets.AddRange(UnityEngine.Object.FindObjectsByType<RigidbodyImpulseAction>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string[] names = Array.ConvertAll(Pages, page => page.title);
            int nextPage = EditorGUILayout.Popup(pageIndex, names, EditorStyles.toolbarPopup, GUILayout.Width(160f));
            if (nextPage != pageIndex)
            {
                pageIndex = nextPage;
                scroll = Vector2.zero;
                newName = "";
                Reload();
            }
            search = GUILayout.TextField(search, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField,
                GUILayout.MinWidth(130f));
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(50f))) Reload();
            if (GUILayout.Button("保存修改", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveOpenScenes();
            }
            if (GUILayout.Button("校验内容", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                EditorApplication.ExecuteMenuItem("Tools/3D Adventure/Validate Game Content");
            if (Pages[pageIndex].assetType == typeof(PlayerDefinition) &&
                GUILayout.Button("运动与状态", EditorStyles.toolbarButton, GUILayout.Width(85f)))
                EditorApplication.ExecuteMenuItem("Tools/3D Adventure/Player Movement & State Table");
            EditorGUILayout.EndHorizontal();

            Page page = Pages[pageIndex];
            DrawAddControls(page);
            EditorGUILayout.HelpBox("直接编辑原配置；可按名称或表内数值查找。删除资产前会检查引用，场景组件删除支持 Unity 撤销。", MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll, true, true);
            DrawHeader(page);
            int shown = 0;
            foreach (UnityEngine.Object asset in assets)
            {
                if (asset == null || !Matches(page, asset)) continue;
                shown++;
                if (page.arrayPath == null) DrawAssetRow(page, asset);
                else DrawArrayRows(page, asset);
            }
            if (shown == 0) EditorGUILayout.HelpBox("没有匹配的配置资产。可在 Project 中创建或清空搜索条件。", MessageType.None);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.LabelField($"{page.title} · {shown} 个配置项", EditorStyles.miniLabel);
        }

        private void DrawAddControls(Page page)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("新增", GUILayout.Width(35f));
            bool isScenePage = typeof(Component).IsAssignableFrom(page.assetType);
            bool needsSelection = page.assetType == typeof(ShowTipAction) ||
                                  page.assetType == typeof(RigidbodyImpulseAction);
            GameObject selected = Selection.activeGameObject;
            bool hasSceneSelection = selected != null && selected.scene.IsValid() && selected.scene.isLoaded;
            if (!needsSelection) newName = EditorGUILayout.TextField(newName, GUILayout.Width(190f));
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying || needsSelection && !hasSceneSelection))
            {
                string label = !isScenePage ? "新建配置资产" :
                    needsSelection ? "给选中物体添加组件" : "新建场景物体";
                if (GUILayout.Button(label, GUILayout.Width(isScenePage ? 140f : 110f)))
                {
                    if (isScenePage) CreateSceneEntry(page, hasSceneSelection ? selected : null);
                    else CreateAssetEntry(page);
                    GUIUtility.ExitGUI();
                }
            }
            if (needsSelection && !hasSceneSelection)
                GUILayout.Label("请先选中场景物体", EditorStyles.miniLabel);
            else if (!isScenePage)
                GUILayout.Label("空名称使用默认名称；重名会自动编号", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void CreateAssetEntry(Page page)
        {
            string name = string.IsNullOrWhiteSpace(newName) ? "New" + page.assetType.Name : newName.Trim();
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name == "." || name == "..")
            {
                Debug.LogError("配置名称包含文件名不允许的字符。");
                return;
            }

            string folder = GetAssetFolder(page.assetType);
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogError($"配置目录不存在：{folder}");
                return;
            }

            ScriptableObject created = CreateInstance(page.assetType);
            created.name = name;
            InitializeNewAsset(created, name);
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + ".asset");
            AssetDatabase.CreateAsset(created, path);
            RegisterInCatalog(created);
            EditorUtility.SetDirty(created);
            AssetDatabase.SaveAssets();
            search = "";
            newName = "";
            Reload();
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
        }

        private void InitializeNewAsset(ScriptableObject asset, string name)
        {
            if (asset is IGameContent)
            {
                SerializedObject data = new SerializedObject(asset);
                SerializedProperty stableId = data.FindProperty("stableId");
                if (stableId != null && string.IsNullOrWhiteSpace(stableId.stringValue))
                {
                    stableId.stringValue = GameContentId.Create();
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            if (asset is PlayerDefinition player) player.displayName = name;
            else if (asset is WpConfig weapon)
            {
                weapon.Name = name;
                weapon.ID = NextLegacyId(typeof(WpConfig));
            }
            else if (asset is EnemyArchetype enemy)
            {
                enemy.Name = name;
                enemy.ID = NextLegacyId(typeof(EnemyArchetype));
            }
            else if (asset is SkillConfig skill) skill.skillName = name;
            else if (asset is ItemConfig item)
            {
                item.itemName = name;
                item.id = NextLegacyId(typeof(ItemConfig));
                item.CreateRuntimeItem();
            }
            else if (asset is QuestInfo quest) quest.displayName = name;
        }

        private static int NextLegacyId(Type type)
        {
            int max = -1;
            foreach (string guid in AssetDatabase.FindAssets("t:" + type.Name))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ScriptableObject existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (type == typeof(ItemConfig) && existing is ItemConfig item) max = Mathf.Max(max, item.id);
                else if (type == typeof(WpConfig) && existing is WpConfig weapon) max = Mathf.Max(max, weapon.ID);
                else if (type == typeof(EnemyArchetype) && existing is EnemyArchetype enemy) max = Mathf.Max(max, enemy.ID);
            }
            return max + 1;
        }

        private static string GetAssetFolder(Type type)
        {
            if (type == typeof(PlayerDefinition)) return "Assets/GameData/Player";
            if (type == typeof(WpConfig)) return "Assets/Scripts/SO/weapons";
            if (type == typeof(EnemyArchetype)) return "Assets/GameData/Enemies";
            if (type == typeof(SkillConfig)) return "Assets/Scripts/SO/Enemy/Skills";
            if (type == typeof(ItemConfig)) return "Assets/Scripts/SO/Item";
            if (type == typeof(QuestInfo)) return "Assets/Resources/Quest";
            return "Assets/GameData/Resources";
        }

        private static void RegisterInCatalog(ScriptableObject asset)
        {
            string propertyName = asset is ItemConfig ? "items" : asset is WpConfig ? "weapons" :
                asset is EnemyArchetype ? "enemies" : asset is QuestInfo ? "quests" : null;
            if (propertyName == null) return;
            GameContentCatalog catalog = GameContentCatalog.Default;
            if (catalog == null)
            {
                Debug.LogWarning("新配置已创建，但没有找到 GameContentCatalog；请手动登记。", asset);
                return;
            }

            SerializedObject data = new SerializedObject(catalog);
            SerializedProperty list = data.FindProperty(propertyName);
            if (list == null || !list.isArray) return;
            int index = list.arraySize;
            list.InsertArrayElementAtIndex(index);
            list.GetArrayElementAtIndex(index).objectReferenceValue = asset;
            data.ApplyModifiedProperties();
        }

        private void CreateSceneEntry(Page page, GameObject selected)
        {
            bool attach = page.assetType == typeof(ShowTipAction) || page.assetType == typeof(RigidbodyImpulseAction);
            GameObject target;
            if (attach)
            {
                target = selected;
                if (target == null) return;
            }
            else
            {
                string name = string.IsNullOrWhiteSpace(newName) ? "New " + page.assetType.Name : newName.Trim();
                target = new GameObject(name);
                Undo.RegisterCreatedObjectUndo(target, "Create " + page.title);
                GameObject parent = selected != null ? selected : GameObject.Find("__Level");
                if (parent != null) GameObjectUtility.SetParentAndAlign(target, parent);
            }

            Component component = Undo.AddComponent(target, page.assetType);
            if (component is LevelEventTrigger)
            {
                SerializedObject data = new SerializedObject(component);
                data.FindProperty("mode").enumValueIndex = (int)LevelEventMode.Manual;
                data.ApplyModifiedProperties();
            }
            EditorSceneManager.MarkSceneDirty(target.scene);
            search = "";
            newName = "";
            Reload();
            Selection.activeGameObject = target;
            EditorGUIUtility.PingObject(target);
        }

        private bool Matches(Page page, UnityEngine.Object asset)
        {
            if (string.IsNullOrWhiteSpace(search)) return true;
            string query = search.Trim();
            if (asset.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            SerializedObject data = new SerializedObject(asset);
            data.Update();
            SerializedProperty array = page.arrayPath == null ? null : data.FindProperty(page.arrayPath);
            if (array != null && array.isArray)
            {
                for (int i = 0; i < array.arraySize; i++)
                    if (MatchesColumns(page.columns, array.GetArrayElementAtIndex(i), query)) return true;
                return false;
            }
            foreach (Column column in page.columns)
                if (MatchesValue(data.FindProperty(column.path), query)) return true;
            if (asset is PlayerDefinition)
            {
                if (MatchesPlayerList(data.FindProperty("startingItems"), query) ||
                    MatchesPlayerList(data.FindProperty("startingWeapons"), query)) return true;
            }
            return false;
        }

        private static bool MatchesPlayerList(SerializedProperty list, string query)
        {
            if (list == null || !list.isArray) return false;
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                if (MatchesValue(element, query)) return true;
                if (element.propertyType == SerializedPropertyType.Generic &&
                    (MatchesValue(element.FindPropertyRelative("item"), query) ||
                     MatchesValue(element.FindPropertyRelative("amount"), query))) return true;
            }
            return false;
        }

        private static bool MatchesColumns(Column[] columns, SerializedProperty element, string query)
        {
            foreach (Column column in columns)
                if (MatchesValue(element.FindPropertyRelative(column.path), query)) return true;
            return false;
        }

        private static bool MatchesValue(SerializedProperty property, string query)
        {
            if (property == null) return false;
            string value;
            switch (property.propertyType)
            {
                case SerializedPropertyType.String: value = property.stringValue; break;
                case SerializedPropertyType.Integer: value = property.intValue.ToString(); break;
                case SerializedPropertyType.Float: value = property.floatValue.ToString(); break;
                case SerializedPropertyType.Boolean: value = property.boolValue ? "true" : "false"; break;
                case SerializedPropertyType.Enum:
                    value = property.enumValueIndex >= 0 && property.enumValueIndex < property.enumDisplayNames.Length
                        ? property.enumDisplayNames[property.enumValueIndex] : "";
                    break;
                case SerializedPropertyType.ObjectReference:
                    value = property.objectReferenceValue != null ? property.objectReferenceValue.name : "";
                    break;
                case SerializedPropertyType.Vector3: value = property.vector3Value.ToString(); break;
                default: return false;
            }
            return !string.IsNullOrEmpty(value) && value.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void DrawHeader(Page page)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("配置资产", EditorStyles.boldLabel, GUILayout.Width(190f));
            if (page.arrayPath != null) GUILayout.Label("序号", EditorStyles.boldLabel, GUILayout.Width(45f));
            foreach (Column column in page.columns)
                GUILayout.Label(new GUIContent(column.title, column.hint), EditorStyles.boldLabel, GUILayout.Width(column.width));
            GUILayout.Label("操作", EditorStyles.boldLabel, GUILayout.Width(page.arrayPath == null ? 95f : 160f));
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawAssetButton(UnityEngine.Object asset)
        {
            if (GUILayout.Button(asset.name, EditorStyles.linkLabel, GUILayout.Width(190f)))
            {
                Selection.activeObject = asset is Component component ? component.gameObject : asset;
                EditorGUIUtility.PingObject(asset);
            }
        }

        private void DrawAssetRow(Page page, UnityEngine.Object asset)
        {
            SerializedObject data = new SerializedObject(asset);
            data.Update();
            EditorGUILayout.BeginHorizontal();
            DrawAssetButton(asset);
            EditorGUI.BeginChangeCheck();
            DrawColumns(page.columns, data, null);
            bool changed = EditorGUI.EndChangeCheck();
            if (GUILayout.Button(asset is Component ? "删除组件" : "删除资产", GUILayout.Width(80f)))
                EditorApplication.delayCall += () => DeleteEntry(asset);
            EditorGUILayout.EndHorizontal();
            if (changed) data.ApplyModifiedProperties();
            if (asset is PlayerDefinition) DrawPlayerLoadout(data);
        }

        private static void DrawPlayerLoadout(SerializedObject data)
        {
            data.Update();
            SerializedProperty items = data.FindProperty("startingItems");
            SerializedProperty weapons = data.FindProperty("startingWeapons");
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(550f));
            EditorGUILayout.LabelField("开局配置", EditorStyles.boldLabel);
            if (items != null)
            {
                EditorGUILayout.LabelField("初始物品", EditorStyles.miniBoldLabel);
                for (int i = 0; i < items.arraySize; i++)
                {
                    SerializedProperty entry = items.GetArrayElementAtIndex(i);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("item"), GUIContent.none, GUILayout.Width(300f));
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("amount"), GUIContent.none, GUILayout.Width(95f));
                    if (EditorGUI.EndChangeCheck()) data.ApplyModifiedProperties();
                    if (GUILayout.Button("删除", GUILayout.Width(55f)))
                    {
                        items.DeleteArrayElementAtIndex(i);
                        data.ApplyModifiedProperties();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("+ 添加初始物品", GUILayout.Width(150f)))
                {
                    int index = items.arraySize;
                    items.InsertArrayElementAtIndex(index);
                    SerializedProperty entry = items.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("item").objectReferenceValue = null;
                    entry.FindPropertyRelative("amount").intValue = 1;
                    data.ApplyModifiedProperties();
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                }
            }
            if (weapons != null)
            {
                EditorGUILayout.LabelField("初始武器", EditorStyles.miniBoldLabel);
                for (int i = 0; i < weapons.arraySize; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(weapons.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(395f));
                    if (EditorGUI.EndChangeCheck()) data.ApplyModifiedProperties();
                    if (GUILayout.Button("删除", GUILayout.Width(55f)))
                    {
                        int before = weapons.arraySize;
                        weapons.DeleteArrayElementAtIndex(i);
                        if (weapons.arraySize == before) weapons.DeleteArrayElementAtIndex(i);
                        data.ApplyModifiedProperties();
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        GUIUtility.ExitGUI();
                    }
                    EditorGUILayout.EndHorizontal();
                }
                if (GUILayout.Button("+ 添加初始武器", GUILayout.Width(150f)))
                {
                    int index = weapons.arraySize;
                    weapons.InsertArrayElementAtIndex(index);
                    weapons.GetArrayElementAtIndex(index).objectReferenceValue = null;
                    data.ApplyModifiedProperties();
                    EditorGUILayout.EndVertical();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawArrayRows(Page page, UnityEngine.Object asset)
        {
            SerializedObject data = new SerializedObject(asset);
            data.Update();
            SerializedProperty array = data.FindProperty(page.arrayPath);
            if (array == null || !array.isArray) return;

            for (int i = 0; i < array.arraySize; i++)
            {
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                DrawAssetButton(asset);
                GUILayout.Label((i + 1).ToString(), GUILayout.Width(45f));
                EditorGUI.BeginChangeCheck();
                DrawColumns(page.columns, data, element);
                bool changed = EditorGUI.EndChangeCheck();
                if (GUILayout.Button("删除", GUILayout.Width(50f)))
                {
                    // Unity 对引用数组第一次删除可能只清空引用；双重删除确保整行移除。
                    int before = array.arraySize;
                    array.DeleteArrayElementAtIndex(i);
                    if (array.arraySize == before) array.DeleteArrayElementAtIndex(i);
                    data.ApplyModifiedProperties();
                    EditorGUILayout.EndHorizontal();
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
                if (changed) data.ApplyModifiedProperties();
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(190f);
            if (GUILayout.Button("+ 添加到 " + asset.name, GUILayout.Width(170f)))
            {
                int index = array.arraySize;
                array.InsertArrayElementAtIndex(index);
                SerializedProperty added = array.GetArrayElementAtIndex(index);
                foreach (Column column in page.columns)
                {
                    SerializedProperty field = added.FindPropertyRelative(column.path);
                    if (field == null) continue;
                    if (field.propertyType == SerializedPropertyType.ObjectReference) field.objectReferenceValue = null;
                    else if (field.propertyType == SerializedPropertyType.Integer) field.intValue = 1;
                    else if (field.propertyType == SerializedPropertyType.Float) field.floatValue = 0f;
                }
                data.ApplyModifiedProperties();
                EditorGUILayout.EndHorizontal();
                GUIUtility.ExitGUI();
            }
            if (GUILayout.Button(asset is Component ? "删除组件" : "删除资产", GUILayout.Width(80f)))
                EditorApplication.delayCall += () => DeleteEntry(asset);
            EditorGUILayout.EndHorizontal();
        }

        private void DeleteEntry(UnityEngine.Object target)
        {
            if (target == null || EditorApplication.isPlaying) return;
            if (target is Component component)
            {
                if (!EditorUtility.DisplayDialog("删除场景组件", $"从 {component.gameObject.name} 删除 {component.GetType().Name}？", "删除", "取消"))
                    return;
                Undo.DestroyObjectImmediate(component);
                Reload();
                return;
            }

            if (!(target is ScriptableObject asset)) return;
            GameContentCatalog catalog = GameContentCatalog.Default;
            if (asset is PlayerDefinition && catalog != null && catalog.Player == asset)
            {
                EditorUtility.DisplayDialog("无法删除", "这是当前 GameContentCatalog 使用的玩家定义，请先替换该引用。", "确定");
                return;
            }
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (!SceneManager.GetSceneAt(i).isDirty) continue;
                EditorUtility.DisplayDialog("请先保存场景", "当前场景有未保存修改。先保存场景，才能准确检查资产引用。", "确定");
                return;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return;
            List<string> references = FindReferences(path, catalog);
            if (references.Count > 0)
            {
                EditorUtility.DisplayDialog("资产仍被引用",
                    asset.name + " 仍被以下文件使用，请先移除引用：\n" + string.Join("\n", references), "确定");
                return;
            }
            if (!EditorUtility.DisplayDialog("删除配置资产", $"确定删除 {asset.name}？\n{path}", "删除", "取消"))
                return;

            if (!DeleteAssetFile(asset, catalog))
            {
                Debug.LogError("删除配置资产失败：" + path);
                return;
            }
            if (Selection.activeObject == asset) Selection.activeObject = null;
            Reload();
        }

        internal static bool DeleteAssetFile(ScriptableObject asset, GameContentCatalog catalog)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return false;
            bool wasRegistered = RemoveFromCatalog(asset, catalog);
            if (!AssetDatabase.DeleteAsset(path))
            {
                if (wasRegistered) RegisterInCatalog(asset);
                return false;
            }
            AssetDatabase.SaveAssets();
            return true;
        }

        internal static List<string> FindReferences(string targetPath, GameContentCatalog catalog)
        {
            List<string> references = new List<string>();
            string catalogPath = catalog != null ? AssetDatabase.GetAssetPath(catalog) : null;
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path == targetPath || path == catalogPath)
                    continue;
                string extension = Path.GetExtension(path);
                if (extension != ".asset" && extension != ".prefab" && extension != ".unity" &&
                    extension != ".controller" && extension != ".playable" && extension != ".mat")
                    continue;
                foreach (string dependency in AssetDatabase.GetDependencies(path, false))
                {
                    if (!string.Equals(dependency, targetPath, StringComparison.OrdinalIgnoreCase)) continue;
                    references.Add(path);
                    break;
                }
                if (references.Count >= 6) break;
            }
            return references;
        }

        internal static bool RemoveFromCatalog(ScriptableObject asset, GameContentCatalog catalog)
        {
            if (catalog == null) return false;
            string propertyName = asset is ItemConfig ? "items" : asset is WpConfig ? "weapons" :
                asset is EnemyArchetype ? "enemies" : asset is QuestInfo ? "quests" : null;
            if (propertyName == null) return false;

            SerializedObject data = new SerializedObject(catalog);
            SerializedProperty list = data.FindProperty(propertyName);
            if (list == null || !list.isArray) return false;
            bool removed = false;
            for (int i = list.arraySize - 1; i >= 0; i--)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue != asset) continue;
                removed = true;
                int before = list.arraySize;
                list.DeleteArrayElementAtIndex(i);
                if (list.arraySize == before) list.DeleteArrayElementAtIndex(i);
            }
            data.ApplyModifiedProperties();
            return removed;
        }

        private static void DrawColumns(Column[] columns, SerializedObject data, SerializedProperty element)
        {
            foreach (Column column in columns)
            {
                SerializedProperty field = element == null ? data.FindProperty(column.path) :
                    element.FindPropertyRelative(column.path);
                if (field == null) GUILayout.Label("字段不存在", GUILayout.Width(column.width));
                else EditorGUILayout.PropertyField(field, GUIContent.none, GUILayout.Width(column.width));
            }
        }
    }
}
#endif
