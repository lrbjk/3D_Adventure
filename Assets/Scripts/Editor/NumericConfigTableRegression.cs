#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Astraparasitus.Editor
{
    public static class NumericConfigTableRegression
    {
        [MenuItem("Tools/3D Adventure/Run Numeric Table CRUD Regression")]
        public static void Run()
        {
            string name = "__NumericTableRegression_" + Guid.NewGuid().ToString("N");
            string path = "Assets/Scripts/SO/Item/" + name + ".asset";
            UnityEngine.Object previousSelection = Selection.activeObject;
            NumericConfigTableWindow window = ScriptableObject.CreateInstance<NumericConfigTableWindow>();
            GameContentCatalog catalog = GameContentCatalog.Default;
            try
            {
                if (catalog == null) throw new InvalidOperationException("GameContentCatalog is missing.");
                Type windowType = typeof(NumericConfigTableWindow);
                FieldInfo pagesField = windowType.GetField("Pages", BindingFlags.Static | BindingFlags.NonPublic);
                FieldInfo nameField = windowType.GetField("newName", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo create = windowType.GetMethod("CreateAssetEntry", BindingFlags.Instance | BindingFlags.NonPublic);
                if (pagesField == null || nameField == null || create == null)
                    throw new InvalidOperationException("Numeric table creation entry point is missing.");

                object itemPage = null;
                foreach (object page in (Array)pagesField.GetValue(null))
                {
                    Type type = (Type)page.GetType().GetField("assetType").GetValue(page);
                    if (type == typeof(ItemConfig)) { itemPage = page; break; }
                }
                if (itemPage == null) throw new InvalidOperationException("Item page is missing.");
                nameField.SetValue(window, name);
                create.Invoke(window, new[] { itemPage });

                ItemConfig item = AssetDatabase.LoadAssetAtPath<ItemConfig>(path);
                if (item == null || item.itemName != name || string.IsNullOrWhiteSpace(item.StableId))
                    throw new InvalidOperationException("New item asset was not initialized correctly.");
                if (!Contains(catalog, item))
                    throw new InvalidOperationException("New item was not registered in GameContentCatalog.");
                if (NumericConfigTableWindow.FindReferences(path, catalog).Count != 0)
                    throw new InvalidOperationException("A new item was incorrectly reported as referenced.");
                foreach (string guid in AssetDatabase.FindAssets("t:ItemConfig"))
                {
                    ItemConfig other = AssetDatabase.LoadAssetAtPath<ItemConfig>(AssetDatabase.GUIDToAssetPath(guid));
                    if (other != null && other != item && other.id == item.id)
                        throw new InvalidOperationException("New item reused a legacy numeric ID.");
                }
                FieldInfo searchField = windowType.GetField("search", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo matches = windowType.GetMethod("Matches", BindingFlags.Instance | BindingFlags.NonPublic);
                searchField.SetValue(window, item.id.ToString());
                if (!(bool)matches.Invoke(window, new object[] { itemPage, item }))
                    throw new InvalidOperationException("Numeric search did not find the new item ID.");
                searchField.SetValue(window, "__no_match__");
                if ((bool)matches.Invoke(window, new object[] { itemPage, item }))
                    throw new InvalidOperationException("Search included an unrelated item.");
                if (!NumericConfigTableWindow.DeleteAssetFile(item, catalog))
                    throw new InvalidOperationException("New item asset could not be deleted.");
                if (AssetDatabase.LoadAssetAtPath<ItemConfig>(path) != null || Contains(catalog, item))
                    throw new InvalidOperationException("Deletion left an asset or catalog reference behind.");
                Debug.Log("Numeric table CRUD regression passed: create, unique IDs, catalog, reference check, search and delete.");
            }
            catch (Exception error)
            {
                Debug.LogError("Numeric table add regression failed: " + error);
            }
            finally
            {
                ItemConfig created = AssetDatabase.LoadAssetAtPath<ItemConfig>(path);
                if (created != null)
                    NumericConfigTableWindow.DeleteAssetFile(created, catalog);
                AssetDatabase.SaveAssets();
                Selection.activeObject = previousSelection;
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        private static bool Contains(GameContentCatalog catalog, ItemConfig item)
        {
            foreach (ItemConfig candidate in catalog.Items)
                if (candidate == item) return true;
            return false;
        }

    }
}
#endif
