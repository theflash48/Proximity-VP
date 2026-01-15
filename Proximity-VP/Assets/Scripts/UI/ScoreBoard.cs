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

    [Header("Mostrar Join Code")]
    [SerializeField] Text joinCodeDisplay;

    private float refreshCd;

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

        if (resultUploader == null)
            resultUploader = FindFirstObjectByType<GameResultUploader>();

        var netUi = FindFirstObjectByType<NetworkUIManager>();
        if (netUi != null && joinCodeDisplay != null)
            joinCodeDisplay.text = "JoinCode: " + netUi.joinCode;

        LocatePlayers();
        UpdateScores();
    }

    void Update()
    {
        // refresco suave para que “place” no dependa solo de eventos
        refreshCd -= Time.deltaTime;
        if (refreshCd <= 0f)
        {
            refreshCd = 0.25f;
            UpdateScores();
        }
    }

    private bool IsOnlineSession()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    private void LocatePlayers()
    {
        localPlayers.Clear();
        onlinePlayers.Clear();

        // 1) por tag (rápido)
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            if (p == null) continue;

            var pcOnline = p.GetComponent<PlayerControllerOnline>();
            if (pcOnline != null) { onlinePlayers.Add(pcOnline); continue; }

            var pcLocal = p.GetComponent<PlayerControllerLocal>();
            if (pcLocal != null) localPlayers.Add(pcLocal);
        }

        // 2) fallback: por ConnectedClients (más robusto en online)
        if (IsOnlineSession() && onlinePlayers.Count == 0)
        {
            foreach (var kv in NetworkManager.Singleton.ConnectedClients)
            {
                if (kv.Value?.PlayerObject == null) continue;
                var go = kv.Value.PlayerObject.gameObject;
                var pcOnline = go.GetComponent<PlayerControllerOnline>();
                if (pcOnline != null && !onlinePlayers.Contains(pcOnline))
                    onlinePlayers.Add(pcOnline);
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

    private int SlotIndexOf(PlayerControllerOnline p)
    {
        var id = p != null ? p.GetComponent<PlayerIdentityOnline>() : null;
        if (id == null) return 999;
        return id.SlotIndex.Value < 0 ? 999 : id.SlotIndex.Value;
    }

    void UpdateScores()
    {
        if (localPlayers.Count == 0 && onlinePlayers.Count == 0)
            LocatePlayers();

        bool online = IsOnlineSession();

        if (online && onlinePlayers.Count > 0)
        {
            onlinePlayers = onlinePlayers
                .Where(p => p != null)
                .OrderByDescending(p => p.score)
                .ThenBy(p => SlotIndexOf(p))
                .ToList();

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

        return "Player";
    }

    private int DetectMapIdFromScene()
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null) continue;
            if (roots[i].name == "DebugMap") return 1;
            if (roots[i].name == "Nuketown") return 2;
        }
        return 1;
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
            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                int idx = Mathf.Clamp(i, 0, scoreBanners.Length - 1);
                GameObject ban = Instantiate(scoreBanners[idx]);
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
            for (int i = 0; i < localPlayers.Count; i++)
            {
                int idx = Mathf.Clamp(i, 0, scoreBanners.Length - 1);
                GameObject ban = Instantiate(scoreBanners[idx]);
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
        if (resultUploader == null) yield break;

        // Crear game_id si hace falta
        if (resultUploader.CurrentGameId <= 0)
        {
            int totalPlayers = onlinePlayers.Count;
            int mapId = DetectMapIdFromScene();
            yield return resultUploader.StartGameOnServer(totalPlayers, mapId);
        }

        if (resultUploader.CurrentGameId <= 0)
            yield break;

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
            if (no != null && NetworkManager.Singleton != null && no.OwnerClientId == NetworkManager.ServerClientId)
                isHost = 1;

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
        StartCoroutine(ShutdownAndLoad(SceneManager.GetActiveScene().name, clearSession: false));
    }

    public void BackToMain()
    {
        // Tu botón Next llama aquí
        StartCoroutine(ShutdownAndLoad("MainMenu", clearSession: true));
    }

    private IEnumerator ShutdownAndLoad(string sceneName, bool clearSession)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // IMPORTANTÍSIMO: limpiar statics del online para poder crear/entrar en otra sala
        PlayerIdentityOnline.ResetStaticState();

        if (clearSession && AccountSession.Instance != null)
            AccountSession.Instance.ClearSession();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            yield return null; // 1 frame para que se asiente
        }

        SceneManager.LoadScene(sceneName);
    }
}
