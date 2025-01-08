using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    public enum MissionType { KillEnemies, FindItem, Survive }
    public enum DifficultyLevel { Easy, Normal, Hard, VeryHard, Extreme, Insane }

    private Dictionary<DifficultyLevel, int> difficultyMap = new Dictionary<DifficultyLevel, int>
    {
        { DifficultyLevel.Easy, 1 },
        { DifficultyLevel.Normal, 2 },
        { DifficultyLevel.Hard, 3 },
        { DifficultyLevel.VeryHard, 4 },
        { DifficultyLevel.Extreme, 5 },
        { DifficultyLevel.Insane, 6 }
    };

    private MissionType currentMission;
    private DifficultyLevel currentDifficulty;

    [SerializeField] private GameObject coin;
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TMP_Text xpInfo;
    [SerializeField] private CharacterStateController playerController;
    [SerializeField] private MissionInfo missionInfo;
    [SerializeField] private const float TimeWeight = 2.5f;
    [SerializeField] private const float KillsWeight = 5f;

    private Health playerHealth;
    private PerlinTilemapGenerator tilemapGenerator;
    private EnemySpawnerController enemySpawner;

    private bool coinPlaced = false;
    private bool enemiesSpawned = false;
    private int enemiesKilled = 0;
    private int enemiesToKill;

    private float timeToSurvive;
    private float survivalTimeRemaining;
    private float totalPlayTime = 0f;
    private bool gainLevel = false;

    private void OnEnable()
    {
        Cursor.visible = false;
        deathPanel.SetActive(false);
        tilemapGenerator = FindObjectOfType<PerlinTilemapGenerator>();
        enemySpawner = FindObjectOfType<EnemySpawnerController>();

        CoinController.OnCoinCollected += HandleCoinCollected;
        EnemyController.OnEnemyDeath += IncrementEnemyKillCount;
    }

    void Start()
    {
        string savedDifficulty = PlayerPrefs.GetString("SelectedDifficulty", "Normal");
        currentDifficulty = (DifficultyLevel)System.Enum.Parse(typeof(DifficultyLevel), savedDifficulty);

        tilemapGenerator.InitMap();
        playerHealth = playerController.GetComponent<Health>();
        playerHealth.onDeath.AddListener(() => StartCoroutine(OnDeath()));

        UpdateDifficulty(currentDifficulty);
        SelectRandomMission();
    }

    private void LateUpdate()
    {
        if (tilemapGenerator.PlayerPlaced() && !enemiesSpawned)
        {
            enemySpawner.SpawnInitialEnemies(20);
            StartCoroutine(enemySpawner.StartSpawningAfterDelay());
            enemiesSpawned = true;

            if (currentMission == MissionType.FindItem && !coinPlaced)
                PlaceMissionCoin();
        }

        if (currentMission == MissionType.Survive && survivalTimeRemaining > 0 && playerHealth.CurrentHealth > 0)
        {
            survivalTimeRemaining -= Time.deltaTime;
            if (survivalTimeRemaining < 0) survivalTimeRemaining = 0;
            missionInfo.UpdateSurvivalTime(survivalTimeRemaining);

            if (survivalTimeRemaining <= 0)
            {
                survivalTimeRemaining = 0;
                OnWin();
            }
        }

        if (playerHealth.CurrentHealth > 0)
            totalPlayTime += Time.deltaTime;
    }

    private void OnDisable()
    {
        CoinController.OnCoinCollected -= HandleCoinCollected;
        EnemyController.OnEnemyDeath -= IncrementEnemyKillCount;
        CleanupGameState();
    }

    private void OnDestroy()
    {
        CleanupGameState();
    }

    private void CleanupGameState()
    {
        if (enemySpawner != null)
        {
            enemySpawner.StopAllCoroutines();
            enemySpawner.ClearEnemies();
        }

        coinPlaced = false;
        enemiesSpawned = false;
        enemiesKilled = 0;
        survivalTimeRemaining = 0f;
        totalPlayTime = 0f;

        if (deathPanel != null) deathPanel.SetActive(false);
        if (winPanel != null) winPanel.SetActive(false);

        Time.timeScale = 1;
    }

    private void IncrementEnemyKillCount()
    {
        enemiesKilled++;
        if (currentMission == MissionType.KillEnemies)
        {
            missionInfo.UpdateEnemiesKilled(enemiesKilled, enemiesToKill);

            if (enemiesKilled == enemiesToKill)
                OnWin();
        }
    }

    private void HandleCoinCollected()
    {
        OnWin();
    }

    private void SelectRandomMission()
    {
        currentMission = (MissionType)Random.Range(0, System.Enum.GetValues(typeof(MissionType)).Length);

        switch (currentMission)
        {
            case MissionType.KillEnemies:
                missionInfo.SetKillEnemiesMission(enemiesToKill);
                break;

            case MissionType.Survive:
                missionInfo.SetSurviveMission(timeToSurvive);
                survivalTimeRemaining = timeToSurvive;
                break;

            case MissionType.FindItem:
                missionInfo.SetFindItemMission("gold coin");
                break;
        }
    }

    private void PlaceMissionCoin()
    {
        Vector3 coinPosition = tilemapGenerator.FindValidPositionAwayFromPlayer(100f);
        coinPosition.y *= 2f;

        Instantiate(coin, coinPosition, Quaternion.identity);
        coinPlaced = true;
    }

    public void OnTryAgain()
    {
        Cursor.visible = false;
        var initialPlayerPosition = tilemapGenerator.InitialPlayerPosition;
        playerController.ResetState(initialPlayerPosition);

        tilemapGenerator.ResetFogWarState();
        enemySpawner.StopAllCoroutines();
        enemySpawner.ClearEnemies();
        tilemapGenerator.ClearItems();
        tilemapGenerator.GenerateItems();

        deathPanel.SetActive(false);
        winPanel.SetActive(false);
        enemiesSpawned = false;
        coinPlaced = false;
        enemiesKilled = 0;
        survivalTimeRemaining = timeToSurvive;
        totalPlayTime = 0f;

        switch (currentMission)
        {
            case MissionType.KillEnemies:
                missionInfo.SetKillEnemiesMission(enemiesToKill);
                break;

            case MissionType.Survive:
                missionInfo.SetSurviveMission(timeToSurvive);
                break;

            case MissionType.FindItem:
                missionInfo.SetFindItemMission("gold coin");
                coinPlaced = false;
                PlaceMissionCoin();
                break;
        }
    }

    private void UpdateDifficulty(DifficultyLevel difficulty)
    {
        switch (difficulty)
        {
            case DifficultyLevel.Easy:
                enemySpawner.enemyDamge = 10f;
                enemySpawner.enemyMaxHealth = 30f;
                enemiesToKill = 10;
                timeToSurvive = 120f;
                break;

            case DifficultyLevel.Normal:
                enemySpawner.enemyDamge = 15f;
                enemySpawner.enemyMaxHealth = 50f;
                enemiesToKill = 20;
                timeToSurvive = 240f;
                break;

            case DifficultyLevel.Hard:
                enemySpawner.enemyDamge = 20f;
                enemySpawner.enemyMaxHealth = 70f;
                enemiesToKill = 25;
                timeToSurvive = 360f;
                break;

            case DifficultyLevel.VeryHard:
                enemySpawner.enemyDamge = 30f;
                enemySpawner.enemyMaxHealth = 80f;
                enemiesToKill = 30;
                timeToSurvive = 480f;
                break;

            case DifficultyLevel.Extreme:
                enemySpawner.enemyDamge = 35f;
                enemySpawner.enemyMaxHealth = 90f;
                enemiesToKill = 40;
                timeToSurvive = 600f;
                break;

            case DifficultyLevel.Insane:
                enemySpawner.enemyDamge = 40f;
                enemySpawner.enemyMaxHealth = 120f;
                enemiesToKill = 50;
                timeToSurvive = 720f;
                break;
        }
        timeToSurvive++;
    }

    private void CalculateAndShowXPData()
    {
        int initialLevel = GameDataController.Instance.GetCurrentPlayerLevel();
        int difficultyValue = difficultyMap[currentDifficulty];
        int totalXP = Mathf.RoundToInt(((totalPlayTime * TimeWeight) + (enemiesKilled * KillsWeight)) * difficultyValue);
        GameDataController.Instance.AddXP(totalXP);

        int newLevel = GameDataController.Instance.GetCurrentPlayerLevel();
        int levelsGained = newLevel - initialLevel;

        if(initialLevel >= 31)
        {
            xpInfo.text = $"You have already reach maximum level";
            return;
        }

        if (levelsGained > 0)
        {
            gainLevel = true;
            xpInfo.text = $"You have gained {totalXP} XP and leveled up to level {newLevel}!\n" +
                          $"You have earned {levelsGained} skill points to spend.";
        }
        else
            xpInfo.text = $"You have gained {totalXP} XP.";

        GameDataController.Instance.SaveData();
    }

    public void ContinueAfterPanelShown()
    {
        if (gainLevel)
            SceneManager.LoadScene("Upgrades");
        else
            SceneManager.LoadScene("MainMenu");
    }

    private void OnWin()
    {
        playerController.disablePlayer();

        CalculateAndShowXPData();
        winPanel.SetActive(true);
        Time.timeScale = 0;
        Cursor.visible = true;
    }

    private IEnumerator OnDeath()
    {
        yield return new WaitForSeconds(2f);
        deathPanel.SetActive(true);
        Cursor.visible = true;
    }
}
