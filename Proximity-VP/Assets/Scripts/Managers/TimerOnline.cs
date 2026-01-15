using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TimerOnline : NetworkBehaviour
{
    [SerializeField] private Text timerText;
    [SerializeField] public float startingTime = 60f;
    [SerializeField] private GameObject uiCanvas;

    [Header("Start rules (online)")]
    [SerializeField] private int minPlayers = 2;
    [SerializeField] private int maxPlayers = 4;

    private NetworkVariable<float> remainingTimeNet = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> countingNet = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private NetworkVariable<bool> gameStartedNet = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float remainingTime => remainingTimeNet.Value;
    public bool gameStarted => gameStartedNet.Value;

    public delegate void OnTryStartGame();
    public static OnTryStartGame onTryStartGame;

    public delegate void OnEndGame();
    public static OnEndGame onEndGame;

    private void Start()
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

    private void Update()
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
        if (NetworkManager.Singleton == null) return;

        int connected = NetworkManager.Singleton.ConnectedClientsIds.Count;
        if (connected < minPlayers || connected > maxPlayers) return;

        // No empezar hasta que TODOS los conectados estén verificados
        if (!PlayerIdentityOnline.ServerAllConnectedVerified()) return;

        // Teleport a spawns aleatorio sin repetir
        TeleportAllPlayersRandomNoRepeatServer();

        gameStartedNet.Value = true;
        countingNet.Value = true;

        StartGameClientRpc();
    }

    private void TeleportAllPlayersRandomNoRepeatServer()
    {
        if (SpawnManager.Instance == null)
        {
            Debug.LogWarning("TimerOnline: SpawnManager.Instance null, no se hace TP.");
            return;
        }

        // Asegurar spawnPoints (si no estaban asignados manualmente)
        if (SpawnManager.Instance.spawnPoints == null || SpawnManager.Instance.spawnPoints.Length == 0)
            SpawnManager.Instance.AutoDiscoverSpawnPoints();

        var spawns = SpawnManager.Instance.spawnPoints;
        if (spawns == null || spawns.Length == 0)
        {
            Debug.LogWarning("TimerOnline: no hay spawnPoints, no se hace TP.");
            return;
        }

        // barajar índices
        List<int> idx = new List<int>(spawns.Length);
        for (int i = 0; i < spawns.Length; i++) idx.Add(i);

        for (int i = idx.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (idx[i], idx[j]) = (idx[j], idx[i]);
        }

        int k = 0;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) || client.PlayerObject == null)
                continue;

            Transform sp = spawns[idx[k % idx.Count]];
            k++;

            Vector3 pos = sp.position;
            Quaternion rot = sp.rotation;

            var go = client.PlayerObject.gameObject;
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = pos;
                rb.rotation = rot;
            }
            else
            {
                go.transform.SetPositionAndRotation(pos, rot);
            }

            // owner-side para cubrir owner-authoritative transforms
            var target = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
            };
            TeleportOwnerClientRpc(pos, rot, target);
        }
    }

    [ClientRpc]
    private void TeleportOwnerClientRpc(Vector3 pos, Quaternion rot, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.LocalClient == null) return;
        if (NetworkManager.Singleton.LocalClient.PlayerObject == null) return;

        var go = NetworkManager.Singleton.LocalClient.PlayerObject.gameObject;
        var rb = go.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = pos;
            rb.rotation = rot;
        }
        else
        {
            go.transform.SetPositionAndRotation(pos, rot);
        }
    }

    // Compatibilidad: otros scripts te llaman esto
    public void ForceEndGameServer()
    {
        if (!IsServer) return;
        if (!gameStartedNet.Value) return;

        remainingTimeNet.Value = 0f;
        countingNet.Value = false;
        EndGameClientRpc();
    }

    // Compatibilidad: rematch / volver a lobby
    public void ResetToLobbyServer()
    {
        if (!IsServer) return;

        remainingTimeNet.Value = startingTime;
        countingNet.Value = false;
        gameStartedNet.Value = false;

        ResetToLobbyClientRpc();
    }

    [ClientRpc]
    private void ResetToLobbyClientRpc()
    {
        if (uiCanvas != null)
            uiCanvas.SetActive(false);
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
