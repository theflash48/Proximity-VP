using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class Shoot : MonoBehaviour
{
    [Header("Fallback (si no se pasa origin desde el controller)")]
    public GameObject firingPoint;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shootSound;

    private PlayerControllerOnline pcOnline;
    private NetworkObject netObj;
    private Collider[] selfColliders;

    void Awake()
    {
        pcOnline = GetComponent<PlayerControllerOnline>();
        netObj = GetComponentInParent<NetworkObject>();
        selfColliders = GetComponentsInChildren<Collider>(true);
    }

    private bool IsOnlineMode()
    {
        // Señal explícita por prefab (más fiable que IsListening)
        if (pcOnline != null && pcOnline.enabled) return true;
        if (netObj != null) return true;
        return false;
    }

    private bool IsSelfCollider(Collider c)
    {
        if (c == null || selfColliders == null) return false;
        for (int i = 0; i < selfColliders.Length; i++)
        {
            if (selfColliders[i] == c) return true;
        }
        return false;
    }

    // Mantengo esta firma por compatibilidad (por si algo más la llama)
    public bool ShootBullet(GameObject playerCamera)
    {
        Transform aim = playerCamera != null ? playerCamera.transform : null;
        Vector3 origin = firingPoint != null ? firingPoint.transform.position : transform.position;

        // En este overload no podemos devolver el punto final, así que lo ignoramos.
        return ShootBullet(aim, origin, 100f, out _);
    }

    /// <summary>
    /// Local: aplica daño y devuelve true si el objetivo murió. Devuelve endPoint para dibujar el LineRenderer.
    /// Online: solo sonido/feedback; no aplica daño (lo hace el server).
    /// </summary>
    public bool ShootBullet(Transform aimTransform, Vector3 origin, float maxDistance, out Vector3 endPoint)
    {
        endPoint = origin + (aimTransform != null ? aimTransform.forward : transform.forward) * maxDistance;

        // Audio siempre
        if (audioSource != null && shootSound != null)
            audioSource.PlayOneShot(shootSound);

        // Bloquear si estás clicando UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return false;

        if (aimTransform == null)
            return false;

        // Online: este script NO aplica daño. (daño lo hace PlayerControllerOnline en server)
        if (IsOnlineMode())
            return false;

        Vector3 dir = aimTransform.forward;

        // RaycastAll para saltarnos nuestro propio collider (si el origin está cerca/dentro)
        RaycastHit[] hits = Physics.RaycastAll(origin, dir, maxDistance, ~0, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
            return false;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // Elegir el primer impacto que NO sea un collider nuestro
        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.collider == null) continue;

            if (IsSelfCollider(hit.collider))
                continue;

            // Primer impacto real => aquí se para bala y line
            endPoint = hit.point;

            var phLocal = hit.collider.GetComponentInParent<PlayerHealthLocal>();
            if (phLocal != null)
            {
                phLocal.TakeDamage();
                return phLocal.currentLives <= 0;
            }

            // Si es pared/suelo/etc => se para aquí y NO atraviesa
            return false;
        }

        return false;
    }
}
