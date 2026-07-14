using UnityEngine;
using UnityEngine.UI;

namespace GenericGachaRPG
{
    /// <summary>Persistent, camera-facing label used above battle characters.</summary>
    [DisallowMultipleComponent]
    public sealed class WorldNameplateView : MonoBehaviour
    {
        private const float WorldScale = 0.0045f;

        private RectTransform visualRoot;
        private Text label;
        private Camera targetCamera;

        private void Awake()
        {
            EnsureVisuals();
        }

        private void LateUpdate()
        {
            if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null && visualRoot != null)
            {
                visualRoot.rotation = targetCamera.transform.rotation;
            }
        }

        public void Initialize(string text, Color color, Camera cameraToFace = null)
        {
            EnsureVisuals();
            targetCamera = cameraToFace;
            label.text = text ?? string.Empty;
            label.color = color;
        }

        private void EnsureVisuals()
        {
            if (visualRoot != null && label != null)
            {
                return;
            }

            var canvasObject = new GameObject(
                "NameplateCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.layer = gameObject.layer;
            visualRoot = canvasObject.GetComponent<RectTransform>();
            visualRoot.SetParent(transform, false);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one * WorldScale;
            visualRoot.sizeDelta = new Vector2(220f, 40f);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 31;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            scaler.referencePixelsPerUnit = 100f;

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
            label.fontSize = 21;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;

            Outline outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.015f, 0.02f, 0.04f, 0.92f);
            outline.effectDistance = new Vector2(1.6f, -1.6f);
            outline.useGraphicAlpha = true;
        }
    }
}
