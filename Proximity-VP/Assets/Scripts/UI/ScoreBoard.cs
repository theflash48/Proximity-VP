using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ScoreBoard : MonoBehaviour
{
    public List<PlayerControllerLocal> localPlayers = new List<PlayerControllerLocal>();
    public List<PlayerControllerOnline> onlinePlayers = new List<PlayerControllerOnline>();

    [Header("UI")]
    public GameObject endScreen;
    public GameObject[] scoreBanners;
    public GameObject scorePanel;

    [Header("OPCIONAL: envío BD")]
    public GameResultUploader resultUploader;
    public int currentMapId = 1;

    [Header("Mostrar Join Code")]
    [SerializeField] Text joinCodeDisplay;

    void OnEnable()
    {
        TimerLocal.onTryStartGame += LocatePlayers;
        TimerLocal.onEndGame += PrintScores;

        TimerOnline.onTryStartGame += LocatePlayers;
        TimerOnline.onEndGame += PrintScores;

        PlayerControllerLocal.onScoreUP += UpdateScores;
        PlayerControllerOnline.onScoreUPOnline += UpdateScores;
    }

    void OnDisable()
    {
        TimerLocal.onTryStartGame -= LocatePlayers;
        TimerLocal.onEndGame -= PrintScores;

        TimerOnline.onTryStartGame -= LocatePlayers;
        TimerOnline.onEndGame -= PrintScores;

        PlayerControllerLocal.onScoreUP -= UpdateScores;
        PlayerControllerOnline.onScoreUPOnline -= UpdateScores;
    }

    void Start()
    {
        if (endScreen != null)
            endScreen.SetActive(false);

        var netUi = FindFirstObjectByType<NetworkUIManager>();
        if (netUi != null && joinCodeDisplay != null)
            joinCodeDisplay.text = "JoinCode: " + netUi.joinCode;
    }

    private bool IsOnlineSession()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    private void LocatePlayers()
    {
        localPlayers.Clear();
        onlinePlayers.Clear();

        var players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null || players.Length == 0)
            return;

        foreach (var p in players)
        {
            if (p == null) continue;

            var pcOnline = p.GetComponent<PlayerControllerOnline>();
            if (pcOnline != null && pcOnline.enabled)
            {
                onlinePlayers.Add(pcOnline);
                continue;
            }

            var pcLocal = p.GetComponent<PlayerControllerLocal>();
            if (pcLocal != null && pcLocal.enabled)
            {
                localPlayers.Add(pcLocal);
            }
        }
    }

    private void SetPlace(PlayerHUD hud, int i)
    {
        if (hud == null) return;

        switch (i)
        {
            case 0: hud._cPlace.text = "1st"; break;
            case 1: hud._cPlace.text = "2nd"; break;
            case 2: hud._cPlace.text = "3rd"; break;
            case 3: hud._cPlace.text = "4th"; break;
            default: hud._cPlace.text = (i + 1) + "th"; break;
        }
    }

    void UpdateScores()
    {
        if (localPlayers.Count == 0 && onlinePlayers.Count == 0)
            LocatePlayers();

        bool online = IsOnlineSession();

        if (online && onlinePlayers.Count > 0)
        {
            onlinePlayers = onlinePlayers.Where(p => p != null).OrderByDescending(p => p.score).ToList();
            for (int i = 0; i < onlinePlayers.Count; i++)
                SetPlace(onlinePlayers[i].GetComponent<PlayerHUD>(), i);
            return;
        }

        if (!online && localPlayers.Count > 0)
        {
            localPlayers = localPlayers.Where(p => p != null).OrderByDescending(p => p.score).ToList();
            for (int i = 0; i < localPlayers.Count; i++)
                SetPlace(localPlayers[i].GetComponent<PlayerHUD>(), i);
        }
    }

    private string GetOnlineName(GameObject player)
    {
        var id = player.GetComponent<PlayerIdentityOnline>();
        if (id != null && id.Username.Value.Length > 0)
            return id.Username.Value.ToString();

        // fallback
        return "Player";
    }

    public void PrintScores()
    {
        LocatePlayers();
        UpdateScores();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (endScreen != null)
            endScreen.SetActive(true);

        if (scorePanel != null)
        {
            foreach (Transform child in scorePanel.transform)
                Destroy(child.gameObject);
        }

        bool online = IsOnlineSession();

        if (online && onlinePlayers.Count > 0)
        {
            onlinePlayers = onlinePlayers.Where(p => p != null).OrderByDescending(p => p.score).ToList();

            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                GameObject ban = Instantiate(i >= 3 ? scoreBanners[3] : scoreBanners[i]);
                ban.transform.SetParent(scorePanel.transform, false);

                string posStr = (i == 0) ? "1st" :
                                (i == 1) ? "2nd" :
                                (i == 2) ? "3rd" : "4th";

                string username = GetOnlineName(onlinePlayers[i].gameObject);

                ban.GetComponent<ScoreBanner>().UpdateBanner(
                    posStr,
                    username,
                    onlinePlayers[i].score + " Kills"
                );
            }
        }
        else if (!online && localPlayers.Count > 0)
        {
            localPlayers = localPlayers.Where(p => p != null).OrderByDescending(p => p.score).ToList();

            for (int i = 0; i < localPlayers.Count; i++)
            {
                GameObject ban = Instantiate(i >= 3 ? scoreBanners[3] : scoreBanners[i]);
                ban.transform.SetParent(scorePanel.transform, false);

                string posStr = (i == 0) ? "1st" :
                                (i == 1) ? "2nd" :
                                (i == 2) ? "3rd" : "4th";

                var pi = localPlayers[i].GetComponent<PlayerInput>();
                string label = (pi != null) ? ("Player " + (pi.playerIndex + 1)) : "Player";

                ban.GetComponent<ScoreBanner>().UpdateBanner(
                    posStr,
                    label,
                    localPlayers[i].score + " Kills"
                );
            }
        }

        // Subida a BD: SOLO HOST
        if (online &&
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.IsServer &&
            resultUploader != null)
        {
            StartCoroutine(SendResultsCoroutineHostOnly());
        }
    }

    IEnumerator SendResultsCoroutineHostOnly()
    {
        // GameId
        if (resultUploader.CurrentGameId <= 0)
        {
            int totalPlayers = onlinePlayers.Count;
            yield return resultUploader.StartGameOnServer(totalPlayers, currentMapId);
        }

        // Winner por score
        int winnerAccId = 0;
        int bestScore = int.MinValue;

        List<GameResultUploader.PlayerResult> players = new List<GameResultUploader.PlayerResult>();

        foreach (var p in onlinePlayers)
        {
            if (p == null) continue;

            var id = p.GetComponent<PlayerIdentityOnline>();
            int accId = (id != null) ? id.AccId.Value : 0;

            if (p.score > bestScore)
            {
                bestScore = p.score;
                winnerAccId = accId;
            }

            int isHost = 0;
            var no = p.GetComponent<NetworkObject>();
            if (no != null && no.OwnerClientId == 0) isHost = 1;

            players.Add(new GameResultUploader.PlayerResult
            {
                acc_id = accId,
                kills = p.score,
                deaths = 0,
                is_host = isHost
            });
        }

        yield return resultUploader.SendResults(winnerAccId, players);
    }

    public void ReloadScene()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMain()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene("MainMenu");
    }
}
