using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnScript : MonoBehaviour
{
    public Transform[] SpawnPoints;
    int m_playerCount; //Contador de Jugadores

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        playerInput.transform.position = SpawnPoints[m_playerCount].transform.position;
        m_playerCount++;
    }
}
