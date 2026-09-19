using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using WindowGarden.Plant;
using WindowGarden.Core;

namespace WindowGarden.UI
{
    public class PlantCardUI : MonoBehaviour, IPointerClickHandler
    {
        [Header("DatailPanel")] 
        private GameObject detailPanel;
        private PlantData plantData;

        [SerializeField] 
        private TMP_Text nameText;

        [SerializeField] private TMP_Text generationText;

        [SerializeField] 
        private Image plantImage;
        [SerializeField] private TMP_Text lockedMarkText;
        
        private HarvestRecordData harvestRecordData;
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (harvestRecordData == null || detailPanel == null)
                return;

            DetailPanelUI panelUI = detailPanel.GetComponent<DetailPanelUI>();
            if (panelUI == null)
                return;
            
            detailPanel.SetActive(true);
            panelUI.Setup(plantData, harvestRecordData);
            Debug.Log("카드 클릭됨");
        }

        // TODO: 세대/ 기간/ 특성 - 코어 데이터(수확기록) 연결 후 채울 예정
        public void Setup(PlantData data, GameObject detail)
        {
            if (data == null) return;
            
            plantData = data;
            detailPanel = detail;
            harvestRecordData = SaveManager.Instance?.GetLatestHarvestRecord(data.plantId);

            bool isUnlocked = harvestRecordData != null;

            nameText.text = isUnlocked ? data.displayName : "?";

            plantImage.enabled = isUnlocked;

            if (lockedMarkText != null)
            {
                lockedMarkText.text = "?";
                lockedMarkText.gameObject.SetActive(!isUnlocked);
            }
            
            if (generationText != null)
            {
                generationText.text = isUnlocked
                    ? $"{harvestRecordData.generation}세대"
                    : "?";
            }
            
            if (!isUnlocked)
                return;
            
            // 스프라이트 안전 접근
            if (data.growthStageSprites != null && data.growthStageSprites.Length > 0)
            {
                plantImage.sprite = data.growthStageSprites[data.growthStageSprites.Length - 1];
            }
            else
            {
                plantImage.sprite = null; // 없으면 빈 이미지
            }
        }

    }    
}

