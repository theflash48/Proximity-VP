using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public float speed;
    
    // Update is called once per frame
    void FixedUpdate()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime, Space.World);
    }
}
