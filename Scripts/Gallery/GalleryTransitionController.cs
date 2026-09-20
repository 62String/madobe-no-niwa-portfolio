using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace WindowGarden.UI
{
    public class GalleryTransitionController : MonoBehaviour
    {
        [SerializeField] private RawImage sourceSnapshot;
        [SerializeField] private RectTransform transitionImage;
        [SerializeField] private float slideDuration = 0.3f;

        [SerializeField] private Image fadeImage;
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float blackHoldDuration = 0.12f;

        [SerializeField, Range(0f, 1f)] private float incomingDarkness = 0.65f;
        [SerializeField] private RawImage destinationSnapshot;

        [Header("Gradient Wipe")] 
        [SerializeField] private RectTransform gradientWipeRoot;
        [SerializeField] private RectTransform solidBlack;
        [SerializeField] private RectTransform leftGradient;
        [SerializeField] private RectTransform rightGradient;

        [SerializeField] private float gradientWidth = 240f;
        [SerializeField] private float coverDuration = 0.3f;
        [SerializeField] private float revealDuration = 0.3f;

        private static bool isTransitioning;

        private Texture2D capturedSourceTexture;
        private Texture2D capturedDestinationTexture;

        private string pendingSceneName;
        private float movementDirection;

        private Material runtimeFadeMaterial;

        private static readonly int ProgressPropertyId =
            Shader.PropertyToID("_Progress");

        private static readonly int DirectionPropertyId =
            Shader.PropertyToID("_Direction");

        private static readonly int BaseDarknessPropertyId =
            Shader.PropertyToID("_BaseDarkness");

        private static readonly int EffectModePropertyId =
            Shader.PropertyToID("_EffectMode");

        private static readonly int BlackPlateauPropertyId =
            Shader.PropertyToID("_BlackPlateau");


        public static bool IsTransitioning => isTransitioning;

        public void BeginTransition(
            string destinationSceneName,
            float direction)
        {
            if (isTransitioning)
            {
                Destroy(gameObject);
                return;
            }

            isTransitioning = true;
            pendingSceneName = destinationSceneName;
            movementDirection = Mathf.Sign(direction);

            Canvas transitionCanvas = GetComponent<Canvas>();
            if (transitionCanvas != null)
            {
                transitionCanvas.overrideSorting = true;
                transitionCanvas.sortingOrder = short.MaxValue;
            }

            DontDestroyOnLoad(gameObject);
            StartCoroutine(PlayTransitionSequence());
        }

        private bool PrepareDirectionalFadeMaterial()
        {
            Material sourceMaterial =
                fadeImage.material;

            if (sourceMaterial == null ||
                !sourceMaterial.HasProperty(ProgressPropertyId) ||
                !sourceMaterial.HasProperty(DirectionPropertyId) ||
                !sourceMaterial.HasProperty(BaseDarknessPropertyId))
            {
                Debug.LogError(
                    "[GalleryTransitionController]" +
                    "Directional Fade Material이 올바르게 연결되지 않았습니다.",
                    this);

                return false;
            }

            runtimeFadeMaterial =
                    Instantiate(sourceMaterial);

                runtimeFadeMaterial.name =
                    sourceMaterial.name + " (Runtime)";

                fadeImage.material =
                    runtimeFadeMaterial;

                fadeImage.type =
                    Image.Type.Simple;

                SetFadeImageAlpha(0f);

                runtimeFadeMaterial.SetFloat(
                    DirectionPropertyId,
                    movementDirection);

                runtimeFadeMaterial.SetFloat(
                    BaseDarknessPropertyId,
                    incomingDarkness);

                runtimeFadeMaterial.SetFloat(
                    ProgressPropertyId, 0f);

            return true;
        }

        private IEnumerator FadeShaderProgress(
            float startProgress, float endProgress)
        {
            if (runtimeFadeMaterial == null)
                yield break;

            if (fadeDuration <= 0f)
            {
                runtimeFadeMaterial.SetFloat(
                    ProgressPropertyId,
                    endProgress);

                yield break;
            }

            float elapsedTime = 0f;

            runtimeFadeMaterial.SetFloat(
                ProgressPropertyId,
                startProgress);

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(elapsedTime / fadeDuration);

                float easedProgress =
                    Mathf.SmoothStep(0f, 1f, progress);

                float currentProgress =
                    Mathf.Lerp(startProgress, endProgress, easedProgress);

                runtimeFadeMaterial.SetFloat(
                    ProgressPropertyId,
                    currentProgress);

                yield return null;
            }

            runtimeFadeMaterial.SetFloat(
                ProgressPropertyId, endProgress);
        }

        private void ReleaseRuntimeFadeMaterial()
        {
            if (runtimeFadeMaterial == null)
                return;

            Destroy(runtimeFadeMaterial);
            runtimeFadeMaterial = null;
        }

        private void SetFadeImageAlpha(float alpha)
        {
            Color color = fadeImage.color;
            color.a = Mathf.Clamp01(alpha);
            fadeImage.color = color;
        }

        private IEnumerator FadeImageAlpha(
            float startAlpha,
            float endAlpha)
        {
            if (fadeDuration <= 0f)
            {
                SetFadeImageAlpha(endAlpha);
                yield break;
            }

            float elapsedTime = 0f;

            SetFadeImageAlpha(startAlpha);

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsedTime / fadeDuration);

                float easedProgress =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        progress);

                float currentAlpha =
                    Mathf.Lerp(
                        startAlpha,
                        endAlpha,
                        easedProgress);

                SetFadeImageAlpha(currentAlpha);

                yield return null;
            }

            SetFadeImageAlpha(endAlpha);
        }

        private void ConfigureDirectionalFade()
        {
            fadeImage.type =
                Image.Type.Filled;

            fadeImage.fillMethod =
                Image.FillMethod.Horizontal;

            fadeImage.fillOrigin =
                movementDirection > 0f
                    ? (int)Image.OriginHorizontal.Left
                    : (int)Image.OriginHorizontal.Right;

            SetFadeImageAlpha(1f);
        }

        private IEnumerator FadeImageFill(
            float startAmount,
            float endAmount)
        {
            if (fadeDuration <= 0f)
            {
                fadeImage.fillAmount = endAmount;
                yield break;
            }

            float elapsedTime = 0f;
            fadeImage.fillAmount = startAmount;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(elapsedTime / fadeDuration);

                float easedProgress =
                    Mathf.SmoothStep(0f, 1f, progress);

                fadeImage.fillAmount =
                    Mathf.Lerp(startAmount, endAmount, easedProgress);

                yield return null;
            }

            fadeImage.fillAmount = endAmount;
        }

        private IEnumerator RevealSceneWithFade()
        {
            if (fadeDuration <= 0f)
            {
                fadeImage.fillAmount = 0f;
                SetFadeImageAlpha(0f);
                yield break;
            }

            float elapsedTime = 0f;

            fadeImage.fillAmount = 1f;
            SetFadeImageAlpha(1f);

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(elapsedTime / fadeDuration);

                float easedProgress =
                    Mathf.SmoothStep(0f, 1f, progress);

                fadeImage.fillAmount =
                    Mathf.Lerp(1f, 0f, easedProgress);

                float currentAlpha =
                    Mathf.Lerp(1f, 0f, easedProgress);

                SetFadeImageAlpha(currentAlpha);

                yield return null;
            }

            fadeImage.fillAmount = 0f;
            SetFadeImageAlpha(0f);
        }

        private void ConfigureGradientWipe()
        {
            Canvas.ForceUpdateCanvases();

            RectTransform overlayRect =
                (RectTransform)transform;

            float screenWidth =
                overlayRect.rect.width;

            float screenHeight =
                overlayRect.rect.height;

            Vector2 centerAnchor =
                new Vector2(0.5f, 0.5f);

            gradientWipeRoot.anchorMin = centerAnchor;
            gradientWipeRoot.anchorMax = centerAnchor;
            gradientWipeRoot.pivot = centerAnchor;
            gradientWipeRoot.anchoredPosition = Vector2.zero;
            gradientWipeRoot.sizeDelta =
                new Vector2(
                    screenWidth + gradientWidth * 2f,
                    screenHeight);

            ConfigureWipeElement(
                solidBlack,
                new Vector2(screenWidth, screenHeight),
                Vector2.zero);

            ConfigureWipeElement(
                leftGradient,
                new Vector2(gradientWidth, screenHeight),
                Vector2.left *
                (screenWidth * 0.5f + gradientWidth * 0.5f));

            ConfigureWipeElement(
                rightGradient,
                new Vector2(gradientWidth, screenHeight),
                Vector2.right *
                (screenWidth * 0.5f + gradientWidth * 0.5f));
        }

        private void ConfigureWipeElement(
            RectTransform target,
            Vector2 size,
            Vector2 position)
        {
            Vector2 centerAnchor =
                new Vector2(0.5f, 0.5f);

            target.anchorMin = centerAnchor;
            target.anchorMax = centerAnchor;
            target.pivot = centerAnchor;
            target.sizeDelta = size;
            target.anchoredPosition = position;
        }

        private IEnumerator MoveGradientWipe(
            Vector2 startPosition,
            Vector2 endPosition,
            float duration,
            bool easeIn)
        {
            gradientWipeRoot.anchoredPosition =
                startPosition;
            if (duration <= 0f)
            {
                gradientWipeRoot.anchoredPosition =
                    endPosition;

                yield break;
            }

            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(elapsedTime / duration);

                float easedProgress;

                if (easeIn)
                {
                    easedProgress =
                        progress * progress;
                }
                else
                {
                    float inverseProgress =
                        1f - progress;

                    easedProgress =
                        1f - inverseProgress * inverseProgress;
                }

                gradientWipeRoot.anchoredPosition =
                    Vector2.Lerp(
                        startPosition,
                        endPosition,
                        easedProgress);

                yield return null;
            }

            gradientWipeRoot.anchoredPosition =
                endPosition;
        }

        private IEnumerator CaptureSourceSnapshot()
        {
            sourceSnapshot.gameObject.SetActive(false);
            transitionImage.gameObject.SetActive(false);

            yield return new WaitForEndOfFrame();

            capturedSourceTexture =
                new Texture2D(
                    Screen.width,
                    Screen.height,
                    TextureFormat.RGB24,
                    false, false);
            capturedSourceTexture.ReadPixels(
                new Rect(0f, 0f, Screen.width, Screen.height),
                0, 0, false);

            capturedSourceTexture.Apply(false, false);

            sourceSnapshot.texture =
                capturedSourceTexture;

            Color snapshotColor =
                sourceSnapshot.color;

            snapshotColor.a = 1f;
            sourceSnapshot.color =
                snapshotColor;

            sourceSnapshot.rectTransform.anchoredPosition =
                Vector2.zero;

            sourceSnapshot.gameObject.SetActive(true);
        }

        private IEnumerator SlidePanels(
            RectTransform outgoingPanel,
            Vector2 outgoingStartPosition,
            Vector2 outgoingEndPosition,
            RectTransform incomingPanel,
            Vector2 incomingStartPosition,
            Vector2 incomingEndPosition)
        {
            outgoingPanel.anchoredPosition =
                outgoingStartPosition;

            incomingPanel.anchoredPosition =
                incomingStartPosition;

            float elapsedTime = 0f;

            while (elapsedTime < slideDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(elapsedTime / slideDuration);

                float easedProgress =
                    progress * progress;

                outgoingPanel.anchoredPosition =
                    Vector2.Lerp(
                        outgoingStartPosition,
                        outgoingEndPosition,
                        easedProgress);

                incomingPanel.anchoredPosition =
                    Vector2.Lerp(
                        incomingStartPosition,
                        incomingEndPosition,
                        easedProgress);

                yield return null;
            }

            outgoingPanel.anchoredPosition =
                outgoingEndPosition;

            incomingPanel.anchoredPosition =
                incomingEndPosition;
        }

        private IEnumerator SlideConnectedScenes(
            RectTransform destinationRoot,
            bool blockLiveSceneBehind = false)
        {
            Canvas.ForceUpdateCanvases();

            float screenWidth =
                sourceSnapshot.rectTransform.rect.width;

            Image transitionBackdrop = null;
            Material transitionBackdropMaterial = null;

            if (blockLiveSceneBehind)
            {
                transitionBackdrop =
                    Instantiate(fadeImage, fadeImage.transform.parent);
                transitionBackdrop.name = "TransitionBackdrop (Runtime)";
                RectTransform backdropRect = transitionBackdrop.rectTransform;
                backdropRect.anchorMin = Vector2.zero;
                backdropRect.anchorMax = Vector2.one;
                backdropRect.pivot = new Vector2(0.5f, 0.5f);
                backdropRect.anchoredPosition = Vector2.zero;
                backdropRect.sizeDelta = Vector2.zero;
                transitionBackdropMaterial = Instantiate(runtimeFadeMaterial);
                transitionBackdrop.material = transitionBackdropMaterial;
                Color backdropColor = transitionBackdrop.color;
                backdropColor.a = 1f;
                transitionBackdrop.color = backdropColor;
                transitionBackdropMaterial.SetFloat(EffectModePropertyId, 3f);
                transitionBackdrop.transform.SetAsFirstSibling();
            }

            Vector2 sourceStart =
                Vector2.zero;

            Vector2 sourceEnd =
                Vector2.right
                * screenWidth
                * movementDirection;


            Vector2 destinationEnd = 
                    destinationRoot.anchoredPosition;

            Vector2 destinationStart =
                destinationEnd + 
                Vector2.left * screenWidth * movementDirection;

            Vector2 seamStart =
                Vector2.left * screenWidth * 2.375f * movementDirection;

            Vector2 seamEnd =
                Vector2.right * screenWidth * 2.375f * movementDirection;

            Image outgoingFadeImage =
                Instantiate(fadeImage, fadeImage.transform.parent);

            outgoingFadeImage.name = "OutgoingSceneFade (Runtime)";
            RectTransform outgoingFadeRect =
                outgoingFadeImage.rectTransform;
            Material outgoingFadeMaterial =
                Instantiate(runtimeFadeMaterial);
            outgoingFadeImage.material = outgoingFadeMaterial;
            Color outgoingFadeColor = outgoingFadeImage.color;
            outgoingFadeColor.a = 1f;
            outgoingFadeImage.color = outgoingFadeColor;
            outgoingFadeMaterial.SetFloat(EffectModePropertyId, 2f);
            outgoingFadeMaterial.SetFloat(BlackPlateauPropertyId, 0.5f);
            outgoingFadeMaterial.SetFloat(DirectionPropertyId, movementDirection);
            outgoingFadeMaterial.SetFloat(ProgressPropertyId, 0f);

            Image incomingFadeImage =
                Instantiate(fadeImage, fadeImage.transform.parent);

            incomingFadeImage.name = "IncomingSceneFade (Runtime)";
            RectTransform incomingFadeRect =
                incomingFadeImage.rectTransform;
            Material incomingFadeMaterial =
                Instantiate(runtimeFadeMaterial);
            incomingFadeImage.material = incomingFadeMaterial;
            Color incomingFadeColor = incomingFadeImage.color;
            incomingFadeColor.a = 1f;
            incomingFadeImage.color = incomingFadeColor;
            incomingFadeMaterial.SetFloat(EffectModePropertyId, 2f);
            incomingFadeMaterial.SetFloat(DirectionPropertyId, -movementDirection);
            incomingFadeMaterial.SetFloat(ProgressPropertyId, 1f);

            Image blackGapImage =
                Instantiate(fadeImage, fadeImage.transform.parent);
            blackGapImage.name = "SceneGapBlack (Runtime)";
            RectTransform blackGapRect = blackGapImage.rectTransform;
            blackGapRect.anchorMin = new Vector2(0.5f, 0f);
            blackGapRect.anchorMax = new Vector2(0.5f, 1f);
            blackGapRect.pivot = new Vector2(0.5f, 0.5f);
            // Scene panels are physically adjacent now. The moving gradient
            // already contains the solid-black center, so a second solid gap
            // would hide its transparent gradient edges.
            blackGapRect.sizeDelta = Vector2.zero;
            Material blackGapMaterial = Instantiate(runtimeFadeMaterial);
            blackGapImage.material = blackGapMaterial;
            Color blackGapColor = blackGapImage.color;
            blackGapColor.a = 1f;
            blackGapImage.color = blackGapColor;
            blackGapMaterial.SetFloat(EffectModePropertyId, 3f);

            Vector2 blackGapStart =
                Vector2.left * screenWidth * 0.5f * movementDirection;
            Vector2 blackGapEnd =
                Vector2.right * screenWidth * 0.5f * movementDirection;

            runtimeFadeMaterial.SetFloat(EffectModePropertyId, 0f);
            runtimeFadeMaterial.SetFloat(BlackPlateauPropertyId, 0.4f);

            sourceSnapshot.rectTransform.anchoredPosition =
                sourceStart;

            destinationRoot.anchoredPosition =
                destinationStart;

            destinationRoot.gameObject.SetActive(true);

            transitionImage.anchorMin = new Vector2(0.5f, 0f);
            transitionImage.anchorMax = new Vector2(0.5f, 1f);
            transitionImage.pivot = new Vector2(0.5f, 0.5f);
            // A real half-screen gap sits between the scene panels. The
            // animated overlay sweeps across that gap to hide both joins.
            transitionImage.sizeDelta = new Vector2(screenWidth * 3.75f, 0f);

            transitionImage.anchoredPosition =
                seamStart;

            outgoingFadeRect.anchoredPosition = sourceStart;
            incomingFadeRect.anchoredPosition = destinationStart;
            blackGapRect.anchoredPosition = blackGapStart;
            outgoingFadeImage.transform.SetAsLastSibling();
            incomingFadeImage.transform.SetAsLastSibling();
            blackGapImage.transform.SetAsLastSibling();
            transitionImage.SetAsLastSibling();

            SetFadeImageAlpha(1f);

            runtimeFadeMaterial.SetFloat(
                ProgressPropertyId, 0f);

            transitionImage.gameObject.SetActive(true);

            if (slideDuration <= 0f)
            {
                sourceSnapshot.rectTransform.anchoredPosition =
                    sourceEnd;

                destinationRoot.anchoredPosition =
                    destinationEnd;

                transitionImage.anchoredPosition =
                    seamEnd;

                outgoingFadeRect.anchoredPosition = sourceEnd;
                incomingFadeRect.anchoredPosition = destinationEnd;
                blackGapRect.anchoredPosition = blackGapEnd;

                outgoingFadeMaterial.SetFloat(
                    ProgressPropertyId, 1f);
                incomingFadeMaterial.SetFloat(
                    ProgressPropertyId, 0f);
                runtimeFadeMaterial.SetFloat(
                    ProgressPropertyId, 0f);

                Destroy(outgoingFadeImage.gameObject);
                Destroy(outgoingFadeMaterial);
                Destroy(incomingFadeImage.gameObject);
                Destroy(incomingFadeMaterial);
                Destroy(blackGapImage.gameObject);
                Destroy(blackGapMaterial);

                if (transitionBackdrop != null)
                    Destroy(transitionBackdrop.gameObject);
                if (transitionBackdropMaterial != null)
                    Destroy(transitionBackdropMaterial);

                yield break;
            }

            float elapsedTime = 0f;

            while (elapsedTime < slideDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsedTime / slideDuration);

                float easedProgress =
                    Mathf.SmoothStep(0f, 1f, progress);

                // Let the wide gradient enter first and leave last. Scene
                // panels move only while the gradient covers their joins.
                float sceneProgress =
                    Mathf.Clamp01((progress - 0.22f) / 0.5f);

                float sceneEasedProgress =
                    Mathf.SmoothStep(0f, 1f, sceneProgress);

                // Keep the incoming scene aligned with the strip while its
                // directional darkness clears at a slower independent pace.
                const float incomingRevealSpan = 1f;

                float incomingRevealProgress =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        Mathf.Clamp01(
                            sceneProgress / incomingRevealSpan));

                sourceSnapshot.rectTransform.anchoredPosition =
                    Vector2.Lerp(
                        sourceStart,
                        sourceEnd,
                        sceneEasedProgress);

                destinationRoot.anchoredPosition =
                    Vector2.Lerp(
                        destinationStart,
                        destinationEnd,
                        sceneEasedProgress);

                transitionImage.anchoredPosition =
                    Vector2.Lerp(
                        seamStart,
                        seamEnd,
                        easedProgress);

                outgoingFadeRect.anchoredPosition =
                    sourceSnapshot.rectTransform.anchoredPosition;
                incomingFadeRect.anchoredPosition =
                    destinationRoot.anchoredPosition;
                blackGapRect.anchoredPosition =
                    Vector2.Lerp(
                        blackGapStart,
                        blackGapEnd,
                        sceneEasedProgress);

                outgoingFadeMaterial.SetFloat(
                    ProgressPropertyId,
                    sceneEasedProgress);
                incomingFadeMaterial.SetFloat(
                    ProgressPropertyId,
                    1f - incomingRevealProgress);
                runtimeFadeMaterial.SetFloat(
                    ProgressPropertyId,
                    easedProgress);

                yield return null;
            }

            sourceSnapshot.rectTransform.anchoredPosition =
                sourceEnd;

            destinationRoot.anchoredPosition =
                destinationEnd;

            transitionImage.anchoredPosition =
                seamEnd;

            outgoingFadeRect.anchoredPosition = sourceEnd;
            incomingFadeRect.anchoredPosition = destinationEnd;
            blackGapRect.anchoredPosition = blackGapEnd;

            outgoingFadeMaterial.SetFloat(
                ProgressPropertyId, 1f);
            incomingFadeMaterial.SetFloat(
                ProgressPropertyId, 0f);
            runtimeFadeMaterial.SetFloat(
                ProgressPropertyId, 0f);

            Destroy(outgoingFadeImage.gameObject);
            Destroy(outgoingFadeMaterial);
            Destroy(incomingFadeImage.gameObject);
            Destroy(incomingFadeMaterial);
            Destroy(blackGapImage.gameObject);
            Destroy(blackGapMaterial);

            if (transitionBackdrop != null)
                Destroy(transitionBackdrop.gameObject);
            if (transitionBackdropMaterial != null)
                Destroy(transitionBackdropMaterial);

        }

        private IEnumerator CaptureDestinationSnapshot()
        {
            sourceSnapshot.gameObject.SetActive(false);
            destinationSnapshot.gameObject.SetActive(false);
            transitionImage.gameObject.SetActive(false);

            yield return new WaitForEndOfFrame();

            capturedDestinationTexture =
                new Texture2D(
                    Screen.width,
                    Screen.height,
                    TextureFormat.RGB24,
                    false, false);

            capturedDestinationTexture.ReadPixels(
                new Rect(0f, 0f, Screen.width, Screen.height),
                0, 0, false);

            capturedDestinationTexture.Apply(false, false);

            destinationSnapshot.texture =
                capturedDestinationTexture;

            Color snapshotColor =
                destinationSnapshot.color;

            snapshotColor.a = 1f;
            destinationSnapshot.color =
                snapshotColor;

            destinationSnapshot.rectTransform.anchoredPosition =
                Vector2.zero;

            destinationSnapshot.gameObject.SetActive(true);
            sourceSnapshot.gameObject.SetActive(true);
        }

        private void SetSnapshotAlpha(float alpha)
        {
            if (sourceSnapshot == null)
                return;

            Color color = sourceSnapshot.color;
            color.a = Mathf.Clamp01(alpha);
            sourceSnapshot.color = color;
        }

        private IEnumerator FadeSnapshot(
            float startAlpha,
            float endAlpha)
        {
            float elapsedTime = 0f;

            SetSnapshotAlpha(startAlpha);

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsedTime / fadeDuration);

                float easedProgress =
                    Mathf.SmoothStep(
                        0f, 
                        1f, 
                        progress);

                float currentAlpha =
                    Mathf.Lerp(
                        startAlpha,
                        endAlpha,
                        easedProgress);

                SetSnapshotAlpha(currentAlpha);

                yield return null;
            }

            SetSnapshotAlpha(endAlpha);
        }

        private IEnumerator PlayTransitionSequence()
        {
            if (fadeImage == null)
            {
                Debug.LogError(
                    "[GalleryTransitionController]" + 
                    "전환 Fade Image 바인딩이 누락되었습니다.",
                    this);

                isTransitioning = false;
                Destroy(gameObject);
                yield break;
            }

            if (!PrepareDirectionalFadeMaterial())
            {
                isTransitioning = false;
                Destroy(gameObject);
                yield break;
            }

            AsyncOperation loadOperation =
                SceneManager.LoadSceneAsync(
                    pendingSceneName);

            if (loadOperation == null)
            {
                Debug.LogError(
                    "[GalleryTransitionController]" +
                    $"씬 로드를 시작하지 못했습니다: {pendingSceneName}",
                    this);

                ReleaseRuntimeFadeMaterial();

                isTransitioning = false;
                Destroy(gameObject);
                yield break;
            }

            loadOperation.allowSceneActivation = false;

            // Capture what the player sees now, never the scene's initial default sky.
            yield return CaptureSourceSnapshot();

            while (loadOperation.progress < 0.9f)
                yield return null;

            loadOperation.allowSceneActivation = true;
            yield return loadOperation;

            // Let scene Start methods restore state while the outgoing snapshot covers it.
            yield return null;
            foreach (BackgroundSkyController sky in FindObjectsByType<BackgroundSkyController>(FindObjectsSortMode.None))
                sky.ApplyWeatherBeforeReveal();

            Canvas.ForceUpdateCanvases();

            GalleryTransitionTarget transitionTarget =
                FindFirstObjectByType<GalleryTransitionTarget>();

            if (transitionTarget != null)
                yield return SlideConnectedScenes(transitionTarget.Root);
            else
            {
                yield return CaptureDestinationSnapshot();
                yield return SlideConnectedScenes(
                    destinationSnapshot.rectTransform,
                    true);
            }

            transitionImage.gameObject.SetActive(false);
            sourceSnapshot.gameObject.SetActive(false);
            destinationSnapshot.gameObject.SetActive(false);
            sourceSnapshot.texture = null;
            destinationSnapshot.texture = null;

            if (capturedSourceTexture != null)
            {
                Destroy(capturedSourceTexture);
                capturedSourceTexture = null;
            }

            if (capturedDestinationTexture != null)
            {
                Destroy(capturedDestinationTexture);
                capturedDestinationTexture = null;
            }

            ReleaseRuntimeFadeMaterial();

            isTransitioning = false;
            Destroy(gameObject);
        }

        private IEnumerator SlideToDestination(
            RectTransform destinationRoot,
            Vector2 overlayStartPosition,
            Vector2 overlayEndPosition)
        {
            Canvas.ForceUpdateCanvases();

            Vector2 destinationEndPosition =
                destinationRoot.anchoredPosition;

            Vector2 destinationStartPosition =
                destinationEndPosition
                + Vector2.left
                * transitionImage.rect.width
                * movementDirection;

            transitionImage.anchoredPosition =
                overlayStartPosition;

            destinationRoot.anchoredPosition =
                destinationStartPosition;


            float elapsedTime = 0f;

            while (elapsedTime < slideDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(elapsedTime / slideDuration);

                float inverseProgress =
                    1f - progress;

                float easedProgress =
                    1f - inverseProgress * inverseProgress;

                transitionImage.anchoredPosition =
                    Vector2.Lerp(
                        overlayStartPosition,
                        overlayEndPosition,
                        easedProgress);

                destinationRoot.anchoredPosition =
                    Vector2.Lerp(
                        destinationStartPosition,
                        destinationEndPosition,
                        easedProgress);

                yield return null;
            }

            transitionImage.anchoredPosition =
                overlayEndPosition;

            destinationRoot.anchoredPosition =
                destinationEndPosition;
        }

        private IEnumerator SlideImage(
            Vector2 startPosition,
            Vector2 endPosition)
        {
            transitionImage.anchoredPosition = startPosition;

            float elapsedTime = 0f;

            while (elapsedTime < slideDuration)
            {
                elapsedTime += Time.unscaledDeltaTime;

                float progress =
                    Mathf.Clamp01(elapsedTime / slideDuration);

                float inverseProgress =
                    1f - progress;

                float easedProgress =
                    1f - inverseProgress * inverseProgress;

                transitionImage.anchoredPosition =
                    Vector2.Lerp(
                        startPosition,
                        endPosition,
                        easedProgress);

                yield return null;
            }

            transitionImage.anchoredPosition = endPosition;
        }
    }
}
