using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMenu : MonoBehaviour
{
    public void SaveAndReturnToMenu()
    {

        GameDataController.Instance.SaveData();
        SceneManager.LoadScene("MainMenu");
    }
}
