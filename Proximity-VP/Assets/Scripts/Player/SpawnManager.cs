using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;

    [Header("Spawn Settings")]
    public Transform[] spawnPoints; // Array con los 6 spawn points
    
    private List<GameObject> activePlayers = new List<GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Registrar jugadores activos
    public void RegisterPlayer(GameObject player)
    {
        if (!activePlayers.Contains(player))
        {
            activePlayers.Add(player);
        }
    }

    public void UnregisterPlayer(GameObject player)
    {
        if (activePlayers.Contains(player))
        {
            activePlayers.Remove(player);
        }
    }

    // Obtener el spawn point mas lejano de todos los jugadores activos
    public Transform GetFarthestSpawnPoint(GameObject playerToSpawn)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("No hay spawn points bruh");
            return null;
        }

        List<GameObject> otherPlayers = activePlayers.Where(p => p != playerToSpawn && p != null).ToList();
        
        if (otherPlayers.Count == 0)
        {
            return spawnPoints[Random.Range(0, spawnPoints.Length)];
        }

        Transform bestSpawn = spawnPoints[0];
        float maxMinDistance = 0f;

        foreach (Transform spawn in spawnPoints)
        {
            // Calcular la distancia de este spawn a cualquier jugador
            float minDistanceToPlayers = float.MaxValue;

            foreach (GameObject player in otherPlayers)
            {
                if (player != null)
                {
                    float distance = Vector3.Distance(spawn.position, player.transform.position);
                    if (distance < minDistanceToPlayers)
                    {
                        minDistanceToPlayers = distance;
                    }
                }
            }

            // Si este spawn tiene la mayor distancia mnima, es el mejor
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
        if (spawnPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform spawn in spawnPoints)
            {
                if (spawn != null)
                {
                    Gizmos.DrawWireSphere(spawn.position, 0.5f);
                    Gizmos.DrawLine(spawn.position, spawn.position + Vector3.up * 2f);
                }
            }
        }
    }
}