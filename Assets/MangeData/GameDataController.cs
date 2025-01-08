using UnityEngine;
using System.IO;
using System.Linq;

public class GameDataController : MonoBehaviour
{
    public static GameDataController Instance { get; private set; }
    
    [SerializeField] private Weapon[] weapons;
    
    private string[] upgradeNames = { "Health", "MagazineSize", "AssultRifleDmg", "ShotgunDmg", "SniperDmg" };
    private const string SaveFilePath = "gameData.json";

    public UpgradeSaveData CurrentData { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    private void OnApplicationQuit()
    {
        SaveData();
    }

    public void SaveData()
    {
        string json = JsonUtility.ToJson(CurrentData);
        string encryptedJson = EncryptionUtility.Encrypt(json);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveFilePath), encryptedJson);
        Debug.Log("Game data saved.");

    }

    public void UpdateUpgradeState(string upgradeName, int currentLevel)
    {
        var upgrade = CurrentData.Upgrades.FirstOrDefault(u => u.Name == upgradeName);
        if (upgrade != null)
        {
            upgrade.CurrentLevel = currentLevel;
            upgrade.CurrentPercentage = CalculatePercentage(currentLevel);

        }
    }
    private int CalculatePercentage(int level)
    {
        return level == 6 ? 100 : level * 15;
    }

    public int GetCurrentPlayerLevel()
    {
        return CurrentData.PlayerLevel;
    }

    public int GetCurrentXP()
    {
        return CurrentData.CurrentXP;
    }

    public int GetXPForNextLevel()
    {
        return Mathf.CeilToInt(100 * Mathf.Pow(1.2f, CurrentData.PlayerLevel - 1));
    }

    public void AddXP(int amount)
    {
        if (CurrentData.PlayerLevel >= 31)
            return;

        CurrentData.CurrentXP += amount;

        while (CurrentData.CurrentXP >= GetXPForNextLevel())
        {
            CurrentData.CurrentXP -= GetXPForNextLevel();
            CurrentData.PlayerLevel++;

            CurrentData.AvailablePoints++;
        }

        SaveData();
    }

    public void SyncWeaponsBonusesFromUpgrades()
    {
        var upgradeMagazinesSize = CurrentData.Upgrades.FirstOrDefault(u => u.Name == "MagazineSize");

        foreach (var weapon in weapons)
        {
            var upgrade = CurrentData.Upgrades.FirstOrDefault(u => u.Name == weapon.GetWeaponUpgradesName());
            weapon.UpdateWeaponStats(upgrade.CurrentPercentage, upgradeMagazinesSize.CurrentPercentage);
        }
    }

    public int GetHealthPlayerBonus() => CurrentData.Upgrades.FirstOrDefault(u => u.Name == "Health").CurrentPercentage;


    private void LoadData()
    {
        string fullPath = Path.Combine(Application.persistentDataPath, SaveFilePath);

        if (!File.Exists(fullPath))
        {
            Debug.Log("Save file not found, initializing with default data.");
            CreateNewData();
            return;
        }

        try
        {
            string encryptedJson = File.ReadAllText(fullPath);
            string json = EncryptionUtility.Decrypt(encryptedJson);
            CurrentData = JsonUtility.FromJson<UpgradeSaveData>(json);
            SyncWeaponsBonusesFromUpgrades();
            Debug.Log("Game data loaded successfully.");
        }
        catch
        {
            CreateNewData();
        }
    }

    private void CreateNewData()
    {

        foreach (var weapon in weapons)
        {
            weapon.BackToInitial();
        }

        CurrentData = new UpgradeSaveData
        {
            AvailablePoints = 0,
            PlayerLevel = 1,
            CurrentXP = 0,
            Upgrades = new UpgradeSaveModel[upgradeNames.Length]
        };

        for (int i = 0; i < CurrentData.Upgrades.Length; i++)
        {
            CurrentData.Upgrades[i] = new UpgradeSaveModel
            {
                Name = upgradeNames[i],
                CurrentLevel = 0,
                CurrentPercentage = 0
            };
        }

        SaveData();
    }


    public int GetAvailablePoints()
    {
        return CurrentData.AvailablePoints;
    }

    public void DeductUpgradePoint()
    {
        if (CurrentData.AvailablePoints > 0)
        {
            CurrentData.AvailablePoints--;
            SaveData();
            SyncWeaponsBonusesFromUpgrades();
        }
    }
}
