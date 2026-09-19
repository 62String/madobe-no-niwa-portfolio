using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using WindowGarden.Core;
using WindowGarden.Plant;

namespace WindowGarden.UI
{
    public class CollectionUI : MonoBehaviour
    {
        private enum CollectionTab
        {
            Plant,
            Seed,
            Equipment
        }

        [Header("PlantTab")]
        public GameObject plantTab;
        public Button plantTabButton;
        
        [Header("Plant Pages")] 
        public TMP_Text pageLabel;
        public int cardsPerPage = 9;
        private int currentPage = 0;
        private bool hasStarted;

        [Header("Plant Page Controlls")] 
        public Button[] controllButtons;
        [SerializeField] private TMP_Text harvestCountText;
        
        [Header("SeedTab")]
        public GameObject seedTab;
        public Button seedTabButton;
        [SerializeField] private SeedCollectionListUI seedCollectionList;

        [Header("EquipmentTab")]
        public GameObject equipmentTab;
        public Button equipmentTabButton;
        [SerializeField] private EquipmentCollectionListUI equipmentCollectionList;

        [Header("Tab Button Sprites")]
        [SerializeField] private Sprite activeTabSprite;
        [SerializeField] private Sprite inactiveTabSprite;

        [Header("Spawner")] 
        public GameObject plantCardPrefab;
        public Transform plantGridParent;
        public PlantData[] plantDataList;

        [Header("Detail Panel")] 
        public GameObject detailPanel;

        private PopupAnimatorUI collectionAnimator;

        void Awake()
        {
            // OnEnable에서 팝업 애니메이션이 시작되기 전에 초기 탭 모양을 확정한다.
            RefreshReferences();
            ApplyTabVisualState(CollectionTab.Plant);
        }

        void Start()
        {
            RefreshReferences();
            ShowTab(CollectionTab.Plant);

            if (plantTabButton != null)
                plantTabButton.onClick.AddListener(() => ShowTab(CollectionTab.Plant));

            if (seedTabButton != null)
                seedTabButton.onClick.AddListener(() => ShowTab(CollectionTab.Seed));

            if (equipmentTabButton != null)
                equipmentTabButton.onClick.AddListener(() => ShowTab(CollectionTab.Equipment));
            
            if (controllButtons != null && controllButtons.Length >= 2)
            {
                controllButtons[0].onClick.AddListener(PrevPage);
                controllButtons[1].onClick.AddListener(NextPage);
            }
            
            currentPage = 0;
            ShowPage(currentPage);

            hasStarted = true;
        }

        void ShowTab(CollectionTab tab)
        {
            bool isPlant = tab == CollectionTab.Plant;
            bool isSeed = tab == CollectionTab.Seed;
            bool isEquipment = tab == CollectionTab.Equipment;

            if (!isEquipment && equipmentCollectionList != null)
                equipmentCollectionList.CloseItemInfo();

            ApplyTabVisualState(tab);

            if (isPlant)
            {
                ShowPage(currentPage);
            }
            else if (isSeed)
            {
                RefreshSeedCollectionList();
            }
            else
            {
                RefreshEquipmentCollectionList();
            }
        }

        private void ApplyTabVisualState(CollectionTab tab)
        {
            bool isPlant = tab == CollectionTab.Plant;
            bool isSeed = tab == CollectionTab.Seed;
            bool isEquipment = tab == CollectionTab.Equipment;

            if (plantTab != null)
                plantTab.SetActive(isPlant);

            if (seedTab != null)
                seedTab.SetActive(isSeed);

            if (equipmentTab != null)
                equipmentTab.SetActive(isEquipment);

            if (harvestCountText != null)
                harvestCountText.gameObject.SetActive(isPlant);

            if (plantTabButton != null)
                plantTabButton.interactable = !isPlant;

            if (seedTabButton != null)
                seedTabButton.interactable = !isSeed;

            if (equipmentTabButton != null)
                equipmentTabButton.interactable = !isEquipment;

            RefreshTabButtonSprites(isPlant, isSeed, isEquipment);
        }
        
        void ShowPage(int index)
        {
            if (plantGridParent == null)
                return;

            List<PlantData> harvestedPlants = GetHarvestedPlants();

            int pageCount = Mathf.Max(1, Mathf.CeilToInt((float)harvestedPlants.Count / Mathf.Max(1, cardsPerPage)));

            index = Mathf.Clamp(index, 0, pageCount - 1);
            currentPage = index;
            
            foreach (Transform child in plantGridParent)
            {
                Destroy(child.gameObject);
            }
            
            int start = index * cardsPerPage;
            //현재 페이지 몫만 깔기
            for (int i = 0; i < cardsPerPage; i++)
            {
                int dataIndex = start + i;
                
                if (dataIndex >= harvestedPlants.Count)
                    break;
                
                GameObject card = Instantiate(plantCardPrefab, plantGridParent);
                PlantCardUI cardUI = card.GetComponent<PlantCardUI>();
                
                if (cardUI != null)
                    cardUI.Setup(harvestedPlants[dataIndex], detailPanel);
            }
            
            if (pageLabel != null)
                pageLabel.text = $"{index + 1} / {pageCount}";

            if (controllButtons != null && controllButtons.Length >= 2)
            {
                controllButtons[0].interactable = index > 0;
                controllButtons[1].interactable = index < pageCount - 1;
            }

            if (harvestCountText != null)
            {
                int harvestCount = SaveManager.Instance != null
                    ? SaveManager.Instance.HarvestRecordCount
                    : 0;

                harvestCountText.text = $"{harvestCount}그루 수확";
            }
        }

