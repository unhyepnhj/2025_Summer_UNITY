using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("아이템 아이콘 (index = itemId)")]
    public Sprite[] itemIcons; // Inspector에서 blockPrefabs 순서로 아이콘 할당

    public int hotbarSize = 9;
    public int maxStack = 64;

    // hotbar만 구현
    public int[] hotbarCounts; // length = hotbarSize
    public int[] hotbarItemIds; // length = hotbarSize, -1 = 빈칸

    public event Action OnInventoryChanged;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;

        hotbarItemIds = new int[hotbarSize];
        hotbarCounts = new int[hotbarSize];
        for (int i = 0; i < hotbarSize; i++) hotbarItemIds[i] = -1;
    }

    // 빈 슬롯 먼저 찾고 합치기
    public bool AddItem(int itemId, int count = 1)
    {
        if (itemId < 0 || itemId >= itemIcons.Length) return false;

        // case 1: 이미 있는 아이템이면 슬롯에 합치기
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarItemIds[i] == itemId && hotbarCounts[i] < maxStack)
            {
                int canAdd = Mathf.Min(count, maxStack - hotbarCounts[i]);
                hotbarCounts[i] += canAdd;
                count -= canAdd;
                if (count <= 0) { OnInventoryChanged?.Invoke(); return true; }
            }
        }

        // case 2: 빈 슬롯에 넣기
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarItemIds[i] == -1)
            {
                int add = Mathf.Min(count, maxStack);
                hotbarItemIds[i] = itemId;
                hotbarCounts[i] = add;
                count -= add;
                if (count <= 0) { OnInventoryChanged?.Invoke(); return true; }
            }
        }

        // 남은 count는 현재 핫바에 넣을 수 없음 -> 실패
        OnInventoryChanged?.Invoke();
        return false;
    }

    // 슬롯 정보 조회 (UI용)
    public (int id, int count) GetHotbarSlot(int slot)
    {
        if (slot < 0 || slot >= hotbarSize) return (-1, 0);
        return (hotbarItemIds[slot], hotbarCounts[slot]);
    }
}
