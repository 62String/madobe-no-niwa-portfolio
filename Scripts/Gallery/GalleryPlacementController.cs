using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using WindowGarden.Core;
using WindowGarden.Plant;

namespace WindowGarden.UI
{
    public class GalleryPlacementController : MonoBehaviour
    {
        [Header("Gallery Roots")] 
        [SerializeField] private RectTransform roomRoot;
        [SerializeField] private RectTransform unplacedRoot;
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private GallerySwipeController swipeController;
        [SerializeField] private GalleryPlantDragItem plantItemPrefab;


        [SerializeField] private RectTransform placementSlotsRoot;

        private readonly List<GalleryPlacementSlot> placementSlots =
            new List<GalleryPlacementSlot>();

        [SerializeField, Min(0f)] private float slotSnapDistance = 220f;
        
        private readonly Dictionary<string, GalleryPlantStateData> states =
            new Dictionary<string, GalleryPlantStateData>();

        private readonly List<GalleryPlantDragItem> spawnedItems = new List<GalleryPlantDragItem>();

        private int nextSortingOrder;
        
        public bool IsEditing { get; private set; }

        private IEnumerator Start()
        {
            // SaveManager.Start()의 Load가 끝난 다음 생성한다.
            yield return null;

            InitializePlacementSlots();
            Refresh();
        }

        private void InitializePlacementSlots()
        {
            placementSlots.Clear();

            if (placementSlotsRoot == null)
                return;

            for (int i = 0;
                 i < placementSlotsRoot.childCount;
                 i++)
            {
                Transform child =
                    placementSlotsRoot.GetChild(i);

                GalleryPlacementSlot slot =
                    child.GetComponent<GalleryPlacementSlot>();

                if (slot == null)
                    continue;

                slot.SetSlotIndex(placementSlots.Count);
                placementSlots.Add(slot);
            }
        }

        private void Refresh()
        {
            ClearSpawnedItems();
            SpawnHarvestedPlants();
        }

        private Sprite GetGallerySprite(
            PlantData plantData, out int growthStage)
        {
            growthStage = 0;
            
            if (plantData == null)
                return null;

            Sprite[] growthSprites = 
                plantData.growthStageSprites;

            if (growthSprites != null)
            {
                for (int i = growthSprites.Length - 1; 
                     i >= 0; 
                     i--)
                {
                    if (growthSprites[i] == null)
                        continue;

                    growthStage = i + 1;
                    return growthSprites[i];
                }
            }

            return plantData.seedSprite;
        }

        private GalleryPlacementSlot GetPlacementSlot(int slotIndex)
        {
            if (slotIndex < 0 || placementSlots == null)
                return null;

            foreach (GalleryPlacementSlot slot in placementSlots)
            {
                if (slot != null &&
                    slot.SlotIndex == slotIndex)
                {
                    return slot;
                }
            }

            return null;
        }

        private bool IsSlotOccupied(
            int slotIndex,
            GalleryPlantDragItem ignoredItem = null)
        {
            if (slotIndex < 0)
                return false;

            foreach (GalleryPlantDragItem item in spawnedItems)
            {
                if (item == null || item == ignoredItem)
                    continue;

                if (!states.TryGetValue(
                        item.PlantId,
                        out GalleryPlantStateData state))
                {
                    continue;
                }

                if (state.isPlaced &&
                    state.slotIndex == slotIndex)
                {
                    return true;
                }
            }

            return false;
        }

        private void RefreshSlotGuides()
        {
            if (placementSlots == null)
                return;

            foreach (GalleryPlacementSlot slot in placementSlots)
            {
                if (slot == null)
                    continue;

                bool isOccupied =
                    IsSlotOccupied(slot.SlotIndex);

                slot.SetGuideVisible(
                    IsEditing && !isOccupied);
            }
        }

        private GalleryPlacementSlot FindClosestAvailableSlot(
            PointerEventData eventData,
            GalleryPlantDragItem ignoredItem)
        {
            if (eventData == null || roomRoot == null)
                return null;

            bool converted =
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    roomRoot,
                    eventData.position,
                    eventData.pressEventCamera,
                    out Vector2 pointerLocalPosition);

            if (!converted)
                return null;

            GalleryPlacementSlot closestSlot = null;
            float closestDistance = slotSnapDistance;

