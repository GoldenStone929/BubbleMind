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
                new Color(0.025f, 0.035f, 0.055f, 0.95f),
                new Vector2(0f, 0.91f),
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            DemoUiFactory.CreateDivider(
                "TopBarLine",
                topBar.transform,
                Vector2.zero,
                new Vector2(1f, 0f),
                Vector2.zero,
                new Vector2(0f, 2f));

            Image profile = DemoUiFactory.CreatePanel(
                "PlayerProfile",
                topBar.transform,
                new Color(0.08f, 0.105f, 0.15f, 0.92f),
                new Vector2(0.012f, 0.14f),
                new Vector2(0.205f, 0.86f),
                Vector2.zero,
                Vector2.zero);
            Text avatar = DemoUiFactory.CreateText(
                "AvatarGlyph",
                profile.transform,
                "BM",
                24,
                TextAnchor.MiddleCenter,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            SetAnchors(avatar.rectTransform, 0.02f, 0.10f, 0.24f, 0.90f);
            playerText = DemoUiFactory.CreateText(
                "PlayerText",
                profile.transform,
                "Observer 07  /  LV.12",
                19,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(playerText.rectTransform, 0.27f, 0.12f, 0.98f, 0.88f);

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
                new Color(0.11f, 0.17f, 0.22f, 0.96f),
                () => openFormation?.Invoke());
            SetAnchors(formation.GetComponent<RectTransform>(), 0.825f, 0.14f, 0.905f, 0.86f);

            Button mail = DemoUiFactory.CreateButton(
                "MailButton",
                topBar.transform,
                "MAIL",
                new Color(0.09f, 0.12f, 0.17f, 0.96f),
                () => openLockedFeature?.Invoke("MAIL"));
            SetAnchors(mail.GetComponent<RectTransform>(), 0.91f, 0.14f, 0.952f, 0.86f);
            Text mailLabel = mail.GetComponentInChildren<Text>();
            if (mailLabel != null)
            {
                mailLabel.text = "M";
            }

            Button settings = DemoUiFactory.CreateButton(
                "SettingsButton",
                topBar.transform,
                "SETTINGS",
                new Color(0.09f, 0.12f, 0.17f, 0.96f),
                () => openSettings?.Invoke());
            SetAnchors(settings.GetComponent<RectTransform>(), 0.956f, 0.14f, 0.995f, 0.86f);
            Text settingsLabel = settings.GetComponentInChildren<Text>();
            if (settingsLabel != null)
            {
                settingsLabel.text = "S";
            }

            Image dock = DemoUiFactory.CreatePanel(
                "BottomNavigation",
                safe,
                new Color(0.025f, 0.035f, 0.055f, 0.97f),
                Vector2.zero,
                new Vector2(1f, 0.095f),
                Vector2.zero,
                Vector2.zero);
            DemoUiFactory.CreateDivider(
                "BottomLine",
                dock.transform,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(0f, -2f),
                Vector2.zero);

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
                    ? new Color(0.14f, 0.34f, 0.34f, 1f)
                    : new Color(0.055f, 0.075f, 0.11f, 0.96f);
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
                new Color(0.055f, 0.075f, 0.11f, 0.96f),
                () => navigate?.Invoke(route));
            DemoUiFactory.SetLayout(button.gameObject, 185f, 76f, 1f, 1f);
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.fontSize = 18;
                text.resizeTextMaxSize = 20;
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
            Image chip = DemoUiFactory.CreatePanel(
                name + "Chip",
                parent,
                new Color(0.055f, 0.07f, 0.10f, 0.94f),
                new Vector2(minX, 0.19f),
                new Vector2(maxX, 0.81f),
                Vector2.zero,
                Vector2.zero);
            Text text = DemoUiFactory.CreateText(
                name,
                chip.transform,
                value,
                18,
                TextAnchor.MiddleCenter,
                color,
                FontStyle.Bold);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = 19;
            text.rectTransform.offsetMin = new Vector2(10f, 4f);
            text.rectTransform.offsetMax = new Vector2(-10f, -4f);
            return text;
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
