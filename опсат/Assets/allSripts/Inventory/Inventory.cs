using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    [Header("UI")]
    public GameObject Inv;
    public Image dragIcon;
    public GameObject dropZone;

    [Header("Hotbar Appearance")]
    public float normalOpacity = 0.58f;

    private List<Slot> invSlots = new List<Slot>();
    private List<Slot> allSlots = new List<Slot>();

    private Slot draggedSlot;
    private bool isDragging;

    public GameObject player;

    private DropZone dropZoneComponent;


    public Text allCost;

    private void Awake()
    {
        invSlots.AddRange(Inv.GetComponentsInChildren<Slot>());
       
        allSlots.AddRange(invSlots);

        if (dropZone != null)
            dropZoneComponent = dropZone.GetComponent<DropZone>();

        if (dropZone != null)
            dropZone.SetActive(false);
    }

    private void Update()
    {
        StartDrag();
        UpdateDragItemPosition();
        EndDrag();
        GetTotalInventoryCost();
    }

    // ================= ADD ITEM =================

    // Проверяет сколько штук можно добавить (0 = инвентарь полон)
    public int GetAvailableSpace(ItemSO itemToAdd)
    {
        int space = 0;

        foreach (Slot slot in allSlots)
        {
            if (slot.HasItem() && slot.GetItem() == itemToAdd)
                space += itemToAdd.maxStackSize - slot.GetAmount();
        }

        foreach (Slot slot in allSlots)
        {
            if (!slot.HasItem())
                space += itemToAdd.maxStackSize;
        }

        return space;
    }

    // Возвращает сколько штук реально удалось добавить
    public int AddItem(ItemSO itemToAdd, int amount)
    {
        int remaining = amount;

        // Сначала добавляем в уже существующие стаки
        foreach (Slot slot in allSlots)
        {
            if (remaining <= 0) break;
            if (!slot.HasItem() || slot.GetItem() != itemToAdd) continue;

            int space = itemToAdd.maxStackSize - slot.GetAmount();
            if (space <= 0) continue;

            int add = Mathf.Min(space, remaining);
            slot.AddAmount(add);
            remaining -= add;
        }

        // Затем в пустые слоты
        foreach (Slot slot in allSlots)
        {
            if (remaining <= 0) break;
            if (slot.HasItem()) continue;

            int add = Mathf.Min(itemToAdd.maxStackSize, remaining);
            slot.SetItem(itemToAdd, add);
            remaining -= add;
        }

        return amount - remaining;
    }

    // ================= DRAG =================

    private void StartDrag()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        Slot hovered = GetHoveredSlot();
        if (hovered == null || !hovered.HasItem()) return;

        draggedSlot = hovered;
        isDragging = true;

        dragIcon.sprite = hovered.GetItem().icon;
        dragIcon.color = new Color(1, 1, 1, 0.5f);
        dragIcon.enabled = true;

        if (dropZone != null)
            dropZone.SetActive(true);
    }

    private void EndDrag()
    {
        if (!Input.GetMouseButtonUp(0) || !isDragging) return;

        Slot hovered = GetHoveredSlot();

        ShopSellUI shop = FindFirstObjectByType<ShopSellUI>();
        bool droppedOnSellSlot = false;

        if (hovered == null && shop != null && shop.sellSlot.hovering)
        {
            droppedOnSellSlot = true;
        }

        if (droppedOnSellSlot)
        {
            // Сразу продаём 1 штуку из draggedSlot, не перекладывая в слот
            shop.SellOneFromSlot(draggedSlot);
        }
        else if (hovered != null)
        {
            HandleDrop(draggedSlot, hovered);
        }
        else if (dropZoneComponent != null && dropZoneComponent.IsHovering)
        {
            DropItemToWorld(draggedSlot);
        }

        dragIcon.enabled = false;
        draggedSlot = null;
        isDragging = false;

        if (dropZone != null)
            dropZone.SetActive(false);
    }

    private void UpdateDragItemPosition()
    {
        if (isDragging)
            dragIcon.transform.position = Input.mousePosition;
    }

    private Slot GetHoveredSlot()
    {
        foreach (Slot s in allSlots)
            if (s.hovering) return s;
        return null;
    }

    private void HandleDrop(Slot from, Slot to)
    {
        if (from == to) return;

        if (to.HasItem() && to.GetItem() == from.GetItem())
        {
            int space = to.GetItem().maxStackSize - to.GetAmount();
            if (space > 0)
            {
                int move = Mathf.Min(space, from.GetAmount());
                to.AddAmount(move);
                from.RemoveAmount(move);
                return;
            }
        }

        ItemSO tempItem = to.GetItem();
        int tempAmount = to.GetAmount();

        to.SetItem(from.GetItem(), from.GetAmount());
        from.SetItem(tempItem, tempAmount);
    }

    // ================= DROP IN WORLD =================

    private void DropItemToWorld(Slot slot)
    {
        if (!slot.HasItem()) return;

        ItemSO itemSO = slot.GetItem();
        GameObject prefab = itemSO.pickupPrefab;
        if (prefab == null) return;

        if (itemSO.dropSound != null)
            AudioSource.PlayClipAtPoint(itemSO.dropSound, player.transform.position);

        GameObject dropped = Instantiate(
            prefab,
            player.transform.position + Vector3.up * 1.5f,
            Quaternion.identity
        );

        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = player.transform.forward * 3f + Vector3.up * 2f;

        Item item = dropped.GetComponent<Item>();
        if (item != null)
        {
            item.item = itemSO;
            item.amount = slot.GetAmount();
        }

        slot.ClearSlot();
    }

    // ================= УТИЛИТЫ =================

    public int GetTotalInventoryCost()
    {
        int totalCost = 0;
        foreach (Slot slot in allSlots)
        {
            if (!slot.HasItem()) continue;
            totalCost += slot.GetItem().cost * slot.GetAmount();
        }
        allCost.text = totalCost.ToString(totalCost + "$");
        return totalCost;
        
    }

    public void ClearAllSlots()
    {
        foreach (Slot slot in allSlots)
            slot.ClearSlot();
    }
}