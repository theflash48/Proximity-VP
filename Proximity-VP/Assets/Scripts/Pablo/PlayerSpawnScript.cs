using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawnScript : MonoBehaviour
{
    public Transform[] SpawnPoints;

    List<PlayerController> players = new List<PlayerController>();
    List<PlayerController> playersScore =  new List<PlayerController>();

    public void OnPlayerJoined(PlayerInput playerInput)
    {
        int idx = playerInput.playerIndex;

        while (players.Count <= idx) players.Add(null);

        Transform sp = SpawnPoints[idx % SpawnPoints.Length];
        playerInput.transform.SetPositionAndRotation(sp.position, sp.rotation);

        // Guarda referencia a su PlayerController
        players[idx] = playerInput.GetComponent<PlayerController>();
    }

    public void OnPlayerLeft(PlayerInput playerInput)
    {
        int idx = playerInput.playerIndex;
        if (idx >= 0 && idx < players.Count) players[idx] = null;
    }

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
