using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using UnityEngine.SceneManagement;

public class LoadingController : MonoBehaviour
{
    public GameObject loadingScreen;
    public TextMeshProUGUI loadingText; 

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    IEnumerator LoadSceneAsync(string sceneName)
    {
        if (loadingScreen.activeSelf)
        {
            yield break;
        }

        loadingScreen.SetActive(true);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float displayedProgress = 0f;

        while (!operation.isDone)
        {
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f) * 100;

            while (displayedProgress < targetProgress)
            {
                displayedProgress += 1; 
                if (displayedProgress > targetProgress)
                {
                    displayedProgress = targetProgress;
                }

                if (loadingText != null)
                {
                    loadingText.text = $"Loading... {displayedProgress:F0}%"; 
                }

                yield return new WaitForSeconds(0.02f); 
            }

            if (operation.progress >= 0.9f)
            {
                loadingText.text = "Loading... 100%";
                yield return new WaitForSeconds(0.1f);

                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
