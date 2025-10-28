using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class TimerLocal : MonoBehaviour
{
    [SerializeField] Text timerText;
    [SerializeField]float remainingTime;

    bool counting = false;
    bool gameStarted = false;

    void Update()
    {
        //Iniciar Juego
        if (Input.GetKeyDown(KeyCode.Space) && !gameStarted)
        {
            TryStartGame();
        }

        //Actualizar temporizador si está en marcha
        if (counting)
        {
            remainingTime -=Time.deltaTime;

            if (remainingTime <= 0)
            {
                counting = false;
                remainingTime = 0;
                Debug.Log("Tiempo Acabado");
            }
        }
        
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        
    }

    void TryStartGame()
    {
        if (SpawnManager.Instance.ActivePlayersCount >= 2 && !gameStarted)
        {
            gameStarted = true;
            counting = true;
        }
    }
}
