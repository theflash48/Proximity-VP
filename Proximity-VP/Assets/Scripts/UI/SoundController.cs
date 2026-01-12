using UnityEngine;
using UnityEngine.UI;

public class VolumeSlider : MonoBehaviour
{
    public Slider volumeSlider;

    void Start()
    {
        // Cargar volumen guardado
        volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
        AudioListener.volume = volumeSlider.value;

        volumeSlider.onValueChanged.AddListener(ChangeVolume);
    }

    void ChangeVolume(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
    }
}