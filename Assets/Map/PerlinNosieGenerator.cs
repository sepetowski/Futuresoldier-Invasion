using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using FischlWorks_FogWar;
using System.Reflection;
using Unity.VisualScripting.Antlr3.Runtime.Tree;

public class PerlinTilemapGenerator : MonoBehaviour
{
    [Header("Tile Variants")]
    [SerializeField] private GameObject[] tileBotVariants;
    [SerializeField] private GameObject[] tileTopVariants;
    [SerializeField] private GameObject[] tree1Variants;
    [SerializeField] private GameObject[] tree2Variants;

    [Header("Prefabs")]
    [SerializeField] private GameObject healthPackPrefab;
    [SerializeField] private GameObject ammoPackPrefab;

    [Header("Map Settings")]
    [SerializeField] private int width = 64;
    [SerializeField] private int height = 64;
    [SerializeField] private float scale = 10f;
    [SerializeField] private int maxHeight = 7;
    [SerializeField] private float hillSmoothing = 3f;
    [SerializeField] private float topTileOffsetY = -2f;

    [Header("Objects Settings")]
    [SerializeField] private int maxTrees = 200;
    [SerializeField] private int maxHealthPacks = 10;
    [SerializeField] private int maxAmmoPacks = 5;

    [Header("FogWar")]
    [SerializeField] private GameObject fogWarObject;

    public Vector3 InitialPlayerPosition { get; private set; }

    private Transform map;
    private GameObject tileBot;
    private GameObject tileTop;
    private GameObject[] treeObjects;
    private NavMeshSurface navMeshSurface;

    private float offsetX;
    private float offsetY;
    private int[,] heightMap;

    private int currentHealthPackCount = 0;
    private int currentAmmoPackCount = 0;
    private int currentTreeCount = 0;

    private bool isPlayerPlaced = false;

    public bool PlayerPlaced() => isPlayerPlaced;

    private void LateUpdate()
    {
        if (!isPlayerPlaced)
        {
            ActivateFogWar();
            PlacePlayerOnMap();
        }
    }

    public void InitMap()
    {
        InitializeSettings();
        GenerateTilemap();
        GenerateTrees();
        GenerateItems();
        BakeNavMesh();
        GenerateBorderWalls();
    }

    private void InitializeSettings()
    {
        offsetX = Random.Range(0, 99999f);
        offsetY = Random.Range(0, 99999f);

        int variantIndex = Random.Range(0, tileBotVariants.Length);
        tileBot = tileBotVariants[variantIndex];
        tileTop = tileTopVariants[variantIndex];
        treeObjects = new GameObject[] { tree1Variants[variantIndex], tree2Variants[variantIndex] };

        map = transform.Find("Map");
        if (map == null)
        {
            map = new GameObject("Map").transform;
            map.parent = transform;
        }
    }

