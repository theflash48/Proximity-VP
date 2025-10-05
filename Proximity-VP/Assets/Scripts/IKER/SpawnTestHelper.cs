using UnityEngine;

/// <summary>
/// Script de ayuda para testear el sistema de spawns cuando estás solo
/// Presiona la tecla T para crear jugadores dummy en posiciones aleatorias
/// Presiona la tecla R para eliminarlos y volver a probar
/// Presiona la tecla Y para teleportarte al spawn más lejano
/// CORTESIA DE LA IA SAGRADA
/// </summary>
public class SpawnTestHelper : MonoBehaviour
{
    [Header("Test Settings")]
    public GameObject dummyPlayerPrefab; // Opcional: un cubo simple para simular jugadores
    public int numberOfDummies = 3;
    public float spawnRadius = 20f;
    
    private GameObject[] dummyPlayers;
    private GameObject realPlayer;

    void Start()
    {
        // Encontrar al jugador real
        realPlayer = GameObject.FindGameObjectWithTag("Player");
        if (realPlayer == null)
        {
            Debug.LogWarning("No se encontró un jugador con tag 'Player'");
        }
    }

    void Update()
    {
        // Presiona T para crear jugadores dummy
        if (Input.GetKeyDown(KeyCode.T))
        {
            CreateDummyPlayers();
        }

        // Presiona R para eliminar jugadores dummy
        if (Input.GetKeyDown(KeyCode.R))
        {
            RemoveDummyPlayers();
        }

        // Presiona Y para teleportarte al spawn más lejano
        if (Input.GetKeyDown(KeyCode.Y))
        {
            TeleportToFarthestSpawn();
        }
    }

    void CreateDummyPlayers()
    {
        RemoveDummyPlayers(); // Limpiar los anteriores

        dummyPlayers = new GameObject[numberOfDummies];

        for (int i = 0; i < numberOfDummies; i++)
        {
            // Crear posición aleatoria
            Vector3 randomPos = new Vector3(
                Random.Range(-spawnRadius, spawnRadius),
                1f,
                Random.Range(-spawnRadius, spawnRadius)
            );

            // Crear dummy player
            GameObject dummy;
            if (dummyPlayerPrefab != null)
            {
                dummy = Instantiate(dummyPlayerPrefab, randomPos, Quaternion.identity);
            }
            else
            {
                // Crear un cubo simple si no hay prefab
                dummy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dummy.transform.position = randomPos;
                dummy.transform.localScale = Vector3.one * 2f;
                dummy.GetComponent<Renderer>().material.color = Random.ColorHSV();
            }

            dummy.name = "DummyPlayer_" + (i + 1);
            dummy.tag = "Player";
            dummyPlayers[i] = dummy;

            // Registrar en el SpawnManager
            if (SpawnManager.Instance != null)
            {
                SpawnManager.Instance.RegisterPlayer(dummy);
            }

            Debug.Log("Dummy player creado en: " + randomPos);
        }

        Debug.Log("✅ " + numberOfDummies + " jugadores dummy creados. Presiona Y para ir al spawn más lejano.");
    }

    void RemoveDummyPlayers()
    {
        if (dummyPlayers != null)
        {
            foreach (GameObject dummy in dummyPlayers)
            {
                if (dummy != null)
                {
                    // Desregistrar del SpawnManager
                    if (SpawnManager.Instance != null)
                    {
                        SpawnManager.Instance.UnregisterPlayer(dummy);
                    }
                    Destroy(dummy);
                }
            }
            dummyPlayers = null;
            Debug.Log("❌ Jugadores dummy eliminados.");
        }
    }

    void TeleportToFarthestSpawn()
    {
        if (realPlayer == null)
        {
            Debug.LogWarning("No hay jugador real para teleportar");
            return;
        }

        if (SpawnManager.Instance == null)
        {
            Debug.LogWarning("No hay SpawnManager en la escena");
            return;
        }

        Transform farthestSpawn = SpawnManager.Instance.GetFarthestSpawnPoint(realPlayer);
        
        if (farthestSpawn != null)
        {
            realPlayer.transform.position = farthestSpawn.position;
            realPlayer.transform.rotation = farthestSpawn.rotation;
            
            // Resetear velocidad si tiene Rigidbody
            Rigidbody rb = realPlayer.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            Debug.Log("🎯 Teleportado al spawn más lejano: " + farthestSpawn.name + " en posición: " + farthestSpawn.position);
            
            // Mostrar distancias a los dummies
            if (dummyPlayers != null)
            {
                foreach (GameObject dummy in dummyPlayers)
                {
                    if (dummy != null)
                    {
                        float distance = Vector3.Distance(farthestSpawn.position, dummy.transform.position);
                        Debug.Log("Distancia a " + dummy.name + ": " + distance.ToString("F2") + " metros");
                    }
                }
            }
        }
    }

    void OnGUI()
    {
        // Mostrar instrucciones en pantalla
        GUI.color = Color.white;
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        
        GUI.Label(new Rect(10, 10, 500, 30), "=== TEST DE SPAWNS ===", style);
        GUI.Label(new Rect(10, 40, 500, 30), "T - Crear jugadores dummy", style);
        GUI.Label(new Rect(10, 70, 500, 30), "Y - Teleportarse al spawn más lejano", style);
        GUI.Label(new Rect(10, 100, 500, 30), "R - Eliminar jugadores dummy", style);
    }
}