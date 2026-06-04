using UnityEngine;
using UnityEngine.UI;

public class UICredits : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject menuBackground;
    [SerializeField] private Button     backButton;

    public void Init()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    public void SetActive(bool active)
    {
        if (creditsPanel   != null) creditsPanel.SetActive(active);
        if (menuBackground != null) menuBackground.SetActive(active);
    }

    public UIManager.UIType GetUIType() => UIManager.UIType.Credits;

    private void OnBackClicked() => GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.MainMenu);
}
