using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using WindowGarden.Core;
using WindowGarden.Plant;

public class HarvestController : MonoBehaviour
{
    [SerializeField] private Button harvestButton;
    [SerializeField] private HarvestPopupUI harvestPopup;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetupHarvestButton();
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        HandleHarvestDebugInput();
#endif
        UpdateHarvestButton();
    }
    
#if UNITY_EDITOR
    void HandleHarvestDebugInput()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.lKey.wasPressedThisFrame)
            return;

        var state = GameManager.Instance?.PlantState;
        if (state == null)
        {
            Debug.LogWarning("[HarvestController][DEBUG] PlantState가 없어 완전 성장 디버그를 적용할 수 없습니다.");
            return;
        }

        state.growthStage = PlantState.MaxGrowthStage;
        state.growthProgress = 100f;
        state.Clamp();
        
        Debug.Log($"[HarvestController] generation={state.generation}, " +
                  $"stage={state.growthStage}," +
                  $"progress={state.growthProgress}, " +
                  $"harvestReady = {state.harvestReady}");
    }
#endif
    void SetupHarvestButton()
    {
        if (harvestButton == null) return;

        harvestButton.onClick.AddListener(OnHarvestClicked);
        harvestButton.gameObject.SetActive(false);
    }

    void OnHarvestClicked()
    {
        Debug.Log("[HarvestController] 수확 버튼 클릭");

        harvestButton.gameObject.SetActive(false);

        var data = PlantManager.Instance?.CurrentPlantData;
        var state = PlantManager.Instance?.State;

        if (data == null || state == null) return;

        harvestPopup.Open(data, state);
    }

    void UpdateHarvestButton()
    {
        if (harvestButton == null) return;

        var state = GameManager.Instance?.PlantState;

        bool popupOpen = harvestPopup != null && harvestPopup.gameObject.activeSelf;
        
        harvestButton.gameObject.SetActive(state != null && state.harvestReady && !popupOpen);
    }
}
