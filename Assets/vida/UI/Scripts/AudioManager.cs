using UnityEngine;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle muteToggle;

    private float lastVolume = 1f;

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("Volume", 1f);
        bool savedMute = PlayerPrefs.GetInt("Muted", 0) == 1;

        lastVolume = savedVolume;
        AudioListener.volume = savedMute ? 0f : savedVolume;

        volumeSlider.value = savedVolume;
        muteToggle.isOn = savedMute;

        volumeSlider.onValueChanged.AddListener(SetVolume);
        muteToggle.onValueChanged.AddListener(SetMute);
    }

    public void SetVolume(float value)
    {
        lastVolume = value;
        if (!muteToggle.isOn)
        {
            AudioListener.volume = value;
        }
        PlayerPrefs.SetFloat("Volume", value);
    }

    public void SetMute(bool isMuted)
    {
        AudioListener.volume = isMuted ? 0f : lastVolume;
        PlayerPrefs.SetInt("Muted", isMuted ? 1 : 0);
    }
}