using UnityEditor;

[System.Serializable]
public class UpgradeSaveData
{
    public UpgradeSaveModel[] Upgrades; 
    public int AvailablePoints;         
    public int PlayerLevel = 1;         
    public int CurrentXP = 0;           
}

[System.Serializable]
public class UpgradeSaveModel
{
    public string Name;
    public int CurrentLevel;  // Highest unlocked level(e.g., 3 means levels 1, 2, 3 are unlocked)
    public int CurrentPercentage;
}
