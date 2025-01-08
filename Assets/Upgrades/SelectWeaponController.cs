using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SelectWeaponController : MonoBehaviour
{

    [SerializeField] private Weapon[] weapons;
    [SerializeField] private Image weaponImageUI;
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private TextMeshProUGUI statsLeftText;
    [SerializeField] private TextMeshProUGUI statsRightText;

    private int currentWeaponIndex = 0;
    void Start()
    {
        currentWeaponIndex = PlayerPrefs.GetInt("SelectedWeaponIndex", 0);
        UpdateUI(); 
    }

    public void NextWeapon()
    {
        currentWeaponIndex = (currentWeaponIndex + 1) % weapons.Length;
        UpdateUI();
        SaveWeaponIndex();
    }

    public void PreviousWeapon()
    {
        currentWeaponIndex = (currentWeaponIndex - 1 + weapons.Length) % weapons.Length;
        UpdateUI();
        SaveWeaponIndex();
    }

    public void UpdateUI()
    {
        Weapon currentWeapon = weapons[currentWeaponIndex];
        var info = currentWeapon.GetWeaponInforamtion();

        weaponImageUI.sprite = info.weaponImage;
        weaponNameText.text = $"Selected weapon: {info.Name}";

        statsLeftText.text = $"Damage: {info.Damage}\nMagazines: {info.Magazines}\nMagazine size: {info.MagazineSize}";
        statsRightText.text = $"Reload time: {info.ReloadTime}s\nRange: {info.Range}m\nFire mode:{info.FireMode}";
    }

    private void SaveWeaponIndex()
    {
        PlayerPrefs.SetInt("SelectedWeaponIndex", currentWeaponIndex); 
        PlayerPrefs.Save(); 
    }
}
