using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GameResultUploader : MonoBehaviour
{
    [Header("PHP URL")]
    public string startGameUrl = "http://proximityvp.alwaysdata.net/screencheat/start_game.php";
    public string endGameUrl   = "http://proximityvp.alwaysdata.net/screencheat/end_game.php";

    public int CurrentGameId { get; private set; }

    public void ResetGameId()
    {
        CurrentGameId = 0;
    }

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
                Debug.LogWarning("Error start_game.php: " + www.error + " | " + www.downloadHandler.text);
                yield break;
            }

            var resp = JsonUtility.FromJson<StartGameResp>(www.downloadHandler.text);
            if (resp != null && resp.success)
            {
                CurrentGameId = resp.game_id;
                Debug.Log("Game ID = " + CurrentGameId);
            }
            else
            {
                Debug.LogWarning("start_game.php respondió sin success: " + www.downloadHandler.text);
            }
        }
    }

    public IEnumerator SendResults(int winnerAccId, List<PlayerResult> players)
    {
        return SendResults(winnerAccId, players, null);
    }

    public IEnumerator SendResults(int winnerAccId, List<PlayerResult> players, List<DeathRecord> deaths)
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
            players       = players != null ? players.ToArray() : new PlayerResult[0],
            deaths        = deaths != null ? deaths.ToArray() : null
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
                Debug.LogWarning("Error end_game.php: " + www.error + " | " + www.downloadHandler.text);
            else
                Debug.Log("Resultados enviados correctamente: " + www.downloadHandler.text);
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
    public class DeathRecord
    {
        public int acc_id;
        public float x_pos;
        public float y_pos;
        public float z_pos;
        public float death_time;

        public DeathRecord() { }

        public DeathRecord(int accId, Vector3 pos)
        {
            acc_id = accId;
            x_pos = pos.x;
            y_pos = pos.y;
            z_pos = pos.z;
            death_time = 0f;
        }
    }

    [System.Serializable]
    class GameResultData
    {
        public int game_id;
        public int winner_acc_id;
        public PlayerResult[] players;
        public DeathRecord[] deaths;
    }

    [System.Serializable]
    class StartGameResp
    {
        public bool success;
        public int game_id;
    }
}
