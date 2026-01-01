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

    private Camera cam;

    void Start()
    {
        _cReloadBar.fillAmount = 0;
    }

    public void _fUpdateScore(int score)
    {
        _cScore.text = score.ToString();
    }

    public void _fHealthUI(int cur, int max)
    {
        if (cur <= max / 2)
            _cHUD.color = new Color32(255, 0, 0, 255);
        else
            _cHUD.color = new Color32(255, 255, 255, 255);
    }

    public void _fToggleInvisibilityUI(bool isVisible)
    {
        if (isVisible)
            _cInvisibility.enabled = false;
        else
            _cInvisibility.enabled = true;
    }

    public void _fReloadUI(float loadDelay)
    {
        Debug.Log("HUD delay: " + loadDelay);
        StartCoroutine(_fReloadBar(loadDelay));
    }

    private IEnumerator _fReloadBar(float reDelay)
    {
        _cReloadBar.fillAmount = 1;
        for (float i = 0; i < reDelay; i += 0.02f)
        {
            _cReloadBar.fillAmount -= 0.02f / reDelay;
            yield return new WaitForSeconds(0.02f);
        }
        _cReloadBar.fillAmount = 0;
    }
}
