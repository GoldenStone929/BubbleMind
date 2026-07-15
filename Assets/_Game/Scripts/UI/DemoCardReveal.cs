using UnityEngine;

namespace GenericGachaRPG
{
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class DemoCardReveal : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float duration = 0.38f;

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private float elapsed;
        private bool playing;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Play()
        {
            if (rectTransform == null)
            {
                rectTransform = GetComponent<RectTransform>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            elapsed = 0f;
            playing = true;
            canvasGroup.alpha = 0f;
            rectTransform.localScale = Vector3.one * 0.94f;
        }

        private void Update()
        {
            if (!playing)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, duration));
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            canvasGroup.alpha = eased;
            rectTransform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
            if (t >= 1f)
            {
                playing = false;
                canvasGroup.alpha = 1f;
                rectTransform.localScale = Vector3.one;
            }
        }
    }
}
