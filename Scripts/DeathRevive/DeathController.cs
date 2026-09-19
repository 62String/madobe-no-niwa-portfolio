using System;
using TMPro;
using Unity.Multiplayer.Center.Common;
using UnityEngine;
using UnityEngine.UI;
using WindowGarden.Events;
using WindowGarden.Core;

namespace WindowGarden.UI
{
    public class DeathController : MonoBehaviour
    {
        
        [SerializeField] private SpriteRenderer plantRenderer;
        [SerializeField] private GameObject warningBadge;
        [SerializeField] private StatusPopupUI statusPopup;
        [SerializeField] private GameObject clickBlocker;
        [SerializeField] private Button deathConfirmButton;

        [Header("응급키트 관련")] 
        [SerializeField] private string emergencyKitItemId = "2";
        [SerializeField] private int fallbackEmergencyKitPrice = 100;
        [SerializeField] private float reviveVitality = 40f;
        private bool _useKit;

        [Header("죽음 팝업 관련")] 
        [SerializeField] private GameObject deathRevivePopup;
        [SerializeField] private Button actionButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private TMP_Text popupNameText;
        [SerializeField] private TMP_Text popupDescText;
        [SerializeField] private TMP_Text actionButtonLabel;
        private const string DeathBody = "꾸준한 관리가 없어 식물이 끝내 시들었습니다.";
        
        [Header("최초 회생 이벤트")]
        [SerializeField] private EventData reviveEvent;
        [SerializeField] private bool firstReviveUsed;
        [SerializeField] private EventCardUI reviveEventCardUI;
        
        
        void Start()
        {
            PlantManager.Instance.OnPlantDied += HandleDeath;

            if (deathConfirmButton != null)
            {
                deathConfirmButton.onClick.RemoveListener(OnDeathConfirmClicked);
                deathConfirmButton.onClick.AddListener(OnDeathConfirmClicked);
            }
            
            actionButton.onClick.AddListener(() => TryRevive(_useKit));
            cancelButton.onClick.AddListener(ShowFinalDeath);
            confirmButton.onClick.AddListener(OnFinalConfirm);

            if (EventManager.Instance != null)
                EventManager.Instance.OnEventResolved += OnEventResolved;
        }
        void OnDestroy()
        {
            if (PlantManager.Instance != null)
                PlantManager.Instance.OnPlantDied -= HandleDeath;

            if (EventManager.Instance != null)
                EventManager.Instance.OnEventResolved -= OnEventResolved;
        }

        void Update()
        {
            RefreshColor();
            RefreshClickBlocker();
        }

        void HandleDeath()
        {
            if (statusPopup == null) return;
            
            statusPopup.Hide();
            
            if (deathConfirmButton != null)
                deathConfirmButton.gameObject.SetActive(true);
            
            Debug.Log("[DeathController] 죽음 신호 받음!");
        }

        void RefreshColor()
        {
            if (plantRenderer == null) return;
            var pm = PlantManager.Instance;
            if (pm == null || !pm.HasPlant)
            {
                plantRenderer.color = Color.white;
                if (warningBadge != null) warningBadge.SetActive(false);
                return;
            }
                

            if (pm.IsDead)
                plantRenderer.color = new Color(0.4f, 0.4f, 0.4f);
            else if (pm.State.vitality <= 30f)
                plantRenderer.color = new Color(1f, 0.6f, 0.6f);
            else plantRenderer.color = Color.white;

            if (warningBadge != null)
                warningBadge.SetActive(pm.State.vitality <= 30f);
        }

        void RefreshClickBlocker()
        {
            if (clickBlocker == null) return;
            
            var pm = PlantManager.Instance;

            bool shouldBlock = pm != null && pm.State != null && pm.IsDead;
            clickBlocker.SetActive(shouldBlock);
        }

