using UnityEngine;

public enum BlockType
{
    None = 0,
    Grass = 1,
    Dirt = 2,
    Stone = 3,
    Sand = 4
}

public class WorldGenerator : MonoBehaviour
{
    [Header("월드 설정")]
    public int width = 20;
    public int height = 10;
    public int depth = 20;

    [Header("블록 prefab 목록 (0:Grass, 1:Dirt, 2:Stone, 3:Sand)")]
    public GameObject[] blockPrefabs;

    private BlockType[,,] worldBlocks;
    private Vector3 worldOrigin = Vector3.zero;

    [Header("지형 옵션")]   // perin 어쩌고로 지형 다양하게 생성
    public bool usePerlin = false;
    public float perlinScale = 0.1f;         // 낮을수록 부드럽게
    public float perlinHeightMultiplier = 1f;

    [Header("Perlin 설정 (usePerlin=true)")]
    public int perlinSeed = 1337;
    public int perlinOctaves = 1;
    public float perlinPersistence = 1f;
    public float perlinLacunarity = 0.5f;

    [Header("스폰")]
    public Transform player;
    public string blockLayerName = "Block";  // 생성된 블록의 Layer로 설정

    void Start()
    {
        if (blockPrefabs == null || blockPrefabs.Length < 4)
        {
            Debug.LogWarning("WorldGenerator: blockPrefabs에 4개 프리팹(Dirt,Grass,Stone,Sand)을 할당하세요.");
        }

        worldOrigin = Vector3.zero;

        if (perlinHeightMultiplier <= 0f) perlinHeightMultiplier = Mathf.Max(1f, height - 2);

        if (usePerlin)  // perlin 노이즈 사용할 때
            InitBlocks_perlin();
        else
            InitBlocks();

        GenerateWorld();
        SpawnPlayer();
    }

