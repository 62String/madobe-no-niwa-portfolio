using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace WindowGarden.UI
{
    public class GalleryPlantDragItem :
        MonoBehaviour, 
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField] private Image plantImage;

        [Header("Visual Scale")] 
        [SerializeField, Min(0.01f)] private float galleryBaseScale = 0.15f;
        [SerializeField, Min(0.01f)] private float unplacedScaleMultiplier = 0.6f;
        [SerializeField, Min(0.01f)] private float placedScaleMultiplier = 1.5f;
        [SerializeField] private float placedBaselineOffsetY = 0f;
        
        private float normalizedVisualScale = 1f;


        private enum DragMode
        {
            None,
            Plant,
            Scroll
        }

        private DragMode dragMode;
        private ScrollRect scrollRect;
        private bool isPlaced;
        
        public string PlantId { get; private set; }
        public RectTransform RectTransform { get; private set; }

        private GalleryPlacementController controller;
        private CanvasGroup canvasGroup;

        public void Initialize(
            GalleryPlacementController owner, 
            string plantId, Sprite plantSprite, float visualScale)
        {
            controller = owner;
            PlantId = plantId;
            normalizedVisualScale = visualScale;
            RectTransform = transform as RectTransform;
            scrollRect = GetComponentInParent<ScrollRect>(true);

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            if (plantImage != null)
            {
                plantImage.sprite = plantSprite;
                plantImage.enabled = plantSprite != null;
                plantImage.preserveAspect = true;

                if (plantSprite != null)
                    plantImage.SetNativeSize();

                ApplyVisualAlignment();
                ApplyVisualScale();
            }
        }
        
        public void SetPlacedVisual(bool isPlaced)
        {
            this.isPlaced = isPlaced;
            
            Image slotImage = GetComponent<Image>();

            if (slotImage != null)
                slotImage.enabled = !isPlaced;

            if (plantImage != null)
                plantImage.raycastTarget = isPlaced;

            ApplyVisualAlignment();
            ApplyVisualScale();
        }

        private void ApplyVisualAlignment()
        {
            if (plantImage == null)
                return;

            RectTransform imageRect = plantImage.rectTransform;

            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);

            imageRect.pivot = isPlaced
                ? new Vector2(0.5f, 0f)
                : new Vector2(0.5f, 0.5f);

            imageRect.anchoredPosition = isPlaced
                ? new Vector2(0f, placedBaselineOffsetY)
                : Vector2.zero;

        }

        private void ApplyVisualScale()
        {
            if (plantImage == null)
                return;

            float stateMultiplier =
                isPlaced
                    ? placedScaleMultiplier
                    : unplacedScaleMultiplier;

            plantImage.rectTransform.localScale =
                Vector3.one *
                galleryBaseScale *
                normalizedVisualScale *
                stateMultiplier;
        }

        private bool CanScrollHorizontally()
        {
            if (scrollRect == null || 
                !scrollRect.horizontal ||
                scrollRect.content == null ||
                scrollRect.viewport == null)
            {
                return false;
            }

            return scrollRect.content.rect.width >
                   scrollRect.viewport.rect.width + 1f;
        }

        private void TryChooseDragMode(
            PointerEventData eventData)
        {
            if (dragMode != DragMode.None ||
                eventData == null)
            {
                return;
            }

            Vector2 dragDelta =
                eventData.position - eventData.pressPosition;

            const float decisionThreshold = 10f;

            if (dragDelta.sqrMagnitude <
                decisionThreshold * decisionThreshold)
            {
                return;
            }

            bool isHorizontal =
                Mathf.Abs(dragDelta.x) >
                Mathf.Abs(dragDelta.y);

            if (!isPlaced && isHorizontal)
            {
                if (CanScrollHorizontally())
                {
                    dragMode = DragMode.Scroll;
                    scrollRect.OnBeginDrag(eventData);
                }

                return;
            }

            if (!isPlaced && dragDelta.y <= 0f)
                return;

            dragMode = DragMode.Plant;

            scrollRect?.StopMovement();
            canvasGroup.blocksRaycasts = false;
            controller.BeginPlantDrag(this, eventData);
        }

        public void OnBeginDrag(
            PointerEventData eventData)
        {
            if (controller == null ||
                !controller.IsEditing)
            {
                return;
            }

            dragMode = DragMode.None;
            canvasGroup.blocksRaycasts = true;
        }

        public void OnDrag(
            PointerEventData eventData)
        {
            if (controller == null ||
                !controller.IsEditing)
            {
                return;
            }

            TryChooseDragMode(eventData);

            if (dragMode == DragMode.Plant)
            {
                controller.DragPlant(this, eventData);
            }
            else if (dragMode == DragMode.Scroll)
            {
                scrollRect.OnDrag(eventData);
            }
        }

        public void OnEndDrag(
            PointerEventData eventData)
        {
            if (controller == null)
                return;

            if (dragMode == DragMode.Plant)
            {
                controller.EndPlantDrag(this, eventData);
            }
            else if (dragMode == DragMode.Scroll)
            {
                scrollRect.OnEndDrag(eventData);
            }

            canvasGroup.blocksRaycasts = true;
            dragMode = DragMode.None;
        }
    }
}