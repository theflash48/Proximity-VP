using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnScript : MonoBehaviour
{
    public Transform[] SpawnPoints;

    // Guardamos el PlayerController por índice de jugador
    List<PlayerController_PlayerInput> players = new List<PlayerController_PlayerInput>();
    List<PlayerController_PlayerInput> playersScore =  new List<PlayerController_PlayerInput>();

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        int idx = playerInput.playerIndex;

        // Asegura capacidad de la lista
        while (players.Count <= idx) players.Add(null);

        // Coloca al jugador en su punto de spawn (cíclico si hay más jugadores que spawn points)
        Transform sp = SpawnPoints[idx % SpawnPoints.Length];
        playerInput.transform.SetPositionAndRotation(sp.position, sp.rotation);

        // Guarda referencia a su PlayerController
        players[idx] = playerInput.GetComponent<PlayerController_PlayerInput>();
    }

    // (Opcional) Limpieza si un jugador se va
    public void OnPlayerLeft(PlayerInput playerInput)
    {
        int idx = playerInput.playerIndex;
        if (idx >= 0 && idx < players.Count) players[idx] = null;
    }

    // Leer el score de un jugador por índice
    public int GetScore(int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= players.Count) return 0;
        var pc = players[playerIndex];
        return pc ? pc.score : 0;
    }

    public void OnTimeFinished()
    {
        playersScore.Clear();

        foreach (var p in players)
        {
            if (p != null)
            {
                playersScore.Add(p);
            }
        }
        playersScore.Sort((a, b) => b.score.CompareTo(a.score));
    }
}
