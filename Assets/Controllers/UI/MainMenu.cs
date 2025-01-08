using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    private void Start()
    {
        GetDifficulty();
    }
    public void PlayGame()
    {
        SetDifficulty();
        FindObjectOfType<LoadingController>().LoadScene("Game");
    }
    public void GoToUpgrades()
    {
        SetDifficulty();
        SceneManager.LoadScene("Upgrades");
    }

    private void SetDifficulty()
    {
        string difficulty = dropdown.options[dropdown.value].text;
        string formattedDifficulty = difficulty.Replace(" ", "");
        PlayerPrefs.SetString("SelectedDifficulty", formattedDifficulty);
    }
    private void GetDifficulty()
    {
        string savedDifficulty = PlayerPrefs.GetString("SelectedDifficulty", "Normal");
        int dropdownIndex = dropdown.options.FindIndex(option =>
        option.text.Replace(" ", "") == savedDifficulty);

        dropdown.value = dropdownIndex;
    }
    public void ExitGame()
    {
        SetDifficulty();
        Application.Quit();
    }
}
