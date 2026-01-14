using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerHealthOnline : NetworkBehaviour
{
    [Header("Health Settings")]
    public int maxLives = 2;

    public NetworkVariable<int> currentLives = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Respawn Settings")]
    public float respawnDelay = 3f;

    public TimerLocal timer;

    private NetworkVariable<bool> isRespawning = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsRespawning => isRespawning.Value;

    private Collider col;
    private Rigidbody rb;
    private PlayerHUD hud;
    private PlayerControllerOnline controller;

    void Awake()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        hud = GetComponent<PlayerHUD>();
        controller = GetComponent<PlayerControllerOnline>();
    }

    public override void OnNetworkSpawn()
    {
        if (timer == null)
            timer = Object.FindAnyObjectByType<TimerLocal>();

        if (IsServer)
        {
            currentLives.Value = maxLives;
            isRespawning.Value = false;
        }

        currentLives.OnValueChanged += OnLivesChanged;
        isRespawning.OnValueChanged += OnRespawnChanged;

        OnLivesChanged(0, currentLives.Value);
        OnRespawnChanged(false, isRespawning.Value);
    }

    public override void OnNetworkDespawn()
    {
        currentLives.OnValueChanged -= OnLivesChanged;
        isRespawning.OnValueChanged -= OnRespawnChanged;
    }

    private void OnLivesChanged(int previous, int current)
    {
        if (hud != null)
            hud._fHealthUI(current, maxLives);
    }

    private void OnRespawnChanged(bool previous, bool current)
    {
        if (col != null)
            col.enabled = !current;

        if (current && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // Compatibilidad: si algún sitio aún llama a TakeDamageServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(ulong shooterClientId)
    {
        ApplyDamageFromServer(shooterClientId);
    }

    // Llamado desde el server (por el raycast validado del shooter)
    public void ApplyDamageFromServer(ulong shooterClientId)
    {
        if (!IsServer) return;
        if (isRespawning.Value) return;

        if (timer != null)
        {
            if (!timer.gameStarted || timer.remainingTime <= 0)
                return;
        }

        currentLives.Value--;

        DamageBlinkClientRpc();

        if (currentLives.Value <= 0)
        {
            AwardKillServer(shooterClientId);
            StartCoroutine(RespawnCoroutineServer());
        }
    }

    private void AwardKillServer(ulong shooterClientId)
    {
        if (!IsServer) return;
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId, out var shooterClient) &&
            shooterClient.PlayerObject != null)
        {
            var shooterController = shooterClient.PlayerObject.GetComponent<PlayerControllerOnline>();
            if (shooterController != null)
                shooterController.AddScoreServer(1);
        }
    }

    private IEnumerator RespawnCoroutineServer()
    {
        isRespawning.Value = true;

        yield return new WaitForSeconds(respawnDelay);

        Transform spawn = SpawnManager.Instance != null
            ? SpawnManager.Instance.GetFarthestSpawnPoint(gameObject)
            : null;

        Vector3 pos = spawn != null ? spawn.position : transform.position;
        Quaternion rot = spawn != null ? spawn.rotation : transform.rotation;

        var targetOwnerOnly = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        };

        TeleportOwnerClientRpc(pos, rot, targetOwnerOnly);

        currentLives.Value = maxLives;
        isRespawning.Value = false;

        DamageBlinkClientRpc();
    }

    [ClientRpc]
    private void TeleportOwnerClientRpc(Vector3 pos, Quaternion rot, ClientRpcParams clientRpcParams = default)
    {
        if (!IsOwner) return;

        transform.SetPositionAndRotation(pos, rot);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    [ClientRpc]
    private void DamageBlinkClientRpc()
    {
        if (controller != null)
            controller.StartDamageBlink();
    }
}
