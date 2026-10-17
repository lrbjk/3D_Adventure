#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Astraparasitus.Editor
{
    // 运动配置直接编辑 PlayerDefinition；状态表从实际代码读取，不另存一份流程。
    public class PlayerMovementTableWindow : EditorWindow
    {
        private sealed class MotionField
        {
            public readonly string group;
            public readonly string title;
            public readonly string path;
            public readonly string hint;

            public MotionField(string group, string title, string path, string hint = "")
            {
                this.group = group;
                this.title = title;
                this.path = path;
                this.hint = hint;
            }
        }

        private sealed class Transition
        {
            public readonly string target;
            public readonly int line;

            public Transition(string target, int line)
            {
                this.target = target;
                this.line = line;
            }
        }

        private sealed class StateRow
        {
            public string state;
            public string className;
            public string subStates;
            public string motionMode;
            public string animationRotation;
            public int line;
            public int customIndex = -1;
            public readonly List<Transition> transitions = new List<Transition>();
        }

        private sealed class ExternalTransition
        {
            public string path;
            public string target;
            public int line;
        }

        private const string StatePath = "Assets/Scripts/Player/States/PlayerStates.cs";
        private const string ControlPath = "Assets/Scripts/Player/PlayerControl.cs";
        private static readonly string[] ExternalPaths =
        {
            ControlPath,
            "Assets/Scripts/Player/PlayerGetDamage.cs",
            "Assets/Scripts/Weapons/WeaponControl.cs"
        };

        private static readonly MotionField[] MotionFields =
        {
            new MotionField("输入与朝向", "移动阈值", "moveInputThreshold", "超过此输入强度才进入移动状态"),
            new MotionField("输入与朝向", "走路朝向", "walkFacingMode", "相机前方适合侧移；输入方向适合自由移动"),
            new MotionField("输入与朝向", "跑步朝向", "runFacingMode"),
            new MotionField("输入与朝向", "转向速度", "turnSpeed", "度/秒"),
            new MotionField("输入与朝向", "原地转身开始角", "idleTurnStartAngle", "相机与角色朝向差达到此角度时开始"),
            new MotionField("输入与朝向", "原地转身结束角", "idleTurnStopAngle", "朝向差低于此角度时结束"),
            new MotionField("输入与朝向", "跑步急转角", "runTurnAngle", "超过此角度播放急转动画"),
            new MotionField("输入与朝向", "跑步微调角", "runSteerTriggerAngle", "超过此角度才触发左右转身参数"),
            new MotionField("重力与碰撞", "重力", "gravity"),
            new MotionField("重力与碰撞", "最大下落速度", "maxFallSpeed", "米/秒"),
            new MotionField("重力与碰撞", "接地向下速度", "groundedDownSpeed", "帮助 CharacterController 贴地"),
            new MotionField("Root Motion 倍率", "待机", "idleRootMotionScale"),
            new MotionField("Root Motion 倍率", "走路", "walkRootMotionScale"),
            new MotionField("Root Motion 倍率", "跑步", "runRootMotionScale"),
            new MotionField("Root Motion 倍率", "瞄准与换弹", "aimRootMotionScale"),
            new MotionField("Root Motion 倍率", "攻击与互动", "actionRootMotionScale"),
            new MotionField("Root Motion 倍率", "空中", "airborneRootMotionScale"),
            new MotionField("Root Motion 倍率", "演出", "cinematicRootMotionScale"),
            new MotionField("动作手感", "动画混合秒数", "moveBlendDampTime"),
            new MotionField("动作手感", "脚步间距", "footstepDistance", "累计水平位移达到此值时播放脚步声"),
            new MotionField("动作手感", "瞄准近距", "aimNearObstacleDistance", "进入近距离瞄准姿态"),
            new MotionField("动作手感", "瞄准远距", "aimFarObstacleDistance", "退出近距离瞄准姿态"),
            new MotionField("动画状态名", "待机", "idleAnimation"),
            new MotionField("动画状态名", "原地转身", "idleTurnAnimation"),
            new MotionField("动画状态名", "走路", "walkAnimation"),
            new MotionField("动画状态名", "跑步", "runAnimation"),
            new MotionField("动画状态名", "左急转", "runTurnLeftAnimation"),
            new MotionField("动画状态名", "右急转", "runTurnRightAnimation"),
            new MotionField("动画状态名", "瞄准移动", "aimMoveAnimation"),
            new MotionField("动画状态名", "换弹", "reloadAnimation"),
            new MotionField("动画状态名", "下落", "fallAnimation"),
            new MotionField("动画状态名", "落地", "landAnimation"),
            new MotionField("Animator 参数名", "前后混合", "verticalBlendParameter"),
            new MotionField("Animator 参数名", "左右混合", "horizontalBlendParameter"),
            new MotionField("Animator 参数名", "左转触发器", "leftTurnTrigger"),
            new MotionField("Animator 参数名", "右转触发器", "rightTurnTrigger"),
            new MotionField("Animator 参数名", "近距瞄准", "aimCloseParameter")
        };

        private readonly List<StateRow> states = new List<StateRow>();
        private readonly List<ExternalTransition> externalTransitions = new List<ExternalTransition>();
        private PlayerDefinition definition;
        private Vector2 scroll;
        private string search = "";
        private int tab;
        private bool matrixView;
        private bool showExternalOverrides;
        private int selectedState = -1;
        private float lastRepaint;

        [MenuItem("Tools/3D Adventure/Player Movement & State Table")]
        private static void Open()
        {
            PlayerMovementTableWindow window = GetWindow<PlayerMovementTableWindow>("玩家运动与状态表");
            window.minSize = new Vector2(850f, 450f);
            window.Reload();
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += Reload;
            EditorApplication.update += RepaintInPlayMode;
            Reload();
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= Reload;
            EditorApplication.update -= RepaintInPlayMode;
        }

        private void RepaintInPlayMode()
        {
            if (!EditorApplication.isPlaying || Time.realtimeSinceStartup - lastRepaint < 0.2f) return;
            lastRepaint = Time.realtimeSinceStartup;
            Repaint();
        }

        private void Reload()
        {
            if (definition == null)
            {
                definition = AssetDatabase.LoadAssetAtPath<PlayerDefinition>("Assets/GameData/Player/Player.asset");
                if (definition == null)
                {
                    string[] guids = AssetDatabase.FindAssets("t:PlayerDefinition");
                    if (guids.Length > 0)
                        definition = AssetDatabase.LoadAssetAtPath<PlayerDefinition>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
            ReloadFlow();
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            int nextTab = GUILayout.Toolbar(tab, new[] { "运动配置", "状态转移" }, EditorStyles.toolbarButton, GUILayout.Width(210f));
            if (nextTab != tab) { tab = nextTab; scroll = Vector2.zero; }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(55f))) Reload();
            if (GUILayout.Button("校验内容", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                EditorApplication.ExecuteMenuItem("Tools/3D Adventure/Validate Game Content");
            EditorGUILayout.EndHorizontal();

            if (tab == 0) DrawMotionTable();
            else DrawStateTable();
        }

        private void DrawMotionTable()
        {
            EditorGUILayout.BeginHorizontal();
            definition = (PlayerDefinition)EditorGUILayout.ObjectField("玩家配置", definition, typeof(PlayerDefinition), false);
            using (new EditorGUI.DisabledScope(definition == null))
            {
                if (GUILayout.Button("定位资产", GUILayout.Width(85f)))
                {
                    Selection.activeObject = definition;
                    EditorGUIUtility.PingObject(definition);
                }
                if (GUILayout.Button("保存修改", GUILayout.Width(85f))) AssetDatabase.SaveAssets();
            }
            EditorGUILayout.EndHorizontal();
            if (definition == null)
            {
                EditorGUILayout.HelpBox("请选择 PlayerDefinition 资产。", MessageType.Warning);
                return;
            }

            search = EditorGUILayout.TextField("筛选参数", search);
            EditorGUILayout.HelpBox("这里直接编辑 PlayerDefinition。动画状态名和参数名须与角色 Animator Controller 一致。", MessageType.Info);
            SerializedObject data = new SerializedObject(definition);
            data.Update();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("分类", EditorStyles.boldLabel, GUILayout.Width(130f));
            GUILayout.Label("参数", EditorStyles.boldLabel, GUILayout.Width(160f));
            GUILayout.Label("当前值", EditorStyles.boldLabel, GUILayout.Width(250f));
            GUILayout.Label("作用", EditorStyles.boldLabel, GUILayout.MinWidth(230f));
            EditorGUILayout.EndHorizontal();

            string shownGroup = null;
            EditorGUI.BeginChangeCheck();
            foreach (MotionField field in MotionFields)
            {
                if (!Matches(field)) continue;
                if (shownGroup != field.group)
                {
                    shownGroup = field.group;
                    EditorGUILayout.LabelField(shownGroup, EditorStyles.boldLabel);
                }
                SerializedProperty property = data.FindProperty(field.path);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(field.group, GUILayout.Width(130f));
                GUILayout.Label(new GUIContent(field.title, field.hint), GUILayout.Width(160f));
                if (property == null) GUILayout.Label("字段不存在", GUILayout.Width(250f));
                else EditorGUILayout.PropertyField(property, GUIContent.none, GUILayout.Width(250f));
                GUILayout.Label(field.hint, EditorStyles.wordWrappedMiniLabel, GUILayout.MinWidth(230f));
                EditorGUILayout.EndHorizontal();
            }
            if (EditorGUI.EndChangeCheck()) data.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();
        }

        private bool Matches(MotionField field)
        {
            if (string.IsNullOrWhiteSpace(search)) return true;
            return field.group.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   field.title.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   field.path.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   field.hint.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void DrawStateTable()
        {
            EditorGUILayout.BeginHorizontal();
            definition = (PlayerDefinition)EditorGUILayout.ObjectField("转移配置", definition, typeof(PlayerDefinition), false);
            using (new EditorGUI.DisabledScope(definition == null))
            {
                if (GUILayout.Button("保存修改", GUILayout.Width(85f))) AssetDatabase.SaveAssets();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox("内置状态从源码读取；可新增自定义状态，设置动画和运动参数，并挂接机制脚本资产。内置状态的已有跳转可改写到自定义状态。", MessageType.Info);
            DrawCustomStateActions();
            if (states.Count == 0)
            {
                EditorGUILayout.HelpBox("未读取到玩家状态。请检查 PlayerStates.cs。", MessageType.Warning);
                return;
            }
            matrixView = GUILayout.Toolbar(matrixView ? 1 : 0, new[] { "状态列表", "转移矩阵" }, GUILayout.Width(210f)) == 1;
            string activeClass = null;
            if (EditorApplication.isPlaying && PlayerControl.Current != null &&
                PlayerControl.Current.stateMachine != null && PlayerControl.Current.stateMachine.CurrentState != null)
                activeClass = PlayerControl.Current.stateMachine.CurrentState.GetType().Name;

            if (selectedState < 0) EditorGUILayout.HelpBox("点击状态名或矩阵行，编辑该状态已有的跳转。", MessageType.None);
            else DrawSelectedState();
            scroll = EditorGUILayout.BeginScrollView(scroll, true, true);
            if (matrixView) DrawMatrix(activeClass);
            else DrawStateRows(activeClass);
            EditorGUILayout.Space();
            DrawExternalTransitions();
            EditorGUILayout.EndScrollView();
        }

        private void DrawStateRows(string activeClass)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("当前", GUILayout.Width(45f));
            GUILayout.Label("状态", GUILayout.Width(100f));
            GUILayout.Label("运行类", GUILayout.Width(205f));
            GUILayout.Label("子状态", GUILayout.Width(170f));
            GUILayout.Label("运动模式", GUILayout.Width(130f));
            GUILayout.Label("动画旋转", GUILayout.Width(85f));
            GUILayout.Label("直接跳转到", GUILayout.MinWidth(300f));
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < states.Count; i++)
            {
                StateRow row = states[i];
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(activeClass == row.className && (row.customIndex < 0 ||
                    PlayerControl.Current.stateMachine.CurrentState is PlayerState_Custom custom &&
                    custom.definition.id == definition.customStates[row.customIndex].id) ? "●" : "", GUILayout.Width(45f));
                if (GUILayout.Button(row.state, EditorStyles.miniButton, GUILayout.Width(100f))) selectedState = i;
                if (GUILayout.Button(row.className, EditorStyles.linkLabel, GUILayout.Width(205f)))
                    OpenSource(row.customIndex < 0 ? StatePath : "Assets/Scripts/Player/PlayerCustomStateLogic.cs", row.line);
                GUILayout.Label(row.subStates, GUILayout.Width(170f));
                GUILayout.Label(row.motionMode, GUILayout.Width(130f));
                GUILayout.Label(row.animationRotation, GUILayout.Width(85f));
                GUILayout.Label(row.transitions.Count == 0 ? "—" : JoinTargets(row), GUILayout.MinWidth(300f));
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawCustomStateActions()
        {
            using (new EditorGUI.DisabledScope(definition == null))
            {
                if (!GUILayout.Button("＋ 新增状态", GUILayout.Width(120f))) return;
                Undo.RecordObject(definition, "新增玩家状态");
                if (definition.customStates == null)
                    definition.customStates = new List<PlayerCustomStateDefinition>();
                definition.customStates.Add(new PlayerCustomStateDefinition
                {
                    id = Guid.NewGuid().ToString("N"),
                    displayName = "新状态 " + (definition.customStates.Count + 1)
                });
                EditorUtility.SetDirty(definition);
                ReloadFlow();
                selectedState = states.Count - 1;
            }
        }

        private void DrawCustomStateEditor(int index)
        {
            if (definition == null || definition.customStates == null || index >= definition.customStates.Count) return;
            SerializedObject data = new SerializedObject(definition);
            data.Update();
            SerializedProperty row = data.FindProperty("customStates").GetArrayElementAtIndex(index);
            EditorGUILayout.LabelField("自定义状态", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.PropertyField(row.FindPropertyRelative("id"), new GUIContent("调用 ID"));
            EditorGUILayout.PropertyField(row.FindPropertyRelative("displayName"), new GUIContent("名称"));
            EditorGUILayout.PropertyField(row.FindPropertyRelative("animationState"), new GUIContent("动画状态"));
            EditorGUILayout.PropertyField(row.FindPropertyRelative("motionMode"), new GUIContent("运动模式"));
            EditorGUILayout.PropertyField(row.FindPropertyRelative("applyAnimationRotation"), new GUIContent("使用动画旋转"));
            EditorGUILayout.PropertyField(row.FindPropertyRelative("rootMotionScale"), new GUIContent("Root Motion 倍率"));
            EditorGUILayout.PropertyField(row.FindPropertyRelative("crossFadeTime"), new GUIContent("动画过渡时间"));
            EditorGUILayout.PropertyField(row.FindPropertyRelative("returnToIdleOnAnimationEnd"), new GUIContent("动画结束回 Idle"));
            EditorGUILayout.PropertyField(row.FindPropertyRelative("logic"), new GUIContent("机制逻辑资产"));
            if (data.ApplyModifiedProperties()) ReloadFlow();
            EditorGUILayout.HelpBox("在代码中调用 player.ChangeState(\"调用 ID\")。机制脚本继承 PlayerCustomStateLogic，创建该脚本类型的资产并拖到上方；只配动画时可留空。", MessageType.None);
            if (GUILayout.Button("删除这个状态", GUILayout.Width(120f)))
            {
                string id = definition.customStates[index].id;
                bool used = definition.transitionOverrides != null && definition.transitionOverrides.Exists(
                    rule => rule != null && rule.useCustomDestination && rule.customDestinationId == id);
                if (used)
                    EditorUtility.DisplayDialog("无法删除", "请先重置指向此状态的转移规则。", "知道了");
                else
                {
                    Undo.RecordObject(definition, "删除玩家状态");
                    definition.customStates.RemoveAt(index);
                    EditorUtility.SetDirty(definition);
                    selectedState = -1;
                    ReloadFlow();
                }
            }
        }

        private void DrawMatrix(string activeClass)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("来源 \\ 目标", GUILayout.Width(130f));
            foreach (StateRow column in states)
                GUILayout.Label(column.state, GUILayout.Width(76f));
            EditorGUILayout.EndHorizontal();
            for (int i = 0; i < states.Count; i++)
            {
                StateRow row = states[i];
                EditorGUILayout.BeginHorizontal();
                string label = activeClass == row.className ? "● " + row.state : row.state;
                if (GUILayout.Button(label, EditorStyles.linkLabel, GUILayout.Width(130f))) selectedState = i;
                foreach (StateRow column in states)
                {
                    bool linked = row.transitions.Exists(edge => EffectiveTarget(row, edge) == column.state);
                    GUILayout.Label(linked ? "●" : "·", GUILayout.Width(76f));
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawSelectedState()
        {
            if (selectedState < 0 || selectedState >= states.Count) return;
            StateRow row = states[selectedState];
            if (row.customIndex >= 0) { DrawCustomStateEditor(row.customIndex); return; }
            EditorGUILayout.LabelField(row.state + " 的转移规则", EditorStyles.boldLabel);
            if (row.transitions.Count == 0) EditorGUILayout.LabelField("无直接跳转（可能由外部事件控制或是终态）");
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("代码请求", GUILayout.Width(110f));
            GUILayout.Label("启用", GUILayout.Width(55f));
            GUILayout.Label("实际目标", GUILayout.Width(120f));
            GUILayout.Label("触发条件", GUILayout.Width(160f));
            GUILayout.Label("操作", GUILayout.Width(65f));
            EditorGUILayout.EndHorizontal();
            foreach (Transition edge in row.transitions)
                DrawEditableEdge(row.state, edge.target, StatePath, edge.line);
            showExternalOverrides = EditorGUILayout.Foldout(showExternalOverrides, "外部事件跳转（按当前状态覆盖）", true);
            if (!showExternalOverrides) return;
            HashSet<string> shown = new HashSet<string>();
            foreach (Transition edge in row.transitions) shown.Add(edge.target);
            foreach (ExternalTransition entry in externalTransitions)
            {
                if (shown.Add(entry.target)) DrawEditableEdge(row.state, entry.target, entry.path, entry.line);
            }
        }

        private void DrawEditableEdge(string source, string requestedName, string path, int line)
        {
            if (!Enum.TryParse(source, out PlayerState from) ||
                !Enum.TryParse(requestedName, out PlayerState requested)) return;
            PlayerTransitionOverride rule = definition != null
                ? definition.FindTransitionOverride(from, requested) : null;
            bool enabled = rule == null || rule.enabled;
            string destination = rule == null ? requestedName :
                rule.useCustomDestination ? rule.customDestinationId : rule.destinationState.ToString();
            List<string> optionNames = new List<string>();
            List<string> optionIds = new List<string>();
            foreach (StateRow row in states)
            {
                if (row.customIndex < 0 && !Enum.TryParse(row.state, out PlayerState _)) continue;
                optionNames.Add(row.state);
                optionIds.Add(row.customIndex < 0 ? row.state : definition.customStates[row.customIndex].id);
            }
            string[] options = optionNames.ToArray();
            int destinationIndex = optionIds.IndexOf(destination);
            if (destinationIndex < 0) destinationIndex = optionIds.IndexOf(requestedName);
            if (destinationIndex < 0) destinationIndex = 0;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(requestedName, Path.GetFileName(path)), GUILayout.Width(110f));
            using (new EditorGUI.DisabledScope(definition == null))
            {
                EditorGUI.BeginChangeCheck();
                bool nextEnabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(55f));
                int nextDestination = EditorGUILayout.Popup(destinationIndex, options, GUILayout.Width(120f));
                if (EditorGUI.EndChangeCheck())
                {
                    SetOverride(from, requested, nextEnabled, optionIds[nextDestination]);
                }
            }
            if (GUILayout.Button(Path.GetFileName(path) + ":" + line, EditorStyles.linkLabel, GUILayout.Width(160f)))
                OpenSource(path, line);
            using (new EditorGUI.DisabledScope(rule == null))
            {
                if (GUILayout.Button("重置", GUILayout.Width(65f))) RemoveOverride(from, requested);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawExternalTransitions()
        {
            EditorGUILayout.LabelField("外部入口", EditorStyles.boldLabel);
            if (externalTransitions.Count == 0) EditorGUILayout.LabelField("未找到状态类以外的直接跳转。");
            foreach (ExternalTransition entry in externalTransitions)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(Path.GetFileName(entry.path) + " → " + entry.target, GUILayout.Width(300f));
                if (GUILayout.Button("查看第 " + entry.line + " 行", EditorStyles.linkLabel, GUILayout.Width(120f)))
                    OpenSource(entry.path, entry.line);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.HelpBox("AimMove 和换弹移动属于现有状态类的内部子状态。外部入口也会按当前主状态匹配同一条覆盖规则；没有覆盖时沿用代码目标。停用关键跳转可能让角色停留在原状态。", MessageType.None);
        }

        private string JoinTargets(StateRow row)
        {
            string[] targets = new string[row.transitions.Count];
            for (int i = 0; i < row.transitions.Count; i++)
            {
                Transition edge = row.transitions[i];
                string effective = EffectiveTarget(row, edge);
                targets[i] = effective == null ? edge.target + "（停用）" :
                    effective == edge.target ? edge.target : edge.target + "→" + effective;
            }
            return string.Join("、", targets);
        }

        private string EffectiveTarget(StateRow row, Transition edge)
        {
            if (definition == null || !Enum.TryParse(row.state, out PlayerState from) ||
                !Enum.TryParse(edge.target, out PlayerState requested)) return edge.target;
            PlayerTransitionOverride rule = definition.FindTransitionOverride(from, requested);
            if (rule == null) return edge.target;
            if (!rule.enabled) return null;
            if (!rule.useCustomDestination) return rule.destinationState.ToString();
            PlayerCustomStateDefinition custom = definition.FindCustomState(rule.customDestinationId);
            return custom != null ? custom.displayName : null;
        }

        private void SetOverride(PlayerState from, PlayerState requested, bool enabled, string destination)
        {
            if (definition == null) return;
            bool isCustom = !Enum.TryParse(destination, out PlayerState builtInTarget);
            Undo.RecordObject(definition, "修改玩家状态转移");
            if (definition.transitionOverrides == null)
                definition.transitionOverrides = new List<PlayerTransitionOverride>();
            PlayerTransitionOverride rule = definition.FindTransitionOverride(from, requested);
            if (enabled && !isCustom && builtInTarget == requested)
            {
                if (rule != null) definition.transitionOverrides.Remove(rule);
            }
            else if (rule == null)
            {
                definition.transitionOverrides.Add(new PlayerTransitionOverride
                {
                    fromState = from,
                    requestedState = requested,
                    enabled = enabled,
                    destinationState = isCustom ? requested : builtInTarget,
                    useCustomDestination = isCustom,
                    customDestinationId = isCustom ? destination : null
                });
            }
            else
            {
                rule.enabled = enabled;
                rule.destinationState = isCustom ? requested : builtInTarget;
                rule.useCustomDestination = isCustom;
                rule.customDestinationId = isCustom ? destination : null;
            }
            EditorUtility.SetDirty(definition);
            Repaint();
        }

        private void RemoveOverride(PlayerState from, PlayerState requested)
        {
            if (definition == null) return;
            PlayerTransitionOverride rule = definition.FindTransitionOverride(from, requested);
            if (rule == null) return;
            Undo.RecordObject(definition, "重置玩家状态转移");
            definition.transitionOverrides.Remove(rule);
            EditorUtility.SetDirty(definition);
            Repaint();
        }

        private static void OpenSource(string path, int line)
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            if (script != null) AssetDatabase.OpenAsset(script, line);
        }

        private void ReloadFlow()
        {
            states.Clear();
            externalTransitions.Clear();
            if (!File.Exists(StatePath) || !File.Exists(ControlPath)) return;
            string control = File.ReadAllText(ControlPath);
            Dictionary<string, string> classStates = new Dictionary<string, string>();
            MatchCollection mappings = Regex.Matches(control,
                @"case\s+PlayerState\.(\w+)\s*:\s*stateMachine\.ChangeState<(\w+)>\s*\(\s*\)");
            foreach (Match mapping in mappings)
                classStates[mapping.Groups[2].Value] = mapping.Groups[1].Value;

            string source = File.ReadAllText(StatePath);
            MatchCollection classes = Regex.Matches(source, @"public\s+class\s+(\w+)\s*:\s*PlayerBaseState\b");
            for (int i = 0; i < classes.Count; i++)
            {
                Match match = classes[i];
                int end = i + 1 < classes.Count ? classes[i + 1].Index : source.Length;
                string body = source.Substring(match.Index, end - match.Index);
                string className = match.Groups[1].Value;
                StateRow row = new StateRow
                {
                    state = classStates.TryGetValue(className, out string state) ? state : className,
                    className = className,
                    line = LineAt(source, match.Index),
                    subStates = FindSubStates(body),
                    motionMode = FindMotionMode(body),
                    animationRotation = FindAnimationRotation(body)
                };
                foreach (Match transition in Regex.Matches(body, @"player\.ChangeState\s*\(\s*PlayerState\.(\w+)\s*\)"))
                {
                    string target = transition.Groups[1].Value;
                    if (row.transitions.Exists(edge => edge.target == target)) continue;
                    row.transitions.Add(new Transition(target, LineAt(source, match.Index + transition.Index)));
                }
                states.Add(row);
            }

            if (definition != null && definition.customStates != null)
                for (int i = 0; i < definition.customStates.Count; i++)
                {
                    PlayerCustomStateDefinition custom = definition.customStates[i];
                    if (custom == null) continue;
                    StateRow row = new StateRow
                    {
                        state = custom.displayName,
                        className = "PlayerState_Custom",
                        subStates = "自定义",
                        motionMode = custom.motionMode.ToString(),
                        animationRotation = custom.applyAnimationRotation ? "是" : "否",
                        customIndex = i
                    };
                    if (custom.returnToIdleOnAnimationEnd) row.transitions.Add(new Transition("Idle", 0));
                    states.Add(row);
                }

            foreach (string path in ExternalPaths)
            {
                if (!File.Exists(path)) continue;
                string externalSource = File.ReadAllText(path);
                foreach (Match transition in Regex.Matches(externalSource, @"\bChangeState\s*\(\s*PlayerState\.(\w+)\s*\)"))
                {
                    // Start 的初始 Idle 没有来源状态，不属于可覆盖转移。
                    if (path == ControlPath && transition.Groups[1].Value == "Idle" &&
                        transition.Index < externalSource.IndexOf("void Update()", StringComparison.Ordinal)) continue;
                    externalTransitions.Add(new ExternalTransition
                    {
                        path = path,
                        target = transition.Groups[1].Value,
                        line = LineAt(externalSource, transition.Index)
                    });
                }
            }
            selectedState = Mathf.Clamp(selectedState, -1, states.Count - 1);
        }

        private static string FindSubStates(string body)
        {
            Match sub = Regex.Match(body, @"private\s+enum\s+\w+\s*\{([^}]*)\}", RegexOptions.Singleline);
            if (!sub.Success) return "—";
            List<string> names = new List<string>();
            foreach (Match value in Regex.Matches(sub.Groups[1].Value, @"\b\w+\b"))
                names.Add(value.Value);
            return names.Count == 0 ? "—" : string.Join(" / ", names);
        }

        private static string FindMotionMode(string body)
        {
            Match expression = Regex.Match(body, @"override\s+PlayerMotionMode\s+MotionMode\s*=>\s*(.*?);", RegexOptions.Singleline);
            if (!expression.Success) return "Action（默认）";
            List<string> modes = new List<string>();
            foreach (Match mode in Regex.Matches(expression.Groups[1].Value, @"PlayerMotionMode\.(\w+)"))
            {
                string name = mode.Groups[1].Value;
                if (!modes.Contains(name)) modes.Add(name);
            }
            return modes.Count == 0 ? "动态计算" : string.Join(" / ", modes);
        }

        private static string FindAnimationRotation(string body)
        {
            Match expression = Regex.Match(body, @"override\s+bool\s+ApplyAnimationRotation\s*=>\s*(.*?);");
            if (!expression.Success) return "关闭";
            return expression.Groups[1].Value.Trim() == "true" ? "开启" : "按子状态";
        }

        private static int LineAt(string source, int index)
        {
            int line = 1;
            for (int i = 0; i < index && i < source.Length; i++)
                if (source[i] == '\n') line++;
            return line;
        }
    }
}
#endif
