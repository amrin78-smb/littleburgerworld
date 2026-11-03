using System;
using System.IO;
using UnityEngine;

namespace LittleBurgerWorld.Save
{
    /// <summary>Result of loading the save file, useful for diagnostics and tests.</summary>
    public enum SaveLoadResult
    {
        Loaded,
        CreatedNew,
        RecoveredFromBackup,
        ResetAfterCorruption,
        Migrated
    }

    /// <summary>
    /// Local, offline JSON save. Writes atomically (temp file + replace) and keeps one backup.
    /// Corrupted files are preserved as *.corrupt for inspection and never crash the game.
    /// </summary>
    public sealed class SaveManager
    {
        public const string DefaultFileName = "littleburgerworld_save.json";

        private readonly string m_Path;
        private bool m_Dirty;

        public SaveData Data { get; private set; }
        public SaveLoadResult LastLoadResult { get; private set; }
        public string FilePath => m_Path;
        public string BackupPath => m_Path + ".bak";

        public SaveManager(string path = null)
        {
            m_Path = string.IsNullOrEmpty(path)
                ? Path.Combine(Application.persistentDataPath, DefaultFileName)
                : path;
        }

        public SaveData Load()
        {
            if (!File.Exists(m_Path))
            {
                if (TryRead(BackupPath, out SaveData fromBackup))
                {
                    Data = SaveMigration.Migrate(fromBackup);
                    Data.Sanitize();
                    LastLoadResult = SaveLoadResult.RecoveredFromBackup;
                    Save();
                    return Data;
                }
                Data = new SaveData();
                LastLoadResult = SaveLoadResult.CreatedNew;
                return Data;
            }

            if (TryRead(m_Path, out SaveData data))
            {
                int oldVersion = data.version;
                Data = SaveMigration.Migrate(data);
                Data.Sanitize();
                LastLoadResult = oldVersion < SaveData.CurrentVersion ? SaveLoadResult.Migrated : SaveLoadResult.Loaded;
                if (LastLoadResult == SaveLoadResult.Migrated) Save();
                return Data;
            }

            // Main file unreadable: keep a copy and fall back to the backup or fresh data.
            try { File.Copy(m_Path, m_Path + ".corrupt", true); } catch (Exception) { /* best effort */ }

            if (TryRead(BackupPath, out SaveData backup))
            {
                Data = SaveMigration.Migrate(backup);
                Data.Sanitize();
                LastLoadResult = SaveLoadResult.RecoveredFromBackup;
            }
            else
            {
                Data = new SaveData();
                LastLoadResult = SaveLoadResult.ResetAfterCorruption;
            }
            Save();
            return Data;
        }

        private static bool TryRead(string path, out SaveData data)
        {
            data = null;
            try
            {
                if (!File.Exists(path)) return false;
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return false;
                data = JsonUtility.FromJson<SaveData>(json);
                if (data == null || data.version <= 0 || data.version > SaveData.CurrentVersion + 100) return false;
                return true; // migration happens in Load() so it can be reported and persisted
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Could not read {path}: {e.Message}");
                data = null;
                return false;
            }
        }

        /// <summary>Marks data as changed; it is written on the next <see cref="Flush"/>.</summary>
        public void MarkDirty() => m_Dirty = true;

        public void Flush()
        {
            if (m_Dirty) Save();
        }

        public bool Save()
        {
            if (Data == null) Data = new SaveData();
            try
            {
                Data.version = SaveData.CurrentVersion;
                string json = JsonUtility.ToJson(Data, true);
                string dir = Path.GetDirectoryName(m_Path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                string tmp = m_Path + ".tmp";
                File.WriteAllText(tmp, json);
                if (File.Exists(m_Path)) File.Copy(m_Path, BackupPath, true);
                if (File.Exists(m_Path)) File.Delete(m_Path);
                File.Move(tmp, m_Path);
                m_Dirty = false;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Save failed: {e.Message}");
                return false;
            }
        }

        /// <summary>Clears progress but keeps the parent's audio/language settings.</summary>
        public void ResetProgress()
        {
            SettingsData keep = Data?.settings ?? new SettingsData();
            Data = new SaveData { settings = keep };
            Save();
        }
    }
}
