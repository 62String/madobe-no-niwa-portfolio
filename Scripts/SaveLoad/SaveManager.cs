using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using WindowGarden.UI;
using WindowGarden.Events;

namespace WindowGarden.Core
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance {get; private set;}
        public bool HasSaveFile => System.IO.File.Exists(SavePath);
        public int HarvestRecordCount => harvestRecords.Count;
        
#if DEMO_BUILD
        private const string SaveFileName = "save.json";
#else
        private const string SaveFileName = "save_release.json";
#endif
        private string SavePath => 
            System.IO.Path.Combine(Application.persistentDataPath, SaveFileName);
        private List<HarvestRecordData> harvestRecords = new();
        private List<GalleryPlantStateData> galleryPlants = new();
        private bool suppressLifecycleSave;
        private bool firstReviveUsed;
        private string dailyRewardCycleStartDate;
        private string dailyRewardLastClaimDate;
        private int dailyRewardClaimedMask;
        private NotificationStateData notificationState = new();

        public string DailyRewardCycleStartDate => dailyRewardCycleStartDate;
        public string DailyRewardLastClaimDate => dailyRewardLastClaimDate;
        public int DailyRewardClaimedMask => dailyRewardClaimedMask;
        public NotificationStateData NotificationState => notificationState;

        void Awake()
        {
            if (Instance != null && Instance != this) {Destroy(gameObject); return; }

            Instance = this;
            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            Load();
        }

        void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame) Save();
            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                Debug.Log("[SaveManager] F9 입력 감지");
                ResetSave();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                Save();
        }

        private void OnApplicationQuit()
        {
            if (!suppressLifecycleSave)
                Save();
        }

        public void Save()
        {
            suppressLifecycleSave = false;
            
            // 1) 빈 SaveDAta
            var data = new SaveData();
            
            // 2) 각 매니저에서 모으기
            var pm = PlantManager.Instance;
            data.hasPlant = pm != null && pm.HasPlant;
            data.plantId = pm != null ? pm.CurrentPlantData?.plantId : null;
            data.PlantState = pm != null ? pm.State : null;

            data.seeds = SeedInventoryManager.GetOrCreate().CreateSaveList();
            data.items = ItemInventoryManager.GetOrCreate().CreateSaveList();
            data.harvestRecords = new List<HarvestRecordData>(harvestRecords);
            data.galleryPlants = new List<GalleryPlantStateData>(galleryPlants);
            
            var deathController = FindFirstObjectByType<DeathController>();
            
            if (deathController != null)
            {
                firstReviveUsed = deathController.GetFirstReviveUsedForSave();
            }

            data.firstReviveUsed = firstReviveUsed;

            data.lastSavedUtcTicks = System.DateTime.UtcNow.Ticks;
            data.weatherState = GameManager.Instance?.WeatherState;
            data.dailyRewardCycleStartDate = dailyRewardCycleStartDate;
            data.dailyRewardLastClaimDate = dailyRewardLastClaimDate;
            data.dailyRewardClaimedMask = dailyRewardClaimedMask;
            data.notificationState = notificationState;
            
            // 3) JSON -> 파일
            string json = JsonUtility.ToJson(data, true);
            System.IO.File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveManager] 저장 완료 : {SavePath} / 수확기록 {data.harvestRecords.Count} 개" );
        }

        public void Load()
        {
            // 1) 저장 파일이 없으면 그냥 나감 (첫 실행 = 저장 없음)
            if (!System.IO.File.Exists(SavePath)) return;
            
            // 2) 파일 읽어서 SaveData로 역직렬화
            string json = System.IO.File.ReadAllText(SavePath);
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return;
            firstReviveUsed = data.firstReviveUsed;
            dailyRewardCycleStartDate = data.dailyRewardCycleStartDate;
            dailyRewardLastClaimDate = data.dailyRewardLastClaimDate;
            dailyRewardClaimedMask = data.dailyRewardClaimedMask;
            notificationState = data.notificationState ?? new NotificationStateData();

            if (data.weatherState != null)
                GameManager.Instance?.SetWeather(data.weatherState);

            harvestRecords = data.harvestRecords ?? new List<HarvestRecordData>();
            galleryPlants = data.galleryPlants ?? new List<GalleryPlantStateData>();
            Debug.Log($"[SaveManager] 수확기록 로드 : {harvestRecords.Count}개");
            
            // 3) 각 매니저에 분배 (Save에서 CreateSaveList 했던 것의 반대 = SetXXX)
            SeedInventoryManager.GetOrCreate().SetSeeds(data.seeds);
            ItemInventoryManager.GetOrCreate().SetItems(data.items);

            var deathController = FindFirstObjectByType<DeathController>();
            if (deathController != null)
                deathController.SetFirstReviveUsedFromSave(firstReviveUsed);
            
            // 4) 식물 복원
            if (data.hasPlant && !string.IsNullOrEmpty(data.plantId))
            {
                var plantData = GameManager.Instance.GetPlantData(data.plantId);
                if (plantData != null)
                {
                    PlantManager.Instance.Initialize(data.PlantState,plantData);
                }
            }
            Debug.Log("[SaveManager] 로드완료");
        }

        public void ResetSave()
        {
            HardReset();
        }

        public void HardReset()
        {
            Debug.Log($"[SaveManager][HardReset][1/8] 초기화 시작: {SavePath}");

            GameManager gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError(
                    "[SaveManager][HardReset] GameManager가 없어 초기화를 중단합니다.");
                return;
            }

            SeedInventoryManager seedManager =
                SeedInventoryManager.GetOrCreate();

            ItemInventoryManager itemManager =
                ItemInventoryManager.GetOrCreate();

            suppressLifecycleSave = true;

            try
            {
                if (System.IO.File.Exists(SavePath))
                {
                    System.IO.File.Delete(SavePath);
                    Debug.Log(
                        $"[SaveManager][HardReset][2/8] 저장 파일 삭제: {SavePath}");
                }
                else
                {
                    Debug.Log(
                        $"[SaveManager][HardReset][2/8] 삭제할 저장 파일 없음: {SavePath}");
                }
            }
            catch (Exception exception)
            {
                suppressLifecycleSave = false;

                Debug.LogError(
                    $"[SaveManager][HardReset] 저장 파일 삭제 실패:" +
                    $"{exception.Message}");

                return;
            }

            harvestRecords.Clear();
            galleryPlants.Clear();
            firstReviveUsed = false;
            dailyRewardCycleStartDate = null;
            dailyRewardLastClaimDate = null;
            dailyRewardClaimedMask = 0;
            notificationState = new NotificationStateData();

            Debug.Log(
                "[SaveManager][HardReset][3/8] " +
                "수확 기록, 모아보기 배치, 부활 기록 초기화");

            PlantManager.Instance?.ClearPlant();
            Debug.Log("[SaveManager][HardReset][4/8] 현재 식물 제거");

            var defaultSeeds = gameManager.GetDefaultSeeds();
            seedManager.SetSeeds(defaultSeeds);
            Debug.Log($"[SaveManager][HardReset][5/8]" +
                      $"기본 씨앗 복원: {defaultSeeds.Count}개");

            var defaultItems = gameManager.GetDefaultItems();
            itemManager.SetItems(defaultItems);
            Debug.Log(
                $"[SaveManager][HardReset][6/8]" +
                $"기본 아이템 복원: {defaultItems.Count}종");

            EventManager.Instance?.ResetRuntimeState();

            var deathController = FindFirstObjectByType<DeathController>();

            if (deathController != null)
                deathController.SetFirstReviveUsedFromSave(false);
            Debug.Log(
                "[SaveManager][HardReset][7/8]" +
                "이벤트 및 죽음 상태 초기화");

            Debug.Log(
                "[SaveManager][HardReset][8/8] Main 씬 재로드");

            SceneManager.LoadScene("Main");

        }

        public void AddHarvestRecord(HarvestRecordData record)
        {
            if (record == null || string.IsNullOrEmpty(record.plantId))
                return;
            
            harvestRecords.Add(record);
        }

        public void SetDailyRewardState(string cycleStartDate, string lastClaimDate, int claimedMask)
        {
            dailyRewardCycleStartDate = cycleStartDate;
            dailyRewardLastClaimDate = lastClaimDate;
            dailyRewardClaimedMask = claimedMask;
        }

        public bool HasHarvestRecord(string plantId)
        {
            return GetLatestHarvestRecord(plantId) != null;
        }

        public HarvestRecordData GetLatestHarvestRecord(string plantId)
        {
            if (string.IsNullOrEmpty(plantId))
                return null;

            for (int i = harvestRecords.Count - 1; i >= 0; i--)
            {
                HarvestRecordData record = harvestRecords[i];

                if (record != null && record.plantId == plantId)
                    return record;
            }
            return null;
        }

        public GalleryPlantStateData GetGalleryPlantState(string plantId)
        {
            if (string.IsNullOrEmpty(plantId))
                return null;
            for (int i = galleryPlants.Count - 1; i >= 0; i--)
            {
                GalleryPlantStateData data = galleryPlants[i];

                if (data != null && data.plantId == plantId)
                    return data;
            }
            return null;
        }

        public void SetGalleryPlantState(GalleryPlantStateData state)
        {
            if (state == null || string.IsNullOrEmpty(state.plantId))
                return;

            GalleryPlantStateData existing =
                GetGalleryPlantState(state.plantId);
            
            if (existing == null)
            {
                galleryPlants.Add(state);
                return;
            }

            existing.isPlaced = state.isPlaced;
            existing.slotIndex = state.slotIndex;
            existing.normalizedX = state.normalizedX;
            existing.normalizedY = state.normalizedY;
            existing.listOrder = state.listOrder;
            existing.sortingOrder = state.sortingOrder;
        }
    }
}