            foreach (GalleryPlacementSlot slot in placementSlots)
            {
                if (slot == null ||
                    IsSlotOccupied(slot.SlotIndex, ignoredItem))
                {
                    continue;
                }
                
                Vector3 slotCenterWorld =
                    slot.RectTransform.TransformPoint(
                        slot.RectTransform.rect.center);

                Vector3 slotCenterLocal =
                    roomRoot.InverseTransformPoint(slotCenterWorld);

                float distance = Vector2.Distance(
                    pointerLocalPosition,
                    new Vector2(
                        slotCenterLocal.x,
                        slotCenterLocal.y));

                if (distance > closestDistance)
                    continue;

                closestDistance = distance;
                closestSlot = slot;
            }

            return closestSlot;
        }

        private void AttachItemToSlot(
            GalleryPlantDragItem item,
            GalleryPlacementSlot slot)
        {
            if (item == null ||
                item.RectTransform == null ||
                slot == null)
            {
                return;
            }

            RectTransform itemRect = item.RectTransform;

            itemRect.SetParent(slot.RectTransform, false);
            itemRect.anchorMin = new Vector2(0.5f, 0.5f);
            itemRect.anchorMax = new Vector2(0.5f, 0.5f);
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.localScale = Vector3.one;
            itemRect.SetAsLastSibling();

            item.SetPlacedVisual(true);
            slot.SetGuideVisible(false);
        }

        private void SpawnPlantItem(
            PlantData plantData, 
            GalleryPlantStateData state)
        {
            if (plantItemPrefab == null ||
                plantData == null ||
                state == null)
            {
                return;
            }

            GalleryPlantDragItem item =
                Instantiate(plantItemPrefab, unplacedRoot);

            Sprite gallerySprite =
                GetGallerySprite(
                    plantData,
                    out int growthStage);

            float visualScale =
                plantData.galleryVisualScaleOverride > 0f
                    ? plantData.galleryVisualScaleOverride
                    : plantData.GetGrowthStageVisualScale(growthStage);
            
            item.Initialize(
                this,
                plantData.plantId,
                gallerySprite,
                visualScale);

            spawnedItems.Add(item);

            if (!state.isPlaced)
            {
                item.SetPlacedVisual(false);
                return;
            }

            GalleryPlacementSlot savedSlot =
                GetPlacementSlot(state.slotIndex);

            bool canRestoreToSlot =
                savedSlot != null &&
                !IsSlotOccupied(state.slotIndex, item);

            if (!canRestoreToSlot)
            {
                state.isPlaced = false;
                state.slotIndex = -1;

                item.SetPlacedVisual(false);
                SaveManager.Instance?.SetGalleryPlantState(state);
                return;
            }

            AttachItemToSlot(item, savedSlot);
        }

        private void ClearSpawnedItems()
        {
            foreach (GalleryPlantDragItem item in spawnedItems)
            {
                if (item != null)
                    Destroy(item.gameObject);
            }

            spawnedItems.Clear();
            states.Clear();
            nextSortingOrder = 0;
        }

        private GalleryPlantStateData GetOrCreateState(
            SaveManager saveManager,
            string plantId,
            int listOrder)
        {
            GalleryPlantStateData state =
                saveManager.GetGalleryPlantState(plantId);

            if (state != null)
                return state;

            state = new GalleryPlantStateData
            {
                plantId = plantId,
                isPlaced = false,
                slotIndex = -1,
                normalizedX = 0.5f,
                normalizedY = 0.5f,
                listOrder = listOrder,
                sortingOrder = nextSortingOrder
            };

            saveManager.SetGalleryPlantState(state);
            return state;
        }

        private void SpawnHarvestedPlants()
        {
            GameManager gameManager = GameManager.Instance;
            SaveManager saveManager = SaveManager.Instance;

            if (gameManager == null ||
                saveManager == null ||
                gameManager.availablePlants == null)
                return;

            int listOrder = 0;

            foreach (PlantData plantData in gameManager.availablePlants)
            {
                if (plantData == null ||
                    string.IsNullOrEmpty(plantData.plantId))
                    continue;

                if (!saveManager.HasHarvestRecord(plantData.plantId))
                    continue;

                GalleryPlantStateData state =
                    GetOrCreateState(
                        saveManager,
                        plantData.plantId,
                        listOrder);

                states[plantData.plantId] = state;

                nextSortingOrder =
                    Mathf.Max(nextSortingOrder, state.sortingOrder + 1);

                SpawnPlantItem(plantData, state);
                listOrder++;
            }

            RefreshSlotGuides();
        }

