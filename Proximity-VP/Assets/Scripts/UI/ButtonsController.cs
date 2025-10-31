using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonsController : MonoBehaviour
{
    //public string currentScenename = SceneManager.GetActiveScene().name;

    public void GoToMainMenu(string currentScenename)
    {
        Time.timeScale = 1;
        currentScenename = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScenename);
    }

    public void Restart ()
    {
        Time.timeScale = 1;
        // Reinicia la escena
        string currentScenename = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentScenename);
    }
}
