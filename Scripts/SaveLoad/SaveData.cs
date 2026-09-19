using System;
using System.Collections.Generic;
using WindowGarden.Items;
using WindowGarden.Plant;
using WindowGarden.Weather;

namespace WindowGarden.Core
{
    [Serializable]
    public class SaveData
    {
            public bool hasPlant;
            public string plantId;
            public PlantState PlantState;

            public List<SeedInstanceData> seeds = new();
            public List<ItemStackData> items = new();

            public bool firstReviveUsed;
            
            public long lastSavedUtcTicks;

            // Last successfully received weather, restored before the next API refresh.
            public WeatherState weatherState;

            // Daily attendance reward state (local calendar dates: yyyy-MM-dd).
            public string dailyRewardCycleStartDate;
            public string dailyRewardLastClaimDate;
            public int dailyRewardClaimedMask;

            public List<HarvestRecordData> harvestRecords = new();
            public List<GalleryPlantStateData> galleryPlants = new();
            public NotificationStateData notificationState = new();

            //TODO (회의 후: events(쿨다운), curtainClosed, dex(도감 수확 기록)
    }

    [Serializable]
    public class NotificationStateData
    {
        public long emptyPotSinceUtcTicks;
        public bool firstDeathNotificationUsed;
    }

    [Serializable]
    public class HarvestRecordData
    {
        public string plantId;
        public int generation;
        public List<string> traitIds = new();
        public long plantedAtUtcTicks;
        public long harvestedAtUtcTicks;
    }

    [Serializable]
    public class GalleryPlantStateData
    {
        public string plantId;
        public bool isPlaced;
        public int slotIndex = -1;
        
        public float normalizedX;
        public float normalizedY;
        public int listOrder;
        public int sortingOrder;
    }
}
