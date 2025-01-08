using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoliderInfo : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI magText;
    [SerializeField] private TextMeshProUGUI weaponModeText;
    [SerializeField] private CanvasGroup rifleUI;
    [SerializeField] private CanvasGroup sniperUI;
    [SerializeField] private CanvasGroup shotgunUI;



    public void ShowWeaponUI(string weaponType)
    {
        HideAllGroups();

        switch (weaponType)
        {
            case "Rifle":
                ShowGroup(rifleUI);
                break;
            case "Sniper":
                ShowGroup(sniperUI);
                break;
            case "Shotgun":
                ShowGroup(shotgunUI);
                break;
            default:
                Debug.LogWarning("Unknow waepon type: " + weaponType);
                break;
        }
    }

    private void HideAllGroups()
    {
        foreach (CanvasGroup group in GetComponentsInChildren<CanvasGroup>())
        {
            group.alpha = 0;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    private void ShowGroup(CanvasGroup group)
    {
        group.alpha = 1;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    public void SetMaxAmmo(int maxAmmo)
    {
        slider.maxValue = maxAmmo;
        slider.value = maxAmmo;
    }

    public void UpdateFireModeUI(bool isSingleShotMode)
    {

        weaponModeText.text = isSingleShotMode ? "Single Fire" : "Automatic";
    }

    public void SetMagAmmount(int mags)
    {
        magText.text = $"x{mags}";
    }

    public void SetAmmo(int ammo)
    {
        slider.value = ammo;
    }


}
