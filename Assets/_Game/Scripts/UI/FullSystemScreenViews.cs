using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GenericGachaRPG
{
    public sealed class HomeHubScreenView : DemoScreenView
    {
        private readonly Text currencyText;
        private readonly Text stageText;
        private readonly Text progressText;
        private readonly Text recruitStatusText;
        private readonly Text heroesStatusText;
        private readonly Text formationStatusText;
        private readonly Button continueButton;

        public HomeHubScreenView(
            Transform parent,
            Action openWorld,
            Action openGacha,
            Action openCollection,
            Action openFormation,
            Action<string> openLockedFeature)
            : base(CreateRoot(parent))
        {
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 34f);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            Text location = DemoUiFactory.CreateText(
                "Location",
                safe,
                "ABYSSAL OBSERVATORY  /  INNER RING",
                16,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Cyan,
                FontStyle.Bold);
            SetAnchors(location.rectTransform, 0.045f, 0.83f, 0.52f, 0.875f);
            ApplyBackdropShadow(location, 0.72f);

            Text title = DemoUiFactory.CreateText(
                "Title",
                safe,
                "BubbleMind",
                62,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Pearl,
                FontStyle.Bold);
            SetAnchors(title.rectTransform, 0.045f, 0.72f, 0.52f, 0.83f);
            ApplyBackdropShadow(title, 0.78f);

            Text subtitle = DemoUiFactory.CreateText(
                "Subtitle",
                safe,
                "The observatory is stable. A fracture remains beyond the eastern ring.",
                19,
                TextAnchor.UpperLeft,
                new Color(DemoUiFactory.Pearl.r, DemoUiFactory.Pearl.g, DemoUiFactory.Pearl.b, 0.88f));
            SetAnchors(subtitle.rectTransform, 0.047f, 0.64f, 0.49f, 0.72f);
            ApplyBackdropShadow(subtitle, 0.72f);

            Image accountBand = DemoUiFactory.CreateFramedPanel(
                "AccountSummary",
                safe,
                DemoUiFactory.PearlOverlay,
                new Vector2(0.045f, 0.555f),
                new Vector2(0.405f, 0.615f),
                Vector2.zero,
                Vector2.zero,
                DemoUiFactory.InkLine);
            Image accountAccent = DemoUiFactory.CreatePanel(
                "AccountAccent",
                accountBand.transform,
                DemoUiFactory.Gold,
                Vector2.zero,
                new Vector2(0.012f, 1f),
                Vector2.zero,
                Vector2.zero);
            accountAccent.raycastTarget = false;
            currencyText = DemoUiFactory.CreateText(
                "Currency",
                accountBand.transform,
                "CRYSTALS 0",
                17,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Ink,
                FontStyle.Bold);
            SetAnchors(currencyText.rectTransform, 0.05f, 0.08f, 0.48f, 0.92f);
            progressText = DemoUiFactory.CreateText(
                "HubProgress",
                accountBand.transform,
                "0 STAGES CLEARED",
                15,
                TextAnchor.MiddleRight,
                DemoUiFactory.InkSoft,
                FontStyle.Bold);
            SetAnchors(progressText.rectTransform, 0.50f, 0.08f, 0.96f, 0.92f);

            recruitStatusText = CreateHotspot(
                "HomeRecruitHotspot",
                safe,
                "SIGNAL DECK",
                "RECRUIT",
                "STANDARD SIGNAL",
                new Vector2(0.585f, 0.69f),
                new Vector2(0.77f, 0.815f),
                DemoUiFactory.Cyan,
                () => openGacha?.Invoke());
            heroesStatusText = CreateHotspot(
                "HomeHeroesHotspot",
                safe,
                "ARCHIVE",
                "CHARACTERS",
                "7 SIGNALS RECORDED",
                new Vector2(0.775f, 0.535f),
                new Vector2(0.955f, 0.66f),
                DemoUiFactory.Gold,
                () => openCollection?.Invoke());
            formationStatusText = CreateHotspot(
                "HomeFormationHotspot",
                safe,
                "FIVE SLOTS",
                "FORMATION",
                "ACTIVE TEAM",
                new Vector2(0.60f, 0.385f),
                new Vector2(0.79f, 0.51f),
                DemoUiFactory.Leaf,
                () => openFormation?.Invoke());

            Image trialBand = DemoUiFactory.CreateFramedPanel(
                "ContinueBand",
                safe,
                DemoUiFactory.InkGlass,
                new Vector2(0.535f, 0.155f),
                new Vector2(0.955f, 0.315f),
                Vector2.zero,
                Vector2.zero,
                new Color(DemoUiFactory.Pearl.r, DemoUiFactory.Pearl.g, DemoUiFactory.Pearl.b, 0.34f),
                2f);
            Image trialAccent = DemoUiFactory.CreatePanel(
                "ContinueAccent",
                trialBand.transform,
                DemoUiFactory.Coral,
                Vector2.zero,
                new Vector2(0.014f, 1f),
                Vector2.zero,
                Vector2.zero);
            trialAccent.raycastTarget = false;
            Text eyebrow = DemoUiFactory.CreateText(
                "ContinueEyebrow",
                trialBand.transform,
                "CURRENT OBJECTIVE",
                13,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Coral,
                FontStyle.Bold);
            SetAnchors(eyebrow.rectTransform, 0.055f, 0.62f, 0.52f, 0.88f);
            stageText = DemoUiFactory.CreateText(
                "CurrentStage",
                trialBand.transform,
                "CHAPTER 1-1",
                25,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Pearl,
                FontStyle.Bold);
            SetAnchors(stageText.rectTransform, 0.055f, 0.15f, 0.66f, 0.64f);
            stageText.resizeTextForBestFit = true;
            stageText.resizeTextMinSize = 17;
            stageText.resizeTextMaxSize = 25;
            continueButton = DemoUiFactory.CreateButton(
                "ContinueStageButton",
                trialBand.transform,
                "CHALLENGE",
                DemoUiFactory.Coral,
                () => openWorld?.Invoke());
            SetAnchors(continueButton.GetComponent<RectTransform>(), 0.70f, 0.19f, 0.95f, 0.81f);

            Image featureStrip = DemoUiFactory.CreateFramedPanel(
                "FutureFeatures",
                safe,
                DemoUiFactory.PearlOverlay,
                new Vector2(0.045f, 0.17f),
                new Vector2(0.485f, 0.265f),
                Vector2.zero,
                Vector2.zero,
                DemoUiFactory.InkLine);
            CreateFeatureButton(featureStrip.transform, "ArenaFeatureButton", "ARENA", 0.02f, 0.25f,
                () => openLockedFeature?.Invoke("ARENA"));
            CreateFeatureButton(featureStrip.transform, "EventsFeatureButton", "EVENTS", 0.27f, 0.50f,
                () => openLockedFeature?.Invoke("EVENTS"));
            CreateFeatureButton(featureStrip.transform, "ShopFeatureButton", "SHOP", 0.52f, 0.75f,
                () => openLockedFeature?.Invoke("SHOP"));
            CreateFeatureButton(featureStrip.transform, "GuildFeatureButton", "GUILD", 0.77f, 1.00f,
                () => openLockedFeature?.Invoke("GUILD"));
        }

        public void Refresh(PlayerState state, GameDatabase database)
        {
            RefreshCommandStatuses(state, database);

            int currency = state == null ? 0 : state.Currency;
            int cleared = state == null || state.ClearedStageIds == null
                ? 0
                : state.ClearedStageIds.Count;
            currencyText.text = $"CRYSTALS  {currency:N0}";
            progressText.text = $"{cleared} STAGE{(cleared == 1 ? string.Empty : "S")} CLEARED";

            StageDefinition stage = state == null || database == null
                ? null
                : database.GetCurrentStage(state);
            if (stage == null)
            {
                stageText.text = state == null ? "PROFILE DATA UNAVAILABLE" : "WORLD DATA UNAVAILABLE";
                continueButton.interactable = false;
                return;
            }

            stageText.text = $"{FormatStageId(stage.Id)}  {stage.DisplayName}";
            continueButton.interactable = true;
        }

        private void RefreshCommandStatuses(PlayerState state, GameDatabase database)
        {
            GachaBannerDefinition banner = database == null ? null : database.DefaultBanner;
            if (banner == null)
            {
                recruitStatusText.text = "SIGNAL DATA OFFLINE";
            }
            else
            {
                string bannerName = string.IsNullOrWhiteSpace(banner.DisplayName)
                    ? "STANDARD SIGNAL"
                    : banner.DisplayName.Trim().ToUpperInvariant();
                if (state == null)
                {
                    recruitStatusText.text = $"{bannerName}  /  OFFLINE";
                }
                else if (state.Currency >= banner.SingleDrawCost)
                {
                    recruitStatusText.text = $"{bannerName}  /  READY";
                }
                else
                {
                    int needed = Mathf.Max(0, banner.SingleDrawCost - state.Currency);
                    recruitStatusText.text = $"{bannerName}  /  NEED {needed:N0}";
                }
            }

            string ownedCount = state == null || state.OwnedCharacters == null
                ? "--"
                : state.OwnedCharacters.Count.ToString();
            string totalCount = database == null || database.Characters == null
                ? "--"
                : database.Characters.Count.ToString();
            heroesStatusText.text = $"OWNED {ownedCount}  /  {totalCount}";

            int requiredSlots = TeamFormationState.RequiredMemberCount;
            formationStatusText.text = state == null || state.TeamFormation == null
                ? $"SLOTS --  /  {requiredSlots}"
                : $"SLOTS {state.TeamFormation.Count}  /  {requiredSlots}";
        }

        private static GameObject CreateRoot(Transform parent)
        {
            return DemoUiFactory.CreateScreenRoot(
                "HomeScreen",
                parent,
                1f,
                new Color(0.018f, 0.028f, 0.045f, 0.18f));
        }

        private static Text CreateHotspot(
            string name,
            Transform parent,
            string eyebrow,
            string title,
            string status,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color accent,
            Action action)
        {
            Button button = DemoUiFactory.CreateCommandButton(
                name,
                parent,
                eyebrow,
                title,
                status,
                accent,
                () => action?.Invoke(),
                out Text statusText);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return statusText;
        }

        private static void CreateFeatureButton(
            Transform parent,
            string name,
            string label,
            float minX,
            float maxX,
            Action action)
        {
            Button button = DemoUiFactory.CreateButton(
                name,
                parent,
                label + "  LOCKED",
                new Color(DemoUiFactory.Ink.r, DemoUiFactory.Ink.g, DemoUiFactory.Ink.b, 0.08f),
                () => action?.Invoke());
            SetAnchors(button.GetComponent<RectTransform>(), minX, 0.12f, maxX - 0.015f, 0.88f);
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.color = DemoUiFactory.Ink;
                text.fontSize = 13;
                text.resizeTextMinSize = 10;
                text.resizeTextMaxSize = 14;
                text.rectTransform.offsetMin = new Vector2(6f, 4f);
                text.rectTransform.offsetMax = new Vector2(-6f, -4f);
            }

            Outline outline = button.GetComponent<Outline>();
            if (outline != null)
            {
                outline.effectColor = DemoUiFactory.InkLine;
            }
        }

        private static void ApplyBackdropShadow(Text text, float alpha)
        {
            Shadow shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.01f, 0.02f, 0.035f, alpha);
            shadow.effectDistance = new Vector2(2f, -2f);
            shadow.useGraphicAlpha = true;
        }

        private static string FormatStageId(string stageId)
        {
            return string.IsNullOrEmpty(stageId)
                ? "STAGE"
                : stageId.Replace("stage_", string.Empty).Replace('_', '-');
        }

        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public sealed class WorldStageScreenView : DemoScreenView
    {
        private readonly GameDatabase database;
        private readonly Dictionary<string, Button> stageButtons = new Dictionary<string, Button>(StringComparer.Ordinal);
        private readonly Text stageTitle;
        private readonly Text stageDescription;
        private readonly Text stageStatus;
        private readonly Text stageRewards;
        private readonly Text formationStatus;
        private readonly Button deployButton;
        private string selectedStageId = string.Empty;

        public WorldStageScreenView(
            Transform parent,
            GameDatabase gameDatabase,
            Action<string> selectStage,
            Action deploy,
            Action back)
            : base(CreateRoot(parent))
        {
            database = gameDatabase;
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 28f);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            Button backButton = DemoUiFactory.CreateButton(
                "BackButton",
                safe,
                "BACK",
                new Color(0.06f, 0.08f, 0.11f, 0.96f),
                () => back?.Invoke());
            SetAnchors(backButton.GetComponent<RectTransform>(), 0.02f, 0.79f, 0.10f, 0.87f);

            Text eyebrow = DemoUiFactory.CreateText(
                "WorldEyebrow",
                safe,
                "WORLD  /  CHAPTER 01",
                16,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            SetAnchors(eyebrow.rectTransform, 0.12f, 0.83f, 0.58f, 0.88f);
            Text title = DemoUiFactory.CreateText(
                "WorldTitle",
                safe,
                "Abyssal Observatory",
                40,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(title.rectTransform, 0.12f, 0.75f, 0.58f, 0.84f);

            Image routeBand = DemoUiFactory.CreatePanel(
                "StageRoute",
                safe,
                new Color(0.02f, 0.035f, 0.055f, 0.58f),
                new Vector2(0.03f, 0.14f),
                new Vector2(0.64f, 0.72f),
                Vector2.zero,
                Vector2.zero);
            CreateRouteLine(routeBand.transform, 0.19f, 0.31f, 0.49f, 0.54f);
            CreateRouteLine(routeBand.transform, 0.49f, 0.54f, 0.78f, 0.35f);

            Vector2[] positions =
            {
                new Vector2(0.20f, 0.31f),
                new Vector2(0.50f, 0.54f),
                new Vector2(0.79f, 0.35f)
            };
            if (database != null)
            {
                for (int index = 0; index < database.Stages.Count; index++)
                {
                    StageDefinition stage = database.Stages[index];
                    if (stage == null)
                    {
                        continue;
                    }

                    Vector2 position = positions[Mathf.Min(index, positions.Length - 1)];
                    Button node = DemoUiFactory.CreateButton(
                        "StageNode_" + stage.Id,
                        routeBand.transform,
                        $"{FormatStageId(stage.Id)}\n{stage.DisplayName}",
                        new Color(0.09f, 0.14f, 0.19f, 0.98f),
                        () => selectStage?.Invoke(stage.Id));
                    RectTransform nodeRect = node.GetComponent<RectTransform>();
                    nodeRect.anchorMin = position - new Vector2(0.105f, 0.12f);
                    nodeRect.anchorMax = position + new Vector2(0.105f, 0.12f);
                    nodeRect.offsetMin = Vector2.zero;
                    nodeRect.offsetMax = Vector2.zero;
                    stageButtons.Add(stage.Id, node);
                }
            }

            Image detail = DemoUiFactory.CreatePanel(
                "StageDetail",
                safe,
                new Color(0.035f, 0.055f, 0.08f, 0.96f),
                new Vector2(0.67f, 0.14f),
                new Vector2(0.97f, 0.72f),
                Vector2.zero,
                Vector2.zero);
            stageStatus = DemoUiFactory.CreateText(
                "StageStatus",
                detail.transform,
                "SELECT A STAGE",
                15,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Warning,
                FontStyle.Bold);
            SetAnchors(stageStatus.rectTransform, 0.07f, 0.86f, 0.93f, 0.96f);
            stageTitle = DemoUiFactory.CreateText(
                "StageTitle",
                detail.transform,
                "Stage",
                29,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(stageTitle.rectTransform, 0.07f, 0.72f, 0.93f, 0.87f);
            stageDescription = DemoUiFactory.CreateText(
                "StageDescription",
                detail.transform,
                string.Empty,
                18,
                TextAnchor.UpperLeft,
                DemoUiFactory.TextSecondary);
            SetAnchors(stageDescription.rectTransform, 0.07f, 0.49f, 0.93f, 0.72f);
            stageRewards = DemoUiFactory.CreateText(
                "StageRewards",
                detail.transform,
                string.Empty,
                17,
                TextAnchor.UpperLeft,
                DemoUiFactory.TextSecondary,
                FontStyle.Bold);
            SetAnchors(stageRewards.rectTransform, 0.07f, 0.29f, 0.93f, 0.49f);
            formationStatus = DemoUiFactory.CreateText(
                "FormationStatus",
                detail.transform,
                string.Empty,
                15,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextMuted);
            SetAnchors(formationStatus.rectTransform, 0.07f, 0.20f, 0.93f, 0.29f);
            deployButton = DemoUiFactory.CreateButton(
                "StageDeployButton",
                detail.transform,
                "PREPARE FORMATION",
                DemoUiFactory.Action,
                () => deploy?.Invoke());
            SetAnchors(deployButton.GetComponent<RectTransform>(), 0.07f, 0.05f, 0.93f, 0.19f);
        }

        public void Refresh(PlayerState state, string preferredStageId = "")
        {
            if (database == null || database.Stages.Count == 0)
            {
                stageTitle.text = "No stage data";
                stageDescription.text = "Run Generate or Repair Demo.";
                deployButton.interactable = false;
                return;
            }

            if (!string.IsNullOrEmpty(preferredStageId) && database.GetStage(preferredStageId) != null)
            {
                selectedStageId = preferredStageId;
            }
            else if (string.IsNullOrEmpty(selectedStageId))
            {
                StageDefinition current = database.GetCurrentStage(state) ?? database.FirstStage;
                selectedStageId = current == null ? string.Empty : current.Id;
            }

            foreach (KeyValuePair<string, Button> pair in stageButtons)
            {
                StageDefinition candidate = database.GetStage(pair.Key);
                bool unlocked = database.IsStageUnlocked(candidate, state);
                bool cleared = state != null && state.IsStageCleared(pair.Key);
                bool selected = string.Equals(pair.Key, selectedStageId, StringComparison.Ordinal);
                Image image = pair.Value.targetGraphic as Image;
                if (image != null)
                {
                    image.color = selected
                        ? new Color(0.18f, 0.46f, 0.43f, 1f)
                        : cleared
                            ? new Color(0.17f, 0.31f, 0.25f, 0.98f)
                            : unlocked
                                ? new Color(0.10f, 0.18f, 0.25f, 0.98f)
                                : new Color(0.055f, 0.065f, 0.08f, 0.90f);
                }

                Text label = pair.Value.GetComponentInChildren<Text>();
                if (label != null && candidate != null)
                {
                    string stateLabel = cleared ? "CLEARED" : unlocked ? "OPEN" : "LOCKED";
                    label.text = $"{FormatStageId(candidate.Id)}  {stateLabel}\n{candidate.DisplayName}";
                }
            }

            StageDefinition stage = database.GetStage(selectedStageId) ?? database.FirstStage;
            if (stage == null)
            {
                deployButton.interactable = false;
                return;
            }

            bool isUnlocked = database.IsStageUnlocked(stage, state);
            bool isCleared = state != null && state.IsStageCleared(stage.Id);
            bool hasFormation = state != null && state.TeamFormation != null && state.TeamFormation.IsComplete;
            bool hasEnergy = state != null && state.Energy >= stage.EnergyCost;
            stageStatus.text = isCleared ? "CLEARED  /  REPEATABLE" : isUnlocked ? "CURRENT OBJECTIVE" : "LOCKED";
            stageStatus.color = isCleared
                ? DemoUiFactory.Positive
                : isUnlocked
                    ? DemoUiFactory.Warning
                    : DemoUiFactory.TextMuted;
            stageTitle.text = $"{FormatStageId(stage.Id)}  {stage.DisplayName}";
            stageDescription.text = stage.Description;
            stageRewards.text =
                $"ENERGY  {stage.EnergyCost}    POWER  {stage.RecommendedPower:N0}\n" +
                $"FIRST CLEAR  {stage.FirstClearCrystalReward} CRYSTALS\n" +
                $"BATTLE DROP  {stage.GoldReward} GOLD + {stage.MaterialReward} ECHO GEL";
            formationStatus.text = !isUnlocked
                ? $"Clear {FormatStageId(stage.PrerequisiteStageId)} first."
                : !hasFormation
                    ? "A complete five-slot formation is required."
                    : !hasEnergy
                        ? "Not enough energy."
                        : "Formation synchronized and ready.";
            formationStatus.color = isUnlocked && hasFormation && hasEnergy
                ? DemoUiFactory.Positive
                : DemoUiFactory.Warning;
            deployButton.interactable = isUnlocked && hasEnergy;
            Text deployLabel = deployButton.GetComponentInChildren<Text>();
            if (deployLabel != null)
            {
                deployLabel.text = hasFormation ? "ENTER FORMATION" : "COMPLETE FORMATION";
            }
        }

        public string SelectedStageId => selectedStageId;

        private static GameObject CreateRoot(Transform parent)
        {
            return DemoUiFactory.CreateScreenRoot("WorldScreen", parent, 0.72f);
        }

        private static void CreateRouteLine(
            Transform parent,
            float startX,
            float startY,
            float endX,
            float endY)
        {
            Vector2 start = new Vector2(startX, startY);
            Vector2 end = new Vector2(endX, endY);
            Vector2 midpoint = (start + end) * 0.5f;
            float distance = Vector2.Distance(start, end);
            RectTransform line = DemoUiFactory.CreateRect(
                "RouteLine",
                parent,
                midpoint,
                midpoint,
                new Vector2(-distance * 350f, -2f),
                new Vector2(distance * 350f, 2f));
            Image image = line.gameObject.AddComponent<Image>();
            image.color = new Color(0.55f, 0.78f, 0.75f, 0.44f);
            float angle = Mathf.Atan2(endY - startY, endX - startX) * Mathf.Rad2Deg;
            line.localRotation = Quaternion.Euler(0f, 0f, angle);
            image.raycastTarget = false;
        }

        private static string FormatStageId(string stageId)
        {
            return string.IsNullOrEmpty(stageId)
                ? "STAGE"
                : stageId.Replace("stage_", string.Empty).Replace('_', '-');
        }

        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public sealed class InventoryScreenView : DemoScreenView
    {
        private readonly Dictionary<string, Text> amountLabels = new Dictionary<string, Text>(StringComparer.Ordinal);
        private readonly Text detailName;
        private readonly Text detailCategory;
        private readonly Text detailDescription;
        private readonly Text detailAmount;
        private string selectedItemId = "standard_ticket";

        public InventoryScreenView(Transform parent, Action back)
            : base(CreateRoot(parent))
        {
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 30f);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            Button backButton = DemoUiFactory.CreateButton(
                "InventoryBackButton",
                safe,
                "BACK",
                new Color(0.06f, 0.08f, 0.11f, 0.96f),
                () => back?.Invoke());
            SetAnchors(backButton.GetComponent<RectTransform>(), 0.02f, 0.79f, 0.10f, 0.87f);

            Text title = DemoUiFactory.CreateText(
                "InventoryTitle",
                safe,
                "Inventory",
                40,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(title.rectTransform, 0.12f, 0.78f, 0.54f, 0.88f);
            Text tabs = DemoUiFactory.CreateText(
                "InventoryTabs",
                safe,
                "ALL    MATERIALS    SHARDS    RECRUITMENT",
                16,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            SetAnchors(tabs.rectTransform, 0.12f, 0.72f, 0.65f, 0.78f);

            Image grid = DemoUiFactory.CreatePanel(
                "InventoryGrid",
                safe,
                new Color(0.02f, 0.03f, 0.045f, 0.64f),
                new Vector2(0.03f, 0.14f),
                new Vector2(0.66f, 0.70f),
                Vector2.zero,
                Vector2.zero);
            IReadOnlyList<DemoInventoryDefinition> definitions = DemoInventoryCatalog.All;
            for (int index = 0; index < definitions.Count; index++)
            {
                DemoInventoryDefinition item = definitions[index];
                int column = index % 2;
                int row = index / 2;
                float minX = 0.04f + column * 0.48f;
                float maxX = minX + 0.44f;
                float maxY = 0.93f - row * 0.45f;
                float minY = maxY - 0.37f;
                Button button = DemoUiFactory.CreateButton(
                    "InventoryItem_" + item.Id,
                    grid.transform,
                    item.Name,
                    new Color(0.08f, 0.12f, 0.17f, 0.97f),
                    () => SelectItem(item.Id));
                SetAnchors(button.GetComponent<RectTransform>(), minX, minY, maxX, maxY);
                Text label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.alignment = TextAnchor.UpperLeft;
                    label.fontSize = 20;
                    label.rectTransform.offsetMin = new Vector2(18f, 14f);
                    label.rectTransform.offsetMax = new Vector2(-18f, -14f);
                }

                Text amount = DemoUiFactory.CreateText(
                    "ItemAmount_" + item.Id,
                    button.transform,
                    "x0",
                    24,
                    TextAnchor.LowerRight,
                    DemoUiFactory.Warning,
                    FontStyle.Bold);
                amount.rectTransform.offsetMin = new Vector2(18f, 12f);
                amount.rectTransform.offsetMax = new Vector2(-18f, -12f);
                amountLabels.Add(item.Id, amount);
            }

            Image detail = DemoUiFactory.CreatePanel(
                "InventoryDetail",
                safe,
                new Color(0.035f, 0.055f, 0.08f, 0.96f),
                new Vector2(0.69f, 0.14f),
                new Vector2(0.97f, 0.70f),
                Vector2.zero,
                Vector2.zero);
            detailCategory = DemoUiFactory.CreateText(
                "ItemCategory",
                detail.transform,
                "CATEGORY",
                15,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            SetAnchors(detailCategory.rectTransform, 0.08f, 0.85f, 0.92f, 0.95f);
            detailName = DemoUiFactory.CreateText(
                "ItemName",
                detail.transform,
                "Item",
                29,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(detailName.rectTransform, 0.08f, 0.69f, 0.92f, 0.86f);
            detailAmount = DemoUiFactory.CreateText(
                "ItemOwned",
                detail.transform,
                "OWNED 0",
                20,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Warning,
                FontStyle.Bold);
            SetAnchors(detailAmount.rectTransform, 0.08f, 0.58f, 0.92f, 0.69f);
            detailDescription = DemoUiFactory.CreateText(
                "ItemDescription",
                detail.transform,
                string.Empty,
                18,
                TextAnchor.UpperLeft,
                DemoUiFactory.TextSecondary);
            SetAnchors(detailDescription.rectTransform, 0.08f, 0.25f, 0.92f, 0.56f);
            Text useHint = DemoUiFactory.CreateText(
                "ItemUseHint",
                detail.transform,
                "Materials are consumed by future progression systems. No item is spent from this screen.",
                15,
                TextAnchor.LowerLeft,
                DemoUiFactory.TextMuted);
            SetAnchors(useHint.rectTransform, 0.08f, 0.06f, 0.92f, 0.23f);
        }

        public void Refresh(PlayerState state)
        {
            IReadOnlyList<DemoInventoryDefinition> definitions = DemoInventoryCatalog.All;
            for (int index = 0; index < definitions.Count; index++)
            {
                DemoInventoryDefinition item = definitions[index];
                if (amountLabels.TryGetValue(item.Id, out Text amount))
                {
                    amount.text = $"x{(state == null ? 0 : state.GetItemAmount(item.Id)):N0}";
                }
            }

            ShowSelected(state);
        }

        private void SelectItem(string itemId)
        {
            selectedItemId = itemId ?? string.Empty;
            PlayerState state = UnityEngine.Object.FindAnyObjectByType<DemoGameController>()?.CurrentPlayerState;
            ShowSelected(state);
        }

        private void ShowSelected(PlayerState state)
        {
            DemoInventoryDefinition item = DemoInventoryCatalog.Get(selectedItemId) ?? DemoInventoryCatalog.All[0];
            selectedItemId = item.Id;
            detailCategory.text = item.Category;
            detailName.text = item.Name;
            detailDescription.text = item.Description;
            detailAmount.text = $"OWNED  {(state == null ? 0 : state.GetItemAmount(item.Id)):N0}";
        }

        private static GameObject CreateRoot(Transform parent)
        {
            return DemoUiFactory.CreateScreenRoot("InventoryScreen", parent, 0.34f);
        }

        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public sealed class MissionsScreenView : DemoScreenView
    {
        private sealed class MissionRow
        {
            public Text ProgressText;
            public Image ProgressFill;
            public Button ActionButton;
        }

        private readonly Dictionary<string, MissionRow> rows = new Dictionary<string, MissionRow>(StringComparer.Ordinal);
        private readonly Text feedbackText;
        private PlayerState currentState;

        public MissionsScreenView(
            Transform parent,
            Action<string> claimMission,
            Action<string> goToMission,
            Action back)
            : base(CreateRoot(parent))
        {
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 30f);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            Button backButton = DemoUiFactory.CreateButton(
                "MissionsBackButton",
                safe,
                "BACK",
                new Color(0.06f, 0.08f, 0.11f, 0.96f),
                () => back?.Invoke());
            SetAnchors(backButton.GetComponent<RectTransform>(), 0.02f, 0.79f, 0.10f, 0.87f);
            Text title = DemoUiFactory.CreateText(
                "MissionsTitle",
                safe,
                "Missions",
                40,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(title.rectTransform, 0.12f, 0.78f, 0.50f, 0.88f);
            Text tabs = DemoUiFactory.CreateText(
                "MissionTabs",
                safe,
                "STORY    TRAINING    ACHIEVEMENTS",
                16,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            SetAnchors(tabs.rectTransform, 0.12f, 0.72f, 0.65f, 0.78f);

            RectTransform list = DemoUiFactory.CreateRect(
                "MissionList",
                safe,
                new Vector2(0.07f, 0.16f),
                new Vector2(0.93f, 0.69f),
                Vector2.zero,
                Vector2.zero);
            VerticalLayoutGroup layout = DemoUiFactory.AddVerticalLayout(
                list.gameObject,
                10f,
                new RectOffset(0, 0, 0, 0));
            layout.childForceExpandHeight = true;

            IReadOnlyList<DemoMissionDefinition> definitions = DemoMissionCatalog.All;
            for (int index = 0; index < definitions.Count; index++)
            {
                DemoMissionDefinition mission = definitions[index];
                Image row = DemoUiFactory.CreatePanel(
                    "Mission_" + mission.Id,
                    list,
                    new Color(0.04f, 0.065f, 0.095f, 0.96f),
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
                DemoUiFactory.SetLayout(row.gameObject, -1f, 112f, 1f, 1f);
                Text rowTitle = DemoUiFactory.CreateText(
                    "Title",
                    row.transform,
                    mission.Title,
                    21,
                    TextAnchor.MiddleLeft,
                    DemoUiFactory.TextPrimary,
                    FontStyle.Bold);
                SetAnchors(rowTitle.rectTransform, 0.03f, 0.50f, 0.52f, 0.89f);
                Text description = DemoUiFactory.CreateText(
                    "Description",
                    row.transform,
                    mission.Description,
                    16,
                    TextAnchor.MiddleLeft,
                    DemoUiFactory.TextSecondary);
                SetAnchors(description.rectTransform, 0.03f, 0.12f, 0.52f, 0.52f);

                Image progressTrack = DemoUiFactory.CreatePanel(
                    "ProgressTrack",
                    row.transform,
                    new Color(0.02f, 0.03f, 0.045f, 1f),
                    new Vector2(0.55f, 0.25f),
                    new Vector2(0.75f, 0.44f),
                    Vector2.zero,
                    Vector2.zero);
                Image progressFill = DemoUiFactory.CreatePanel(
                    "ProgressFill",
                    progressTrack.transform,
                    DemoUiFactory.Accent,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
                Text progressText = DemoUiFactory.CreateText(
                    "ProgressText",
                    row.transform,
                    "0 / 1",
                    15,
                    TextAnchor.MiddleCenter,
                    DemoUiFactory.TextPrimary,
                    FontStyle.Bold);
                SetAnchors(progressText.rectTransform, 0.55f, 0.45f, 0.75f, 0.72f);
                Text reward = DemoUiFactory.CreateText(
                    "Reward",
                    row.transform,
                    $"{mission.CrystalReward} CRYSTAL  +  {mission.GoldReward} GOLD",
                    14,
                    TextAnchor.MiddleCenter,
                    DemoUiFactory.Warning,
                    FontStyle.Bold);
                SetAnchors(reward.rectTransform, 0.55f, 0.70f, 0.75f, 0.92f);

                Button action = DemoUiFactory.CreateButton(
                    "MissionAction_" + mission.Id,
                    row.transform,
                    "GO",
                    DemoUiFactory.SurfaceLight,
                    () => HandleMissionAction(mission, claimMission, goToMission));
                SetAnchors(action.GetComponent<RectTransform>(), 0.79f, 0.19f, 0.96f, 0.81f);
                rows.Add(mission.Id, new MissionRow
                {
                    ProgressText = progressText,
                    ProgressFill = progressFill,
                    ActionButton = action
                });
            }

            feedbackText = DemoUiFactory.CreateText(
                "MissionFeedback",
                safe,
                string.Empty,
                16,
                TextAnchor.MiddleCenter,
                DemoUiFactory.Positive,
                FontStyle.Bold);
            SetAnchors(feedbackText.rectTransform, 0.18f, 0.105f, 0.82f, 0.15f);
        }

        public void Refresh(PlayerState state, string feedback = "")
        {
            currentState = state;
            feedbackText.text = feedback ?? string.Empty;
            IReadOnlyList<DemoMissionDefinition> definitions = DemoMissionCatalog.All;
            for (int index = 0; index < definitions.Count; index++)
            {
                DemoMissionDefinition mission = definitions[index];
                MissionRow row = rows[mission.Id];
                int progress = Mathf.Min(mission.Target, mission.GetProgress(state));
                bool complete = mission.IsComplete(state);
                bool claimed = state != null && state.IsMissionClaimed(mission.Id);
                row.ProgressText.text = $"{progress} / {mission.Target}";
                row.ProgressFill.rectTransform.anchorMax = new Vector2(
                    Mathf.Clamp01(progress / (float)mission.Target),
                    1f);
                Text actionLabel = row.ActionButton.GetComponentInChildren<Text>();
                if (actionLabel != null)
                {
                    actionLabel.text = claimed ? "CLAIMED" : complete ? "CLAIM" : "GO";
                }

                row.ActionButton.interactable = !claimed;
                Image image = row.ActionButton.targetGraphic as Image;
                if (image != null)
                {
                    image.color = complete && !claimed
                        ? DemoUiFactory.Action
                        : DemoUiFactory.SurfaceLight;
                }
            }
        }

        private void HandleMissionAction(
            DemoMissionDefinition mission,
            Action<string> claimMission,
            Action<string> goToMission)
        {
            if (mission.IsComplete(currentState))
            {
                claimMission?.Invoke(mission.Id);
            }
            else
            {
                goToMission?.Invoke(mission.Id);
            }
        }

        private static GameObject CreateRoot(Transform parent)
        {
            return DemoUiFactory.CreateScreenRoot("MissionsScreen", parent, 0.30f);
        }

        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public sealed class SettingsScreenView : DemoScreenView
    {
        private readonly Slider musicSlider;
        private readonly Slider effectsSlider;
        private readonly Toggle fullscreenToggle;
        private readonly Toggle frameRateToggle;
        private readonly GameObject confirmationPanel;
        private bool suppressCallbacks;

        public SettingsScreenView(
            Transform parent,
            Action<float> setMusic,
            Action<float> setEffects,
            Action<bool> setFullscreen,
            Action<bool> setSixtyFps,
            Action resetData,
            Action back)
            : base(CreateRoot(parent))
        {
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 30f);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            Button backButton = DemoUiFactory.CreateButton(
                "BackButton",
                safe,
                "BACK",
                new Color(0.06f, 0.08f, 0.11f, 0.96f),
                () => back?.Invoke());
            SetAnchors(backButton.GetComponent<RectTransform>(), 0.04f, 0.82f, 0.13f, 0.90f);
            Text title = DemoUiFactory.CreateText(
                "SettingsTitle",
                safe,
                "Settings",
                42,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(title.rectTransform, 0.16f, 0.80f, 0.54f, 0.91f);

            Image panel = DemoUiFactory.CreatePanel(
                "SettingsPanel",
                safe,
                new Color(0.035f, 0.055f, 0.08f, 0.96f),
                new Vector2(0.18f, 0.18f),
                new Vector2(0.82f, 0.76f),
                Vector2.zero,
                Vector2.zero);
            musicSlider = CreateSliderRow(panel.transform, "MusicVolume", "MUSIC VOLUME", 0.70f, value =>
            {
                if (!suppressCallbacks)
                {
                    setMusic?.Invoke(value);
                }
            });
            effectsSlider = CreateSliderRow(panel.transform, "EffectsVolume", "EFFECTS VOLUME", 0.52f, value =>
            {
                if (!suppressCallbacks)
                {
                    setEffects?.Invoke(value);
                }
            });
            fullscreenToggle = CreateToggleRow(panel.transform, "FullscreenToggle", "FULLSCREEN", 0.34f, value =>
            {
                if (!suppressCallbacks)
                {
                    setFullscreen?.Invoke(value);
                }
            });
            frameRateToggle = CreateToggleRow(panel.transform, "FrameRateToggle", "60 FPS MODE", 0.22f, value =>
            {
                if (!suppressCallbacks)
                {
                    setSixtyFps?.Invoke(value);
                }
            });

            Button reset = DemoUiFactory.CreateButton(
                "OpenResetConfirmationButton",
                panel.transform,
                "RESET LOCAL DEMO DATA",
                new Color(0.34f, 0.10f, 0.13f, 0.96f),
                ShowResetConfirmation);
            SetAnchors(reset.GetComponent<RectTransform>(), 0.55f, 0.06f, 0.93f, 0.17f);
            Text resetHint = DemoUiFactory.CreateText(
                "ResetHint",
                panel.transform,
                "Reset removes local recruitment, formation, stage, mission and inventory progress.",
                14,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextMuted);
            SetAnchors(resetHint.rectTransform, 0.07f, 0.05f, 0.52f, 0.18f);

            Image confirmationOverlay = DemoUiFactory.CreatePanel(
                "ResetConfirmPanel",
                safe,
                new Color(0.01f, 0.015f, 0.025f, 0.78f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            confirmationPanel = confirmationOverlay.gameObject;
            Image confirmation = DemoUiFactory.CreatePanel(
                "ResetConfirmDialog",
                confirmationOverlay.transform,
                new Color(0.025f, 0.035f, 0.055f, 0.99f),
                new Vector2(0.31f, 0.30f),
                new Vector2(0.69f, 0.68f),
                Vector2.zero,
                Vector2.zero);
            Text confirmTitle = DemoUiFactory.CreateText(
                "ConfirmTitle",
                confirmation.transform,
                "Reset all local progress?",
                30,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(confirmTitle.rectTransform, 0.08f, 0.63f, 0.92f, 0.88f);
            Text confirmBody = DemoUiFactory.CreateText(
                "ConfirmBody",
                confirmation.transform,
                "This cannot be undone. The default five-character formation will be restored.",
                18,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextSecondary);
            SetAnchors(confirmBody.rectTransform, 0.10f, 0.38f, 0.90f, 0.63f);
            Button cancel = DemoUiFactory.CreateButton(
                "CancelResetButton",
                confirmation.transform,
                "CANCEL",
                DemoUiFactory.SurfaceLight,
                HideResetConfirmation);
            SetAnchors(cancel.GetComponent<RectTransform>(), 0.10f, 0.11f, 0.46f, 0.30f);
            Button confirmReset = DemoUiFactory.CreateButton(
                "ConfirmResetButton",
                confirmation.transform,
                "RESET",
                DemoUiFactory.Danger,
                () =>
                {
                    HideResetConfirmation();
                    resetData?.Invoke();
                });
            SetAnchors(confirmReset.GetComponent<RectTransform>(), 0.54f, 0.11f, 0.90f, 0.30f);
            confirmationPanel.SetActive(false);
        }

        public void Refresh(PlayerSettingsState settings)
        {
            HideResetConfirmation();
            suppressCallbacks = true;
            PlayerSettingsState source = settings ?? new PlayerSettingsState();
            musicSlider.value = source.MusicVolume;
            effectsSlider.value = source.EffectsVolume;
            fullscreenToggle.isOn = source.Fullscreen;
            frameRateToggle.isOn = source.SixtyFps;
            suppressCallbacks = false;
        }

        public bool TryCloseModal()
        {
            if (!confirmationPanel.activeSelf)
            {
                return false;
            }

            HideResetConfirmation();
            return true;
        }

        private void ShowResetConfirmation()
        {
            confirmationPanel.SetActive(true);
        }

        private void HideResetConfirmation()
        {
            confirmationPanel.SetActive(false);
        }

        private static Slider CreateSliderRow(
            Transform parent,
            string name,
            string label,
            float centerY,
            Action<float> changed)
        {
            Text rowLabel = DemoUiFactory.CreateText(
                name + "Label",
                parent,
                label,
                18,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(rowLabel.rectTransform, 0.07f, centerY - 0.06f, 0.35f, centerY + 0.06f);

            RectTransform sliderRect = DemoUiFactory.CreateRect(
                name,
                parent,
                new Vector2(0.39f, centerY - 0.045f),
                new Vector2(0.91f, centerY + 0.045f),
                Vector2.zero,
                Vector2.zero);
            Slider slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.75f;

            Image background = DemoUiFactory.CreatePanel(
                "Background",
                sliderRect,
                new Color(0.02f, 0.03f, 0.045f, 1f),
                new Vector2(0f, 0.35f),
                new Vector2(1f, 0.65f),
                Vector2.zero,
                Vector2.zero);
            Image fill = DemoUiFactory.CreatePanel(
                "Fill",
                sliderRect,
                DemoUiFactory.Accent,
                new Vector2(0f, 0.35f),
                new Vector2(1f, 0.65f),
                Vector2.zero,
                Vector2.zero);
            Image handle = DemoUiFactory.CreatePanel(
                "Handle",
                sliderRect,
                DemoUiFactory.TextPrimary,
                new Vector2(0f, 0.10f),
                new Vector2(0.035f, 0.90f),
                Vector2.zero,
                Vector2.zero);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            background.raycastTarget = true;
            slider.onValueChanged.AddListener(value => changed?.Invoke(value));
            return slider;
        }

        private static Toggle CreateToggleRow(
            Transform parent,
            string name,
            string label,
            float centerY,
            Action<bool> changed)
        {
            Text rowLabel = DemoUiFactory.CreateText(
                name + "Label",
                parent,
                label,
                18,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(rowLabel.rectTransform, 0.07f, centerY - 0.05f, 0.62f, centerY + 0.05f);

            Image track = DemoUiFactory.CreatePanel(
                name,
                parent,
                new Color(0.06f, 0.08f, 0.11f, 1f),
                new Vector2(0.78f, centerY - 0.055f),
                new Vector2(0.91f, centerY + 0.055f),
                Vector2.zero,
                Vector2.zero);
            Toggle toggle = track.gameObject.AddComponent<Toggle>();
            Image check = DemoUiFactory.CreatePanel(
                "Checkmark",
                track.transform,
                DemoUiFactory.Accent,
                new Vector2(0.54f, 0.12f),
                new Vector2(0.94f, 0.88f),
                Vector2.zero,
                Vector2.zero);
            toggle.targetGraphic = track;
            toggle.graphic = check;
            toggle.isOn = true;
            toggle.onValueChanged.AddListener(value => changed?.Invoke(value));
            return toggle;
        }

        private static GameObject CreateRoot(Transform parent)
        {
            return DemoUiFactory.CreateScreenRoot("SettingsScreen", parent, 0.20f);
        }

        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public sealed class LockedFeatureScreenView : DemoScreenView
    {
        private readonly Text featureTitle;
        private readonly Text featureBody;

        public LockedFeatureScreenView(Transform parent, Action back)
            : base(CreateRoot(parent))
        {
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 32f);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            Button backButton = DemoUiFactory.CreateButton(
                "BackButton",
                safe,
                "BACK",
                new Color(0.06f, 0.08f, 0.11f, 0.96f),
                () => back?.Invoke());
            SetAnchors(backButton.GetComponent<RectTransform>(), 0.05f, 0.82f, 0.14f, 0.90f);

            featureTitle = DemoUiFactory.CreateText(
                "LockedFeatureTitle",
                safe,
                "FEATURE LOCKED",
                48,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(featureTitle.rectTransform, 0.20f, 0.56f, 0.80f, 0.72f);
            featureBody = DemoUiFactory.CreateText(
                "LockedFeatureBody",
                safe,
                string.Empty,
                22,
                TextAnchor.UpperCenter,
                DemoUiFactory.TextSecondary);
            SetAnchors(featureBody.rectTransform, 0.24f, 0.32f, 0.76f, 0.56f);
        }

        public void ShowFeature(string feature, PlayerState state)
        {
            string normalized = string.IsNullOrWhiteSpace(feature) ? "FEATURE" : feature.Trim().ToUpperInvariant();
            featureTitle.text = normalized;
            switch (normalized)
            {
                case "ARENA":
                    featureBody.text = "Clear Chapter 1-3 to unlock competitive modes.\nOnline ranking is intentionally disabled in this offline build.";
                    break;
                case "EVENTS":
                    featureBody.text = "Seasonal events require a signed schedule and reward service.\nThe entrance is reserved, but no fake timer or reward is presented.";
                    break;
                case "SHOP":
                    featureBody.text = "Real-money purchases are outside this demo.\nA future offline material shop will use authored, testable prices.";
                    break;
                case "MAIL":
                    featureBody.text = "Mail requires an authoritative account service.\nNo unearned attachments are simulated in this build.";
                    break;
                case "GUILD":
                    featureBody.text = "Guild systems unlock after the connected-service milestone.\nYour local progress remains available offline.";
                    break;
                default:
                    featureBody.text = "This area has a defined system boundary and will unlock when its data service is ready.";
                    break;
            }
        }

        private static GameObject CreateRoot(Transform parent)
        {
            return DemoUiFactory.CreateScreenRoot("FeatureLockedScreen", parent, 0.48f);
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