    void InitBlocks()
    {
        worldBlocks = new BlockType[width, height, depth];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int groundHeight;

                if (usePerlin)
                {
                    float noise = Mathf.PerlinNoise((x + 1000f) * perlinScale, (z + 1000f) * perlinScale); // offset to vary
                    groundHeight = Mathf.Clamp(Mathf.FloorToInt(noise * perlinHeightMultiplier) + 1, 1, height - 1);
                }
                else
                {
                    // 기본: 낮은 평지
                    groundHeight = height / 2; // 중앙에 지표면 생성
                }

                for (int y = 0; y < height; y++)
                {
                    if (y > groundHeight)
                    {
                        worldBlocks[x, y, z] = BlockType.None; // 공기
                    }
                    else if (y == groundHeight)
                    {
                        worldBlocks[x, y, z] = BlockType.Grass; // 표면: 풀
                    }
                    else if (y > groundHeight - 3)
                    {
                        float r = Random.value;
                        if (r < 0.7f) worldBlocks[x, y, z] = BlockType.Dirt;
                        else worldBlocks[x, y, z] = BlockType.Stone; // 표면 아래: 흙/돌 랜덤
                    }
                    else
                    {
                        // 더 아래: 돌/모래/흙 랜덤
                        float r = Random.value;
                        if (r < 0.15f) worldBlocks[x, y, z] = BlockType.Stone;
                        else if (r < 0.30f) worldBlocks[x, y, z] = BlockType.Sand;
                        else worldBlocks[x, y, z] = BlockType.Dirt;
                    }
                }
            }
        }
    }

    void GenerateWorld()
    {
        int blockLayer = LayerMask.NameToLayer(blockLayerName);
        if (blockLayer < 0)
        {
            Debug.LogWarning($"WorldGenerator: Layer '{blockLayerName}'가 존재하지 않습니다. 블록 레이어는 설정하세요.");
        }

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    BlockType type = worldBlocks[x, y, z];
                    if (type == BlockType.None) continue;

                    Vector3 pos = worldOrigin + new Vector3(x + 0.5f, y + 0.5f, z + 0.5f);

                    int prefabIndex = (int)type - 1; // Dirt(1)->0 ...
                    if (prefabIndex < 0 || prefabIndex >= blockPrefabs.Length)
                    {
                        Debug.LogWarning($"WorldGenerator: BlockType {type}에 해당하는 prefab이 없습니다.");
                        continue;
                    }

                    GameObject inst = Instantiate(blockPrefabs[prefabIndex], pos, Quaternion.identity, transform);

                    // 생성된 오브젝트의 레이어를 Block으로 설정 (자식 포함)
                    if (blockLayer >= 0)
                        SetLayerRecursively(inst, blockLayer);

                    // collider가 없다면 BoxCollider 추가 (안전 장치)
                    if (inst.GetComponent<Collider>() == null)
                        inst.AddComponent<BoxCollider>();
                }
            }
        }
    }

    void SpawnPlayer()
    {
        if (player == null) return;

        // 중앙
        int cx = width / 2;
        int cz = depth / 2;
        int centerGround = -1;
        for (int y = height - 1; y >= 0; y--)
        {
            if (worldBlocks[cx, y, cz] != BlockType.None)
            {
                centerGround = y;
                break;
            }
        }

        // 지표면이 발견되지 않으면 기본 높이 사용
        if (centerGround < 0) centerGround = 0;

        // 플레이어가 지표면 바로 위에 오도록 Y 계산
        // block의 top surface y = worldOrigin.y + centerGround + 1.0f (블록 높이 1.0 가정)
        float footClearance = 0.1f; // 발밑 여유 (필요시 조절)
        Vector3 spawnPos = worldOrigin + new Vector3(width / 2f, centerGround + 1.0f + footClearance, depth / 2f);

        // 안전하게 위치 이동: CharacterController / Rigidbody / 기본 Transform 순서로 처리
        var cc = player.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            player.position = spawnPos;
            cc.enabled = true;
            return;
        }

        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Rigidbody는 velocity 초기화 후 바로 위치 설정
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = spawnPos;
            return;
        }

        // 둘 다 없으면 Transform 이동
        player.position = spawnPos;
    }

    void InitBlocks_perlin()
    {
        worldBlocks = new BlockType[width, height, depth];

        // 안전 보정
        float scale = Mathf.Max(0.0001f, perlinScale);
        float heightMul = Mathf.Max(0.0001f, perlinHeightMultiplier);

        // seed offsets (seed로 지형 재현 가능)
        float seedX = perlinSeed * 1.0f;
        float seedZ = perlinSeed * 7.0f + 1000f;

        int minGround = int.MaxValue;
        int maxGround = int.MinValue;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                // fBm (Fractal Brownian Motion) — 여러 옥타브 합성
                float amplitude = 1f;
                float frequency = 1f;
                float noiseSum = 0f;
                float ampSum = 0f;

                for (int o = 0; o < perlinOctaves; o++)
                {
                    float sampleX = (x + seedX) * scale * frequency;
                    float sampleZ = (z + seedZ) * scale * frequency;
                    // PerlinNoise 반환 0..1, 변환해서 -1..1 범위로 사용
                    float n = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f;
                    noiseSum += n * amplitude;
                    ampSum += amplitude;

                    amplitude *= perlinPersistence;
                    frequency *= perlinLacunarity;
                }

                // 정규화(0..1)
                float noise = (noiseSum / ampSum + 1f) * 0.5f;

                // 지면 높이 계산: 1 .. height-1 범위로 매핑
                int maxPossible = Mathf.Max(1, height - 2);
                int groundHeight = Mathf.Clamp(Mathf.FloorToInt(noise * maxPossible * heightMul) + 1, 1, height - 1);

                if (groundHeight < minGround) minGround = groundHeight;
                if (groundHeight > maxGround) maxGround = groundHeight;

                // y 채우기
                for (int y = 0; y < height; y++)
                {
                    if (y > groundHeight)
                    {
                        worldBlocks[x, y, z] = BlockType.None;
                    }
                    else if (y == groundHeight)
                    {
                        worldBlocks[x, y, z] = BlockType.Grass;
                    }
                    else if (y > groundHeight - 3)
                    {
                        float r = Random.value;
                        if (r < 0.7f) worldBlocks[x, y, z] = BlockType.Dirt;
                        else worldBlocks[x, y, z] = BlockType.Stone;
                    }
                    else
                    {
                        float r = Random.value;
                        if (r < 0.15f) worldBlocks[x, y, z] = BlockType.Stone;
                        else if (r < 0.30f) worldBlocks[x, y, z] = BlockType.Sand;
                        else worldBlocks[x, y, z] = BlockType.Dirt;
                    }
                }
            }
        }

        Debug.Log($"InitBlocks_perlin: groundHeight range = {minGround} .. {maxGround} (height={height})");
    }


    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform t in obj.transform)
            SetLayerRecursively(t.gameObject, layer);
    }
}
