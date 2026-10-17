using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Astraparasitus
{
    public static class SAVE
    {
        public static string GetPath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }
        public static void JsonSave(string fileName, object data)
        {
            if (string.IsNullOrWhiteSpace(fileName) || data == null)
            {
                Debug.LogWarning("Save was skipped because file name or data is invalid.");
                return;
            }

            string path = GetPath(fileName);
            string tempPath = path + ".tmp";

            try
            {
                string json = JsonUtility.ToJson(data);
                File.WriteAllText(tempPath, json);

                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, null);
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Save failed: {path}\n{exception.Message}");
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
                return;
            }

            #if UNITY_EDITOR
            #endif
        }
        public static T JsonLoad<T>(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return default;
            }

            string path = GetPath(fileName);
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    var data = JsonUtility.FromJson<T>(json);
                    return data;
                }
                catch (System.Exception exception)
                {
                    Debug.LogError($"Load failed: {path}\n{exception.Message}");
                    return default;
                }
            }
            else
            {
                return default;
            }
        }
        public static void JsonDelete(string fileName)
        {
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                File.Delete(GetPath(fileName));
            }
        }
    }
}
