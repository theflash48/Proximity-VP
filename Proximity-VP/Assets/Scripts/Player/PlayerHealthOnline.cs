using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerHealthOnline : NetworkBehaviour
{
    [Header("Health Settings")]
    public int maxLives = 2;

    public NetworkVariable<int> currentLives = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Muertes en esta partida (cada vez que llegas a 0)
    private NetworkVariable<int> deathsNet = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int DeathsInMatch => deathsNet.Value;

    [Header("Respawn Settings")]
    public float respawnDelay = 3f;

    public TimerOnline timer;

    private NetworkVariable<bool> isRespawning = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsRespawning => isRespawning.Value;

    private Collider col;
    private Rigidbody rb;
    private PlayerHUD hud;
    private PlayerControllerOnline controller;
    private PlayerIdentityOnline identity;

    void Awake()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        hud = GetComponent<PlayerHUD>();
        controller = GetComponent<PlayerControllerOnline>();
        identity = GetComponent<PlayerIdentityOnline>();
    }

    public override void OnNetworkSpawn()
    {
        if (timer == null)
            timer = Object.FindAnyObjectByType<TimerOnline>();

        if (IsServer)
        {
            currentLives.Value = maxLives;
            deathsNet.Value = 0;
            isRespawning.Value = false;
        }

        currentLives.OnValueChanged += OnLivesChanged;
        isRespawning.OnValueChanged += OnRespawnChanged;

        // Estado inicial UI
        OnLivesChanged(0, currentLives.Value);
        OnRespawnChanged(false, isRespawning.Value);
    }

    public override void OnNetworkDespawn()
    {
        currentLives.OnValueChanged -= OnLivesChanged;
        isRespawning.OnValueChanged -= OnRespawnChanged;
    }

    private bool IsMatchRunning()
    {
        if (timer == null) return false;
        return timer.gameStarted && timer.remainingTime > 0f;
    }

    private void OnLivesChanged(int previous, int current)
    {
        if (hud != null)
        {
            hud._fHealthUI(current, maxLives);

            // HUD blanco al spawn/respawn; rojo desde el primer hit mientras la partida esté en marcha
            if (IsMatchRunning())
                hud._fSetHudDamageState(current < maxLives);
            else
                hud._fSetHudDamageState(false);
        }
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

        // DeathScreen ON durante respawn
        if (hud != null)
            hud._fToggleDeathScreen(current);
    }

    public void ApplyDamageFromServer(ulong shooterClientId)
    {
        if (!IsServer) return;
        if (isRespawning.Value) return;

        // Solo daño con partida en marcha
        if (timer != null)
        {
            if (!timer.gameStarted || timer.remainingTime <= 0f)
                return;
        }

        currentLives.Value--;

        DamageBlinkClientRpc();

        if (currentLives.Value <= 0)
        {
            // Cuenta muerte de partida
            deathsNet.Value++;

            // ✅ registrar death en OnlineMatchManager (victima + posición)
            if (OnlineMatchManager.Instance != null && OnlineMatchManager.Instance.IsServer)
            {
                int victimAcc = (identity != null) ? identity.AccId.Value : 0;
                if (victimAcc > 0)
                    OnlineMatchManager.Instance.ServerRecordDeath(victimAcc, transform.position);
            }

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

    // Para rejoin/rematch (server)
    public void SetDeathsServer(int newDeaths)
    {
        if (!IsServer) return;
        deathsNet.Value = Mathf.Max(0, newDeaths);
    }

    public void ResetForRematchServer()
    {
        if (!IsServer) return;
        currentLives.Value = maxLives;
        deathsNet.Value = 0;
        isRespawning.Value = false;
    }
}
