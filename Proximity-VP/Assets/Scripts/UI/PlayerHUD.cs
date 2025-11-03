using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Image _cHUD;
    [SerializeField] private Image _cReloadBar;
    [SerializeField] private Image _cInvisibility;
    public Text _cPlace;
    public Text _cScore;


    void Start()
    {
        _cReloadBar.fillAmount = 0;
    }

    public void _fUpdateScore(int score)
    {
        _cScore.text = score.ToString();
    }

    public void _fHealthUI(float cur, float max)
    {
        float damage = cur / max;
        _cHUD.color = new Color (1, damage, damage, 1);
        _cReloadBar.color = new Color (1, damage, damage, 1);
    }

    public void _fToggleInvisibilityUI(bool isVisible)
    {
        if (isVisible)
            _cInvisibility.enabled = false;
        else
            _cInvisibility.enabled = true;
    }

    private float _vReDelay;
    public void _fReloadUI(float loadDelay)
    {
        _vReDelay = loadDelay;
        StartCoroutine("_fReloadBar");
    }

    private IEnumerator _fReloadBar()
    {
        _cReloadBar.fillAmount = 1;
        for (float i = 0; i < _vReDelay; i += 0.02f)
        {
            _cReloadBar.fillAmount -= 0.02f / _vReDelay;
            yield return new WaitForSeconds(0.02f);
        }
        _cReloadBar.fillAmount = 0;
    }
}
