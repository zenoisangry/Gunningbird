using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Audio;

public class PauseMenu : MonoBehaviour
{
    public static bool GameIsPaused = false;

    [Header("UI References")]
    public GameObject pauseDisplay;
    public GameObject settingsDisplay;
    public Slider musicSlider;
    public Slider SFXSlider;
    public Slider volumeSlider;
    public Button returnButton;

    [Header("Audio")]
    public AudioMixer masterMixer;

    private bool isPauseMenuActive = false;
    private bool isSettingsActive = false;

    private void Awake()
    {
        LoadSettings();

        pauseDisplay.SetActive(false);
        settingsDisplay.SetActive(false);

        if (returnButton != null)
            returnButton.onClick.AddListener(ClosePauseMenu);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!isPauseMenuActive)
                OpenPauseMenu();
            else
                ClosePauseMenu();
        }
    }

    // ---------------------------------------------------------
    // MENU PAUSA
    // ---------------------------------------------------------

    public void OpenPauseMenu()
    {
        isPauseMenuActive = true;
        GameIsPaused = true;

        pauseDisplay.SetActive(true);
        settingsDisplay.SetActive(false);

        Time.timeScale = 0f;
    }

    public void ClosePauseMenu()
    {
        isPauseMenuActive = false;
        GameIsPaused = false;

        pauseDisplay.SetActive(false);
        settingsDisplay.SetActive(false);

        Time.timeScale = 1f;
    }

    public void OpenSettings()
    {
        isSettingsActive = true;
        settingsDisplay.SetActive(true);
    }

    public void CloseSettings()
    {
        isSettingsActive = false;
        settingsDisplay.SetActive(false);
        SaveSettings();
    }

    // ---------------------------------------------------------
    // IMPOSTAZIONI AUDIO
    // ---------------------------------------------------------

    public void SetVolume()
    {
        float music = musicSlider.value;
        masterMixer.SetFloat("BackgroundMusic", Mathf.Log10(Mathf.Clamp(music, 0.001f, 1f)) * 20);

        float volume = volumeSlider.value;
        masterMixer.SetFloat("VolumeMusic", Mathf.Log10(Mathf.Clamp(volume, 0.001f, 1f)) * 20);

        float sfx = SFXSlider.value;
        masterMixer.SetFloat("SoundEffects", Mathf.Log10(Mathf.Clamp(sfx, 0.001f, 1f)) * 20);
    }

    // ---------------------------------------------------------
    // SALVATAGGIO / CARICAMENTO
    // ---------------------------------------------------------

    private void SaveSettings()
    {
        PlayerPrefs.SetFloat("BackgroundMusic", musicSlider.value);
        PlayerPrefs.SetFloat("VolumeMusic", volumeSlider.value);
        PlayerPrefs.SetFloat("SoundEffects", SFXSlider.value);
        PlayerPrefs.Save();
    }

    private void LoadSettings()
    {
        float music = PlayerPrefs.GetFloat("BackgroundMusic", 1.0f);
        float volume = PlayerPrefs.GetFloat("VolumeMusic", 1.0f);
        float sfx = PlayerPrefs.GetFloat("SoundEffects", 1.0f);

        if (masterMixer != null)
        {
            masterMixer.SetFloat("BackgroundMusic", Mathf.Log10(Mathf.Clamp(music, 0.001f, 1f)) * 20);
            masterMixer.SetFloat("VolumeMusic", Mathf.Log10(Mathf.Clamp(volume, 0.001f, 1f)) * 20);
            masterMixer.SetFloat("SoundEffects", Mathf.Log10(Mathf.Clamp(sfx, 0.001f, 1f)) * 20);
        }

        musicSlider.value = music;
        volumeSlider.value = volume;
        SFXSlider.value = sfx;
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}