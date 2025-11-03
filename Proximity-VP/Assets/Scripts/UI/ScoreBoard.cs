using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

public class ScoreBoard : MonoBehaviour
{
    List<PlayerControllerLocal> localPlayers = new List<PlayerControllerLocal>();
    List<PlayerControllerOnline> onlinePlayers = new List<PlayerControllerOnline>();

    private void OnEnable()
    {
        PlayerControllerLocal.onScoreUP += UpdateScores;
    }

    private void Start()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (players[0].GetComponent<PlayerControllerLocal>() != null)
        {
            for (int i = 0; i < players.Length; i++)
            {
                localPlayers.Add(players[i].GetComponent<PlayerControllerLocal>());
            }
            UpdateScores();
        }
        else if (players[0].GetComponent<PlayerControllerOnline>() != null)
        {
            for (int i = 0; i < players.Length; i++)
            {
                onlinePlayers.Add(players[i].GetComponent<PlayerControllerOnline>());
            }
            UpdateScores();
        }
        else
        {
            Debug.LogError("There are no players in the game");
            return;
        }
    }

    private void OnDisable()
    {
        PlayerControllerLocal.onScoreUP -= UpdateScores;
    }

    void UpdateScores()
    {
        if (localPlayers.Count > 0)
        {
            localPlayers = localPlayers.OrderByDescending(lP => lP.score).ToList();

            for (int i = 0; localPlayers.Count > 0; i++)
            {
                switch (i)
                {
                    case 0:
                        localPlayers[0].gameObject.GetComponent<PlayerHUD>()._cPlace.text = "1st";
                        break;
                    case 1:
                        localPlayers[1].gameObject.GetComponent<PlayerHUD>()._cPlace.text = "2nd";
                        break;
                    case 2:
                        localPlayers[2].gameObject.GetComponent<PlayerHUD>()._cPlace.text = "3rd";
                        break;
                    case 3:
                        localPlayers[3].gameObject.GetComponent<PlayerHUD>()._cPlace.text = "4th";
                        break;
                }
            }
            
                
        } else if (onlinePlayers.Count > 0)
        {
            onlinePlayers = onlinePlayers.OrderByDescending(oP => oP.score).ToList();

            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                switch (i)
                {
                    case 0:
                        onlinePlayers[0].gameObject.GetComponent<PlayerHUD>()._cPlace.text = "1st";
                        break;
                    case 1:
                        onlinePlayers[1].gameObject.GetComponent<PlayerHUD>()._cPlace.text = "2nd";
                        break; 
                    case 2:
                        onlinePlayers[2].gameObject.GetComponent<PlayerHUD>()._cPlace.text = "3rd";
                        break;
                    case 3:
                        onlinePlayers[3].gameObject.GetComponent<PlayerHUD>()._cPlace.text = "4th";
                        break;
                }
            }
            
        }
    }
}
