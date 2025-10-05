using System;
using Unity.VisualScripting;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public float speed;
    
    [HideInInspector]
    public GameObject owner; // El jugador que disparo esta bala
    
    Rigidbody rb;
    
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, 3);
    }
    
    void FixedUpdate()
    {
        rb.MovePosition(rb.position + transform.forward * speed * Time.fixedDeltaTime);
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
            Destroy(gameObject);
        }
        else
        {
            // Si choca con otro jugador, tambien se destruye
            Destroy(gameObject);
        }
    }
}