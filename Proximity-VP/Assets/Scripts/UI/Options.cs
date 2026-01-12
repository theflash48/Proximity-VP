using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSystem : MonoBehaviour
{
    public GameObject panelOpciones;
    public GameObject panelSonidos;
    public GameObject panelGameplay;
    
    public void Options()
    {
        panelOpciones.SetActive(!panelOpciones.activeSelf);
        Debug.Log("Opciones abiertas/cerradas");
    }
    
    public void Sonidos()
    {
        panelSonidos.SetActive(true);
        panelGameplay.SetActive(false);

        Debug.Log("Panel de sonidos activo");
    }

    public void Gameplay()
    {
        panelGameplay.SetActive(true);
        panelSonidos.SetActive(false);

        Debug.Log("Panel de gráficos activo");
    }
}