using System;
using UnityEngine;

namespace GenericGachaRPG
{
    /// <summary>Builds the project-owned Pixel2D battle view without touching simulation data.</summary>
    public static class PixelCharacterBuilder
    {
        public const string SpriteResourcePrefix = "BattleSprites/Pixel_";
        public const string ShadowResourcePath = "BattleSprites/PixelShadow";
        public const float PixelsPerUnit = 64f;

        public static CharacterView Create(
            string runtimeId,
            CharacterDefinition definition,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Camera camera,
            bool enemy)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id))
            {
                return null;
            }

            Sprite bodySprite = Resources.Load<Sprite>(SpriteResourcePrefix + definition.Id);
            if (bodySprite == null)
            {
                Debug.LogWarning(
                    $"[GenericGachaRPG] Pixel battle sprite is missing for '{definition.Id}'. " +
                    "The authored 3D fallback will be used.");
                return null;
            }

            GameObject root = new GameObject($"{runtimeId}_{definition.DisplayName}");
            root.transform.SetParent(parent, false);
            root.transform.position = position;
            root.transform.rotation = rotation;

            Transform modelRoot = CreateTransform("ModelRoot", root.transform, Vector3.zero);
            float rarityScale = CatherineYukiBattleKit.IsCatherine(definition.Id) ? 1.22f : 1f;
            modelRoot.localScale = Vector3.one * rarityScale;

            Transform billboard = CreateTransform("PixelBillboard", modelRoot, new Vector3(0f, 0.08f, 0f));
            SpriteRenderer glow = CreateSpriteRenderer(
                "TeamGlow",
                billboard,
                bodySprite,
                enemy
                    ? new Color(1f, 0.16f, 0.20f, 0.16f)
                    : new Color(0.22f, 0.72f, 1f, 0.14f));
            glow.transform.localScale = new Vector3(1.055f, 1.055f, 1f);

            SpriteRenderer body = CreateSpriteRenderer("BodySprite", billboard, bodySprite, Color.white);
            body.gameObject.AddComponent<PixelSpriteMarker>().Configure(definition.Id);

            Sprite shadowSprite = Resources.Load<Sprite>(ShadowResourcePath);
            Transform shadowBillboard = CreateTransform("PixelShadow", root.transform, new Vector3(0f, 0.07f, 0.08f));
            SpriteRenderer shadow = CreateSpriteRenderer(
                "ShadowSprite",
                shadowBillboard,
                shadowSprite,
                Color.white);
            float shadowScale = CatherineYukiBattleKit.IsCatherine(definition.Id) ? 2.8f : 2.25f;
            shadow.transform.localScale = new Vector3(shadowScale, 1.35f, 1f);

            float bodyHeight = Mathf.Max(1.25f, bodySprite.bounds.size.y * rarityScale);
            Transform rightHand = CreateTransform("RightHandSocket", root.transform, new Vector3(0.46f, bodyHeight * 0.52f, 0f));
            Transform leftHand = CreateTransform("LeftHandSocket", root.transform, new Vector3(-0.46f, bodyHeight * 0.52f, 0f));
            Transform skillVfx = CreateTransform("SkillVfxSocket", root.transform, new Vector3(0f, bodyHeight * 0.54f, 0f));
            Transform projectile = CreateTransform("ProjectileSocket", root.transform, new Vector3(enemy ? -0.58f : 0.58f, bodyHeight * 0.50f, 0f));
            Transform groundVfx = CreateTransform("GroundVfxSocket", root.transform, new Vector3(0f, 0.08f, 0f));
            Transform target = CreateTransform("TargetSocket", root.transform, new Vector3(0f, bodyHeight * 0.56f, 0f));
            Transform healthBar = CreateTransform("HealthBarSocket", root.transform, new Vector3(0f, bodyHeight + 0.32f, 0f));

            CharacterView view = root.AddComponent<CharacterView>();
            view.ConfigureSockets(
                modelRoot,
                rightHand,
                leftHand,
                skillVfx,
                projectile,
                groundVfx,
                target,
                healthBar);

            PixelCharacterVisual pixelVisual = root.AddComponent<PixelCharacterVisual>();
            pixelVisual.Configure(
                definition.Id,
                billboard,
                body,
                glow,
                shadowBillboard,
                shadow,
                camera,
                enemy);

            if (CatherineYukiBattleKit.IsCatherine(definition.Id))
            {
                CatherineSkillVfxController vfx = root.AddComponent<CatherineSkillVfxController>();
                vfx.Configure(skillVfx, billboard, camera, Shader.Find("BubbleMind/Black Hole VFX"));
            }

            view.ResetView();
            return view;
        }

        private static Transform CreateTransform(string name, Transform parent, Vector3 localPosition)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            return child.transform;
        }

        private static SpriteRenderer CreateSpriteRenderer(
            string name,
            Transform parent,
            Sprite sprite,
            Color color)
        {
            GameObject child = new GameObject(name, typeof(SpriteRenderer));
            child.transform.SetParent(parent, false);
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }
    }

    /// <summary>Stable runtime marker used by verification and smoke tests.</summary>
    [DisallowMultipleComponent]
    public sealed class PixelSpriteMarker : MonoBehaviour
    {
        [SerializeField] private string characterId;

        public string CharacterId => characterId;

        public void Configure(string id)
        {
            characterId = id ?? string.Empty;
        }
    }
}