    private void GenerateTilemap()
    {
        Vector3 tileSize = tileBot.GetComponent<Renderer>().bounds.size;
        heightMap = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GenerateTile(x, y, tileSize);
            }
        }
    }

    private void GenerateTile(int x, int y, Vector3 tileSize)
    {
        float xCoord = (float)x / width * scale + offsetX;
        float yCoord = (float)y / height * scale + offsetY;

        float noiseValue = Mathf.PerlinNoise(xCoord, yCoord);
        noiseValue = Mathf.Lerp(0.5f, noiseValue, hillSmoothing);

        int heightLevel = Mathf.FloorToInt(noiseValue * maxHeight / 2);
        heightMap[x, y] = heightLevel;

        Vector3 basePosition = new Vector3(x * tileSize.x, 0, y * tileSize.z);
        GameObject baseTile = Instantiate(tileBot, basePosition, Quaternion.identity, map);

        if (heightLevel == 0)
        {
            baseTile.layer = LayerMask.NameToLayer("Ground");
        }

        for (int i = 1; i <= heightLevel; i++)
        {
            GenerateHeightTile(x, y, i, heightLevel, tileSize);
        }
    }

    private void GenerateHeightTile(int x, int y, int level, int heightLevel, Vector3 tileSize)
    {
        Vector3 position = new Vector3(x * tileSize.x, level * tileSize.y, y * tileSize.z);
        GameObject tile;

        if (level == heightLevel)
        {
            position.y += topTileOffsetY;
            tile = Instantiate(tileTop, position, Quaternion.identity, map);
        }
        else
        {
            tile = Instantiate(tileBot, position, Quaternion.identity, map);
        }

        if (heightLevel > 1)
        {
            tile.layer = LayerMask.NameToLayer("HighGround");
        }
        else if (heightLevel <= 1)
        {
            tile.layer = LayerMask.NameToLayer("Ground");
        }
    }

    private void GenerateTrees()
    {
        Vector3 tileSize = tileBot.GetComponent<Renderer>().bounds.size;

        while (currentTreeCount < maxTrees)
        {
            Vector3? validPosition = FindValidPosition(tileSize, minHeight: 1);
            if (validPosition.HasValue)
            {
                GameObject treePrefab = GetRandomTree();
                GameObject tree = Instantiate(treePrefab, validPosition.Value, Quaternion.identity, map);

                tree.layer = LayerMask.NameToLayer("Obstacle");
                currentTreeCount++;
            }
        }
    }

    public void ClearItems()
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (var obj in allObjects)
        {
            if (obj.layer == LayerMask.NameToLayer("HealthPack") || obj.layer == LayerMask.NameToLayer("AmmoPack"))
            {
                Destroy(obj);
            }
        }

        currentHealthPackCount = 0;
        currentAmmoPackCount = 0;
        currentHealthPackCount = 0;
        currentAmmoPackCount = 0;
    }

    public void GenerateItems()
    {
        Vector3 tileSize = tileBot.GetComponent<Renderer>().bounds.size;
        HashSet<Vector3> occupiedPositions = new HashSet<Vector3>();

        GenerateSpecificItems(healthPackPrefab, maxHealthPacks, ref currentHealthPackCount, tileSize, occupiedPositions);
        GenerateSpecificItems(ammoPackPrefab, maxAmmoPacks, ref currentAmmoPackCount, tileSize, occupiedPositions);
    }

    private void GenerateSpecificItems(GameObject prefab, int maxCount, ref int currentCount, Vector3 tileSize, HashSet<Vector3> occupiedPositions)
    {
        while (currentCount < maxCount)
        {
            Vector3? validPosition = FindValidPosition(tileSize);
            if (validPosition.HasValue && !occupiedPositions.Contains(validPosition.Value))
            {
                PlaceItemAtPosition(prefab, validPosition.Value);
                occupiedPositions.Add(validPosition.Value);
                currentCount++;
            }
        }
    }
    private void GenerateBorderWalls()
    {
        Vector3 tileSize = tileBot.GetComponent<Renderer>().bounds.size;

        for (int x = -2; x <= width + 1; x++) 
        {
            for (int y = -2; y <= height + 1; y++) 
            {
                if (x == -2 || y == -2 || x == width + 1 || y == height + 1 ||
                    x == -1 || y == -1 || x == width || y == height)
                {
                    GenerateWallColumn(x, y, tileSize);
                }
            }
        }
    }

    private void GenerateWallColumn(int x, int y, Vector3 tileSize)
    {
        for (int level = 0; level < 3; level++) 
        {
            Vector3 position = new Vector3(x * tileSize.x, level * tileSize.y, y * tileSize.z);
            GameObject tile;

            if (level == 2) 
            {
                position.y += topTileOffsetY;
                tile = Instantiate(tileTop, position, Quaternion.identity, map);
            }
            else // Dolne poziomy
            {
                tile = Instantiate(tileBot, position, Quaternion.identity, map);
            }

            if (level == 0)
                tile.layer = LayerMask.NameToLayer("Ground");

            else
                tile.layer = LayerMask.NameToLayer("HighGround");
        }
    }

    private void BakeNavMesh()
    {
        navMeshSurface = GetComponent<NavMeshSurface>();
        navMeshSurface.BuildNavMesh();
    }

    private bool IsPositionValid(Vector3 position, float checkRadius = 0.5f)
    {
        Collider[] colliders = Physics.OverlapSphere(position, checkRadius, LayerMask.GetMask("Obstacle", "HighGround"));
        return colliders.Length == 0;
    }

    private void PlacePlayerOnMap()
    {
        GameObject player = transform.Find("Soldier")?.gameObject;
        Vector3 tileSize = tileBot.GetComponent<Renderer>().bounds.size;

        while (!isPlayerPlaced)
        {
            Vector3? validPosition = FindValidPosition(tileSize, maxHeight: 1);
            if (validPosition.HasValue && IsPositionValid(validPosition.Value))
            {
                var pos = validPosition.Value + Vector3.up * 0.5f;
                player.transform.position = pos;
                player.SetActive(true);
                InitialPlayerPosition = pos;
                isPlayerPlaced = true;
            }
        }
    }


    public Vector3 FindValidPositionAwayFromPlayer(float minDistanceFromPlayer = 25f)
    {
        Vector3 playerPosition = FindObjectOfType<CharacterStateController>().transform.position;
        Vector3 tileSize = tileBot.GetComponent<Renderer>().bounds.size;

        while (true)
        {
            Vector3? position = FindValidPosition(tileSize);

            if (position.HasValue && Vector3.Distance(playerPosition, position.Value) > minDistanceFromPlayer)
            {
                return position.Value;
            }
        }
    }


    private Vector3? FindValidPosition(Vector3 tileSize, int minHeight = 0, int maxHeight = 1)
    {
        int x = Random.Range(0, width);
        int y = Random.Range(0, height);

        int heightLevel = heightMap[x, y];

        if (heightLevel >= minHeight && heightLevel <= maxHeight)
        {
            Vector3 basePosition = new Vector3(x * tileSize.x, heightLevel * tileSize.y, y * tileSize.z);
            Ray ray = new Ray(basePosition + Vector3.up * 2f, Vector3.down);

            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, LayerMask.GetMask("Ground")))
            {
                return hitInfo.point + Vector3.up * 0.05f;
            }
        }

        return null;
    }

    private void ActivateFogWar()
    {
        Vector3 tileSize = tileBot.GetComponent<Renderer>().bounds.size;


        int centerX = width / 2;
        int centerY = height / 2;

        Vector3 centerTilePosition = new Vector3(centerX * tileSize.x, 0, centerY * tileSize.z);
        Transform centerTile = null;

        foreach (Transform tile in map)
        {
            if (Vector3.Distance(tile.position, centerTilePosition) < 0.1f)
            {
                centerTile = tile;
                break;
            }
        }

        var fogWarComponent = fogWarObject.GetComponent<csFogWar>();
        FieldInfo fieldInfo = typeof(csFogWar).GetField("levelMidPoint", BindingFlags.NonPublic | BindingFlags.Instance);

        fieldInfo.SetValue(fogWarComponent, centerTile);
        fogWarObject.SetActive(true);
    }

    public void ResetFogWarState()
    {
        var fogWarComponent = fogWarObject.GetComponent<csFogWar>();

        MethodInfo scanLevelMethod = typeof(csFogWar).GetMethod("ScanLevel", BindingFlags.NonPublic | BindingFlags.Instance);
        scanLevelMethod?.Invoke(fogWarComponent, null);


        MethodInfo forceUpdateMethod = typeof(csFogWar).GetMethod("ForceUpdateFog", BindingFlags.NonPublic | BindingFlags.Instance);
        forceUpdateMethod?.Invoke(fogWarComponent, null);
    }


    private void PlaceItemAtPosition(GameObject itemPrefab, Vector3 position)
    {
        Instantiate(itemPrefab, position, Quaternion.identity, map);
    }

    private GameObject GetRandomTree()
    {
        return treeObjects[Random.Range(0, treeObjects.Length)];
    }
}
