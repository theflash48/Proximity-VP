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
                EndGame();
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

    public delegate void OnTryStartGame();
    public static OnTryStartGame onTryStartGame;
    void TryStartGame()
    {
        if (SpawnManager.Instance.ActivePlayersCount >= 2 && !gameStarted)
        {
            gameStarted = true;
            counting = true;
            onTryStartGame?.Invoke();
        }
    }
    
    public delegate void OnEndGame();
    public static OnEndGame onEndGame;
    void EndGame()
    {
        if (SpawnManager.Instance.ActivePlayersCount >= 2 && !gameStarted)
        {
            counting = false;
            remainingTime = 0;
            Debug.Log("Tiempo Acabado");
            //Time.timeScale = 0;
            uiCanvas.SetActive(true);
            onEndGame?.Invoke();
        }
    }
    
}
