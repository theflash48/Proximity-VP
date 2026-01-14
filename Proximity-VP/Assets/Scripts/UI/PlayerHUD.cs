using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHUD : MonoBehaviour
{
    [Header("HUD Images")]
    [SerializeField] private Image _cHUD;          // La Image del marco HUD (la de tu captura)
    [SerializeField] private Image _cReloadBar;
    [SerializeField] private Image _cInvisibility; // vignette/icono

    [Header("HUD Colors")]
    [SerializeField] private Color _hudNormalColor = Color.white;
    [SerializeField] private Color _hudDamagedColor = Color.red;

    [Header("Death Screen (HUD)")]
    [SerializeField] private GameObject _cDeathScreen; // GO "DeathScreen" (opcional)

    [Header("HUD Texts")]
    [SerializeField] private Text _cName;   // username
    public Text _cPlace;
    public Text _cScore;

    private Coroutine reloadRoutine;

    void Awake()
    {
        // Fallbacks por si algo no está asignado en inspector
        if (_cHUD == null)
            _cHUD = GetComponent<Image>(); // si el script está colgado en el propio GO HUD

        if (_cReloadBar == null)
        {
            var t = transform.Find("Reload");
            if (t != null) _cReloadBar = t.GetComponent<Image>();
        }

        if (_cInvisibility == null)
        {
            var t = transform.parent != null ? transform.parent.Find("VisibilityVignette") : null;
            if (t != null) _cInvisibility = t.GetComponent<Image>();
        }

        if (_cDeathScreen == null)
        {
            var t = transform.Find("DeathScreen");
            if (t != null) _cDeathScreen = t.gameObject;
        }

        if (_cReloadBar != null)
            _cReloadBar.fillAmount = 0f;

        // Por defecto, invisibility apagada (se actualizará en runtime)
        if (_cInvisibility != null)
            _cInvisibility.enabled = false;

        // ✅ Spawn: HUD blanco
        _fSetHudDamageState(false);

        // ✅ DeathScreen OFF por defecto
        _fToggleDeathScreen(false);
    }

    public void _fUpdateName(string username)
    {
        if (_cName == null) return;
        _cName.text = string.IsNullOrWhiteSpace(username) ? "" : username;
    }

    public void _fUpdateScore(int score)
    {
        if (_cScore == null) return;
        _cScore.text = score.ToString();
    }

    public void _fHealthUI(int currentLives, int maxLives)
    {
        if (_cHUD == null) return;
        if (maxLives <= 0) return;

        float pct = Mathf.Clamp01((float)currentLives / maxLives);
        _cHUD.fillAmount = pct; // si la Image no es Filled, Unity lo ignora
    }

    /// <summary>
    /// Recibe "revealed/visible".
    /// ✅ INVERTIDO: la vignette se ENCIENDE cuando eres invisible (revealed == false).
    /// </summary>
    public void _fToggleInvisibilityUI(bool revealed)
    {
        if (_cInvisibility == null) return;
        _cInvisibility.enabled = !revealed;
    }

    public void _fReloadUI(float loadDelay)
    {
        if (_cReloadBar == null) return;

        if (reloadRoutine != null)
            StopCoroutine(reloadRoutine);

        reloadRoutine = StartCoroutine(_fReloadBar(loadDelay));
    }

    private IEnumerator _fReloadBar(float reDelay)
    {
        if (_cReloadBar == null) yield break;

        if (reDelay <= 0f)
        {
            _cReloadBar.fillAmount = 0f;
            yield break;
        }

        _cReloadBar.fillAmount = 1f;

        float t = 0f;
        while (t < reDelay)
        {
            t += Time.deltaTime;
            _cReloadBar.fillAmount = Mathf.Lerp(1f, 0f, t / reDelay);
            yield return null;
        }

        _cReloadBar.fillAmount = 0f;
    }

    // ✅ Nuevo: blanco/rojo del marco HUD
    public void _fSetHudDamageState(bool damaged)
    {
        if (_cHUD == null) return;
        _cHUD.color = damaged ? _hudDamagedColor : _hudNormalColor;
    }

    // DeathScreen ON/OFF
    public void _fToggleDeathScreen(bool on)
    {
        if (_cDeathScreen == null) return;
        _cDeathScreen.SetActive(on);
    }
}
