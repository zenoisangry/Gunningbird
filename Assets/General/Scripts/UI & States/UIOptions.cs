using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIOptions : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private Button backButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Settings")]
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Text volumeValueText;
    [SerializeField] private TMP_Text sensitivityValueText;

    private bool isInGameplay = false;

    public void Init()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            volumeSlider.value = PlayerPrefs.GetFloat("Volume", 1f);
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", 1f);
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            fullscreenToggle.isOn = Screen.fullScreen;
        }
    }

    public void SetActive(bool active)
    {
        if (optionsPanel != null)
            optionsPanel.SetActive(active);

        if (active)
        {
            isInGameplay = GameManager.Instance.isGameActive;
            UpdateDisplayedValues();
        }
    }

    public UIManager.UIType GetUIType()
    {
        return UIManager.UIType.Options;
    }

    private void UpdateDisplayedValues()
    {
        if (volumeValueText != null && volumeSlider != null)
            volumeValueText.text = $"{Mathf.RoundToInt(volumeSlider.value * 100)}%";

        if (sensitivityValueText != null && sensitivitySlider != null)
            sensitivityValueText.text = $"{sensitivitySlider.value:F2}";
    }

    private void OnBackClicked()
    {
        if (isInGameplay)
        {
            OnResumeClicked();
        }
        else
        {
            GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.MainMenu);
        }
    }

    private void OnResumeClicked()
    {
        GameManager.Instance.ResumeGame();
    }

    private void OnMainMenuClicked()
    {
        GameManager.Instance.ReturnToMainMenu();
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        PlayerPrefs.SetFloat("Volume", value);
        if (volumeValueText != null)
            volumeValueText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }

    private void OnSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat("Sensitivity", value);
        if (sensitivityValueText != null)
            sensitivityValueText.text = $"{value:F2}";
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}