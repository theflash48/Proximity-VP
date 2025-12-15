using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreBoard : MonoBehaviour
{

    //TIM ES EL UNICO CODIGO QUE TE TOQUE NO ME MATES
    // Listas de jugadores
    public List<PlayerControllerLocal> localPlayers = new List<PlayerControllerLocal>();
    public List<PlayerControllerOnline> onlinePlayers = new List<PlayerControllerOnline>();

    public List<GameObject> playerOrder = new List<GameObject>();

    [Header("UI")]
    public GameObject endScreen;
    public GameObject[] scoreBanners;
    public GameObject scorePanel;

    [Header("OPCIONAL: envío BD")]
    public GameResultUploader resultUploader;
    public int currentMapId = 1;

    void OnEnable()
    {
        TimerLocal.onTryStartGame += LocatePlayers;
        TimerLocal.onEndGame      += PrintScores;
        PlayerControllerLocal.onScoreUP       += UpdateScores;
        PlayerControllerOnline.onScoreUPOnline += UpdateScores;
    }

    void OnDisable()
    {
        TimerLocal.onTryStartGame -= LocatePlayers;
        TimerLocal.onEndGame      -= PrintScores;
        PlayerControllerLocal.onScoreUP       -= UpdateScores;
        PlayerControllerOnline.onScoreUPOnline -= UpdateScores;
    }

    void Start()
    {
        if (endScreen != null)
            endScreen.SetActive(false);
    }

    private void LocatePlayers()
    {
        localPlayers.Clear();
        onlinePlayers.Clear();
        playerOrder.Clear();

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (var p in players)
            playerOrder.Add(p);

        if (players.Length == 0)
        {
            Debug.LogError("ScoreBoard: no hay jugadores en la escena.");
            return;
        }

        if (players[0].GetComponent<PlayerControllerLocal>() != null)
        {
            foreach (var p in players)
            {
                var pc = p.GetComponent<PlayerControllerLocal>();
                if (pc != null)
                    localPlayers.Add(pc);
            }
        }
        else if (players[0].GetComponent<PlayerControllerOnline>() != null)
        {
            foreach (var p in players)
            {
                var pc = p.GetComponent<PlayerControllerOnline>();
                if (pc != null)
                    onlinePlayers.Add(pc);
            }
        }
        else
        {
            Debug.LogError("ScoreBoard: los jugadores no tienen ni PlayerControllerLocal ni PlayerControllerOnline.");
            return;
        }

        UpdateScores();
    }

    void UpdateScores()
    {
        // LOCAL
        if (localPlayers.Count > 0)
        {
            localPlayers = localPlayers.OrderByDescending(p => p.score).ToList();

            for (int i = 0; i < localPlayers.Count; i++)
            {
                var hud = localPlayers[i].GetComponent<PlayerHUD>();
                if (hud == null) continue;

                switch (i)
                {
                    case 0: hud._cPlace.text = "1st"; break;
                    case 1: hud._cPlace.text = "2nd"; break;
                    case 2: hud._cPlace.text = "3rd"; break;
                    case 3: hud._cPlace.text = "4th"; break;
                    default: hud._cPlace.text = (i + 1) + "th"; break;
                }
            }
        }
        // ONLINE
        else if (onlinePlayers.Count > 0)
        {
            onlinePlayers = onlinePlayers.OrderByDescending(p => p.score).ToList();

            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                var hud = onlinePlayers[i].GetComponent<PlayerHUD>();
                if (hud == null) continue;

                switch (i)
                {
                    case 0: hud._cPlace.text = "1st"; break;
                    case 1: hud._cPlace.text = "2nd"; break;
                    case 2: hud._cPlace.text = "3rd"; break;
                    case 3: hud._cPlace.text = "4th"; break;
                    default: hud._cPlace.text = (i + 1) + "th"; break;
                }
            }
        }
    }

    int playersNameIndex;


    public void PrintScores()
    {
        if (endScreen != null)
            endScreen.SetActive(true);

        // Limpiar panel
        foreach (Transform child in scorePanel.transform)
            Destroy(child.gameObject);

        // LOCAL
        if (localPlayers.Count > 0 && localPlayers[0] != null)
        {
            localPlayers = localPlayers.OrderByDescending(p => p.score).ToList();

            for (int i = 0; i < localPlayers.Count; i++)
            {
                GameObject ban;
                if (i >= 3)
                    ban = Object.Instantiate(scoreBanners[3]);
                else
                    ban = Object.Instantiate(scoreBanners[i]);

                ban.transform.SetParent(scorePanel.transform, false);

                for (int b = 0; b < playerOrder.Count; b++)
                {
                    var pc = playerOrder[b].GetComponent<PlayerControllerLocal>();
                    if (pc != null && pc.score == localPlayers[i].score)
                    {
                        playersNameIndex = b + 1;
                        break;
                    }
                }

                string posStr = (i == 0) ? "1st" :
                                (i == 1) ? "2nd" :
                                (i == 2) ? "3rd" :
                                "4th";

                ban.GetComponent<ScoreBanner>().UpdateBanner(
                    posStr,
                    "Player " + playersNameIndex,
                    localPlayers[i].score + " Kills"
                );
            }
        }
        // ONLINE
        else if (onlinePlayers.Count > 0 && onlinePlayers[0] != null)
        {
            onlinePlayers = onlinePlayers.OrderByDescending(p => p.score).ToList();

            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                GameObject ban;
                if (i >= 3)
                    ban = Object.Instantiate(scoreBanners[3]);
                else
                    ban = Object.Instantiate(scoreBanners[i]);

                ban.transform.SetParent(scorePanel.transform, false);

                for (int b = 0; b < playerOrder.Count; b++)
                {
                    var pc = playerOrder[b].GetComponent<PlayerControllerOnline>();
                    if (pc != null && pc.score == onlinePlayers[i].score)
                    {
                        playersNameIndex = b + 1;
                        break;
                    }
                }

                string posStr = (i == 0) ? "1st" :
                                (i == 1) ? "2nd" :
                                (i == 2) ? "3rd" :
                                "4th";

                ban.GetComponent<ScoreBanner>().UpdateBanner(
                    posStr,
                    "Player " + playersNameIndex,
                    onlinePlayers[i].score + " Kills"
                );
            }
        }

        var gm = FindFirstObjectByType<GameModeManager>();
        if (gm != null &&
            gm.conection == GameModeManager.conectionType.online &&
            AccountSession.Instance != null &&
            AccountSession.Instance.IsLoggedIn &&
            resultUploader != null)
        {
            StartCoroutine(SendResultsCoroutine());
        }
    }

    IEnumerator SendResultsCoroutine()
    {
        // 1) crear partida en servidor si aún no existe
        if (resultUploader.CurrentGameId <= 0)
        {
            int totalPlayers = localPlayers.Count > 0 ? localPlayers.Count : onlinePlayers.Count;
            yield return resultUploader.StartGameOnServer(totalPlayers, currentMapId);
        }

        // 2) preparar datos
        List<GameResultUploader.PlayerResult> players = new List<GameResultUploader.PlayerResult>();

        if (localPlayers.Count > 0)
        {
            foreach (var p in localPlayers)
            {
                players.Add(new GameResultUploader.PlayerResult
                {
                    acc_id = AccountSession.Instance.AccId,
                    kills  = p.score,
                    deaths = 0,
                    is_host = 1
                });
            }
        }
        else if (onlinePlayers.Count > 0)
        {
            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                var p = onlinePlayers[i];
                players.Add(new GameResultUploader.PlayerResult
                {
                    acc_id = AccountSession.Instance.AccId, // simplificado
                    kills  = p.score,
                    deaths = 0,
                    is_host = (i == 0) ? 1 : 0      // host = primer jugador
                });
            }
        }

        int winnerAccId = AccountSession.Instance.AccId;

        yield return resultUploader.SendResults(winnerAccId, players);
    }

    // Botones UI
    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMain()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
