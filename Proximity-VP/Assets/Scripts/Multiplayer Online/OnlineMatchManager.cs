using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OnlineMatchManager : NetworkBehaviour
{
    public static OnlineMatchManager Instance;

    [Serializable]
    public class MapEntry
    {
        public int mapId = 1;
        public GameObject mapPrefab;
    }

    public enum MatchPhase : byte
    {
        Lobby = 0,
        Running = 1,
        Ended = 2
    }

    public struct StandingNet : INetworkSerializable, IEquatable<StandingNet>
    {
        public int AccId;
        public FixedString64Bytes Username;
        public int Kills;
        public int SlotIndex;
        public int IsHost;
        public int WasConnectedAtEnd;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref AccId);
            serializer.SerializeValue(ref Username);
            serializer.SerializeValue(ref Kills);
            serializer.SerializeValue(ref SlotIndex);
            serializer.SerializeValue(ref IsHost);
            serializer.SerializeValue(ref WasConnectedAtEnd);
        }

        public bool Equals(StandingNet other) => AccId == other.AccId;
    }

    private class PlayerState
    {
        public int AccId;
        public string Username;
        public int SlotIndex;
        public ulong ClientId;
        public bool IsConnected;

        public int Kills;
        public int Deaths;
        public bool IsHost;

        public readonly Dictionary<int, float> TimeRemainingAtKillCount = new Dictionary<int, float>();
    }

    [Header("Maps (Online)")]
    public MapEntry[] maps;

    [Header("Max players (online)")]
    public int expectedPlayers = 4; // MAX

    [Header("BD uploader (solo host)")]
    public GameResultUploader resultUploader;

    [Header("References")]
    public TimerOnline timerOnline;

    private readonly NetworkVariable<int> currentMapIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<byte> phaseNet = new NetworkVariable<byte>(
        (byte)MatchPhase.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public readonly NetworkList<StandingNet> FinalStandings = new NetworkList<StandingNet>();

    private GameObject mapInstance;

    private readonly Dictionary<int, PlayerState> statesByAcc = new Dictionary<int, PlayerState>();
    private readonly Dictionary<ulong, int> accByClient = new Dictionary<ulong, int>();

    private struct DeathRec
    {
        public int accId;
        public Vector3 pos;
        public DeathRec(int accId, Vector3 pos) { this.accId = accId; this.pos = pos; }
    }
    private readonly List<DeathRec> deathRecords = new List<DeathRec>();

    public MatchPhase Phase => (MatchPhase)phaseNet.Value;

    public int CurrentMapId
    {
        get
        {
            int idx = currentMapIndex.Value;
            if (idx < 0 || maps == null || idx >= maps.Length) return 0;
            return maps[idx].mapId;
        }
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    public override void OnNetworkSpawn()
    {
        if (timerOnline == null)
            timerOnline = FindFirstObjectByType<TimerOnline>();

        currentMapIndex.OnValueChanged += OnMapIndexChanged;

        if (IsServer)
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            }

            if (currentMapIndex.Value < 0)
                ChooseRandomMapServer();
        }

        ApplyMapLocal();
    }

    public override void OnNetworkDespawn()
    {
        currentMapIndex.OnValueChanged -= OnMapIndexChanged;

        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnMapIndexChanged(int prev, int next) => ApplyMapLocal();

    public void ChooseRandomMapServer()
    {
        if (!IsServer) return;

        if (maps == null || maps.Length == 0)
        {
            Debug.LogError("OnlineMatchManager: no hay maps asignados en inspector.");
            return;
        }

        currentMapIndex.Value = UnityEngine.Random.Range(0, maps.Length);
    }

    private void ApplyMapLocal()
    {
        DestroyKnownMapRoots();

        int idx = currentMapIndex.Value;
        if (idx < 0 || maps == null || idx >= maps.Length) return;

        var prefab = maps[idx].mapPrefab;
        if (prefab == null)
        {
            Debug.LogError("OnlineMatchManager: mapPrefab NULL en maps[" + idx + "].");
            return;
        }

        mapInstance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
        mapInstance.name = prefab.name;

        // Si tu SpawnManager tiene auto-discover, aquí funcionará (en tu proyecto compila)
        if (SpawnManager.Instance != null)
            SpawnManager.Instance.AutoDiscoverSpawnPoints(mapInstance);
    }

    private void DestroyKnownMapRoots()
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        if (roots == null) return;

        HashSet<string> mapNames = null;
        if (maps != null)
        {
            mapNames = new HashSet<string>();
            foreach (var m in maps)
                if (m != null && m.mapPrefab != null)
                    mapNames.Add(m.mapPrefab.name);
        }

        if (mapNames == null || mapNames.Count == 0) return;

        foreach (var go in roots)
        {
            if (go == null) continue;
            if (mapNames.Contains(go.name))
                Destroy(go);
        }

        mapInstance = null;
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        if (accByClient.TryGetValue(clientId, out int accId))
        {
            accByClient.Remove(clientId);
            if (statesByAcc.TryGetValue(accId, out var st))
                st.IsConnected = false;
        }

        // Si la partida estaba corriendo y queda 1 conectado => fin
        if (Phase == MatchPhase.Running)
        {
            int connected = statesByAcc.Values.Count(s => s.IsConnected);
            if (connected <= 1 && timerOnline != null)
                timerOnline.ForceEndGameServer();
        }
    }

    public bool ServerRegisterOrRestore(PlayerIdentityOnline identity)
    {
        if (!IsServer) return false;
        if (identity == null) return false;

        int accId = identity.AccId.Value;
        if (accId <= 0) return false;

        string username = identity.Username.Value.ToString();
        int slot = identity.SlotIndex.Value;
        ulong clientId = identity.OwnerClientId;

        bool existed = statesByAcc.ContainsKey(accId);

        // Denegar join nuevo si no estamos en lobby (permitimos rejoin si existed)
        if (Phase != MatchPhase.Lobby && !existed)
            return false;

        if (!statesByAcc.TryGetValue(accId, out var st))
        {
            st = new PlayerState
            {
                AccId = accId,
                Username = username,
                SlotIndex = slot,
                ClientId = clientId,
                IsConnected = true,
                Kills = 0,
                Deaths = 0,
                IsHost = (clientId == NetworkManager.ServerClientId)
            };
            statesByAcc[accId] = st;
        }
        else
        {
            st.Username = username;
            st.SlotIndex = slot;
            st.ClientId = clientId;
            st.IsConnected = true;
            st.IsHost = (clientId == NetworkManager.ServerClientId);
        }

        accByClient[clientId] = accId;

        var pc = identity.GetComponent<PlayerControllerOnline>();
        if (pc != null) pc.SetScoreServer(st.Kills);

        var ph = identity.GetComponent<PlayerHealthOnline>();
        if (ph != null) ph.SetDeathsServer(st.Deaths);

        return true;
    }

    public bool ServerAllConnectedVerified()
    {
        if (!IsServer) return false;
        if (NetworkManager.Singleton == null) return false;

        foreach (var cid in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!accByClient.ContainsKey(cid))
                return false;
        }
        return true;
    }

    // ✅ MATCH START
    public void ServerOnMatchStarted()
    {
        if (!IsServer) return;

        phaseNet.Value = (byte)MatchPhase.Running;
        FinalStandings.Clear();
        deathRecords.Clear();

        // TP a spawns al empezar (para TODOS)
        StartCoroutine(TeleportAllToStartSpawnsRoutine());

        if (resultUploader != null)
        {
            resultUploader.ResetGameId();

            int totalPlayers = expectedPlayers;
            if (NetworkManager.Singleton != null)
                totalPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;

            StartCoroutine(resultUploader.StartGameOnServer(totalPlayers, CurrentMapId));
        }
    }

    private IEnumerator TeleportAllToStartSpawnsRoutine()
    {
        // Esperar un par de frames (mapa + spawns descubiertos)
        yield return null;
        yield return null;

        if (SpawnManager.Instance == null || SpawnManager.Instance.spawnPoints == null || SpawnManager.Instance.spawnPoints.Length == 0)
            yield break;

        if (NetworkManager.Singleton == null) yield break;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObj = kv.Value.PlayerObject;
            if (playerObj == null) continue;

            var id = playerObj.GetComponent<PlayerIdentityOnline>();
            int slot = (id != null) ? Mathf.Max(0, id.SlotIndex.Value) : 0;

            var spawns = SpawnManager.Instance.spawnPoints;
            int idx = slot % spawns.Length;
            Transform sp = spawns[idx];

            // Server también lo mueve (para host / consistencia)
            playerObj.transform.SetPositionAndRotation(sp.position, sp.rotation);

            // Owner lo aplica en su cliente (porque el movimiento es owner-side en tu controller)
            var target = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { kv.Key } }
            };
            TeleportLocalPlayerClientRpc(sp.position, sp.rotation, target);
        }
    }

    [ClientRpc]
    private void TeleportLocalPlayerClientRpc(Vector3 pos, Quaternion rot, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton == null) return;

        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null) return;

        localPlayer.transform.SetPositionAndRotation(pos, rot);

        var rb = localPlayer.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // tie-break kill time
    public void ServerRecordKill(int killerAccId, int newKillCount, float timeRemaining)
    {
        if (!IsServer) return;
        if (!statesByAcc.TryGetValue(killerAccId, out var st)) return;

        st.Kills = Mathf.Max(st.Kills, newKillCount);
        if (!st.TimeRemainingAtKillCount.ContainsKey(newKillCount))
            st.TimeRemainingAtKillCount[newKillCount] = timeRemaining;
    }

    public void ServerRecordDeath(int victimAccId, Vector3 pos)
    {
        if (!IsServer) return;
        if (!statesByAcc.TryGetValue(victimAccId, out var st)) return;

        st.Deaths += 1;
        deathRecords.Add(new DeathRec(victimAccId, pos));
    }

    public void ServerOnMatchEnded()
    {
        if (!IsServer) return;
        if (Phase != MatchPhase.Running) return;

        phaseNet.Value = (byte)MatchPhase.Ended;

        var ordered = ComputeFinalOrder();

        FinalStandings.Clear();
        foreach (var st in ordered)
        {
            FinalStandings.Add(new StandingNet
            {
                AccId = st.AccId,
                Username = new FixedString64Bytes(st.Username ?? ""),
                Kills = st.Kills,
                SlotIndex = st.SlotIndex,
                IsHost = st.IsHost ? 1 : 0,
                WasConnectedAtEnd = st.IsConnected ? 1 : 0
            });
        }

        if (resultUploader != null && resultUploader.CurrentGameId > 0 && ordered.Count > 0)
            StartCoroutine(UploadEndGameCoroutine(ordered));
    }

    private float TimeAt(PlayerState st, int killCount)
    {
        if (killCount <= 0) return float.NegativeInfinity;
        return st.TimeRemainingAtKillCount.TryGetValue(killCount, out float t) ? t : float.NegativeInfinity;
    }

    private List<PlayerState> ComputeFinalOrder()
    {
        return statesByAcc.Values
            .OrderByDescending(s => s.Kills)
            .ThenByDescending(s => TimeAt(s, s.Kills))
            .ThenBy(s => s.SlotIndex)
            .ToList();
    }

    private IEnumerator UploadEndGameCoroutine(List<PlayerState> ordered)
    {
        if (!IsServer) yield break;
        if (resultUploader == null) yield break;
        if (ordered == null || ordered.Count == 0) yield break;

        var winner = ordered[0];

        var players = new List<GameResultUploader.PlayerResult>();
        foreach (var st in statesByAcc.Values)
        {
            players.Add(new GameResultUploader.PlayerResult
            {
                acc_id = st.AccId,
                kills = st.Kills,
                deaths = st.Deaths,
                is_host = st.IsHost ? 1 : 0
            });
        }

        var deaths = new List<GameResultUploader.DeathRecord>();
        foreach (var d in deathRecords)
            deaths.Add(new GameResultUploader.DeathRecord(d.accId, d.pos));

        yield return resultUploader.SendResults(winner.AccId, players, deaths);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRematchServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId) return;
        ServerStartRematch();
    }

    public void ServerStartRematch()
    {
        if (!IsServer) return;

        phaseNet.Value = (byte)MatchPhase.Lobby;
        FinalStandings.Clear();
        deathRecords.Clear();

        foreach (var st in statesByAcc.Values)
        {
            st.Kills = 0;
            st.Deaths = 0;
            st.TimeRemainingAtKillCount.Clear();
        }

        if (resultUploader != null)
            resultUploader.ResetGameId();

        ChooseRandomMapServer();

        if (timerOnline != null)
            timerOnline.ResetToLobbyServer();

        StartCoroutine(RematchResetPlayersRoutine());
    }

    private IEnumerator RematchResetPlayersRoutine()
    {
        yield return null;
        yield return null;

        if (SpawnManager.Instance != null)
            SpawnManager.Instance.AutoDiscoverSpawnPoints(mapInstance);

        if (NetworkManager.Singleton == null) yield break;

        foreach (var kv in NetworkManager.Singleton.ConnectedClients)
        {
            var playerObj = kv.Value.PlayerObject;
            if (playerObj == null) continue;

            var pc = playerObj.GetComponent<PlayerControllerOnline>();
            if (pc != null) pc.SetScoreServer(0);

            var ph = playerObj.GetComponent<PlayerHealthOnline>();
            if (ph != null) ph.ResetForRematchServer();
        }
    }
}
