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

    private WindPush playerMovement;

    private void Awake()
    {
        LoadSettings();

        pauseDisplay.SetActive(false);
        settingsDisplay.SetActive(false);

        playerMovement = FindAnyObjectByType<WindPush>();

        if (returnButton != null)
            returnButton.onClick.AddListener(ResumeGame);

        HideCursorGameplay();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame ||
            Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (GameIsPaused)
                ResumeGame();
            else
                OpenPauseMenu();
        }
    }

    public void OpenPauseMenu()
    {
        GameIsPaused = true;

        pauseDisplay.SetActive(true);
        settingsDisplay.SetActive(false);

        Time.timeScale = 0f;
        ShowCursorMenu(true);
    }

    public void ResumeGame()
    {
        GameIsPaused = false;

        pauseDisplay.SetActive(false);
        settingsDisplay.SetActive(false);

        Time.timeScale = 1f;

        HideCursorGameplay();
    }

    public void FinishRun()
    {
        GameIsPaused = false;
        Time.timeScale = 1f;

        ShowCursorMenu(false);
    }

    void ShowCursorMenu(bool blockPlayer)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (playerMovement != null)
            playerMovement.PauseInput(blockPlayer);
    }

    void HideCursorGameplay()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        if (playerMovement != null)
            playerMovement.PauseInput(false);
    }

    public void OpenSettings()
    {
        settingsDisplay.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsDisplay.SetActive(false);
        SaveSettings();
    }

    public void SetVolume()
    {
        float music = musicSlider.value;
        float volume = volumeSlider.value;
        float sfx = SFXSlider.value;

        masterMixer.SetFloat("BackgroundMusic", Mathf.Log10(Mathf.Clamp(music, 0.001f, 1f)) * 20);
        masterMixer.SetFloat("VolumeMusic", Mathf.Log10(Mathf.Clamp(volume, 0.001f, 1f)) * 20);
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
        float music = PlayerPrefs.GetFloat("BackgroundMusic", 1f);
        float volume = PlayerPrefs.GetFloat("VolumeMusic", 1f);
        float sfx = PlayerPrefs.GetFloat("SoundEffects", 1f);

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
}