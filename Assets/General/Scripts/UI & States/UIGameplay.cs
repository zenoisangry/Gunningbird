using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIGameplay : MonoBehaviour, IGameUI
{
    [Header("UI Elements")]
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private WeaponUI weaponUI;

    public void Init() { }

    public void SetActive(bool active)
    {
        if (hudPanel != null)
            hudPanel.SetActive(active);

        if (weaponUI != null)
            weaponUI.enabled = active;
    }

    public UIManager.UIType GetUIType()
    {
        return UIManager.UIType.Gameplay;
    }
}