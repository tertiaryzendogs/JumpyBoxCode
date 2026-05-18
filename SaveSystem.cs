using UnityEngine;
using System.IO;
public class SaveSystem
{
    private static SaveData _saveData = new SaveData();

    [System.Serializable]
    public struct SaveData
    {
        public PlayerSaveData playerData;
    }

    public static string SaveFileName()
    {
        string fileName = Application.persistentDataPath + "/save" + ".save";
        return fileName;
    }

    public static void Save()
    {
        HandleSaveData();

        File.WriteAllText(SaveFileName(), JsonUtility.ToJson(_saveData, true));
    }

    private static void HandleSaveData()
    {
        MainMenuController.Instance.Save(ref _saveData.playerData);
    }

    public static void Load()
    {
        if (!File.Exists(SaveFileName()))
        {
            Debug.Log("No save file found");
            return;
        }
        string saveContent = File.ReadAllText(SaveFileName());
        _saveData = JsonUtility.FromJson<SaveData>(saveContent);
        HandleLoadData();
    }

    private static void HandleLoadData()
    {
        MainMenuController.Instance.Load(_saveData.playerData);
    }

    // helper that builds a new PlayerSaveData instance by copying any fields
    // that are present in the old data and leaving the rest at their default
    // values.  This allows us to "migrate" an outdated save file instead of
    // just discarding it.
    private static PlayerSaveData MigratePlayerData(PlayerSaveData old)
    {
        PlayerSaveData result = new PlayerSaveData();
        result.totalStarsEarned = old.totalStarsEarned;

        // always create arrays with the current expected length
        result.starsEarnedPerLevel = new int[31];
        if (old.starsEarnedPerLevel != null)
        {
            int len = Mathf.Min(old.starsEarnedPerLevel.Length, result.starsEarnedPerLevel.Length);
            for (int i = 0; i < len; i++)
                result.starsEarnedPerLevel[i] = old.starsEarnedPerLevel[i];
        }

        result.levelsUnlocked = new bool[30];
        if (old.levelsUnlocked != null)
        {
            int len = Mathf.Min(old.levelsUnlocked.Length, result.levelsUnlocked.Length);
            for (int i = 0; i < len; i++)
                result.levelsUnlocked[i] = old.levelsUnlocked[i];
        }

        return result;
    }

    // Checks an existing save file, migrating any usable data into the current
    // format.  Returns true if the file was already valid (no rewrite needed),
    // false if we had to repair or if validation failed.
    private static bool ValidateAndMigrateExistingSave()
    {
        try
        {
            string content = File.ReadAllText(SaveFileName());
            SaveData loaded = JsonUtility.FromJson<SaveData>(content);

            bool needsRewrite = false;
            PlayerSaveData old = loaded.playerData;

            if (old.starsEarnedPerLevel == null || old.starsEarnedPerLevel.Length < 31)
                needsRewrite = true;
            if (old.levelsUnlocked == null || old.levelsUnlocked.Length < 30)
                needsRewrite = true;

            if (needsRewrite)
            {
                _saveData.playerData = MigratePlayerData(old);
                File.WriteAllText(SaveFileName(), JsonUtility.ToJson(_saveData, true));
                Debug.Log("Migrated save file to new layout");
                return false;
            }

            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("Failed to validate/migrate save file: " + ex.Message);
            return false;
        }
    }

    public static void MakeSaveIfDoesntExist()
    {
        if (File.Exists(SaveFileName()))
        {
            // try to migrate/validate the existing file instead of nuking it
            if (!ValidateAndMigrateExistingSave())
            {
                Debug.Log("Recreating save file from scratch");
                File.Delete(SaveFileName());
                Save();
            }
            return;
        }
        Save();
    }

    // Deletes the current save file (if present) and creates a fresh one.
    // Call this from UI when the player requests a full reset.
    public static void ResetSave()
    {
        try
        {
            if (File.Exists(SaveFileName()))
                File.Delete(SaveFileName());

            // clear any in-memory save state then write a new default save
            _saveData = new SaveData();
            Save();
            Debug.Log("SaveSystem: Reset save file to defaults.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("SaveSystem: Failed to reset save: " + ex.Message);
        }
    }
}
