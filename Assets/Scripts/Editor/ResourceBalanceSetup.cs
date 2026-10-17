#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Astraparasitus.Editor
{
    public static class ResourceBalanceSetup
    {
        private const string Folder = "Assets/GameData/Resources";
        private const string TablePath = Folder + "/Level01ResourceDrops.asset";
        private const string ProfilePath = Folder + "/Level01ResourceBalance.asset";
        private const string GamePlayPath = "Assets/Prefab/GamePlay/GamePlay.prefab";

        [MenuItem("Tools/3D Adventure/Configure Level01 Dynamic Resources")]
        public static void Configure()
        {
            if (!AssetDatabase.IsValidFolder("Assets/GameData"))
                AssetDatabase.CreateFolder("Assets", "GameData");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/GameData", "Resources");

            ItemConfig ammo = AssetDatabase.LoadAssetAtPath<ItemConfig>("Assets/Scripts/SO/Item/PistolBullet.asset");
            ItemConfig green = AssetDatabase.LoadAssetAtPath<ItemConfig>("Assets/Scripts/SO/Item/Green.asset");
            ItemConfig spray = AssetDatabase.LoadAssetAtPath<ItemConfig>("Assets/Scripts/SO/Item/Spray.asset");
            if (ammo?.worldPrefab == null || green?.worldPrefab == null || spray?.worldPrefab == null)
            {
                Debug.LogError("Dynamic resource setup needs pistol ammo, green powder and spray world prefabs.");
                return;
            }

            LootTable table = AssetDatabase.LoadAssetAtPath<LootTable>(TablePath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<LootTable>();
                AssetDatabase.CreateAsset(table, TablePath);
                SerializedObject serialized = new SerializedObject(table);
                SerializedProperty entries = serialized.FindProperty("entries");
                entries.arraySize = 3;
                SetEntry(entries.GetArrayElementAtIndex(0), ammo, 6, 3, 6);
                SetEntry(entries.GetArrayElementAtIndex(1), green, 3, 1, 1);
                SetEntry(entries.GetArrayElementAtIndex(2), spray, 1, 1, 1);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            ResourceBalanceProfile profile = AssetDatabase.LoadAssetAtPath<ResourceBalanceProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<ResourceBalanceProfile>();
                profile.defaultLootTable = table;
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            GameObject root = PrefabUtility.LoadPrefabContents(GamePlayPath);
            try
            {
                GamePlay gamePlay = root.GetComponent<GamePlay>();
                if (gamePlay == null) throw new System.InvalidOperationException("GamePlay prefab has no GamePlay component.");
                ResourceDirector director = root.GetComponent<ResourceDirector>();
                if (director == null) director = root.AddComponent<ResourceDirector>();

                SerializedObject directorData = new SerializedObject(director);
                directorData.FindProperty("profile").objectReferenceValue = profile;
                directorData.ApplyModifiedPropertiesWithoutUndo();
                SerializedObject gameData = new SerializedObject(gamePlay);
                gameData.FindProperty("resourceDirector").objectReferenceValue = director;
                gameData.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, GamePlayPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("Level01 dynamic resources configured: profile, drop table and GamePlay prefab are connected.");
        }

        private static void SetEntry(SerializedProperty entry, ItemConfig item, int weight, int min, int max)
        {
            entry.FindPropertyRelative("item").objectReferenceValue = item;
            entry.FindPropertyRelative("prefab").objectReferenceValue = item.worldPrefab;
            entry.FindPropertyRelative("weight").intValue = weight;
            entry.FindPropertyRelative("minAmount").intValue = min;
            entry.FindPropertyRelative("maxAmount").intValue = max;
        }
    }
}
#endif
