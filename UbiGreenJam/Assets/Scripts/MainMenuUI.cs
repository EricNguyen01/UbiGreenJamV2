using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public string gameplaySceneName = "SampleScene"; // put your gameplay scene name here

    public void StartGame()
    {
        SceneManager.LoadScene(gameplaySceneName);
    }
}