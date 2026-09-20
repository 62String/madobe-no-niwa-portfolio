using UnityEngine;

namespace WindowGarden.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class GalleryTransitionTarget : MonoBehaviour
    {
        public RectTransform Root =>
            (RectTransform)transform;
    }
}