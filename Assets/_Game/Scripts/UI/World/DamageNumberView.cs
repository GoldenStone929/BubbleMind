using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GenericGachaRPG
{
    /// <summary>
    /// A self-contained world-space floating number. It visualizes values that
    /// have already been calculated elsewhere and never alters combat state.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageNumberView : MonoBehaviour
    {
        [Header("Optional Authored References")]
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private Text label;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Motion")]
        [Min(0.15f)] [SerializeField] private float lifetime = 0.9f;
        [Min(0f)] [SerializeField] private float riseDistance = 0.78f;
        [Min(0.001f)] [SerializeField] private float worldScale = 0.008f;
        [SerializeField] private bool destroyWhenFinished = true;
        [SerializeField] private int sortingOrder = 50;

        private Camera targetCamera;
        private Coroutine animationRoutine;
        private Vector3 startWorldPosition;
        private Color textColor = Color.white;
        private float scaleMultiplier = 1f;
        private bool initialized;

        private void Awake()
        {
            EnsureVisuals();
        }

        private void Start()
        {
            if (!initialized)
            {
                Initialize("0", Color.white, false);
            }
        }

        private void OnEnable()
        {
            if (initialized && animationRoutine == null)
            {
                animationRoutine = StartCoroutine(Animate());
            }
        }

        private void OnDisable()
        {
            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
                animationRoutine = null;
            }
        }

        public static DamageNumberView SpawnDamage(
            Vector3 worldPosition,
            float amount,
            bool critical = false,
            Transform parent = null)
        {
            Color color = critical
                ? new Color(1f, 0.78f, 0.12f, 1f)
                : new Color(1f, 0.28f, 0.22f, 1f);
            string text = Mathf.Abs(Mathf.RoundToInt(amount)).ToString();
            return SpawnText(worldPosition, text, color, critical, parent);
        }

        public static DamageNumberView SpawnHealing(
            Vector3 worldPosition,
            float amount,
            Transform parent = null)
        {
            string text = $"+{Mathf.Abs(Mathf.RoundToInt(amount))}";
            return SpawnText(worldPosition, text, new Color(0.28f, 1f, 0.48f, 1f), false, parent);
        }

        public static DamageNumberView SpawnText(
            Vector3 worldPosition,
            string text,
            Color color,
            bool emphasized = false,
            Transform parent = null)
        {
            var host = new GameObject(emphasized ? "CriticalNumber" : "DamageNumber");
            if (parent != null)
            {
                host.transform.SetParent(parent, true);
                host.layer = parent.gameObject.layer;
            }

            host.transform.position = worldPosition;
            DamageNumberView view = host.AddComponent<DamageNumberView>();
            view.Initialize(text, color, emphasized);
            return view;
        }

        public void Initialize(string text, Color color, bool emphasized = false, Camera cameraToFace = null)
        {
            EnsureVisuals();
            initialized = true;
            textColor = color;
            scaleMultiplier = emphasized ? 1.28f : 1f;
            targetCamera = cameraToFace;

            if (label != null)
            {
                label.text = text ?? string.Empty;
                label.color = textColor;
                label.fontSize = emphasized ? 42 : 34;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (animationRoutine != null)
            {
                StopCoroutine(animationRoutine);
            }

            if (isActiveAndEnabled)
            {
                animationRoutine = StartCoroutine(Animate());
            }
        }

        public void SetTargetCamera(Camera cameraToFace)
        {
            targetCamera = cameraToFace;
        }

        public void EnsureVisuals()
        {
            if (visualRoot != null && label != null)
            {
                if (canvasGroup == null)
                {
                    canvasGroup = visualRoot.GetComponent<CanvasGroup>();
                }

                return;
            }

            Transform existing = transform.Find("DamageNumberCanvas");
            if (existing != null)
            {
                visualRoot = existing as RectTransform;
                label = existing.GetComponentInChildren<Text>(true);
                canvasGroup = existing.GetComponent<CanvasGroup>();
                if (visualRoot != null && label != null)
                {
                    return;
                }
            }

            var canvasObject = new GameObject(
                "DamageNumberCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(CanvasGroup));
            canvasObject.layer = gameObject.layer;
            visualRoot = canvasObject.GetComponent<RectTransform>();
            visualRoot.SetParent(transform, false);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one * worldScale;
            visualRoot.sizeDelta = new Vector2(190f, 64f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder + Random.Range(0, 4);

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 12f;
            scaler.referencePixelsPerUnit = 100f;

            canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            var textObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text),
                typeof(Outline));
            textObject.layer = gameObject.layer;
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(visualRoot, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            label = textObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.alignment = TextAnchor.MiddleCenter;
            label.fontStyle = FontStyle.Bold;
            label.fontSize = 34;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            label.color = textColor;

            Outline outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.015f, 0.02f, 0.04f, 0.92f);
            outline.effectDistance = new Vector2(2.2f, -2.2f);
            outline.useGraphicAlpha = true;
        }

        private IEnumerator Animate()
        {
            startWorldPosition = transform.position;
            Vector3 sideOffset = Vector3.right * Random.Range(-0.12f, 0.12f);
            float elapsed = 0f;

            while (elapsed < lifetime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                float rise = 1f - Mathf.Pow(1f - t, 2f);
                transform.position = startWorldPosition + Vector3.up * (riseDistance * rise) + sideOffset * t;

                if (targetCamera == null || !targetCamera.isActiveAndEnabled)
                {
                    targetCamera = Camera.main;
                }

                if (targetCamera != null && visualRoot != null)
                {
                    visualRoot.rotation = targetCamera.transform.rotation;
                }

                if (visualRoot != null)
                {
                    float pop;
                    if (t < 0.18f)
                    {
                        pop = Mathf.Lerp(0.25f, 1.18f, EaseOutBack(t / 0.18f));
                    }
                    else
                    {
                        pop = Mathf.Lerp(1.18f, 0.92f, (t - 0.18f) / 0.82f);
                    }

                    visualRoot.localScale = Vector3.one * (worldScale * scaleMultiplier * pop);
                }

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = t < 0.55f ? 1f : 1f - ((t - 0.55f) / 0.45f);
                }

                yield return null;
            }

            animationRoutine = null;
            if (destroyWhenFinished)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private static float EaseOutBack(float value)
        {
            const float overshoot = 1.70158f;
            float shifted = value - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
        }
    }
}
