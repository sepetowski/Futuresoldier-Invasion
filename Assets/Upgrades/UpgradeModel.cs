using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class UpgradeModel
{
    public string name;
    public GameObject parentObject; 
    public Button increaseButton; 
    public string labelBaseText = "Current Upgrade:";
    [HideInInspector] public LevelModel[] levels; 
    [HideInInspector] public TMP_Text labelText;
}
