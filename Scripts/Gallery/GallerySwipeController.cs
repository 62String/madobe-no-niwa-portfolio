using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WindowGarden.UI
{
    public class GallerySwipeController : MonoBehaviour
    {
        [SerializeField] private GallerySceneController sceneController;
        [SerializeField] private bool returnToMain;
        [SerializeField] private float swipeThreshold = 150f;

        private Vector2 pointerDownPosition;
        private bool isTrackingPointer;
        private bool swipeEnabled = true;

        void Awake()
        {
            if (sceneController == null)
                sceneController = GetComponent<GallerySceneController>();

            if (sceneController == null)
            {
                Debug.LogError("[GallerySwipeController] GallerySceneController 참조가 없습니다.", this);

                enabled = false;
            }
        }

        void Update()
        {
            Pointer pointer = Pointer.current;

            if (!CanReceiveSwipe(pointer))
            {
                isTrackingPointer = false;
                return;
            }

            if (pointer.press.wasPressedThisFrame)
                BeginSwipe(pointer);

            if (pointer.press.wasReleasedThisFrame)
                CompleteSwipe(pointer);
        }

        private bool CanReceiveSwipe(Pointer pointer)
        {
            return pointer != null && swipeEnabled && !UIInputBlocker.IsBlocked;
        }

        private void BeginSwipe(Pointer pointer)
        {
            pointerDownPosition = pointer.position.ReadValue();
            isTrackingPointer = true;
        }

        private void CompleteSwipe(Pointer pointer)
        {
            if (!isTrackingPointer)
                return;

            Vector2 releasedPosition = pointer.position.ReadValue();
            Vector2 swipeDelta = releasedPosition - pointerDownPosition;

            isTrackingPointer = false;

            if (!IsValidSwipe(swipeDelta))
                return;

            ExecuteSceneTransition(swipeDelta.x);
        }

        private bool IsValidSwipe(Vector2 swipeDelta)
        {
            bool isHorizontal = Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y);

            bool passedThreshold = Mathf.Abs(swipeDelta.x) >= swipeThreshold;

            return isHorizontal && passedThreshold;
        }

        private void ExecuteSceneTransition(float horizontalDelta)
        {
            if (returnToMain && horizontalDelta < 0f)
                sceneController.ReturnToMain();
            else if (!returnToMain && horizontalDelta > 0f)
                sceneController.OpenGallery();
        }

        public void SetSwipeEnabled(bool enabled)
        {
            swipeEnabled = enabled;
            isTrackingPointer = false;
        }
    }
}