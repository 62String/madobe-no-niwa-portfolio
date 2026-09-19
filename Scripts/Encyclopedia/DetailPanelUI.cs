using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WindowGarden.Plant;
using WindowGarden.Core;

public class DetailPanelUI : MonoBehaviour
{
    [Header("상세정보")] [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text generationText;
    [SerializeField] private Image plantImage;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text periodText;
    [SerializeField] private TMP_Text traitText;


    void Start()
    {

    }

    // TODO: 세대/ 기간/ 특성 - 코어 데이터(수확기록) 연결 후 채울 예정
    public void Setup(PlantData data, HarvestRecordData record)
    {
        if (data == null || record == null) return;

        if (titleText != null)
            titleText.text = data.displayName;

        if (generationText != null)
            generationText.text = $"{record.generation}세대";

        if (descriptionText != null)
            descriptionText.text = data.description;

        //스프라이트 안전 접근
        if (data.growthStageSprites != null && data.growthStageSprites.Length > 0)
        {
            plantImage.sprite = data.growthStageSprites[data.growthStageSprites.Length - 1];
        }
        else
        {
            plantImage.sprite = null; // 없으면 빈 이미지 
        }

        if (periodText != null)
            periodText.text = GetperiodText(record);

        if (traitText != null)
            traitText.text = GetTraitText(record.traitIds);
    }

    private string GetperiodText(HarvestRecordData record)
    {
        if (record.plantedAtUtcTicks <= 0 || record.harvestedAtUtcTicks <= 0)
            return "-";

        DateTime plantedAt = new DateTime(record.plantedAtUtcTicks, DateTimeKind.Utc).ToLocalTime();
        DateTime harvestedAt = new DateTime(record.harvestedAtUtcTicks, DateTimeKind.Utc).ToLocalTime();

        return $"{plantedAt:yyyy년 M월 d일} - {harvestedAt:yyyy년 M월 d일}";
    }

    private string GetTraitText(List<string> traitIds)
    {
        if (traitIds == null || traitIds.Count == 0)
            return "특성없음";

        var traitDataList = GameManager.Instance?.GetTraitDataList();
        var traitNames = new List<string>();

        foreach (string traitId in traitIds)
        {
            string traitName = traitId;

            if (traitDataList != null)
            {
                foreach (TraitData traitData in traitDataList)
                {
                    if (traitData != null && traitData.traitId == traitId)
                    {
                        traitName = traitData.displayName;
                        break;
                    }
                }
            }

            traitNames.Add(traitName);
        }

        return string.Join(",", traitNames);
    }
}
