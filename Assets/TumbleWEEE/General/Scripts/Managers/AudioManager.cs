using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public Slider musicSlider;
    public AudioSource audioSource;

    void Start()
    {
        musicSlider.value = PlayerPrefs.GetFloat("BackgroundMusic", 1f);
    }

    public void ChangeVolume()
    {
        AudioListener.volume = musicSlider.value;
        PlayerPrefs.SetFloat("BackgroundMusic", musicSlider.value);
    }
}