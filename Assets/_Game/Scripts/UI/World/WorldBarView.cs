using UnityEngine;
using UnityEngine.UI;

namespace GenericGachaRPG
{
    /// <summary>
    /// Lightweight world-space HP and energy bars. Values are supplied by a
    /// presenter/controller; this component performs no combat calculations.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldBarView : MonoBehaviour
    {
        [Header("Optional Authored References")]
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private RectTransform healthFill;
        [SerializeField] private RectTransform energyFill;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Behavior")]
        [SerializeField] private bool buildVisualsIfMissing = true;
        [SerializeField] private bool faceCamera = true;
        [Min(0.1f)] [SerializeField] private float interpolationSpeed = 6f;
        [Min(0.001f)] [SerializeField] private float worldScale = 0.0065f;
        [SerializeField] private int sortingOrder = 30;

        [Header("Colors")]
        [SerializeField] private Color healthColor = new Color(0.25f, 0.95f, 0.42f, 1f);
        [SerializeField] private Color energyColor = new Color(0.18f, 0.64f, 1f, 1f);
        [SerializeField] private Color backgroundColor = new Color(0.025f, 0.035f, 0.065f, 0.94f);

        private Camera targetCamera;
        private float displayedHealth = 1f;
        private float targetHealth = 1f;
        private float displayedEnergy;
        private float targetEnergy;

        public float HealthNormalized => targetHealth;
        public float EnergyNormalized => targetEnergy;
        public bool IsVisible => canvasGroup == null || canvasGroup.alpha > 0.001f;

        private void Awake()
        {
            EnsureVisuals();
            ApplyValuesImmediately();
        }

        private void Update()
        {
            float delta = Mathf.Max(0f, Time.unscaledDeltaTime) * interpolationSpeed;
            displayedHealth = Mathf.MoveTowards(displayedHealth, targetHealth, delta);
            displayedEnergy = Mathf.MoveTowards(displayedEnergy, targetEnergy, delta);
            ApplyFill(healthFill, displayedHealth);
            ApplyFill(energyFill, displayedEnergy);
        }

        private void LateUpdate()
        {
            if (!faceCamera || visualRoot == null)
            {
                return;
            }

            if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                visualRoot.rotation = targetCamera.transform.rotation;
            }
        }

        public void SetTargetCamera(Camera cameraToFace)
        {
            targetCamera = cameraToFace;
        }

        public void SetHealth(float current, float maximum, bool immediate = false)
        {
            SetHealthNormalized(maximum > 0f ? current / maximum : 0f, immediate);
        }

        public void SetHealthNormalized(float normalizedValue, bool immediate = false)
        {
            targetHealth = Mathf.Clamp01(normalizedValue);
            if (immediate)
            {
                displayedHealth = targetHealth;
                ApplyFill(healthFill, displayedHealth);
            }
        }

        public void SetEnergy(float current, float maximum, bool immediate = false)
        {
            SetEnergyNormalized(maximum > 0f ? current / maximum : 0f, immediate);
        }

        public void SetEnergyNormalized(float normalizedValue, bool immediate = false)
        {
            targetEnergy = Mathf.Clamp01(normalizedValue);
            if (immediate)
            {
                displayedEnergy = targetEnergy;
                ApplyFill(energyFill, displayedEnergy);
            }
        }

        public void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(visible);
            }
        }

        public void SetColors(Color newHealthColor, Color newEnergyColor)
        {
            healthColor = newHealthColor;
            energyColor = newEnergyColor;
            if (healthFill != null && healthFill.TryGetComponent(out Image healthImage))
            {
                healthImage.color = healthColor;
            }

            if (energyFill != null && energyFill.TryGetComponent(out Image energyImage))
            {
                energyImage.color = energyColor;
            }
        }

        /// <summary>Creates a compact world-space canvas when no prefab UI was assigned.</summary>
        public void EnsureVisuals()
        {
            if (visualRoot != null && healthFill != null && energyFill != null)
            {
                if (canvasGroup == null)
                {
                    canvasGroup = visualRoot.GetComponent<CanvasGroup>();
                }

                return;
            }

            if (!buildVisualsIfMissing)
            {
                return;
            }

            Transform existing = transform.Find("WorldBarCanvas");
            if (existing != null)
            {
                visualRoot = existing as RectTransform;
                healthFill = FindRect(existing, "HealthFill");
                energyFill = FindRect(existing, "EnergyFill");
                canvasGroup = existing.GetComponent<CanvasGroup>();
                if (visualRoot != null && healthFill != null && energyFill != null)
                {
                    return;
                }
            }

            var canvasObject = new GameObject(
                "WorldBarCanvas",
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
            visualRoot.sizeDelta = new Vector2(166f, 34f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            scaler.referencePixelsPerUnit = 100f;

            canvasGroup = canvasObject.GetComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            RectTransform backdrop = CreateImage(
                "Backdrop",
                visualRoot,
                backgroundColor,
                Vector2.zero,
                new Vector2(166f, 34f));
            backdrop.SetAsFirstSibling();

            RectTransform healthTrack = CreateImage(
                "HealthTrack",
                visualRoot,
                new Color(0.16f, 0.045f, 0.06f, 1f),
                new Vector2(0f, 7f),
                new Vector2(154f, 11f));
            healthFill = CreateStretchFill("HealthFill", healthTrack, healthColor);

            RectTransform energyTrack = CreateImage(
                "EnergyTrack",
                visualRoot,
                new Color(0.035f, 0.08f, 0.17f, 1f),
                new Vector2(0f, -8f),
                new Vector2(154f, 7f));
            energyFill = CreateStretchFill("EnergyFill", energyTrack, energyColor);

            ApplyValuesImmediately();
        }

        private void ApplyValuesImmediately()
        {
            displayedHealth = targetHealth;
            displayedEnergy = targetEnergy;
            ApplyFill(healthFill, displayedHealth);
            ApplyFill(energyFill, displayedEnergy);
        }

        private static void ApplyFill(RectTransform fill, float normalizedValue)
        {
            if (fill == null)
            {
                return;
            }

            Vector2 maximumAnchor = fill.anchorMax;
            maximumAnchor.x = Mathf.Clamp01(normalizedValue);
            fill.anchorMax = maximumAnchor;
        }

        private static RectTransform CreateImage(
            string objectName,
            RectTransform parent,
            Color color,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.layer = parent.gameObject.layer;
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform CreateStretchFill(string objectName, RectTransform parent, Color color)
        {
            var fillObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillObject.layer = parent.gameObject.layer;
            RectTransform rect = fillObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = fillObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return rect;
        }

        private static RectTransform FindRect(Transform root, string objectName)
        {
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == objectName)
                {
                    return rect;
                }
            }

            return null;
        }
    }
}
