using UnityEngine;
using TMPro;

public class MissionInfo : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI missionText;

    public void SetKillEnemiesMission(int totalEnemies)
    {
        missionText.text = $"Kill {totalEnemies} Aliens! 0/{totalEnemies}";
        ShowMissionInfo();
    }

    public void UpdateEnemiesKilled(int killedEnemies, int totalEnemies)
    {
        missionText.text = $"Kill {totalEnemies} Aliens! {killedEnemies}/{totalEnemies}";
    }

    public void SetSurviveMission(float totalTime)
    {
        missionText.text = $"Survive! {FormatTime(totalTime)}";
        ShowMissionInfo();
    }

    public void UpdateSurvivalTime(float timeRemaining)
    {
        missionText.text = $"Survive! {FormatTime(timeRemaining)}";
    }

    public void SetFindItemMission(string itemName)
    {
        missionText.text = $"Find the {itemName}!";
        ShowMissionInfo();
    }

    public void ShowMissionInfo()
    {
        gameObject.SetActive(true);
    }

    public void HideMissionInfo()
    {
        gameObject.SetActive(false);
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return $"{minutes:D2}:{seconds:D2}";
    }
}
