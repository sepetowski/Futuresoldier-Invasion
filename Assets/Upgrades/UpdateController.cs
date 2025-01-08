using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class UpdateController : MonoBehaviour
{
    [SerializeField] private UpgradeModel[] upgrades;
    [SerializeField] private TMP_Text pointsText;
    [SerializeField] private TMP_Text levelText; 

    [SerializeField] private Color lockedColor = new Color(0.6f, 0.35f, 0.2f, 0f);
    [SerializeField] private Color unlockedColor = new Color(0.6f, 0.35f, 0.2f, 1f);

    [SerializeField] private SelectWeaponController selectWeaponController;
    void Start()
    {
        Init();
        UpdatePointsUI();
        UpdatePlayerLevelAndXP(); 
    }

    void OnApplicationQuit()
    {
        GameDataController.Instance.SaveData(); 
    }


    void Init()
    {
        var savedData = GameDataController.Instance.CurrentData;

        for (int i = 0; i < upgrades.Length; i++)
        {
            var upgrade = upgrades[i];
            var savedUpgrade = savedData.Upgrades[i];
            int currentLevel = savedUpgrade.CurrentLevel;

  
            upgrade.name = savedUpgrade.Name;


            var panel = upgrade.parentObject.transform.Find("Panel");
            var label = upgrade.parentObject.transform.Find("Label");

            upgrade.labelText = label.GetComponent<TMP_Text>();

            var levelImages = panel.GetComponentsInChildren<Image>()
                .Where(img => img.name.StartsWith("Level"))
                .OrderBy(img => img.name)
                .ToArray();

            upgrade.levels = levelImages.Select(img => new LevelModel { levelImage = img, bought = false }).ToArray();

            for (int j = 0; j < upgrade.levels.Length; j++)
            {
                var level = upgrade.levels[j];
                level.bought = j < currentLevel;
                level.levelImage.color = level.bought ? unlockedColor : lockedColor;

                TMP_Text text = level.levelImage.GetComponentInChildren<TMP_Text>();

                Color vertexColor = text.color;
                vertexColor.a = level.bought ? 1f : 0.5f;
                text.color = vertexColor;
            }

            UpdateLabel(upgrade);
            upgrade.increaseButton.onClick.RemoveAllListeners();
            upgrade.increaseButton.onClick.AddListener(() => UnlockNextLevel(upgrade));
        }
    }

    void UnlockNextLevel(UpgradeModel upgrade)
    {
        int availablePoints = GameDataController.Instance.GetAvailablePoints();

        if (availablePoints <= 0) return;

        for (int i = 0; i < upgrade.levels.Length; i++)
        {
            var level = upgrade.levels[i];

            if (!level.bought)
            {
                level.bought = true;
                level.levelImage.color = unlockedColor;

                TMP_Text text = level.levelImage.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    Color vertexColor = text.color;
                    vertexColor.a = 1f;
                    text.color = vertexColor;
                }

                int newLevel = i + 1;
                GameDataController.Instance.UpdateUpgradeState(upgrade.name, newLevel);

                GameDataController.Instance.DeductUpgradePoint();
                UpdatePointsUI();

                int currentPercentage = GameDataController.Instance.CurrentData.Upgrades
                    .First(u => u.Name == upgrade.name).CurrentPercentage;

                upgrade.labelText.text = $"{upgrade.labelBaseText} +{currentPercentage}%";
                break;
            }
        }

        UpdateLabel(upgrade);
        selectWeaponController.UpdateUI();

        if (AreAllLevelsBought(upgrade))
            upgrade.increaseButton.interactable = false;
        

        UpdateButtonsInteractivity();
    }


    void UpdatePlayerLevelAndXP()
    {
        int currentLevel = GameDataController.Instance.GetCurrentPlayerLevel();
        int currentXP = GameDataController.Instance.GetCurrentXP();
        int xpForNextLevel = GameDataController.Instance.GetXPForNextLevel();

        if(currentLevel>=31)
            levelText.text = $"You have reach the maximum level!";
        else
            levelText.text = $"Your level: {currentLevel}. XP: {currentXP}/{xpForNextLevel}";
    }

    void UpdatePointsUI()
    {
        int availablePoints = GameDataController.Instance.GetAvailablePoints();
        pointsText.text = $"Available Points: {availablePoints}";
        UpdateButtonsInteractivity();
    }


    void UpdateButtonsInteractivity()
    {
        int availablePoints = GameDataController.Instance.GetAvailablePoints();

        foreach (var upgrade in upgrades)
        {
            bool allLevelsBought = AreAllLevelsBought(upgrade);
            upgrade.increaseButton.interactable = availablePoints > 0 && !allLevelsBought;
        }
    }


    bool AreAllLevelsBought(UpgradeModel upgrade)
    {
        return upgrade.levels.All(level => level.bought);
    }

    void UpdateLabel(UpgradeModel upgrade)
    {
        int unlockedLevels = upgrade.levels.Count(level => level.bought);
        int currentPercentage = unlockedLevels * 15;

        if (unlockedLevels == upgrade.levels.Length)
        {
            upgrade.labelText.text = $"{upgrade.labelBaseText} +{currentPercentage + 10}%";
            return;
        }

        int nextPercentage = (unlockedLevels == upgrade.levels.Length - 1) ? 100 : currentPercentage + 15;
        upgrade.labelText.text = $"{upgrade.labelBaseText} +{currentPercentage}% Next: +{nextPercentage}%";
    }
}
