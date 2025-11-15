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

    private WindPush playerMovement;

    private void Awake()
    {
        LoadSettings();

        pauseDisplay.SetActive(false);
        settingsDisplay.SetActive(false);

        playerMovement = Object.FindAnyObjectByType<WindPush>();

        if (returnButton != null)
            returnButton.onClick.AddListener(TogglePauseMenu);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame)
        {
            TogglePauseMenu();
        }
    }

    public void TogglePauseMenu()
    {
        if (isPauseMenuActive)
            ResumeGame();
        else
            OpenPauseMenu();
    }

    public void OpenPauseMenu()
    {
        isPauseMenuActive = true;
        GameIsPaused = true;

        pauseDisplay.SetActive(true);
        settingsDisplay.SetActive(false);

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (playerMovement != null)
            playerMovement.PauseInput(true);
    }

    public void ResumeGame()
    {
        isPauseMenuActive = false;
        GameIsPaused = false;

        pauseDisplay.SetActive(false);
        settingsDisplay.SetActive(false);

        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (playerMovement != null)
            playerMovement.PauseInput(false);
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

    public void SetVolume()
    {
        float music = musicSlider.value;
        masterMixer.SetFloat("BackgroundMusic", Mathf.Log10(Mathf.Clamp(music, 0.001f, 1f)) * 20);

        float volume = volumeSlider.value;
        masterMixer.SetFloat("VolumeMusic", Mathf.Log10(Mathf.Clamp(volume, 0.001f, 1f)) * 20);

        float sfx = SFXSlider.value;
        masterMixer.SetFloat("SoundEffects", Mathf.Log10(Mathf.Clamp(sfx, 0.001f, 1f)) * 20);
    }

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