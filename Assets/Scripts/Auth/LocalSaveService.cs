using System;
using System.IO;
using UnityEngine;

namespace PGE.Auth
{
    public static class LocalSaveService
    {
        private const string FileName = "pge-save-v2.json";
        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);
        public static string BackupPath => SavePath + ".bak";

        public static bool HasSave => File.Exists(SavePath) || File.Exists(BackupPath);

        public static GameSaveData Load(out string error)
        {
            GameSaveData data = TryRead(SavePath, out error);
            if (data != null) return data;
            string primaryError = error;
            data = TryRead(BackupPath, out error);
            if (data != null) return data;
            error = string.IsNullOrEmpty(primaryError) ? error : primaryError;
            return null;
        }

        public static bool Save(GameSaveData data, out string error)
        {
            error = string.Empty;
            if (data == null || !data.Validate(out error)) return false;
            string tempPath = SavePath + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
                File.WriteAllText(tempPath, JsonUtility.ToJson(data));
                GameSaveData verification = TryRead(tempPath, out error);
                if (verification == null) return false;

                if (File.Exists(SavePath))
                {
                    try { File.Replace(tempPath, SavePath, BackupPath); }
                    catch (PlatformNotSupportedException)
                    {
                        File.Copy(SavePath, BackupPath, true);
                        File.Delete(SavePath);
                        File.Move(tempPath, SavePath);
                    }
                }
                else File.Move(tempPath, SavePath);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                Debug.LogError("[LocalSave] Could not commit local save: " + ex.Message);
                return false;
            }
        }

        private static GameSaveData TryRead(string path, out string error)
        {
            error = string.Empty;
            if (!File.Exists(path)) return null;
            try
            {
                string json = File.ReadAllText(path);
                GameSaveData data = GameSaveData.Migrate(JsonUtility.FromJson<GameSaveData>(json));
                if (data == null || !data.Validate(out error)) return null;
                return data;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }
    }
}
