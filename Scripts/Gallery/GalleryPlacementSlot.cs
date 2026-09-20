using UnityEngine;

namespace WindowGarden.UI
{
    [RequireComponent(typeof(RectTransform))]   
    public class GalleryPlacementSlot : MonoBehaviour
    {
        [SerializeField] 
        private GameObject placementGuide;

        public int SlotIndex { get; private set; } = -1;

        public RectTransform RectTransform =>
            (RectTransform)transform;

        public void SetSlotIndex(int slotIndex)
        {
            SlotIndex = slotIndex;
        }

        public void SetGuideVisible(bool visible)
        {
            if (placementGuide != null)
                placementGuide.SetActive(visible);
        }
    }
}