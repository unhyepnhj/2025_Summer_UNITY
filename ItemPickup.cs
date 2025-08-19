using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public int itemId = 0;
    public int count = 1;
    public float lifetime = 60f;
    public float attractDistance = 3f;
    public float attractSpeed = 6f;
    Transform player;

    Rigidbody rb;
    private Vector3 startPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        player = Camera.main != null ? Camera.main.transform : null;
        Destroy(gameObject, lifetime);
    }

    private void Start()
    {
        startPos = transform.position; // 초기 위치
    }

    // hover 애니메이션 파라미터
    public float hoverAmplitude = 0.25f;   // 위아래 움직임 크기
    public float hoverFrequency = 2f;      // 진동 속도
    public float rotateSpeed = 50f;        // 회전 속도

    void Update()
    {
        if (player == null) return;

        float newY = startPos.y + Mathf.Sin(Time.time * hoverFrequency) * hoverAmplitude;
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // 블록 파괴 후 줍기 전까지 둥실둥실...
        transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= attractDistance)
        {
            Vector3 dir = (player.position - transform.position).normalized;
            transform.position += dir * attractSpeed * Time.deltaTime;
            if (dist < 1.2f) TryPickup();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 플레이어 영역 들어오면 아이템 획득
        if (other.CompareTag("Player"))
            TryPickup();
    }

    void TryPickup()
    {
        bool added = InventoryManager.Instance.AddItem(itemId, count);
        if (added)
        {
            Destroy(gameObject);
        }
        else
        {
            // 더 이상 슬롯에 못 넣을 경우
            Destroy(gameObject);
        }
    }
}
