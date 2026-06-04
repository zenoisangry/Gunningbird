using UnityEngine;
using UnityEngine.UI;

public class UICredits : MonoBehaviour, IGameUI
{
    [SerializeField] private Button backButton;

    public void Init()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    public void SetActive(bool active) => gameObject.SetActive(active);

    public UIManager.UIType GetUIType() => UIManager.UIType.Credits;

    private void OnBackClicked() => GameStateManager.instance.SetCurrentGameState(GameStateManager.GameStates.MainMenu);
}
