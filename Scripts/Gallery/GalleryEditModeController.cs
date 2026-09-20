using UnityEngine;
using WindowGarden.Core;


namespace WindowGarden.UI
{
    public class GalleryEditModeController : MonoBehaviour
    {
        //TODO: 가이드 다시 넣기
        //[SerializeField] private GameObject placementGuide;
        [SerializeField] private GameObject unplacedRoot;
        [SerializeField] private GallerySwipeController swipeController;
        [SerializeField] private GalleryPlacementController placementController;
        

        private bool isEditing;

        void Awake()
        {
            SetEditMode(false);
        }
        
        private void SetEditMode(bool editing)
        {
            isEditing = editing;

            //TODO: 가이드 다시 넣기
            //placementGuide.SetActive(editing);
            
            unplacedRoot.SetActive(editing);

            swipeController.SetSwipeEnabled(!editing);

            placementController.SetEditMode(editing);
        }

        public void ToggleEditMode()
        {
            bool nextEditing = !isEditing;
            
            SetEditMode(nextEditing);

            if (!nextEditing)
                SaveManager.Instance?.Save();
        }
    }
}