        private int PageCount
        {
            get
            {
                int count = GetHarvestedPlants().Count;

                return Mathf.Max(1, Mathf.CeilToInt((float)count / Mathf.Max(1, cardsPerPage)));
            }
        }
        
        void NextPage()
        {
            if (currentPage < PageCount - 1)
            {
                currentPage ++;
                ShowPage(currentPage);
            }

            Debug.Log("NetxPage 호출");
        }

        void PrevPage()
        {
            if (currentPage > 0)
            {
                currentPage --;
                ShowPage(currentPage);    
            }
        }

        void RefreshSeedCollectionList()
        {
            if (seedCollectionList == null && seedTab != null)
            {
                seedCollectionList = seedTab.GetComponent<SeedCollectionListUI>();
                if (seedCollectionList == null)
                    seedCollectionList = seedTab.AddComponent<SeedCollectionListUI>();
            }

            if (seedCollectionList != null)
                seedCollectionList.Refresh();

            HidePlantPageControls();
        }

        void RefreshEquipmentCollectionList()
        {
            if (equipmentCollectionList == null && equipmentTab != null)
            {
                equipmentCollectionList = equipmentTab.GetComponent<EquipmentCollectionListUI>();
                if (equipmentCollectionList == null)
                    equipmentCollectionList = equipmentTab.AddComponent<EquipmentCollectionListUI>();
            }

            if (equipmentCollectionList != null)
                equipmentCollectionList.Refresh();

            HidePlantPageControls();
        }

        private void HidePlantPageControls()
        {
            if (pageLabel != null)
                pageLabel.text = "";

            if (controllButtons != null)
            {
                foreach (Button button in controllButtons)
                {
                    if (button != null)
                        button.interactable = false;
                }
            }
        }

        private void RefreshReferences()
        {
            if (collectionAnimator == null)
            {
                collectionAnimator = GetComponent<PopupAnimatorUI>();
                if (collectionAnimator == null)
                    collectionAnimator = gameObject.AddComponent<PopupAnimatorUI>();
            }

            if (equipmentTab == null)
                equipmentTab = FindChildByName(transform, "EquipmentTab")?.gameObject;

            if (equipmentTabButton == null)
            {
                Transform buttonTransform = FindChildByName(transform, "Collection Equipment")
                    ?? FindChildByName(transform, "EquipmentButton");

                if (buttonTransform != null)
                    equipmentTabButton = buttonTransform.GetComponent<Button>();
            }

            ConfigureTabButtonSprites(plantTabButton);
            ConfigureTabButtonSprites(seedTabButton);
            ConfigureTabButtonSprites(equipmentTabButton);
        }

        private void RefreshTabButtonSprites(bool isPlant, bool isSeed, bool isEquipment)
        {
            SetTabButtonSprite(plantTabButton, isPlant);
            SetTabButtonSprite(seedTabButton, isSeed);
            SetTabButtonSprite(equipmentTabButton, isEquipment);
        }

        private void ConfigureTabButtonSprites(Button button)
        {
            if (button == null)
                return;

            // Android keeps the last touched Button selected. ShowTab controls
            // the tab image explicitly, so automatic Sprite Swap is unnecessary.
            button.transition = Selectable.Transition.None;
        }

        private void SetTabButtonSprite(Button button, bool isActive)
        {
            if (button == null || button.image == null)
                return;

            Sprite sprite = isActive ? activeTabSprite : inactiveTabSprite;
            if (sprite == null)
                return;

            button.image.sprite = sprite;
            button.image.overrideSprite = sprite;

            // 팝업이 닫힐 때 상위 CanvasGroup이 모든 버튼을 Disabled로 만든다.
            // 이때도 현재 선택된 탭만 활성 이미지가 유지되도록 버튼별로 지정한다.
        }

        private Transform FindChildByName(Transform root, string childName)
        {
            if (root == null)
                return null;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == childName)
                    return child;
            }

            return null;
        }

        private List<PlantData> GetHarvestedPlants()
        {
            var result = new List<PlantData>();

            SaveManager saveManager = SaveManager.Instance;
            GameManager gameManager = GameManager.Instance;
            
            if (saveManager == null)
                return result;

            PlantData[] sourcePlants =
                gameManager != null &&
                gameManager.availablePlants != null &&
                gameManager.availablePlants.Length > 0
                    ? gameManager.availablePlants
                    : plantDataList;

            if (sourcePlants == null)
                return result;

            foreach (PlantData plantData in sourcePlants)
            {
                if (plantData == null ||
                    string.IsNullOrEmpty(plantData.plantId))
                {
                    continue;
                }
                
                if (saveManager.HasHarvestRecord(plantData.plantId))
                    result.Add(plantData);
            }

            return result;
        }

        private void OnEnable()
        {
            RefreshReferences();

            if (collectionAnimator != null)
                collectionAnimator.Show();

            if (!hasStarted)
                return;
            
            ShowPage(currentPage);
        }

        public void HideCollection()
        {
            if (equipmentCollectionList != null)
                equipmentCollectionList.CloseItemInfo();

            RefreshReferences();

            if (collectionAnimator != null)
                collectionAnimator.Hide();
            else
                gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (equipmentCollectionList != null)
                equipmentCollectionList.CloseItemInfo(true);
        }
    }
}
