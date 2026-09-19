using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using WindowGarden.Core;
using WindowGarden.Core.Notifications;
using WindowGarden.Plant;
using WindowGarden.UI;

public class HarvestPopupUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Image plantImage;
    [SerializeField] private TMP_Text guideText;
    [SerializeField] private Button[] traitButtons;
    [SerializeField] private Button confirmButton;
    [SerializeField] private PopupAnimatorUI popupAnimator;
    [SerializeField] private StatusPopupUI statusPopup;

    [Header("Harvest Animation")] 
    [SerializeField] private PlantView plantView;
    [SerializeField] private RectTransform collectionButtonTarget;
    [SerializeField] private float harvestFlyDuration = 0.75f;
    [SerializeField, Range(0f, 0.3f)] private float harvestEndScale = 0.05f;

    private bool isHarvesting;
    
    private const int MaxGeneration = 3;
    private PlantState currentState;

    public void Open(PlantData data, PlantState state)
    {
        currentState = state;
        confirmButton.interactable = true;
        
        titleText.text = $"{data.displayName}을(를) 수확했다!";
        plantImage.sprite = data.growthStageSprites[data.growthStageSprites.Length - 1];
        
        bool isFinalGen = state.generation >= MaxGeneration;
        
        guideText.text = isFinalGen
            ? "마지막 세대입니다"
            : $"{state.generation + 1}세대 씨앗을 획득합니다";
        
        HideTraitButtons();
        popupAnimator.Show();

        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnConfirm);
    }

    private void HideTraitButtons()
    {
        if (traitButtons == null)
            return;

        foreach (Button traitButton in traitButtons)
        {
            if (traitButton != null)
                traitButton.gameObject.SetActive(false);
        }
    }
    
    private IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        popupAnimator.Hide();
    }

    public void OnConfirm()
    {
        if (isHarvesting || currentState == null)
            return;
        
        bool isFinalGen = currentState.generation >= MaxGeneration;
        
        // 3세대가 아닐 때만 씨앗 생산
        if (!isFinalGen)
        {
            var seed = new SeedInstanceData(
                currentState.plantId,
                generation: currentState.generation + 1);
            SeedInventoryManager.Instance.AddSeed(seed);
        }
        
        confirmButton.interactable = false;
        
        SaveManager saveManager = SaveManager.Instance;

        if (saveManager != null)
        {
            var record = new HarvestRecordData
            {
                plantId = currentState.plantId,
                generation = currentState.generation,
                plantedAtUtcTicks = currentState.plantedAtUtcTicks,
                harvestedAtUtcTicks = System.DateTime.UtcNow.Ticks
            };
            
            saveManager.AddHarvestRecord(record);
        }

        isHarvesting = true;
        
        if (statusPopup != null)
            statusPopup.Hide(); // 상태카드 닫기

        StartCoroutine(PlayHarvestAnimation(saveManager));

        Debug.Log(
            $"[HarvestPopupUI] 수확 확정: " +
            $"plant={currentState.plantId}, " +
            $"generation={currentState.generation}");

    }

    private IEnumerator PlayHarvestAnimation(SaveManager saveManager)
    {
        CanvasGroup popupCanvasGroup = GetComponent<CanvasGroup>();

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
            popupCanvasGroup.interactable = false;
            popupCanvasGroup.blocksRaycasts = false;
        }

        Camera worldCamera = Camera.main;

        if (plantView == null || collectionButtonTarget == null || worldCamera == null)
        {
            CompleteHarvest(saveManager);
            yield break;
        }

        plantView.SetPlantPointAlignment(false);

        Transform plantTransform = plantView.transform;

        Vector3 startPosition = plantTransform.position;
        
        Vector3 startScale = plantTransform.localScale;

        Canvas targetCanvas = collectionButtonTarget.GetComponentInParent<Canvas>();

        Camera uiCamera =
            targetCanvas != null &&
            targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? targetCanvas.worldCamera
                : null;

        Vector2 targetScreenPosition =
            RectTransformUtility.WorldToScreenPoint(uiCamera, collectionButtonTarget.position);

        float plantDepth = worldCamera.WorldToScreenPoint(startPosition).z;
        
        Vector3 targetWorldPosition = worldCamera.ScreenToWorldPoint(
            new Vector3(
                targetScreenPosition.x,
                targetScreenPosition.y,
                plantDepth));

        targetWorldPosition.z = startPosition.z;

        Vector3 targetScale = startScale * harvestEndScale;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, harvestFlyDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            
            plantTransform.position = 
                Vector3.Lerp(
                    startPosition,
                    targetWorldPosition,
                    eased);
            
            plantTransform.localScale = 
                Vector3.Lerp(
                    startScale,
                    targetScale,
                    eased);

            yield return null;
        }
        PlantManager.Instance?.ClearPlant();

        plantTransform.position = startPosition;
        plantTransform.localScale = startScale;
        plantView.SetPlantPointAlignment(true);
        plantView.Refresh();
        
        CompleteHarvest(saveManager);
    }

    private void CompleteHarvest(SaveManager saveManager)
    {
        if (PlantManager.Instance != null && PlantManager.Instance.HasPlant)
        {
            PlantManager.Instance.ClearPlant();
        }

        GrowthNotificationScheduler.MarkHarvestCompleted();
        
        saveManager?.Save();

        isHarvesting = false;
        popupAnimator.HideImmediate();
    }
}
