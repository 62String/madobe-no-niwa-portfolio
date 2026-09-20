using System.Collections.Generic;
using UnityEngine;

namespace WindowGarden.UI
{
    public class UIInputBlocker : MonoBehaviour
    {
        private static readonly HashSet<UIInputBlocker> blockers = new();

        [SerializeField] private CanvasGroup canvasGroup;

        public static bool IsBlocked
        {
            get
            {
                blockers.RemoveWhere(blocker => blocker == null);

                foreach (UIInputBlocker blocker in blockers)
                {
                    if (blocker.IsCurrentlyBlocking())
                    {
                        //Debug.Log(
                        //    $"[UIInputBlocker] 현재 차단 중: {blocker.gameObject.name}",
                        //    blocker);
                            
                        
                        return true;
                    }
                        
                }

                return false;
            }
        }

        void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

        }

        void OnEnable()
        {
            blockers.Add(this);
        }

        void OnDisable()
        {
            blockers.Remove(this);
        }

        private bool IsCurrentlyBlocking()
        {
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
                return false;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                return true;

            return canvasGroup.alpha > 0.01f && canvasGroup.blocksRaycasts;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetBlockers()
        {
            blockers.Clear();
        }
    }
}