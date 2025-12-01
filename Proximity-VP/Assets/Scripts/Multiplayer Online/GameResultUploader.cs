using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameResultUploader : MonoBehaviour
{
    [Header("PHP URL")]
    public string startGameUrl = "http://localhost/screencheat/start_game.php";
    public string endGameUrl   = "http://localhost/screencheat/end_game.php";

    public int CurrentGameId { get; private set; }

    public IEnumerator StartGameOnServer(int totalPlayers, int mapId)
    {
        WWWForm form = new WWWForm();
        form.AddField("total_players", totalPlayers);
        form.AddField("map_id", mapId);

        using (UnityWebRequest www = UnityWebRequest.Post(startGameUrl, form))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Error start_game.php: " + www.error);
                yield break;
            }

            var resp = JsonUtility.FromJson<StartGameResp>(www.downloadHandler.text);
            if (resp.success)
            {
                CurrentGameId = resp.game_id;
                Debug.Log("Game ID = " + CurrentGameId);
            }
        }
    }

    public IEnumerator SendResults(int winnerAccId, List<PlayerResult> players)
    {
        if (CurrentGameId <= 0)
        {
            Debug.LogWarning("GameResultUploader: CurrentGameId vacío, no se envía nada");
            yield break;
        }

        GameResultData data = new GameResultData
        {
            game_id       = CurrentGameId,
            winner_acc_id = winnerAccId,
            players       = players.ToArray()
        };

        string json = JsonUtility.ToJson(data);

        using (UnityWebRequest www = new UnityWebRequest(endGameUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Error end_game.php: " + www.error);
            }
            else
            {
                Debug.Log("Resultados enviados correctamente");
            }
        }
    }

    [System.Serializable]
    public class PlayerResult
    {
        public int acc_id;
        public int kills;
        public int deaths;
        public int is_host;
    }

    [System.Serializable]
    class GameResultData
    {
        public int game_id;
        public int winner_acc_id;
        public PlayerResult[] players;
    }

    [System.Serializable]
    class StartGameResp
    {
        public bool success;
        public int game_id;
    }
}
