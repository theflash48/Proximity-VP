using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerIdentityOnline : NetworkBehaviour
{
    public NetworkVariable<int> AccId = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> Username = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> SlotIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private static bool _serverHooks;
    private static readonly Dictionary<int, ulong> _accToClient = new Dictionary<int, ulong>();
    private static readonly Dictionary<int, int> _accToSlot = new Dictionary<int, int>();
    private static readonly HashSet<int> _usedSlots = new HashSet<int>();

    public override void OnNetworkSpawn()
    {
        Username.OnValueChanged += OnUsernameChanged;
        SlotIndex.OnValueChanged += (_, __) => ForceCameraReassign();

        if (IsServer)
            EnsureServerHooks();

        if (IsOwner)
        {
            if (AccountSession.Instance == null || !AccountSession.Instance.IsLoggedIn)
            {
                SubmitIdentityServerRpc(0, "");
                return;
            }

            int accId = AccountSession.Instance.AccId;
            string username = AccountSession.Instance.Username;

            SubmitIdentityServerRpc(accId, username);
        }

        ApplyNameToHud();
    }

    public override void OnNetworkDespawn()
    {
        Username.OnValueChanged -= OnUsernameChanged;

        if (IsServer)
        {
            int acc = AccId.Value;
            if (acc > 0)
            {
                if (_accToClient.TryGetValue(acc, out ulong cid) && cid == OwnerClientId)
                    _accToClient.Remove(acc);

                int slot = SlotIndex.Value;
                if (slot >= 0)
                    _usedSlots.Remove(slot);
            }
        }
    }

    private void OnUsernameChanged(FixedString64Bytes prev, FixedString64Bytes curr)
    {
        ApplyNameToHud();
        ForceCameraReassign();
    }

    private void ApplyNameToHud()
    {
        var hud = GetComponent<PlayerHUD>();
        if (hud == null) return;

        string name = Username.Value.Length > 0 ? Username.Value.ToString() : "";
        hud._fUpdateName(name);
    }

    private static void EnsureServerHooks()
    {
        if (_serverHooks) return;
        _serverHooks = true;

        if (NetworkManager.Singleton == null) return;

        NetworkManager.Singleton.OnClientDisconnectCallback += (clientId) =>
        {
            List<int> toRemove = null;

            foreach (var kv in _accToClient)
            {
                if (kv.Value == clientId)
                {
                    toRemove ??= new List<int>();
                    toRemove.Add(kv.Key);
                }
            }

            if (toRemove != null)
            {
                foreach (int acc in toRemove)
                    _accToClient.Remove(acc);
            }
        };
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitIdentityServerRpc(int accId, string username, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (accId <= 0 || string.IsNullOrWhiteSpace(username))
        {
            StartCoroutine(KickWithPopup(senderClientId, "Debes iniciar sesión para jugar online."));
            return;
        }

        username = username.Trim();
        if (username.Length > 20)
            username = username.Substring(0, 20);

        if (_accToClient.TryGetValue(accId, out ulong existingClient) && existingClient != senderClientId)
        {
            StartCoroutine(KickWithPopup(senderClientId, "Esta cuenta ya está conectada a esta sala."));
            return;
        }

        _accToClient[accId] = senderClientId;

        int slot;
        if (_accToSlot.TryGetValue(accId, out int preferred) && !_usedSlots.Contains(preferred))
        {
            slot = preferred;
        }
        else
        {
            slot = FindFirstFreeSlot();
            _accToSlot[accId] = slot;
        }

        _usedSlots.Add(slot);

        AccId.Value = accId;
        Username.Value = new FixedString64Bytes(username);
        SlotIndex.Value = slot;
    }

    private int FindFirstFreeSlot()
    {
        for (int i = 0; i < 4; i++)
            if (!_usedSlots.Contains(i))
                return i;

        return 0;
    }

    private IEnumerator KickWithPopup(ulong clientId, string message)
    {
        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };

        KickPopupClientRpc(message, target);
        yield return new WaitForSeconds(0.2f);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            NetworkManager.Singleton.DisconnectClient(clientId);
    }

    [ClientRpc]
    private void KickPopupClientRpc(string message, ClientRpcParams clientRpcParams = default)
    {
        OnlineKickPopup.Show(message);

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();
    }

    private void ForceCameraReassign()
    {
        var assigner = FindFirstObjectByType<OnlineSplitScreenCameraAssigner>();
        if (assigner != null)
            assigner.AssignAllCameras();
    }
}
