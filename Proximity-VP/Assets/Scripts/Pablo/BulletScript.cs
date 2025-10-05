using System;
using Unity.VisualScripting;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public float currentSpeed = 0;
    public float maxSpeed = 0 ;
    public GameObject spawnInicial;
    
    [HideInInspector]
    public GameObject owner; // El jugador que disparo esta bala
    
    Rigidbody rb;
    
    private void Start()
    {
        spawnInicial = GameObject.Find("SpawnInicial");
        rb = GetComponent<Rigidbody>();
    }
    
    void FixedUpdate()
    {
        rb.MovePosition(rb.position + transform.forward * currentSpeed * Time.fixedDeltaTime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // No destruir si choca con el owner de la bala
        if (other.gameObject == owner)
        {
            return;
        }
        
        // Destruir bala al chocar con cualquier cosa que no sea el player
        if (!other.gameObject.CompareTag("Player"))
        {
            transform.position = spawnInicial.transform.position;
        }
        else
        {
            // Si choca con otro jugador, tambien se destruye
            transform.position = spawnInicial.transform.position;
        }
    }
}