        public void OnDeathConfirmClicked()
        {
            if (!firstReviveUsed && reviveEvent != null && EventManager.Instance != null)
            {
                firstReviveUsed = true;
                
                if (deathConfirmButton != null)
                    deathConfirmButton.gameObject.SetActive(false);
                
                // TODO: TriggerForTest는 실제 강제 이벤트 발동 용도로도 사용 중.
                // 추후 EventManager에 TriggerForced 같은 런타임용 래퍼를 추가해 이름을 정리할 필요 있는지 확인 필요
                EventManager.Instance.TriggerForTest(reviveEvent);
                
                if (reviveEventCardUI != null)
                    reviveEventCardUI.ShowCard(reviveEvent);
                
                return;
            }
            
            var inventory = ItemInventoryManager.GetOrCreate();
            int price = GetEmergencyKitPrice();

            popupNameText.text = $"[{PlantManager.Instance.CurrentPlantData.displayName}] 죽음";

            if (inventory.HasItem(emergencyKitItemId, 1))
            {
                ShowReviveOption(true);
                Debug.Log("[DeathController] 1번팝업: 응급 키트 보유, 사용하기/ 아니오");
                return;
            }

            else if (inventory.Money >= price)
            {
                ShowReviveOption(false);
                Debug.Log("[DeathController] 2번 팝업: 응급 키트 없음, 구매 가능, 구매하기/ 아니오");
                return;
            }

            else
            {
                ShowFinalDeath();
                Debug.Log("[DeathController] 3번 팝업: 응급 키트 없음, 돈 부족, 확인");
            }
        }

        private int GetEmergencyKitPrice()
        {
            var itemData = GameManager.Instance != null
                ? GameManager.Instance.GetItemData(emergencyKitItemId)
                : null;

            int price = itemData != null ? itemData.price : fallbackEmergencyKitPrice;
            return Mathf.Max(0, price);
        }

        void ShowReviveOption(bool useKit)
        {
            _useKit = useKit; // TryRevive가 볼 플래그
            
            deathRevivePopup.SetActive(true);
            
            actionButton.gameObject.SetActive(true);
            cancelButton.gameObject.SetActive(true);
            confirmButton.gameObject.SetActive(false);

            actionButtonLabel.text = useKit ? "사용하기" : "구매하기";
            Debug.Log($"[ShowReviveOption] useKit = {useKit}, 라벨세팅={actionButtonLabel.text}, 대상오브젝트={actionButtonLabel.name}");
            
            string question = useKit
                ? "[응급 키트]로 식물을 살리시겠습니까?"
                : "[응급 키트]를 구매해 식물을 살리시겠습니까?";
            popupDescText.text = DeathBody + "\n" + question;
        }

        void ShowFinalDeath()
        {
            deathRevivePopup.SetActive(true);
            
            actionButton.gameObject.SetActive(false);
            cancelButton.gameObject.SetActive(false);
            confirmButton.gameObject.SetActive(true);
            
            popupDescText.text = DeathBody + "\n다음 식물은 더욱 정성껏 돌봐주세요.";
        }

        void TryRevive(bool useKit)
        {
            var inventory = ItemInventoryManager.GetOrCreate();
            bool ok = useKit
                ? inventory.TryConsumeItem(emergencyKitItemId, 1)
                : inventory.TrySpendMoney(GetEmergencyKitPrice());
            
            if(ok)
                PlantManager.Instance.RevivePlant(reviveVitality);
        }

        void OnFinalConfirm()
        {
            PlantManager.Instance.ClearPlant();
        }

        void OnEventResolved(EventData resolvedEvent, int choiceIndex, string resultMessage)
        {
            if (reviveEvent == null) return;
            
            if (PlantManager.Instance == null) return;
            if (resolvedEvent != reviveEvent) return;
            
            PlantManager.Instance.RevivePlant(reviveVitality);
        }

        public bool GetFirstReviveUsedForSave()
        {
            return firstReviveUsed;
        }

        public void SetFirstReviveUsedFromSave(bool value)
        {
            firstReviveUsed = value;
        }
    }
}
