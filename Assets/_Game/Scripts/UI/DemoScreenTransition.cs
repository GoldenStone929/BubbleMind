using System.Collections;
using UnityEngine;

namespace GenericGachaRPG
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class DemoScreenTransition : MonoBehaviour
    {
        [SerializeField] private float duration = 0.20f;
        [SerializeField] private float travel = 12f;

        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;
        private Coroutine routine;
        private Vector2 restingPosition;
        private bool initialized;
        private bool targetVisible;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
            restingPosition = rectTransform.anchoredPosition;
        }

        public void SetVisible(bool visible)
        {
            EnsureReferences();
            if (!initialized)
            {
                initialized = true;
                targetVisible = visible;
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
                rectTransform.anchoredPosition = restingPosition;
                gameObject.SetActive(visible);
                return;
            }

            if (targetVisible == visible && gameObject.activeSelf == visible)
            {
                return;
            }

            targetVisible = visible;
            if (routine != null)
            {
                StopCoroutine(routine);
            }

            if (visible && !gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            routine = StartCoroutine(Animate(visible));
        }

        private IEnumerator Animate(bool visible)
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = visible;
            float fromAlpha = canvasGroup.alpha;
            float toAlpha = visible ? 1f : 0f;
            Vector2 fromPosition = rectTransform.anchoredPosition;
            Vector2 toPosition = visible ? restingPosition : restingPosition + Vector2.up * (travel * 0.35f);
            if (visible && fromAlpha <= 0.001f)
            {
                fromPosition = restingPosition - Vector2.up * travel;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);
                canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, eased);
                rectTransform.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, eased);
                yield return null;
            }

            canvasGroup.alpha = toAlpha;
            rectTransform.anchoredPosition = visible ? restingPosition : toPosition;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            routine = null;
            if (!visible)
            {
                gameObject.SetActive(false);
                rectTransform.anchoredPosition = restingPosition;
            }
        }

        private void EnsureReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
                restingPosition = rectTransform.anchoredPosition;
            }
        }
    }
}
