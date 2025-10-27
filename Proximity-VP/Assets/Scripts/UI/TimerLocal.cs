using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class TimerLocal : MonoBehaviour

{

    [SerializeField] Text timerText;
    [SerializeField]float remainingTime;

    bool counting = true;
    // Update is called once per frame
    void Update()
    {
        if (counting)
        {
            remainingTime -=Time.deltaTime;
        }
        if (remainingTime <= 0)
        {
            counting = false;
            remainingTime = 0;
        }
        
        int minutes = Mathf.FloorToInt(remainingTime / 60);
        int seconds = Mathf.FloorToInt(remainingTime % 60);
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
