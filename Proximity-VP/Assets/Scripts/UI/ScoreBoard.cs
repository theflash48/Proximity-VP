using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScoreBoard : MonoBehaviour
{
    public List<PlayerControllerLocal> localPlayers = new List<PlayerControllerLocal>();
    List<PlayerControllerOnline> onlinePlayers = new List<PlayerControllerOnline>();

    public List<GameObject> playerOrder = new List<GameObject>();

    public GameObject endScreen;
    public GameObject[] scoreBanners;
    public GameObject scorePanel;

    private void OnEnable()
    {
        TimerLocal.onTryStartGame += LocatePlayers;
        PlayerControllerLocal.onScoreUP += UpdateScores;
        TimerLocal.onEndGame += PrintScores;
    }

    void Start()
    {
        endScreen.SetActive(false);
    }

    private void LocatePlayers()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++) {
            playerOrder.Add(players[i]);
            Debug.Log(i + " added");
        }

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
        TimerLocal.onTryStartGame -= LocatePlayers;
        PlayerControllerLocal.onScoreUP -= UpdateScores;
        TimerLocal.onEndGame -= PrintScores;
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
        endScreen.SetActive(true);
        if (localPlayers[0].GetComponent<PlayerControllerLocal>() != null)
        {
            for (int i = 0; i < localPlayers.Count; i++)
            {
                GameObject ban;
                if (i >= 3)
                    ban = Instantiate(scoreBanners[3]);
                else
                    ban = Instantiate(scoreBanners[i]);
                ban.transform.SetParent(scorePanel.transform);
                switch (i)
                {
                    case 0:
                        
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (localPlayers[0].score == playerOrder[b].GetComponent<PlayerControllerLocal>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("1st", "Player " + playersName.ToString(), localPlayers[0].score.ToString() + " Kills");
                        break;
                    case 1:
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (localPlayers[1].score == playerOrder[b].GetComponent<PlayerControllerLocal>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("2nd", "Player " + playersName.ToString(), localPlayers[1].score.ToString() + " Kills");
                        break;
                    case 2:
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (localPlayers[2].score == playerOrder[b].GetComponent<PlayerControllerLocal>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("3rd", "Player " + playersName.ToString(), localPlayers[2].score.ToString() + " Kills");
                        break;
                    case 3:
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (localPlayers[3].score == playerOrder[b].GetComponent<PlayerControllerLocal>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("2nd", "Player " + playersName.ToString(), localPlayers[3].score.ToString() + " Kills");
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
                        ban.GetComponent<ScoreBanner>().UpdateBanner("1st", "Player " + playersName.ToString(), onlinePlayers[0].score.ToString() + " Kills");
                        break;
                    case 1:
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (onlinePlayers[1].score == playerOrder[b].GetComponent<PlayerControllerOnline>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("2nd", "Player " + playersName.ToString(), onlinePlayers[1].score.ToString() + " Kills");
                        break;
                    case 2:
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (onlinePlayers[2].score == playerOrder[b].GetComponent<PlayerControllerOnline>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("3rd", "Player " + playersName.ToString(), onlinePlayers[2].score.ToString() + " Kills");
                        break;
                    case 3:
                        for (int b = 0; b < playerOrder.Count; b++) {
                            if (onlinePlayers[3].score == playerOrder[b].GetComponent<PlayerControllerOnline>().score)
                                playersName = b + 1;
                        }
                        ban.GetComponent<ScoreBanner>().UpdateBanner("2nd", "Player " + playersName.ToString(), onlinePlayers[3].score.ToString() + " Kills");
                        break;
                }
            }
        }
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMain()
    {
        SceneManager.LoadScene("MainMenu");
    }
}


// CODIGO IKER PERDON TIM

    public GameResultUploader resultUploader; // asignar en el Inspector
    public int currentMapId = 1;              // o el que toque

    public void PrintScores()
    {
        endScreen.SetActive(true);
        var gameMode = FindObjectOfType<GameModeManager>();
        if (gameMode != null && gameMode.conection == GameModeManager.conectionType.online &&
            AccountSession.Instance != null && AccountSession.Instance.IsLoggedIn &&
            resultUploader != null)
        {
            StartCoroutine(SendResultsCoroutine());
        }
    }

    IEnumerator SendResultsCoroutine()
    {
        // 1) si no se ha creado la partida en el server aún, la creamos
        if (resultUploader.CurrentGameId <= 0)
        {
            int totalPlayers = localPlayers.Count > 0 ? localPlayers.Count : onlinePlayers.Count;
            yield return resultUploader.StartGameOnServer(totalPlayers, currentMapId);
        }

        // 2) montamos los datos de cada jugador
        List<GameResultUploader.PlayerResult> players = new List<GameResultUploader.PlayerResult>();

        if (localPlayers.Count > 0)
        {
            foreach (var p in localPlayers)
            {
                // si no hay login, podemos mandar acc_id = 0 o usar un mapa local->online
                players.Add(new GameResultUploader.PlayerResult
                {
                    acc_id = AccountSession.Instance.AccId, // simplificado: todos la misma cuenta
                    kills  = p.score,
                    deaths = 0,
                    is_host = 1 // por ahora
                });
            }
        }
        else if (onlinePlayers.Count > 0)
        {
            foreach (var p in onlinePlayers)
            {
                players.Add(new GameResultUploader.PlayerResult
                {
                    acc_id = AccountSession.Instance.AccId, // luego puedes mapear cada player->acc
                    kills  = p.score,
                    deaths = 0,
                    is_host = (p.playerInput != null && p.playerInput.playerIndex == 0) ? 1 : 0
                });
            }
        }

        // 3) determinar ganador: el primero de la lista ordenada
        int winnerAccId = AccountSession.Instance.AccId;

        yield return resultUploader.SendResults(winnerAccId, players);
    }
