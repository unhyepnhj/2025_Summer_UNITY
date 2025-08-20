using UnityEngine;

public class BlockManager : MonoBehaviour
{
    [Header("블록 prefab 목록 (0:Grass, 1:Dirt, 2:Stone, 3:Sand)")]
    public GameObject[] blockPrefabs;

    [Header("블록별 아이템 prefab 목록")]
    public GameObject[] blockPickupPrefabs; // 0:Grass item prefab, 1:Dirt item prefab ...

    private int selectedIndex = 0;

    [Header("설정")]
    public float maxDistance = 5f;
    public LayerMask blockLayer; // Inspector에서 "Block" 레이어 체크

    void Update()
    {
        HandleBlockSwitch();

        if (Input.GetMouseButtonDown(0)) RemoveBlock();
        if (Input.GetMouseButtonDown(1)) PlaceBlock();
    }

    void HandleBlockSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) selectedIndex = 0; // Grass
        if (Input.GetKeyDown(KeyCode.Alpha2)) selectedIndex = 1; // Dirt
        if (Input.GetKeyDown(KeyCode.Alpha3)) selectedIndex = 2; // Stone
        if (Input.GetKeyDown(KeyCode.Alpha4)) selectedIndex = 3; // Sand

        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, blockPrefabs.Length - 1));
    }

    [Header("아이템 드랍 설정")]
    public GameObject pickupPrefab; // ItemPickup prefab
    public int[] blockDropItemId; // blockPrefabs index -> itemId mapping (Inspector로 설정)

    // 블록 제거
    void RemoveBlock()
    {   // 25.08.19 수정 -> 블록 파괴 시 아이템 드랍
        if (Physics.Raycast(Camera.main.transform.position,
                            Camera.main.transform.forward,
                            out RaycastHit hit, maxDistance,
                            LayerMask.GetMask("Block")))
        {
            GameObject hitObj = hit.collider.gameObject;
            Vector3 spawnPos = hitObj.transform.position + Vector3.up * 0.6f;

            // 블록 prefab 이름으로 아이템 ID 찾기
            int blockIndex = -1;
            string baseName = hitObj.name.Replace("(Clone)", "").Trim();
            for (int i = 0; i < blockPrefabs.Length; i++)
            {
                if (blockPrefabs[i].name == baseName)
                {
                    blockIndex = i;
                    break;
                }
            }

            Destroy(hitObj);    // 블록 파괴

            // 드롭할 프리팹 선택: 우선 blockPickupPrefabs, 없으면 기존 pickupPrefab 사용
            GameObject chosenPickupPrefab = null;
            if (blockIndex >= 0 && blockPickupPrefabs != null && blockIndex < blockPickupPrefabs.Length)
                chosenPickupPrefab = blockPickupPrefabs[blockIndex];

            if (chosenPickupPrefab == null)
                chosenPickupPrefab = pickupPrefab; // fallback

            if (chosenPickupPrefab != null)
            {
                GameObject p = Instantiate(chosenPickupPrefab, spawnPos, Quaternion.identity);
                var ip = p.GetComponent<ItemPickup>();
                if (ip != null)
                {
                    // 기존에 사용하던 blockDropItemId 로 itemId 설정을 유지하려면
                    int dropId = 0;
                    if (blockIndex >= 0 && blockDropItemId != null && blockIndex < blockDropItemId.Length)
                        dropId = blockDropItemId[blockIndex];
                    ip.itemId = dropId;
                    ip.count = 1;
                }
            }
        }
    }

    // 블록 설치
    void PlaceBlock()
    {
        int mask = LayerMask.GetMask("Block", "Default");

        if (Physics.Raycast(
                Camera.main.transform.position,
                Camera.main.transform.forward,
                out RaycastHit hit,
                maxDistance,
                mask))
        {
            // hit에서 법선 방향으로 0.5만큼 떨어진 점 계산
            Vector3 p = hit.point + hit.normal * 0.5f;

            // 그 점을 중앙(0.5 간격)으로 정렬
            Vector3 spawnPos = new Vector3(
                Mathf.Floor(p.x) + 0.5f,
                Mathf.Floor(p.y) + 0.5f,
                Mathf.Floor(p.z) + 0.5f
            );

            // 미 블록이 있는 위치라면 설치하지 않도록 Optional 체크
            if (!Physics.CheckBox(
                    spawnPos,
                    Vector3.one * 0.45f,
                    Quaternion.identity,
                    LayerMask.GetMask("Block")))
            {
                // 인스턴스화
                Instantiate(blockPrefabs[selectedIndex], spawnPos, Quaternion.identity);
            }
        }
    }
}
