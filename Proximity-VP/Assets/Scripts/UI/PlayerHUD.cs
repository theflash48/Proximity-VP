using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHUD : MonoBehaviour
{
    [Header("HUD Images")]
    [SerializeField] private Image _cHUD;
    [SerializeField] private Image _cReloadBar;
    [SerializeField] private Image _cInvisibility;

    [Header("HUD Texts")]
    [SerializeField] private Text _cName;   // NUEVO: username
    public Text _cPlace;
    public Text _cScore;

    private Coroutine reloadRoutine;

    void Start()
    {
        if (_cReloadBar != null)
            _cReloadBar.fillAmount = 0;

        if (_cInvisibility != null)
            _cInvisibility.enabled = false;
    }

    // NUEVO: nombre del jugador (username)
    public void _fUpdateName(string username)
    {
        if (_cName == null) return;
        _cName.text = string.IsNullOrWhiteSpace(username) ? "" : username;
    }

    // Ya lo llamáis desde controllers
    public void _fUpdateScore(int score)
    {
        if (_cScore == null) return;
        _cScore.text = score.ToString();
    }

    // Vidas (si tu HUD no usa fillAmount, no pasa nada)
    public void _fHealthUI(int currentLives, int maxLives)
    {
        if (_cHUD == null) return;
        if (maxLives <= 0) return;

        float pct = Mathf.Clamp01((float)currentLives / maxLives);

        // Si tu imagen no es Filled, Unity lo ignorará y no rompe nada
        _cHUD.fillAmount = pct;
    }

    // Indicador de visibilidad (no el renderer real)
    public void _fToggleInvisibilityUI(bool revealed)
    {
        if (_cInvisibility == null) return;
        _cInvisibility.enabled = revealed;
    }

    // Barra de recarga
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
}
