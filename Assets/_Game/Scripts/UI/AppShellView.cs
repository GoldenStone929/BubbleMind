using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GenericGachaRPG
{
    public sealed class AppShellView
    {
        private readonly Dictionary<AppRoute, Button> routeButtons =
            new Dictionary<AppRoute, Button>();
        private readonly Text playerText;
        private readonly Text crystalText;
        private readonly Text goldText;
        private readonly Text energyText;
        private readonly Text gachaBadge;
        private readonly Text missionBadge;
        private readonly Text worldBadge;

        public AppShellView(
            Transform parent,
            Action<AppRoute> navigate,
            Action openFormation,
            Action openSettings,
            Action<string> openLockedFeature)
        {
            Image rootImage = DemoUiFactory.CreatePanel(
                "AppShell",
                parent,
                new Color(0f, 0f, 0f, 0f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            rootImage.raycastTarget = false;
            Root = rootImage.gameObject;

            RectTransform safe = DemoUiFactory.CreateStretchRect("ShellSafeArea", Root.transform, 18f);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            Image topBar = DemoUiFactory.CreatePanel(
                "TopBar",
                safe,
                DemoUiFactory.PearlOverlay,
                new Vector2(0f, 0.918f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            DemoUiFactory.CreateDivider(
                "TopBarLine",
                topBar.transform,
                Vector2.zero,
                new Vector2(1f, 0f),
                Vector2.zero,
                new Vector2(0f, 2f),
                DemoUiFactory.InkLine);

            Image profile = DemoUiFactory.CreateFramedPanel(
                "PlayerProfile",
                topBar.transform,
                new Color(DemoUiFactory.Ink.r, DemoUiFactory.Ink.g, DemoUiFactory.Ink.b, 0.045f),
                new Vector2(0.012f, 0.14f),
                new Vector2(0.205f, 0.86f),
                Vector2.zero,
                Vector2.zero,
                DemoUiFactory.InkLine);
            Image avatarPlate = DemoUiFactory.CreatePanel(
                "AvatarPlate",
                profile.transform,
                DemoUiFactory.Cyan,
                new Vector2(0.018f, 0.10f),
                new Vector2(0.225f, 0.90f),
                Vector2.zero,
                Vector2.zero);
            Text avatar = DemoUiFactory.CreateText(
                "AvatarGlyph",
                avatarPlate.transform,
                "BM",
                20,
                TextAnchor.MiddleCenter,
                DemoUiFactory.Ink,
                FontStyle.Bold);
            SetAnchors(avatar.rectTransform, 0.05f, 0.05f, 0.95f, 0.95f);
            playerText = DemoUiFactory.CreateText(
                "PlayerText",
                profile.transform,
                "Observer 07  /  LV.12",
                17,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Ink,
                FontStyle.Bold);
            SetAnchors(playerText.rectTransform, 0.26f, 0.12f, 0.98f, 0.88f);
            playerText.resizeTextForBestFit = true;
            playerText.resizeTextMinSize = 12;
            playerText.resizeTextMaxSize = 17;

            crystalText = CreateResourceChip(
                "ShellCrystals",
                topBar.transform,
                "CRYSTAL 0",
                DemoUiFactory.Warning,
                0.44f,
                0.57f);
            goldText = CreateResourceChip(
                "ShellGold",
                topBar.transform,
                "GOLD 0",
                new Color(0.98f, 0.72f, 0.26f, 1f),
                0.575f,
                0.69f);
            energyText = CreateResourceChip(
                "ShellEnergy",
                topBar.transform,
                "ENERGY 0/0",
                DemoUiFactory.Positive,
                0.695f,
                0.82f);

            Button formation = DemoUiFactory.CreateButton(
                "FormationButton",
                topBar.transform,
                "FORMATION",
                DemoUiFactory.Coral,
                () => openFormation?.Invoke());
            SetAnchors(formation.GetComponent<RectTransform>(), 0.825f, 0.14f, 0.894f, 0.86f);
            StyleShellButton(formation, DemoUiFactory.Pearl, 15, true);

            Button mail = DemoUiFactory.CreateButton(
                "MailButton",
                topBar.transform,
                "MAIL",
                new Color(DemoUiFactory.Ink.r, DemoUiFactory.Ink.g, DemoUiFactory.Ink.b, 0.06f),
                () => openLockedFeature?.Invoke("MAIL"));
            SetAnchors(mail.GetComponent<RectTransform>(), 0.900f, 0.14f, 0.948f, 0.86f);
            StyleShellButton(mail, DemoUiFactory.Ink, 12, false);

            Button settings = DemoUiFactory.CreateButton(
                "SettingsButton",
                topBar.transform,
                "MENU",
                new Color(DemoUiFactory.Ink.r, DemoUiFactory.Ink.g, DemoUiFactory.Ink.b, 0.06f),
                () => openSettings?.Invoke());
            SetAnchors(settings.GetComponent<RectTransform>(), 0.952f, 0.14f, 0.999f, 0.86f);
            StyleShellButton(settings, DemoUiFactory.Ink, 12, false);

            Image dock = DemoUiFactory.CreatePanel(
                "BottomNavigation",
                safe,
                DemoUiFactory.PearlOverlay,
                Vector2.zero,
                new Vector2(1f, 0.088f),
                Vector2.zero,
                Vector2.zero);
            DemoUiFactory.CreateDivider(
                "BottomLine",
                dock.transform,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(0f, -2f),
                Vector2.zero,
                DemoUiFactory.InkLine);

            RectTransform nav = DemoUiFactory.CreateStretchRect("NavButtons", dock.transform, 8f);
            HorizontalLayoutGroup layout = DemoUiFactory.AddHorizontalLayout(
                nav.gameObject,
                6f,
                new RectOffset(0, 0, 0, 0));
            layout.childForceExpandWidth = true;

            CreateRouteButton("HomeNavButton", nav, "HOME", AppRoute.Home, navigate);
            Button world = CreateRouteButton("WorldNavButton", nav, "WORLD", AppRoute.World, navigate);
            worldBadge = CreateBadge(world.transform, "WorldBadge");
            CreateRouteButton("CollectionButton", nav, "HEROES", AppRoute.Collection, navigate);
            Button gacha = CreateRouteButton("GachaButton", nav, "RECRUIT", AppRoute.Gacha, navigate);
            gachaBadge = CreateBadge(gacha.transform, "GachaBadge");
            CreateRouteButton("InventoryNavButton", nav, "INVENTORY", AppRoute.Inventory, navigate);
            Button missions = CreateRouteButton("MissionNavButton", nav, "MISSIONS", AppRoute.Missions, navigate);
            missionBadge = CreateBadge(missions.transform, "MissionBadge");
        }

        public GameObject Root { get; }

        public void SetVisible(bool visible)
        {
            Root.SetActive(visible);
        }

        public void Refresh(PlayerState state, GameDatabase database)
        {
            if (state == null)
            {
                playerText.text = "OFFLINE PROFILE";
                crystalText.text = "CRYSTAL 0";
                goldText.text = "GOLD 0";
                energyText.text = "ENERGY 0/0";
                SetBadge(gachaBadge, false, string.Empty);
                SetBadge(missionBadge, false, string.Empty);
                SetBadge(worldBadge, false, string.Empty);
                return;
            }

            playerText.text = $"{state.PlayerName}  /  LV.{state.PlayerLevel}";
            crystalText.text = $"CRYSTAL {state.Currency:N0}";
            goldText.text = $"GOLD {state.Gold:N0}";
            energyText.text = $"ENERGY {state.Energy}/{state.MaxEnergy}";

            bool canDraw = database != null && database.DefaultBanner != null &&
                           state.Currency >= database.DefaultBanner.SingleDrawCost;
            int claimable = DemoMissionCatalog.CountClaimable(state);
            StageDefinition currentStage = database == null ? null : database.GetCurrentStage(state);
            bool hasNewStage = currentStage != null && !state.IsStageCleared(currentStage.Id);
            SetBadge(gachaBadge, canDraw, "!");
            SetBadge(missionBadge, claimable > 0, claimable.ToString());
            SetBadge(worldBadge, hasNewStage, "!");
        }

        public void SetActiveRoute(AppRoute route)
        {
            foreach (KeyValuePair<AppRoute, Button> pair in routeButtons)
            {
                Image image = pair.Value.targetGraphic as Image;
                if (image == null)
                {
                    continue;
                }

                image.color = pair.Key == route
                    ? new Color(DemoUiFactory.Cyan.r, DemoUiFactory.Cyan.g, DemoUiFactory.Cyan.b, 0.42f)
                    : new Color(DemoUiFactory.Ink.r, DemoUiFactory.Ink.g, DemoUiFactory.Ink.b, 0.035f);
                Text label = pair.Value.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.color = pair.Key == route ? DemoUiFactory.Ink : DemoUiFactory.InkSoft;
                }

                Outline outline = pair.Value.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = pair.Key == route
                        ? new Color(DemoUiFactory.Cyan.r, DemoUiFactory.Cyan.g, DemoUiFactory.Cyan.b, 0.85f)
                        : DemoUiFactory.InkLine;
                }
            }
        }

        private Button CreateRouteButton(
            string name,
            Transform parent,
            string label,
            AppRoute route,
            Action<AppRoute> navigate)
        {
            Button button = DemoUiFactory.CreateButton(
                name,
                parent,
                label,
                new Color(DemoUiFactory.Ink.r, DemoUiFactory.Ink.g, DemoUiFactory.Ink.b, 0.035f),
                () => navigate?.Invoke(route));
            DemoUiFactory.SetLayout(button.gameObject, 185f, 70f, 1f, 1f);
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = DemoUiFactory.InkSoft;
                text.fontSize = 16;
                text.resizeTextMinSize = 12;
                text.resizeTextMaxSize = 17;
                text.rectTransform.offsetMin = new Vector2(8f, 6f);
                text.rectTransform.offsetMax = new Vector2(-8f, -6f);
            }

            Outline outline = button.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = DemoUiFactory.InkLine;
            }

            routeButtons.Add(route, button);
            return button;
        }

        private static Text CreateResourceChip(
            string name,
            Transform parent,
            string value,
            Color color,
            float minX,
            float maxX)
        {
            Image chip = DemoUiFactory.CreateFramedPanel(
                name + "Chip",
                parent,
                new Color(DemoUiFactory.Ink.r, DemoUiFactory.Ink.g, DemoUiFactory.Ink.b, 0.045f),
                new Vector2(minX, 0.19f),
                new Vector2(maxX, 0.81f),
                Vector2.zero,
                Vector2.zero,
                DemoUiFactory.InkLine);
            Image rail = DemoUiFactory.CreatePanel(
                "AccentRail",
                chip.transform,
                color,
                Vector2.zero,
                new Vector2(0.026f, 1f),
                Vector2.zero,
                Vector2.zero);
            rail.raycastTarget = false;
            Text text = DemoUiFactory.CreateText(
                name,
                chip.transform,
                value,
                16,
                TextAnchor.MiddleCenter,
                DemoUiFactory.Ink,
                FontStyle.Bold);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 11;
            text.resizeTextMaxSize = 16;
            text.rectTransform.offsetMin = new Vector2(10f, 4f);
            text.rectTransform.offsetMax = new Vector2(-10f, -4f);
            return text;
        }

        private static void StyleShellButton(Button button, Color textColor, int maxFontSize, bool strong)
        {
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = textColor;
                text.fontSize = maxFontSize;
                text.resizeTextMinSize = Mathf.Min(10, maxFontSize);
                text.resizeTextMaxSize = maxFontSize;
                text.rectTransform.offsetMin = new Vector2(6f, 5f);
                text.rectTransform.offsetMax = new Vector2(-6f, -5f);
            }

            Outline outline = button.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = strong
                    ? new Color(DemoUiFactory.Pearl.r, DemoUiFactory.Pearl.g, DemoUiFactory.Pearl.b, 0.40f)
                    : DemoUiFactory.InkLine;
            }
        }

        private static Text CreateBadge(Transform parent, string name)
        {
            Image badge = DemoUiFactory.CreatePanel(
                name,
                parent,
                DemoUiFactory.Danger,
                new Vector2(0.78f, 0.63f),
                new Vector2(0.96f, 0.94f),
                Vector2.zero,
                Vector2.zero);
            Text text = DemoUiFactory.CreateText(
                "Value",
                badge.transform,
                "!",
                14,
                TextAnchor.MiddleCenter,
                Color.white,
                FontStyle.Bold);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 9;
            text.resizeTextMaxSize = 15;
            badge.gameObject.SetActive(false);
            return text;
        }

        private static void SetBadge(Text text, bool visible, string value)
        {
            if (text == null)
            {
                return;
            }

            text.text = value ?? string.Empty;
            text.transform.parent.gameObject.SetActive(visible);
        }

        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
