using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

public class ScoreBoard : MonoBehaviour
{
    List<PlayerControllerLocal> localPlayers = new List<PlayerControllerLocal>();
    List<PlayerControllerOnline> onlinePlayers = new List<PlayerControllerOnline>();

    List<GameObject> playerOrder = new List<GameObject>();

    public GameObject[] scoreBanners;
    public GameObject scorePanel;

    private void OnEnable()
    {
        TimerLocal.onTryStartGame += LocatePlayers;
        PlayerControllerLocal.onScoreUP += UpdateScores;
    }

    private void LocatePlayers()
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

        for (int i = 0; i < players.Length; i++) {
            playerOrder.Add(players[i]);
        }
    }

    private void OnDisable()
    {
        TimerLocal.onTryStartGame -= LocatePlayers;
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

    int playersName;
    public void PrintScores()
    {
        if (localPlayers[0].GetComponent<PlayerControllerLocal>() != null)
        {
            for (int i = 0; i < localPlayers.Count; i++)
            {
                GameObject ban = Instantiate(scoreBanners[0]);
                ban.transform.SetParent(scorePanel.transform);
                switch (i)
                {
                    case 0:
                        
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (localPlayers[0].score == playerOrder[b].GetComponent<PlayerControllerLocal>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("1st", "Player " + playersName.ToString(), localPlayers[0].score.ToString());
                        break;
                    case 1:
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (localPlayers[1].score == playerOrder[b].GetComponent<PlayerControllerLocal>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("2nd", "Player " + playersName.ToString(), localPlayers[1].score.ToString());
                        break;
                    case 2:
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (localPlayers[2].score == playerOrder[b].GetComponent<PlayerControllerLocal>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("3rd", "Player " + playersName.ToString(), localPlayers[2].score.ToString());
                        break;
                    case 3:
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (localPlayers[3].score == playerOrder[b].GetComponent<PlayerControllerLocal>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("2nd", "Player " + playersName.ToString(), localPlayers[3].score.ToString());
                        break;
                }
            }
        }
        else if (onlinePlayers[0].GetComponent<PlayerControllerOnline>() != null)
        {
            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                GameObject ban = Instantiate(scoreBanners[0]);
                ban.transform.SetParent(scorePanel.transform);
                switch (i)
                {
                    case 0:
                        
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (onlinePlayers[0].score == playerOrder[b].GetComponent<PlayerControllerOnline>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("1st", "Player " + playersName.ToString(), onlinePlayers[0].score.ToString());
                        break;
                    case 1:
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (onlinePlayers[1].score == playerOrder[b].GetComponent<PlayerControllerOnline>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("2nd", "Player " + playersName.ToString(), onlinePlayers[1].score.ToString());
                        break;
                    case 2:
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (onlinePlayers[2].score == playerOrder[b].GetComponent<PlayerControllerOnline>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("3rd", "Player " + playersName.ToString(), onlinePlayers[2].score.ToString());
                        break;
                    case 3:
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (onlinePlayers[3].score == playerOrder[b].GetComponent<PlayerControllerOnline>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("2nd", "Player " + playersName.ToString(), onlinePlayers[3].score.ToString());
                        break;
                }
            }
        }
    }
}
