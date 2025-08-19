using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
    public Image[] slotImages; // Inspector에서 9개의 이미지 연결
    public TextMeshProUGUI[] slotCounts;


    void Start()
    {
        InventoryManager.Instance.OnInventoryChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (InventoryManager.Instance != null) InventoryManager.Instance.OnInventoryChanged -= Refresh;
    }

    public void Refresh()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            var info = InventoryManager.Instance.GetHotbarSlot(i);
            int id = info.id; int count = info.count;
            if (id >= 0 && id < InventoryManager.Instance.itemIcons.Length)
            {
                slotImages[i].sprite = InventoryManager.Instance.itemIcons[id];
                slotImages[i].color = Color.white;
                if (slotCounts != null && slotCounts.Length > i && slotCounts[i] != null)
                    slotCounts[i].text = count > 1 ? count.ToString() : "";
            }
            else
            {
                slotImages[i].sprite = null;
                slotImages[i].color = new Color(1, 1, 1, 0); // 투명
                if (slotCounts != null && slotCounts.Length > i && slotCounts[i] != null) slotCounts[i].text = "";
            }
        }
    }
}
