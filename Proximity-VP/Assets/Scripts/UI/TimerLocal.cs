using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TimerLocal : MonoBehaviour
{
    [SerializeField] Text timerText;
    [SerializeField] public float remainingTime;
    [SerializeField] GameObject uiCanvas;
    ButtonsController btnControllers;

    bool counting = false;
    public bool gameStarted = false;

    private void Awake()
    {
        btnControllers = GetComponent<ButtonsController>();

        uiCanvas.SetActive(false);
    }

    void Update()
    {
        //Iniciar Juego
        if (Input.GetKeyDown(KeyCode.Space) && !gameStarted)
        {
            TryStartGame();
        }

        //Actualizar temporizador si est� en marcha
        if (counting)
        {
            remainingTime -=Time.deltaTime;

            if (remainingTime <= 0)
            {
                counting = false;
                remainingTime = 0;
                Debug.Log("Tiempo Acabado");
                //Time.timeScale = 0;
                uiCanvas.SetActive(true);
            }
        }
        
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (Input.GetKeyDown(KeyCode.V))
        {
            btnControllers.Restart();

        }
        
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
