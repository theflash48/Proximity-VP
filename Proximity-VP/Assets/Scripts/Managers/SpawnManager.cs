using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    public GameObject blackScreen;
    public GameObject blackCamera;

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;

    public List<GameObject> activePlayers = new List<GameObject>();
    public int ActivePlayersCount => activePlayers.Count;

    [Header("Online Auto Discover")]
    [SerializeField] private bool autoDiscoverOnlinePlayers = true;
    [SerializeField] private float discoverInterval = 0.5f;

    private Coroutine discoverRoutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void OnEnable()
    {
        if (autoDiscoverOnlinePlayers && discoverRoutine == null)
            discoverRoutine = StartCoroutine(DiscoverLoop());
    }

    void OnDisable()
    {
        if (discoverRoutine != null)
        {
            StopCoroutine(discoverRoutine);
            discoverRoutine = null;
        }
    }

    private IEnumerator DiscoverLoop()
    {
        while (true)
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                RefreshActivePlayersFromScene();

            yield return new WaitForSeconds(discoverInterval);
        }
    }

    private void RefreshActivePlayersFromScene()
    {
        var found = GameObject.FindGameObjectsWithTag("Player");
        var set = new HashSet<GameObject>();

        for (int i = 0; i < found.Length; i++)
        {
            var go = found[i];
            if (!go) continue;

            var netObj = go.GetComponent<NetworkObject>();
            if (!netObj || !netObj.IsSpawned) continue;

            set.Add(go);
        }

        activePlayers.RemoveAll(p => p == null || !set.Contains(p));
        foreach (var go in set)
        {
            if (!activePlayers.Contains(go))
                activePlayers.Add(go);
        }

        ApplyBlackState();
    }

    private void ApplyBlackState()
    {
        bool hasPlayers = activePlayers.Count > 0;
        if (blackScreen != null) blackScreen.SetActive(!hasPlayers);
        if (blackCamera != null) blackCamera.SetActive(!hasPlayers);
    }

    public void RegisterPlayer(GameObject player)
    {
        if (!player) return;
        if (!activePlayers.Contains(player))
            activePlayers.Add(player);
        ApplyBlackState();
    }

    public void UnregisterPlayer(GameObject player)
    {
        if (!player) return;
        if (activePlayers.Contains(player))
            activePlayers.Remove(player);
        ApplyBlackState();
    }

    public Transform GetFarthestSpawnPoint(GameObject playerToSpawn)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No hay spawn points.");
            return null;
        }

        List<GameObject> otherPlayers = activePlayers.Where(p => p != playerToSpawn && p != null).ToList();

        if (otherPlayers.Count == 0)
            return spawnPoints[Random.Range(0, spawnPoints.Length)];

        Transform bestSpawn = spawnPoints[0];
        float maxMinDistance = 0f;

        foreach (Transform spawn in spawnPoints)
        {
            float minDistanceToPlayers = float.MaxValue;

            foreach (GameObject player in otherPlayers)
            {
                float distance = Vector3.Distance(spawn.position, player.transform.position);
                if (distance < minDistanceToPlayers)
                    minDistanceToPlayers = distance;
            }

            if (minDistanceToPlayers > maxMinDistance)
            {
                maxMinDistance = minDistanceToPlayers;
                bestSpawn = spawn;
            }
        }

        return bestSpawn;
    }

    void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.cyan;
        foreach (Transform spawn in spawnPoints)
        {
            if (spawn == null) continue;
            Gizmos.DrawWireSphere(spawn.position, 0.5f);
            Gizmos.DrawLine(spawn.position, spawn.position + Vector3.up * 2f);
        }
    }
}
