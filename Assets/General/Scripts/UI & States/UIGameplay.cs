using UnityEngine;

public class UIGameplay : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private WeaponUI weaponUI;

    private PlayerInput player;

    public void Init() {}
    public void BindPlayer(PlayerInput playerInput)
    {
        player = playerInput;

        if (player == null || weaponUI == null) return;

        weaponUI.Bind(
            player.GetComponent<WeaponManager>(),
            player.GetHealthSystem()
        );
    }

    public void SetActive(bool active)
    {
        if (hudPanel) hudPanel.SetActive(active);
        if (weaponUI) weaponUI.enabled = active;
    }

    public UIManager.UIType GetUIType()
    {
        return UIManager.UIType.Gameplay;
    }
}