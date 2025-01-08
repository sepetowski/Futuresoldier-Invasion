using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawnerController : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs; 
    [SerializeField] private GameObject player; 
    [SerializeField] private float spawnRange = 20f; 
    [SerializeField] private float minSpawnDistance = 5f; 
    [SerializeField] private int maxEnemies = 10; 
    [SerializeField] private float minSpawnInterval = 2f; 
    [SerializeField] private float maxSpawnInterval = 5f; 
    [SerializeField] private int maxGroupSize = 3; 
    [SerializeField] private float spawnStartDelay = 10f; 

    public float enemyDamge = 20f;
    public float enemyMaxHealth = 100f;

    private Transform enemies;

    private int currentEnemyCount = 0;
    private int groundLayer;

    void Start()
    {
        groundLayer = LayerMask.GetMask("Ground");
        enemies = new GameObject("Enemies").transform;
        enemies.SetParent(transform);
    }

    public IEnumerator StartSpawningAfterDelay()
    {
        yield return new WaitForSeconds(spawnStartDelay);
        StartCoroutine(SpawnEnemies());
    }

    IEnumerator SpawnEnemies()
    {
        while (true) 
        {
            if (currentEnemyCount < maxEnemies)
            {
                float spawnDelay = Random.Range(minSpawnInterval, maxSpawnInterval);
                yield return new WaitForSeconds(spawnDelay);

                SpawnEnemyNearPlayer(); 
            }
            else
            {
                yield return new WaitForSeconds(5f);
            }
        }
    }


    void SpawnEnemyNearPlayer()
    {
        int groupSize = Random.Range(1, maxGroupSize + 1);

        Vector3 groupSpawnPosition;
        bool validPosition = false;

        do
        {
            float randomX = Random.Range(-spawnRange, spawnRange);
            float randomZ = Random.Range(-spawnRange, spawnRange);
            groupSpawnPosition = new Vector3(player.transform.position.x + randomX, 10f, player.transform.position.z + randomZ);

            if (Physics.Raycast(groupSpawnPosition, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayer))
            {
                if (Vector3.Distance(hit.point, player.transform.position) >= minSpawnDistance)
                {
                    groupSpawnPosition = hit.point;
                    validPosition = true;
                }
            }
        } while (!validPosition);

        for (int i = 0; i < groupSize; i++)
        {
            float offsetX = Random.Range(-2f, 2f);
            float offsetZ = Random.Range(-2f, 2f);
            Vector3 spawnPosition = new Vector3(groupSpawnPosition.x + offsetX, groupSpawnPosition.y, groupSpawnPosition.z + offsetZ);

            GameObject enemyPrefab = GetRandomEnemyPrefab();
            GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemies);
            currentEnemyCount++;

            EnemyController enemyController = spawnedEnemy.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                enemyController.enemyDamge = enemyDamge;
                enemyController.enemyMaxHealth = enemyMaxHealth;
            }

            if (currentEnemyCount >= maxEnemies)
            {
                break;
            }
        }
    }

    public void SpawnInitialEnemies(int numberOfEnemies)
    {
        float adjustedMinDistance = minSpawnDistance * 2;

        int enemiesToSpawn = numberOfEnemies;

        while (enemiesToSpawn > 0)
        {
            int groupSize = Random.Range(1, maxGroupSize + 1);
            groupSize = Mathf.Min(groupSize, enemiesToSpawn);

            Vector3 groupSpawnPosition;
            bool validPosition = false;

            do
            {
                float randomX = Random.Range(-spawnRange * 2, spawnRange * 2);
                float randomZ = Random.Range(-spawnRange * 2, spawnRange * 2);
                groupSpawnPosition = new Vector3(player.transform.position.x + randomX, 10f, player.transform.position.z + randomZ);

                if (Physics.Raycast(groupSpawnPosition, Vector3.down, out RaycastHit hit, Mathf.Infinity, groundLayer))
                {
                    if (Vector3.Distance(hit.point, player.transform.position) >= adjustedMinDistance)
                    {
                        groupSpawnPosition = hit.point;
                        validPosition = true;
                    }
                }
            } while (!validPosition);

            for (int i = 0; i < groupSize; i++)
            {
                float offsetX = Random.Range(-2f, 2f);
                float offsetZ = Random.Range(-2f, 2f);
                Vector3 spawnPosition = new Vector3(groupSpawnPosition.x + offsetX, groupSpawnPosition.y, groupSpawnPosition.z + offsetZ);

                GameObject enemyPrefab = GetRandomEnemyPrefab();
                GameObject spawnedEnemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, enemies);
                currentEnemyCount++;

                EnemyController enemyController = spawnedEnemy.GetComponent<EnemyController>();
                if (enemyController != null)
                {
                    enemyController.enemyDamge = enemyDamge;
                    enemyController.enemyMaxHealth = enemyMaxHealth;
                }
            }

            enemiesToSpawn -= groupSize;
        }
    }


    public void ClearEnemies()
    {
        foreach (Transform enemyTransform in enemies)
        {
            Destroy(enemyTransform.gameObject); 
        }


        currentEnemyCount = 0; 
    }

    GameObject GetRandomEnemyPrefab()
    {
        int index = Random.Range(0, enemyPrefabs.Length);
        return enemyPrefabs[index];
    }
}
