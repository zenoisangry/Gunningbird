using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIOptions : MonoBehaviour, IGameUI
{
    [Header("Settings")]
    [SerializeField] private Slider   volumeSlider;
    [SerializeField] private Slider   sensitivitySlider;
    [SerializeField] private Toggle   fullscreenToggle;
    [SerializeField] private TMP_Text volumeValueText;
    [SerializeField] private TMP_Text sensitivityValueText;
    [SerializeField] private Button   backButton;

    public void Init()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

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
        gameObject.SetActive(active);
        if (active) UpdateDisplayedValues();
    }

    public UIManager.UIType GetUIType() => UIManager.UIType.Options;

    private void OnBackClicked()
    {
        if (GameManager.Instance.isGameActive)
            GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.Pause);
        else
            GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.MainMenu);
    }

    private void UpdateDisplayedValues()
    {
        if (volumeValueText != null && volumeSlider != null)
            volumeValueText.text = $"{Mathf.RoundToInt(volumeSlider.value * 100)}%";
        if (sensitivityValueText != null && sensitivitySlider != null)
            sensitivityValueText.text = $"{sensitivitySlider.value:F2}";
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

    private void OnFullscreenChanged(bool isFullscreen) => Screen.fullScreen = isFullscreen;
}
