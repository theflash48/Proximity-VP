using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public GameObject gridMainMenu;
    public GameObject gridGameModeSelect;
    public SceneController sceneController;
    public GameObject gameModeManager;
    public GameModeManager gmManager;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        sceneController = GetComponent<SceneController>();
        gameModeManager = GameObject.Find("GameModeManager");
        gmManager = gameModeManager.GetComponent<GameModeManager>();
        GoToMainMenu();
    }

    public void GoToMainMenu()
    {
        gridMainMenu.SetActive(true);
        gridGameModeSelect.SetActive(false);
        gmManager.conection = GameModeManager.conectionType.none;
    }

    public void GoToGameModeSelect()
    {
        gridMainMenu.SetActive(false);
        gridGameModeSelect.SetActive(true);
        gmManager.conection = GameModeManager.conectionType.none;
    }

    public void GoToLocal()
    {
        gmManager.conection = GameModeManager.conectionType.local;
        sceneController.ChangeScene("LocalGame");
    }

    public void GoToOnline()
    {
        gmManager.conection = GameModeManager.conectionType.online;
        sceneController.ChangeScene("OnlineGame");
    }

    public void GoToExit()
    {
        Application.Quit();
    }
}
