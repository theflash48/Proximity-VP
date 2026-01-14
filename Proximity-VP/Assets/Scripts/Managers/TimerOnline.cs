using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TimerOnline : NetworkBehaviour
{
    [SerializeField] Text timerText;
    [SerializeField] public float startingTime = 60f;
    [SerializeField] GameObject uiCanvas;

    private NetworkVariable<float> remainingTimeNet = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> countingNet = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> gameStartedNet = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ✅ Expuestos para el resto del juego
    public float remainingTime => remainingTimeNet.Value;
    public bool gameStarted => gameStartedNet.Value;

    public delegate void OnTryStartGame();
    public static OnTryStartGame onTryStartGame;

    public delegate void OnEndGame();
    public static OnEndGame onEndGame;

    void Start()
    {
        if (uiCanvas != null)
            uiCanvas.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            remainingTimeNet.Value = startingTime;
            countingNet.Value = false;
            gameStartedNet.Value = false;
        }
    }

    void Update()
    {
        UpdateTimerUI(remainingTimeNet.Value);

        if (!IsServer) return;

        if (Input.GetKeyDown(KeyCode.Space) && !gameStartedNet.Value)
            TryStartGameServer();

        if (countingNet.Value)
        {
            remainingTimeNet.Value -= Time.deltaTime;
            if (remainingTimeNet.Value <= 0f)
            {
                remainingTimeNet.Value = 0f;
                EndGameServer();
            }
        }
    }

    private void TryStartGameServer()
    {
        if (SpawnManager.Instance == null) return;
        if (SpawnManager.Instance.ActivePlayersCount < 2) return;

        gameStartedNet.Value = true;
        countingNet.Value = true;

        StartGameClientRpc();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        if (uiCanvas != null)
            uiCanvas.SetActive(false);

        onTryStartGame?.Invoke();
    }

    private void EndGameServer()
    {
        countingNet.Value = false;
        EndGameClientRpc();
    }

    [ClientRpc]
    private void EndGameClientRpc()
    {
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
