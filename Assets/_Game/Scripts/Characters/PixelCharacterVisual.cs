using UnityEngine;

namespace GenericGachaRPG
{
    /// <summary>
    /// Keeps a pixel-art body camera-facing while the deterministic battle root
    /// continues to move in X/Z world space. This component is presentation-only.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PixelCharacterVisual : MonoBehaviour
    {
        public const int VirtualWidth = 480;
        public const int VirtualHeight = 270;

        [SerializeField] private string characterId;
        [SerializeField] private Transform billboardRoot;
        [SerializeField] private Transform shadowRoot;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer glowRenderer;
        [SerializeField] private SpriteRenderer shadowRenderer;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private bool enemy;

        private Vector3 bodyRestLocalPosition;
        private Vector3 shadowRestLocalPosition;
        private bool configured;

        public string CharacterId => characterId;
        public SpriteRenderer BodyRenderer => bodyRenderer;
        public bool IsEnemy => enemy;
        public bool IsConfigured => configured && bodyRenderer != null && bodyRenderer.sprite != null;

        public void Configure(
            string id,
            Transform bodyBillboard,
            SpriteRenderer body,
            SpriteRenderer glow,
            Transform shadowBillboard,
            SpriteRenderer shadow,
            Camera cameraToUse,
            bool isEnemy)
        {
            characterId = id ?? string.Empty;
            billboardRoot = bodyBillboard;
            bodyRenderer = body;
            glowRenderer = glow;
            shadowRoot = shadowBillboard;
            shadowRenderer = shadow;
            targetCamera = cameraToUse;
            enemy = isEnemy;
            bodyRestLocalPosition = billboardRoot == null ? Vector3.zero : billboardRoot.localPosition;
            shadowRestLocalPosition = shadowRoot == null ? Vector3.zero : shadowRoot.localPosition;
            configured = bodyRenderer != null && bodyRenderer.sprite != null;
            RefreshPresentation();
        }

        private void LateUpdate()
        {
            RefreshPresentation();
        }

        private void RefreshPresentation()
        {
            if (!configured)
            {
                return;
            }

            if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera == null)
            {
                return;
            }

            if (billboardRoot != null)
            {
                Vector3 anchor = billboardRoot.parent == null
                    ? transform.position
                    : billboardRoot.parent.TransformPoint(bodyRestLocalPosition);
                billboardRoot.position = SnapWorldToVirtualPixels(anchor);
                billboardRoot.rotation = targetCamera.transform.rotation;
            }

            if (shadowRoot != null)
            {
                Vector3 anchor = shadowRoot.parent == null
                    ? transform.position
                    : shadowRoot.parent.TransformPoint(shadowRestLocalPosition);
                shadowRoot.position = SnapWorldToVirtualPixels(anchor);
                shadowRoot.rotation = targetCamera.transform.rotation;
            }

            bool faceLeft = transform.forward.x < 0f;
            bodyRenderer.flipX = faceLeft;
            if (glowRenderer != null)
            {
                glowRenderer.flipX = faceLeft;
            }

            Vector3 cameraSpace = targetCamera.transform.InverseTransformPoint(transform.position);
            int sortingOrder = 4000 - Mathf.RoundToInt(cameraSpace.z * 16f);
            if (shadowRenderer != null)
            {
                shadowRenderer.sortingOrder = sortingOrder - 2;
            }

            if (glowRenderer != null)
            {
                glowRenderer.sortingOrder = sortingOrder - 1;
            }

            bodyRenderer.sortingOrder = sortingOrder;
        }

        private Vector3 SnapWorldToVirtualPixels(Vector3 worldPosition)
        {
            Vector3 screen = targetCamera.WorldToScreenPoint(worldPosition);
            if (screen.z <= 0f || Screen.width <= 0 || Screen.height <= 0)
            {
                return worldPosition;
            }

            float virtualX = screen.x * VirtualWidth / Screen.width;
            float virtualY = screen.y * VirtualHeight / Screen.height;
            screen.x = Mathf.Round(virtualX) * Screen.width / VirtualWidth;
            screen.y = Mathf.Round(virtualY) * Screen.height / VirtualHeight;
            return targetCamera.ScreenToWorldPoint(screen);
        }
    }
}
