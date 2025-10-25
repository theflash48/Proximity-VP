using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Image _cHUD;
    [SerializeField] private Image _cReloadBar;
    [SerializeField] private Image _cInvisibility;

    float harm = 10;
    [ContextMenu("Try")]
    public void test()
    {
        harm -= 9;
        _fHealthUI(harm, 10);
    }

    public void _fHealthUI(float cur, float max)
    {
        float damage = cur / max;
        Debug.Log(damage);
        _cHUD.color = new Color (255, 1 - damage, 1 - damage, 255);
    }
}
