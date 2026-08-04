using System.IO;
using UnityEngine;

namespace Manatite.Core.SaveSystem
{
    /// <summary>Single-slot JSON save file at Application.persistentDataPath/save.json.</summary>
    public static class SaveFile
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");

        public static void Save(SaveData data)
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }

        public static bool TryLoad(out SaveData data)
        {
            if (!File.Exists(SavePath))
            {
                data = null;
                return false;
            }

            string json = File.ReadAllText(SavePath);
            data = JsonUtility.FromJson<SaveData>(json);
            return data != null;
        }

        public static bool HasSave() => File.Exists(SavePath);

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
    }
}
