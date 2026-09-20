using System.Collections;
using UnityEngine;
using WindowGarden.Core;

namespace WindowGarden.UI
{
    public class GallerySceneController : MonoBehaviour
    {
        [SerializeField] private string gallerySceneName = "Gallery";
        [SerializeField] private string mainSceneName = "Main";
        
        [SerializeField] private GalleryTransitionController transitionPrefab;

        public void OpenGallery()
        {
            SaveManager.Instance?.Save();
            StartTransition(gallerySceneName, 1f);
        }

        public void ReturnToMain()
        {
            StartTransition(mainSceneName, -1f);
        }

        private void StartTransition(
            string destinationSceneName,
            float direction)
        {
            if (GalleryTransitionController.IsTransitioning)
                return;

            if (transitionPrefab == null)
            {
                Debug.LogError(
                    "[GallerySceneController] GalleryTransitionOverlay 프리팹 참조가 없습니다.",
                    this);
                return;
            }

            GalleryTransitionController transition =
                Instantiate(transitionPrefab);

            transition.BeginTransition(
                destinationSceneName,
                direction);
        }
    }
}
