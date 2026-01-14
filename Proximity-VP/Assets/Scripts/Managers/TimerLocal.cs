using UnityEngine;
using UnityEngine.UI;

public class TimerLocal : MonoBehaviour
{
    [SerializeField] Text timerText;
    [SerializeField] public float remainingTime = 60f;
    [SerializeField] GameObject uiCanvas;
    ButtonsController btnControllers;

    bool counting = false;
    public bool gameStarted = false;

    public delegate void OnTryStartGame();
    public static OnTryStartGame onTryStartGame;

    public delegate void OnEndGame();
    public static OnEndGame onEndGame;

    void Start()
    {
        btnControllers = FindFirstObjectByType<ButtonsController>();

        if (uiCanvas != null)
            uiCanvas.SetActive(false);

        UpdateTimerUI(remainingTime);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && !gameStarted)
            TryStartGame();

        if (counting)
        {
            remainingTime -= Time.deltaTime;
            if (remainingTime <= 0f)
            {
                remainingTime = 0f;
                EndGame();
            }

            UpdateTimerUI(remainingTime);
        }

        if (Input.GetKeyDown(KeyCode.V) && btnControllers != null)
            btnControllers.Restart();
    }

    private void TryStartGame()
    {
        if (SpawnManager.Instance != null && SpawnManager.Instance.ActivePlayersCount >= 2 && !gameStarted)
        {
            gameStarted = true;
            counting = true;

            if (uiCanvas != null)
                uiCanvas.SetActive(false);

            onTryStartGame?.Invoke();
        }
    }

    private void EndGame()
    {
        counting = false;

        if (uiCanvas != null)
            uiCanvas.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        onEndGame?.Invoke();
    }

    private void UpdateTimerUI(float t)
    {
        if (timerText == null) return;

        if (t < 0f) t = 0f;
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