        private void MoveItemToPointer(GalleryPlantDragItem item, RectTransform parent, PointerEventData eventData)
        {
            bool converted = RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera,
                    out Vector2 localPosition);

            if (!converted)
                return;

            item.RectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            item.RectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            item.RectTransform.anchoredPosition = localPosition;
        }

        private bool IsPointerInsideRoom(PointerEventData eventData)
        {
            if (roomRoot == null)
                return false;

            return RectTransformUtility.RectangleContainsScreenPoint(
                roomRoot,
                eventData.position,
                eventData.pressEventCamera);
        }

        private void RestoreItemToSavedLocation(
            GalleryPlantDragItem item,
            GalleryPlantStateData state)
        {
            if (state != null && state.isPlaced)
            {
                GalleryPlacementSlot savedSlot =
                    GetPlacementSlot(state.slotIndex);

                if (savedSlot != null)
                {
                    AttachItemToSlot(item, savedSlot);
                    return;
                }
            }

            ReturnItemToUnplacedRoot(item);
        }

        private void PlaceItemInRoom(
            GalleryPlantDragItem item, 
            PointerEventData eventData)
        {
            if (item == null ||
                !states.TryGetValue(
                    item.PlantId,
                    out GalleryPlantStateData state))
            {
                ReturnItemToUnplacedRoot(item);
                return;
            }

            GalleryPlacementSlot closestSlot =
                FindClosestAvailableSlot(eventData, item);

            if (closestSlot == null)
            {
                RestoreItemToSavedLocation(item, state);
                return;
            }

            state.isPlaced = true;
            state.slotIndex = closestSlot.SlotIndex;

            AttachItemToSlot(item, closestSlot);
            SaveManager.Instance?.SetGalleryPlantState(state);
            RefreshSlotGuides();
        }

        private void ReturnItemToUnplacedRoot(GalleryPlantDragItem item)
        {
            if (item == null || unplacedRoot == null)
                return;

            RectTransform itemRect = item.RectTransform;

            itemRect.SetParent(unplacedRoot, false);
            itemRect.anchorMin = new Vector2(0.5f, 0.5f);
            itemRect.anchorMax = new Vector2(0.5f, 0.5f);
            itemRect.anchoredPosition = Vector2.zero;
            itemRect.localScale = Vector3.one;
            itemRect.SetAsLastSibling();

            item.SetPlacedVisual(false);

            if (!states.TryGetValue(
                    item.PlantId,
                    out GalleryPlantStateData state))
                return;

            state.isPlaced = false;
            state.slotIndex = -1;
            state.listOrder = itemRect.GetSiblingIndex();

            SaveManager.Instance?.SetGalleryPlantState(state);
            RefreshSlotGuides();
        }

        public void SetEditMode(bool editing)
        {
            IsEditing = editing;
            RefreshSlotGuides();
        }
        
        public void BeginPlantDrag(
            GalleryPlantDragItem item, PointerEventData eventData)
        {
            if (!IsEditing ||
                item == null ||
                item.RectTransform == null ||
                dragLayer == null)
                return;

            item.RectTransform.SetParent(dragLayer, true);
            item.RectTransform.SetAsLastSibling();

            item.SetPlacedVisual(true);
            MoveItemToPointer(item, dragLayer, eventData);
        }

        public void DragPlant(
            GalleryPlantDragItem item,
            PointerEventData eventData)
        {
            if (!IsEditing ||
                item == null ||
                item.RectTransform == null)
                return;

            MoveItemToPointer(item, dragLayer, eventData);
        }

        public void EndPlantDrag(
            GalleryPlantDragItem item,
            PointerEventData eventData)
        {
            if (!IsEditing ||
                item == null ||
                item.RectTransform == null)
                return;

            if (IsPointerInsideRoom(eventData))
                PlaceItemInRoom(item, eventData);
            else
                ReturnItemToUnplacedRoot(item);
        }
    }
}