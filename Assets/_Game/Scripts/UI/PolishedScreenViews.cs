using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GenericGachaRPG
{
    public sealed class ObservatoryHomeScreenView : DemoScreenView
    {
        private readonly Text currencyText;
        private readonly Text statusText;
        private readonly Button battleButton;

        public ObservatoryHomeScreenView(
            Transform parent,
            Action openGacha,
            Action openCollection,
            Action openFormation,
            Action startBattle,
            Action resetData)
            : base(CreateRoot(parent))
        {
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 34f);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            Text location = DemoUiFactory.CreateText(
                "Location",
                safe,
                "ASTRAL FIELD GUIDE  /  ABYSSAL OBSERVATORY",
                18,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            SetAnchors(location.rectTransform, 0.045f, 0.895f, 0.58f, 0.955f);

            Text title = DemoUiFactory.CreateText(
                "Title",
                safe,
                "BubbleMind",
                52,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(title.rectTransform, 0.045f, 0.79f, 0.52f, 0.90f);

            Text subtitle = DemoUiFactory.CreateText(
                "Subtitle",
                safe,
                "Lead Catherine and four slimes into a five-versus-five Pixel PvP trial.",
                22,
                TextAnchor.UpperLeft,
                DemoUiFactory.TextSecondary);
            SetAnchors(subtitle.rectTransform, 0.047f, 0.67f, 0.48f, 0.79f);

            Image currencyBand = DemoUiFactory.CreateFramedPanel(
                "CurrencyBand",
                safe,
                new Color(0.06f, 0.08f, 0.11f, 0.88f),
                new Vector2(0.79f, 0.885f),
                new Vector2(0.955f, 0.955f),
                Vector2.zero,
                Vector2.zero);
            currencyText = DemoUiFactory.CreateText(
                "Currency",
                currencyBand.transform,
                "CRYSTALS 0",
                22,
                TextAnchor.MiddleCenter,
                DemoUiFactory.Warning,
                FontStyle.Bold);

            Image missionBand = DemoUiFactory.CreateFramedPanel(
                "MissionBand",
                safe,
                new Color(0.05f, 0.07f, 0.10f, 0.80f),
                new Vector2(0.045f, 0.205f),
                new Vector2(0.56f, 0.285f),
                Vector2.zero,
                Vector2.zero);
            Text missionLabel = DemoUiFactory.CreateText(
                "MissionLabel",
                missionBand.transform,
                "CURRENT TRIAL",
                14,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Warning,
                FontStyle.Bold);
            SetAnchors(missionLabel.rectTransform, 0.035f, 0.53f, 0.27f, 0.92f);
            statusText = DemoUiFactory.CreateText(
                "Status",
                missionBand.transform,
                string.Empty,
                19,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextSecondary);
            SetAnchors(statusText.rectTransform, 0.28f, 0.16f, 0.97f, 0.90f);

            Image dock = DemoUiFactory.CreatePanel(
                "NavigationDock",
                safe,
                new Color(0.035f, 0.047f, 0.066f, 0.94f),
                new Vector2(0.03f, 0.03f),
                new Vector2(0.97f, 0.16f),
                Vector2.zero,
                Vector2.zero);
            DemoUiFactory.CreateDivider(
                "DockTopLine",
                dock.transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -1f),
                Vector2.zero);

            RectTransform actions = DemoUiFactory.CreateStretchRect("Actions", dock.transform, 12f);
            HorizontalLayoutGroup layout = DemoUiFactory.AddHorizontalLayout(
                actions.gameObject,
                14f,
                new RectOffset(0, 0, 0, 0));
            layout.childForceExpandWidth = true;

            Button characters = DemoUiFactory.CreateButton(
                "CollectionButton",
                actions,
                "Characters",
                DemoUiFactory.SurfaceLight,
                () => openCollection?.Invoke());
            DemoUiFactory.SetLayout(characters.gameObject, 235f, 100f, 1f, 1f);

            Button summon = DemoUiFactory.CreateButton(
                "GachaButton",
                actions,
                "Recruit",
                new Color(0.14f, 0.30f, 0.32f, 1f),
                () => openGacha?.Invoke());
            DemoUiFactory.SetLayout(summon.gameObject, 235f, 100f, 1f, 1f);

            Button formation = DemoUiFactory.CreateButton(
                "FormationButton",
                actions,
                "Formation",
                DemoUiFactory.SurfaceLight,
                () => openFormation?.Invoke());
            DemoUiFactory.SetLayout(formation.gameObject, 235f, 100f, 1f, 1f);

            battleButton = DemoUiFactory.CreateButton(
                "BattleButton",
                actions,
                "Enter Battle",
                DemoUiFactory.Action,
                () => startBattle?.Invoke());
            DemoUiFactory.SetLayout(battleButton.gameObject, 285f, 100f, 1.2f, 1f);

            Button reset = DemoUiFactory.CreateButton(
                "ResetButton",
                safe,
                "Reset",
                new Color(0.09f, 0.11f, 0.15f, 0.82f),
                () => resetData?.Invoke());
            SetAnchors(reset.GetComponent<RectTransform>(), 0.88f, 0.205f, 0.955f, 0.265f);
        }

        public void Refresh(PlayerState state)
        {
            int currency = state == null ? 0 : state.Currency;
            bool validFormation = state != null && state.TeamFormation != null && state.TeamFormation.IsComplete;
            currencyText.text = $"CRYSTALS  {currency:N0}";
            battleButton.interactable = validFormation;
            statusText.text = validFormation
                ? "Five signals synchronized. The range trial is ready."
                : "Select five unlocked signals in Formation.";
            statusText.color = validFormation ? DemoUiFactory.TextSecondary : DemoUiFactory.Warning;
        }

        private static GameObject CreateRoot(Transform parent)
        {
            return DemoUiFactory.CreateScreenRoot("HomeScreen", parent, 0.82f);
        }

        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public sealed class SummonScreenView : DemoScreenView
    {
        private readonly Text currencyText;
        private readonly Text bannerText;
        private readonly Text oddsText;
        private readonly Text resultText;
        private readonly Text resultPlaceholder;
        private readonly Image bannerPortrait;
        private readonly Image resultPortrait;
        private readonly Image resultFrame;
        private readonly Button pullButton;
        private readonly DemoCardReveal resultReveal;

        public SummonScreenView(Transform parent, Action pull, Action back)
            : base(CreateRoot(parent))
        {
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 34f);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            BuildHeader(safe, back, out currencyText);

            Image banner = DemoUiFactory.CreateFramedPanel(
                "BannerCard",
                safe,
                new Color(0.06f, 0.09f, 0.12f, 0.88f),
                new Vector2(0.03f, 0.105f),
                new Vector2(0.645f, 0.855f),
                Vector2.zero,
                Vector2.zero,
                DemoUiFactory.LineStrong);

            bannerPortrait = DemoUiFactory.CreatePortrait(
                "BannerPortrait",
                banner.transform,
                null,
                new Vector2(0.015f, 0.025f),
                new Vector2(0.56f, 0.975f),
                Vector2.zero,
                Vector2.zero);

            Text eyebrow = DemoUiFactory.CreateText(
                "Eyebrow",
                banner.transform,
                "STANDARD OBSERVATION",
                15,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            SetAnchors(eyebrow.rectTransform, 0.58f, 0.82f, 0.96f, 0.89f);

            bannerText = DemoUiFactory.CreateText(
                "BannerText",
                banner.transform,
                "Standard Signal",
                36,
                TextAnchor.UpperLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(bannerText.rectTransform, 0.58f, 0.58f, 0.96f, 0.82f);
            bannerText.resizeTextForBestFit = true;
            bannerText.resizeTextMinSize = 22;
            bannerText.resizeTextMaxSize = 36;

            DemoUiFactory.CreateDivider(
                "BannerDivider",
                banner.transform,
                new Vector2(0.58f, 0.56f),
                new Vector2(0.96f, 0.56f),
                Vector2.zero,
                new Vector2(0f, 1f));

            oddsText = DemoUiFactory.CreateText(
                "Odds",
                banner.transform,
                string.Empty,
                18,
                TextAnchor.UpperLeft,
                DemoUiFactory.TextSecondary);
            SetAnchors(oddsText.rectTransform, 0.58f, 0.24f, 0.96f, 0.53f);

            Text bannerNote = DemoUiFactory.CreateText(
                "BannerNote",
                banner.transform,
                "Probability details remain visible before every draw.",
                15,
                TextAnchor.LowerLeft,
                DemoUiFactory.TextMuted);
            SetAnchors(bannerNote.rectTransform, 0.58f, 0.07f, 0.96f, 0.20f);

            resultFrame = DemoUiFactory.CreateFramedPanel(
                "ResultCard",
                safe,
                new Color(0.055f, 0.072f, 0.098f, 0.96f),
                new Vector2(0.675f, 0.22f),
                new Vector2(0.97f, 0.855f),
                Vector2.zero,
                Vector2.zero,
                DemoUiFactory.LineStrong,
                2f);
            resultReveal = resultFrame.gameObject.AddComponent<DemoCardReveal>();

            Text resultEyebrow = DemoUiFactory.CreateText(
                "ResultEyebrow",
                resultFrame.transform,
                "LATEST SIGNAL",
                14,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextMuted,
                FontStyle.Bold);
            SetAnchors(resultEyebrow.rectTransform, 0.08f, 0.91f, 0.92f, 0.975f);

            resultPortrait = DemoUiFactory.CreatePortrait(
                "ResultPortrait",
                resultFrame.transform,
                null,
                new Vector2(0.08f, 0.27f),
                new Vector2(0.92f, 0.90f),
                Vector2.zero,
                Vector2.zero);
            resultPortrait.enabled = false;

            resultPlaceholder = DemoUiFactory.CreateText(
                "ResultPlaceholder",
                resultFrame.transform,
                "A new signal will appear here.",
                21,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextMuted);
            SetAnchors(resultPlaceholder.rectTransform, 0.10f, 0.34f, 0.90f, 0.72f);

            resultText = DemoUiFactory.CreateText(
                "ResultText",
                resultFrame.transform,
                "Summon a new signal.",
                22,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextSecondary,
                FontStyle.Bold);
            SetAnchors(resultText.rectTransform, 0.08f, 0.04f, 0.92f, 0.25f);

            pullButton = DemoUiFactory.CreateButton(
                "PullButton",
                safe,
                "Recruit x1",
                DemoUiFactory.Action,
                () => pull?.Invoke());
            SetAnchors(pullButton.GetComponent<RectTransform>(), 0.705f, 0.095f, 0.94f, 0.18f);

            Text disclaimer = DemoUiFactory.CreateText(
                "Disclaimer",
                safe,
                "DEMO ONLY  /  NO REAL-MONEY PURCHASES",
                14,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Warning,
                FontStyle.Bold);
            SetAnchors(disclaimer.rectTransform, 0.035f, 0.035f, 0.42f, 0.085f);
        }

        public void Refresh(PlayerState state, GachaBannerDefinition banner, GameDatabase database)
        {
            int balance = state == null ? 0 : state.Currency;
            currencyText.text = $"CRYSTALS  {balance:N0}";
            if (banner == null)
            {
                bannerText.text = "No banner configured";
                oddsText.text = "Run Generate or Repair Demo.";
                bannerPortrait.sprite = null;
                bannerPortrait.enabled = false;
                pullButton.interactable = false;
                return;
            }

            bannerText.text = $"{banner.DisplayName}\n{banner.Description}";
            pullButton.interactable = banner.TotalWeight > 0f && balance >= banner.SingleDrawCost;
            Text label = pullButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = $"Recruit x1  /  {banner.SingleDrawCost}";
            }

            CharacterDefinition featured = null;
            var lines = new List<string> { "DROP DETAILS" };
            for (int i = 0; i < banner.Entries.Count; i++)
            {
                GachaPoolEntry entry = banner.Entries[i];
                if (entry == null || entry.Weight <= 0f || banner.TotalWeight <= 0f)
                {
                    continue;
                }

                CharacterDefinition character = database == null ? null : database.GetCharacter(entry.CharacterId);
                if (character == null)
                {
                    continue;
                }

                if (featured == null || (int)character.Rarity > (int)featured.Rarity)
                {
                    featured = character;
                }

                float percent = entry.Weight / banner.TotalWeight * 100f;
                lines.Add($"{FormatRarity(character.Rarity),-3}  {character.DisplayName,-20} {percent,5:0.#}%");
            }

            oddsText.text = string.Join("\n", lines);
            bannerPortrait.sprite = featured == null ? null : featured.Portrait;
            bannerPortrait.enabled = bannerPortrait.sprite != null;
        }

        public void ShowResult(GachaResult result, CharacterDefinition character)
        {
            if (result == null || !result.Success || character == null)
            {
                resultPortrait.sprite = null;
                resultPortrait.enabled = false;
                resultPlaceholder.gameObject.SetActive(true);
                resultFrame.color = new Color(0.22f, 0.08f, 0.10f, 0.96f);
                resultText.color = DemoUiFactory.Danger;
                resultText.text = result == null ? "Summon failed." : result.ErrorMessage;
                return;
            }

            resultPortrait.sprite = character.Portrait;
            resultPortrait.enabled = resultPortrait.sprite != null;
            resultPlaceholder.gameObject.SetActive(!resultPortrait.enabled);
            Color rarity = RarityColor(character.Rarity);
            resultFrame.color = new Color(
                Mathf.Lerp(0.055f, rarity.r, 0.10f),
                Mathf.Lerp(0.072f, rarity.g, 0.10f),
                Mathf.Lerp(0.098f, rarity.b, 0.10f),
                0.98f);
            resultText.color = rarity;
            resultText.text =
                $"{FormatRarity(character.Rarity)}{(character.IsLimited ? " / LIMITED" : string.Empty)}\n" +
                $"{character.DisplayName}\n" +
                (result.IsNewCharacter ? "NEW CHARACTER UNLOCKED" : "DUPLICATE SIGNAL REGISTERED");
            resultReveal.Play();
        }

        public void ClearResult()
        {
            resultFrame.color = new Color(0.055f, 0.072f, 0.098f, 0.96f);
            resultPortrait.sprite = null;
            resultPortrait.enabled = false;
            resultPlaceholder.gameObject.SetActive(true);
            resultText.color = DemoUiFactory.TextSecondary;
            resultText.text = "Summon a new signal.";
        }

        private static void BuildHeader(Transform parent, Action back, out Text rightText)
        {
            Button backButton = DemoUiFactory.CreateButton(
                "BackButton",
                parent,
                "Back",
                DemoUiFactory.SurfaceLight,
                () => back?.Invoke());
            SetAnchors(backButton.GetComponent<RectTransform>(), 0.02f, 0.885f, 0.12f, 0.965f);

            Text header = DemoUiFactory.CreateText(
                "Header",
                parent,
                "Recruitment",
                38,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(header.rectTransform, 0.145f, 0.88f, 0.55f, 0.97f);

            rightText = DemoUiFactory.CreateText(
                "RightHeader",
                parent,
                string.Empty,
                21,
                TextAnchor.MiddleRight,
                DemoUiFactory.Warning,
                FontStyle.Bold);
            SetAnchors(rightText.rectTransform, 0.76f, 0.89f, 0.97f, 0.96f);
        }

        private static GameObject CreateRoot(Transform parent)
        {
            return DemoUiFactory.CreateScreenRoot("GachaScreen", parent, 0.46f);
        }

        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public sealed class CharacterPageScreenView : DemoScreenView
    {
        private readonly RectTransform grid;
        private readonly Text ownedText;
        private readonly Image detailPortrait;
        private readonly Text detailName;
        private readonly Text detailTags;
        private readonly Text detailDescription;
        private readonly Text detailStats;
        private readonly Text detailState;
        private readonly Text[] skillTexts = new Text[3];
        private readonly Button combatTabButton;
        private readonly Button archiveTabButton;
        private readonly Button growthTabButton;
        private readonly Button previousDetailButton;
        private readonly Button nextDetailButton;
        private readonly Dictionary<string, Image> cardBackgrounds = new Dictionary<string, Image>();
        private readonly Dictionary<string, Outline> cardOutlines = new Dictionary<string, Outline>();
        private GameDatabase database;
        private PlayerState state;
        private string selectedId;
        private DetailSection detailSection = DetailSection.Combat;
        private int detailPageIndex;

        private enum DetailSection
        {
            Combat,
            Archive,
            Growth
        }

        public CharacterPageScreenView(Transform parent, Action back)
            : base(CreateRoot(parent))
        {
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 30f);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            BuildHeader(safe, back, out ownedText);

            Image library = DemoUiFactory.CreateFramedPanel(
                "LibraryPanel",
                safe,
                new Color(0.055f, 0.072f, 0.10f, 0.94f),
                new Vector2(0.025f, 0.055f),
                new Vector2(0.325f, 0.855f),
                Vector2.zero,
                Vector2.zero);

            Text filter = DemoUiFactory.CreateText(
                "Filter",
                library.transform,
                "ALL SIGNALS",
                14,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextMuted,
                FontStyle.Bold);
            SetAnchors(filter.rectTransform, 0.045f, 0.925f, 0.95f, 0.985f);

            ScrollRect scroll = CreateScrollView(library.transform, out RectTransform content);
            grid = content;
            GridLayoutGroup layout = content.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(228f, 260f);
            layout.spacing = new Vector2(12f, 12f);
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 2;
            layout.childAlignment = TextAnchor.UpperCenter;
            scroll.content = content;

            Image detail = DemoUiFactory.CreateFramedPanel(
                "CharacterDetail",
                safe,
                new Color(0.05f, 0.067f, 0.092f, 0.94f),
                new Vector2(0.345f, 0.055f),
                new Vector2(0.975f, 0.855f),
                Vector2.zero,
                Vector2.zero,
                DemoUiFactory.LineStrong);

            detailPortrait = DemoUiFactory.CreatePortrait(
                "CharacterDetailPortrait",
                detail.transform,
                null,
                new Vector2(0.025f, 0.065f),
                new Vector2(0.45f, 0.95f),
                Vector2.zero,
                Vector2.zero);

            detailName = DemoUiFactory.CreateText(
                "CharacterDetailName",
                detail.transform,
                string.Empty,
                34,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(detailName.rectTransform, 0.49f, 0.83f, 0.96f, 0.94f);

            detailTags = DemoUiFactory.CreateText(
                "CharacterDetailTags",
                detail.transform,
                string.Empty,
                16,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            SetAnchors(detailTags.rectTransform, 0.49f, 0.76f, 0.96f, 0.83f);

            detailDescription = DemoUiFactory.CreateText(
                "CharacterDetailDescription",
                detail.transform,
                string.Empty,
                18,
                TextAnchor.UpperLeft,
                DemoUiFactory.TextSecondary);
            SetAnchors(detailDescription.rectTransform, 0.49f, 0.64f, 0.96f, 0.76f);

            DemoUiFactory.CreateDivider(
                "InfoDivider",
                detail.transform,
                new Vector2(0.49f, 0.62f),
                new Vector2(0.96f, 0.62f),
                Vector2.zero,
                new Vector2(0f, 1f));

            detailStats = DemoUiFactory.CreateText(
                "CharacterDetailStats",
                detail.transform,
                string.Empty,
                19,
                TextAnchor.UpperLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(detailStats.rectTransform, 0.49f, 0.48f, 0.96f, 0.60f);

            detailState = DemoUiFactory.CreateText(
                "CharacterDetailState",
                detail.transform,
                string.Empty,
                16,
                TextAnchor.MiddleLeft,
                DemoUiFactory.Warning,
                FontStyle.Bold);
            SetAnchors(detailState.rectTransform, 0.49f, 0.42f, 0.96f, 0.48f);

            combatTabButton = DemoUiFactory.CreateButton(
                "CharacterTab_Combat",
                detail.transform,
                "Combat",
                DemoUiFactory.Accent,
                () => SetDetailSection(DetailSection.Combat));
            SetAnchors(combatTabButton.GetComponent<RectTransform>(), 0.49f, 0.36f, 0.64f, 0.415f);

            archiveTabButton = DemoUiFactory.CreateButton(
                "CharacterTab_Archive",
                detail.transform,
                "Archive",
                DemoUiFactory.SurfaceLight,
                () => SetDetailSection(DetailSection.Archive));
            SetAnchors(archiveTabButton.GetComponent<RectTransform>(), 0.65f, 0.36f, 0.80f, 0.415f);

            growthTabButton = DemoUiFactory.CreateButton(
                "CharacterTab_Growth",
                detail.transform,
                "Growth",
                DemoUiFactory.SurfaceLight,
                () => SetDetailSection(DetailSection.Growth));
            SetAnchors(growthTabButton.GetComponent<RectTransform>(), 0.81f, 0.36f, 0.96f, 0.415f);

            for (int i = 0; i < skillTexts.Length; i++)
            {
                float top = 0.35f - i * 0.105f;
                Image skillCard = DemoUiFactory.CreateFramedPanel(
                    $"SkillCard_{i + 1}",
                    detail.transform,
                    new Color(0.10f, 0.125f, 0.16f, 0.86f),
                    new Vector2(0.49f, top - 0.095f),
                    new Vector2(0.96f, top),
                    Vector2.zero,
                    Vector2.zero);
                skillTexts[i] = DemoUiFactory.CreateText(
                    "Body",
                    skillCard.transform,
                    string.Empty,
                    15,
                    TextAnchor.MiddleLeft,
                    DemoUiFactory.TextSecondary);
                skillTexts[i].rectTransform.offsetMin = new Vector2(14f, 6f);
                skillTexts[i].rectTransform.offsetMax = new Vector2(-14f, -6f);
                skillTexts[i].resizeTextForBestFit = true;
                skillTexts[i].resizeTextMinSize = 10;
                skillTexts[i].resizeTextMaxSize = 15;
            }

            skillTexts[0].rectTransform.offsetMax = new Vector2(-88f, -6f);
            previousDetailButton = DemoUiFactory.CreateButton(
                "CharacterDetailPrevious",
                detail.transform,
                "<",
                DemoUiFactory.SurfaceLight,
                () => ChangeDetailPage(-1));
            ConfigureDetailNavigationButton(previousDetailButton);
            SetAnchors(
                previousDetailButton.GetComponent<RectTransform>(),
                0.865f,
                0.307f,
                0.907f,
                0.345f);
            nextDetailButton = DemoUiFactory.CreateButton(
                "CharacterDetailNext",
                detail.transform,
                ">",
                DemoUiFactory.SurfaceLight,
                () => ChangeDetailPage(1));
            ConfigureDetailNavigationButton(nextDetailButton);
            SetAnchors(
                nextDetailButton.GetComponent<RectTransform>(),
                0.912f,
                0.307f,
                0.954f,
                0.345f);
            SetDetailNavigationVisible(false);
        }

        public void Refresh(GameDatabase gameDatabase, PlayerState playerState)
        {
            database = gameDatabase;
            state = playerState;
            DemoUiFactory.DestroyChildren(grid);
            cardBackgrounds.Clear();
            cardOutlines.Clear();
            int owned = 0;
            int characterCount = 0;
            string firstOwned = null;

            if (database != null)
            {
                for (int i = 0; i < database.Characters.Count; i++)
                {
                    CharacterDefinition character = database.Characters[i];
                    if (character == null)
                    {
                        continue;
                    }

                    characterCount++;
                    bool unlocked = state != null && state.HasCharacter(character.Id);
                    if (unlocked)
                    {
                        owned++;
                        firstOwned ??= character.Id;
                    }

                    CreateCharacterCard(character, unlocked);
                }
            }

            ResizeCharacterGrid(characterCount);

            ownedText.text = $"OWNED  {owned} / {(database == null ? 0 : database.Characters.Count)}";
            if (database == null || database.Characters.Count == 0)
            {
                SelectCharacter(null);
                return;
            }

            if (string.IsNullOrEmpty(selectedId) || database.GetCharacter(selectedId) == null)
            {
                selectedId = database.GetCharacter("ur_cosmic_slime") != null
                    ? "ur_cosmic_slime"
                    : firstOwned ?? database.Characters[0].Id;
            }

            SelectCharacter(selectedId);
        }

        private void ResizeCharacterGrid(int characterCount)
        {
            GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
            int columns = layout == null ? 2 : Mathf.Max(1, layout.constraintCount);
            int rows = Mathf.Max(1, Mathf.CeilToInt(characterCount / (float)columns));
            float cellHeight = layout == null ? 260f : layout.cellSize.y;
            float spacing = layout == null ? 12f : layout.spacing.y;
            float padding = layout == null ? 24f : layout.padding.vertical;
            float height = padding + rows * cellHeight + Mathf.Max(0, rows - 1) * spacing;
            grid.sizeDelta = new Vector2(grid.sizeDelta.x, height);
        }

        private void CreateCharacterCard(CharacterDefinition character, bool unlocked)
        {
            Color baseColor = unlocked
                ? new Color(0.10f, 0.125f, 0.16f, 1f)
                : new Color(0.065f, 0.075f, 0.09f, 1f);
            Image background = DemoUiFactory.CreateFramedPanel(
                $"Card_{character.Id}",
                grid,
                baseColor,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Button button = background.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            string id = character.Id;
            button.onClick.AddListener(() => SelectCharacter(id));

            Image portrait = DemoUiFactory.CreatePortrait(
                "Portrait",
                background.transform,
                character.Portrait,
                new Vector2(0.04f, 0.25f),
                new Vector2(0.96f, 0.96f),
                Vector2.zero,
                Vector2.zero,
                unlocked ? Color.white : new Color(0.36f, 0.39f, 0.43f, 1f));
            portrait.enabled = portrait.sprite != null;

            Text rarity = DemoUiFactory.CreateBadge(
                "Rarity",
                background.transform,
                FormatRarity(character.Rarity),
                new Color(0.04f, 0.05f, 0.07f, 0.88f),
                RarityColor(character.Rarity));
            SetAnchors(rarity.transform.parent.GetComponent<RectTransform>(), 0.05f, 0.84f, 0.28f, 0.96f);

            string tags = FormatCharacterTags(character);
            Text body = DemoUiFactory.CreateText(
                "Body",
                background.transform,
                $"{character.DisplayName}\n{tags}\nRNG {character.AttackRange:0.0}  /  MOVE {character.MoveSpeed:0.0}",
                15,
                TextAnchor.MiddleLeft,
                unlocked ? DemoUiFactory.TextPrimary : DemoUiFactory.TextMuted,
                FontStyle.Bold);
            SetAnchors(body.rectTransform, 0.06f, 0.025f, 0.95f, 0.25f);

            if (!unlocked)
            {
                Text locked = DemoUiFactory.CreateBadge(
                    "Locked",
                    background.transform,
                    "LOCKED",
                    new Color(0.04f, 0.05f, 0.07f, 0.90f),
                    DemoUiFactory.TextSecondary);
                SetAnchors(locked.transform.parent.GetComponent<RectTransform>(), 0.62f, 0.84f, 0.95f, 0.96f);
            }

            cardBackgrounds[id] = background;
            cardOutlines[id] = background.GetComponent<Outline>();
        }

        private void SelectCharacter(string characterId)
        {
            if (!string.Equals(selectedId, characterId, StringComparison.Ordinal))
            {
                detailPageIndex = 0;
            }

            selectedId = characterId;
            foreach (KeyValuePair<string, Image> pair in cardBackgrounds)
            {
                bool selected = string.Equals(pair.Key, selectedId, StringComparison.Ordinal);
                bool cardUnlocked = state != null && state.HasCharacter(pair.Key);
                pair.Value.color = selected
                    ? cardUnlocked
                        ? new Color(0.13f, 0.18f, 0.22f, 1f)
                        : new Color(0.08f, 0.11f, 0.13f, 1f)
                    : cardUnlocked
                        ? new Color(0.10f, 0.125f, 0.16f, 1f)
                        : new Color(0.065f, 0.075f, 0.09f, 1f);
                if (cardOutlines.TryGetValue(pair.Key, out Outline outline) && outline != null)
                {
                    outline.effectColor = selected ? DemoUiFactory.Accent : DemoUiFactory.LineSubtle;
                    outline.effectDistance = selected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
                }
            }

            CharacterDefinition character = database == null ? null : database.GetCharacter(selectedId);
            if (character == null)
            {
                detailPortrait.sprite = null;
                detailPortrait.enabled = false;
                detailName.text = "No character selected";
                detailTags.text = string.Empty;
                detailDescription.text = string.Empty;
                detailStats.text = string.Empty;
                detailState.text = string.Empty;
                for (int i = 0; i < skillTexts.Length; i++)
                {
                    skillTexts[i].text = string.Empty;
                }

                SetDetailNavigationVisible(false);

                return;
            }

            bool unlocked = state != null && state.HasCharacter(character.Id);
            detailPortrait.sprite = character.Portrait;
            detailPortrait.enabled = detailPortrait.sprite != null;
            detailPortrait.color = unlocked ? Color.white : new Color(0.62f, 0.65f, 0.70f, 1f);
            detailName.text = character.DisplayName;
            detailName.color = RarityColor(character.Rarity);
            CharacterContentProfile profile = character.ContentProfile;
            detailTags.text = profile == null
                ? FormatCharacterTags(character)
                : $"{FormatCharacterTags(character)}  /  {profile.Element.ToString().ToUpperInvariant()}  /  {profile.Faction.ToUpperInvariant()}";
            detailDescription.text = profile == null
                ? character.Description
                : $"{profile.Title}\n{character.Description}";
            detailStats.text =
                $"HP  {character.MaxHealth:0}      ATK  {character.Attack:0}      DEF  {character.Defense:0}\n" +
                $"RANGE  {character.AttackRange:0.0} / 20      INTERVAL  {character.AttackInterval:0.00}s      MOVE  {character.MoveSpeed:0.0}";
            OwnedCharacterState ownedState = state == null ? null : state.FindOwnedCharacter(character.Id);
            detailState.text = unlocked
                ? profile != null && HasKeyword(profile, "maxed-demo")
                    ? $"OWNED  /  MAX PROFILE {profile.MaxLevel}  /  IMAGINARY MASS 30"
                    : $"OWNED  /  LEVEL {ownedState?.Level ?? 1}  /  COPIES {ownedState?.Copies ?? 1}"
                : character.IsLimited
                    ? "ARCHIVE PREVIEW  /  LIMITED SIGNAL"
                    : "ARCHIVE PREVIEW  /  ACQUIRE FROM RECRUITMENT";
            PopulateDetailRows(character, profile, ownedState);
        }

        private void SetDetailSection(DetailSection section)
        {
            if (detailSection != section)
            {
                detailPageIndex = 0;
            }

            detailSection = section;
            SetTabColor(combatTabButton, section == DetailSection.Combat);
            SetTabColor(archiveTabButton, section == DetailSection.Archive);
            SetTabColor(growthTabButton, section == DetailSection.Growth);
            CharacterDefinition character = database == null ? null : database.GetCharacter(selectedId);
            if (character != null)
            {
                OwnedCharacterState owned = state == null ? null : state.FindOwnedCharacter(character.Id);
                PopulateDetailRows(character, character.ContentProfile, owned);
            }
        }

        private void PopulateDetailRows(
            CharacterDefinition character,
            CharacterContentProfile profile,
            OwnedCharacterState owned)
        {
            if (detailSection == DetailSection.Combat)
            {
                SetDetailNavigationVisible(false);
                SkillDefinition[] skills = { character.UltimateSkill, character.Skill2, character.Skill3 };
                for (int i = 0; i < skillTexts.Length; i++)
                {
                    SkillDefinition skill = skills[i];
                    skillTexts[i].text = skill == null
                        ? "Skill data unavailable"
                        : FormatSkill(skill, i);
                }

                return;
            }

            if (profile == null)
            {
                SetDetailNavigationVisible(false);
                skillTexts[0].text = "CONTENT PROFILE\nProfile data unavailable.";
                skillTexts[1].text = string.Empty;
                skillTexts[2].text = string.Empty;
                return;
            }

            if (detailSection == DetailSection.Archive)
            {
                int abilityCount = profile.Abilities.Count;
                detailPageIndex = WrapIndex(detailPageIndex, abilityCount);
                SetDetailNavigationVisible(abilityCount > 1);
                CharacterAbilityRecord ability = abilityCount == 0
                    ? null
                    : profile.Abilities[detailPageIndex];
                if (ability == null)
                {
                    skillTexts[0].text = "ABILITY ARCHIVE\nNo authored ability is available.";
                    skillTexts[1].text = string.Empty;
                    skillTexts[2].text = string.Empty;
                    return;
                }

                string gate = ability.UnlockLevel > 1 ? $"UNLOCK LV.{ability.UnlockLevel}" : "AVAILABLE";
                skillTexts[0].text =
                    $"ABILITY {detailPageIndex + 1} / {abilityCount}  /  {ability.Kind.ToString().ToUpperInvariant()}\n" +
                    $"{ability.DisplayName}  /  {gate}  /  MAX RANK {ability.MaxLevel}  /  {FormatTags(ability.Tags)}";
                skillTexts[1].text =
                    $"OVERVIEW\n{ability.Summary}\n" +
                    $"TRIGGER  {ability.TriggerSummary}\nTARGET  {ability.TargetSummary}";
                skillTexts[2].text =
                    $"EFFECT\n{ability.EffectSummary}\n" +
                    $"RANKS  {FormatAbilityRanks(ability)}";
                return;
            }

            int growthPageCount = Mathf.Max(profile.ProgressionStages.Count, profile.Acquisition.Count);
            detailPageIndex = WrapIndex(detailPageIndex, growthPageCount);
            SetDetailNavigationVisible(growthPageCount > 1);
            ProgressionStageRecord stage = profile.ProgressionStages.Count == 0
                ? null
                : profile.ProgressionStages[detailPageIndex % profile.ProgressionStages.Count];
            AcquisitionRecord source = profile.Acquisition.Count == 0
                ? null
                : profile.Acquisition[detailPageIndex % profile.Acquisition.Count];
            skillTexts[0].text = stage == null
                ? "PROGRESSION\nNo progression stage authored."
                : $"PROGRESSION {detailPageIndex + 1} / {growthPageCount}  /  MAX LEVEL {profile.MaxLevel}\n" +
                  $"{stage.Title}  /  {stage.Track.ToString().ToUpperInvariant()} {stage.RequiredValue}  /  CAP {stage.LevelCap}\n" +
                  stage.Summary;
            skillTexts[1].text = source == null
                ? "ACQUISITION\nNo acquisition source authored."
                : $"ACQUISITION  /  {source.Label}\n{source.Availability}\nDUPLICATES  {source.DuplicateRule}";
            skillTexts[2].text =
                $"PROGRESSION & AWAKENING\n{profile.ProgressionSummary}\n{profile.AwakeningSummary}\n" +
                $"CURRENT  LEVEL {owned?.Level ?? 0}  /  COPIES {owned?.Copies ?? 0}";
        }

        private void ChangeDetailPage(int direction)
        {
            CharacterDefinition character = database == null ? null : database.GetCharacter(selectedId);
            CharacterContentProfile profile = character == null ? null : character.ContentProfile;
            if (profile == null || detailSection == DetailSection.Combat)
            {
                return;
            }

            int pageCount = detailSection == DetailSection.Archive
                ? profile.Abilities.Count
                : Mathf.Max(profile.ProgressionStages.Count, profile.Acquisition.Count);
            if (pageCount <= 1)
            {
                return;
            }

            detailPageIndex = WrapIndex(detailPageIndex + direction, pageCount);
            OwnedCharacterState owned = state == null ? null : state.FindOwnedCharacter(character.Id);
            PopulateDetailRows(character, profile, owned);
        }

        private void SetDetailNavigationVisible(bool visible)
        {
            previousDetailButton.gameObject.SetActive(visible);
            nextDetailButton.gameObject.SetActive(visible);
        }

        private static int WrapIndex(int value, int count)
        {
            if (count <= 0)
            {
                return 0;
            }

            int wrapped = value % count;
            return wrapped < 0 ? wrapped + count : wrapped;
        }

        private static string FormatAbilityRanks(CharacterAbilityRecord ability)
        {
            var ranks = new List<string>();
            for (int rankIndex = 0; rankIndex < ability.Ranks.Count; rankIndex++)
            {
                SkillRankRecord rank = ability.Ranks[rankIndex];
                if (rank == null)
                {
                    continue;
                }

                var values = new List<string>();
                for (int valueIndex = 0; valueIndex < rank.Values.Count; valueIndex++)
                {
                    SkillValueRecord value = rank.Values[valueIndex];
                    if (value != null)
                    {
                        values.Add($"{FormatSkillValueKey(value.Key)} {FormatSkillValue(value)}");
                    }
                }

                string payload = values.Count == 0 ? rank.Summary : string.Join(", ", values);
                ranks.Add($"Lv.{rank.Level} {payload}");
            }

            return ranks.Count == 0 ? "No rank data" : string.Join("  >  ", ranks);
        }

        private static string FormatSkillValue(SkillValueRecord value)
        {
            switch (value.Unit)
            {
                case SkillValueUnit.Percent:
                case SkillValueUnit.PercentOfAttack:
                case SkillValueUnit.PercentOfDamage:
                case SkillValueUnit.PercentOfMaxHealth:
                    return $"{value.Value:0.##}%";
                case SkillValueUnit.Multiplier:
                    return $"{value.Value:0.##}x";
                case SkillValueUnit.Seconds:
                    return $"{value.Value:0.##}s";
                case SkillValueUnit.Stacks:
                    return $"{value.Value:0.##} stacks";
                default:
                    return value.Value.ToString("0.##");
            }
        }

        private static string FormatSkillValueKey(string key)
        {
            switch (key)
            {
                case "damage":
                    return "DMG";
                case "power":
                    return "POWER";
                case "trigger_chance":
                    return "CHANCE";
                case "stacks_per_trigger":
                    return "STACKS";
                case "max_hp_per_stack":
                    return "MAX HP";
                default:
                    return string.IsNullOrWhiteSpace(key)
                        ? "VALUE"
                        : key.Replace('_', ' ').ToUpperInvariant();
            }
        }

        private static string FormatTags(SkillTag tags)
        {
            return tags == SkillTag.None
                ? "UNTAGGED"
                : tags.ToString().Replace(", ", " / ").ToUpperInvariant();
        }

        private static void ConfigureDetailNavigationButton(Button button)
        {
            Text label = button == null ? null : button.GetComponentInChildren<Text>();
            if (label == null)
            {
                return;
            }

            label.fontSize = 16;
            label.resizeTextForBestFit = false;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
        }

        private static void SetTabColor(Button button, bool selected)
        {
            if (button != null && button.targetGraphic is Image image)
            {
                image.color = selected ? DemoUiFactory.Accent : DemoUiFactory.SurfaceLight;
            }
        }

        private static bool HasKeyword(CharacterContentProfile profile, string keyword)
        {
            for (int i = 0; i < profile.Keywords.Count; i++)
            {
                if (string.Equals(profile.Keywords[i], keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string FormatSkill(SkillDefinition skill, int slot)
        {
            string cadence = slot == 0
                ? $"ULTIMATE / {skill.RageCost} RAGE"
                : slot == 1
                    ? "SKILL 2 / AUTO 5s + 10s"
                    : "SKILL 3 / AUTO 10s + 10s";
            string description = string.IsNullOrWhiteSpace(skill.Description)
                ? skill.Category.ToString()
                : skill.Description;
            if (description.Length > 96)
            {
                description = description.Substring(0, 93) + "...";
            }

            return $"{cadence}    {skill.DisplayName}\n{description}";
        }

        private static ScrollRect CreateScrollView(Transform parent, out RectTransform content)
        {
            RectTransform viewport = DemoUiFactory.CreateRect(
                "Viewport",
                parent,
                new Vector2(0.025f, 0.025f),
                new Vector2(0.975f, 0.92f),
                Vector2.zero,
                Vector2.zero);
            Image viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            viewport.gameObject.AddComponent<RectMask2D>();

            content = DemoUiFactory.CreateRect(
                "CharacterGrid",
                viewport,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector2.zero);
            content.pivot = new Vector2(0.5f, 1f);

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 34f;
            return scroll;
        }

        private static void BuildHeader(Transform parent, Action back, out Text rightText)
        {
            Button backButton = DemoUiFactory.CreateButton(
                "BackButton",
                parent,
                "Back",
                DemoUiFactory.SurfaceLight,
                () => back?.Invoke());
            SetAnchors(backButton.GetComponent<RectTransform>(), 0.02f, 0.885f, 0.12f, 0.965f);

            Text title = DemoUiFactory.CreateText(
                "Header",
                parent,
                "Characters",
                38,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(title.rectTransform, 0.145f, 0.88f, 0.55f, 0.97f);

            rightText = DemoUiFactory.CreateText(
                "RightHeader",
                parent,
                string.Empty,
                18,
                TextAnchor.MiddleRight,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            SetAnchors(rightText.rectTransform, 0.78f, 0.89f, 0.97f, 0.96f);
        }

        private static GameObject CreateRoot(Transform parent)
        {
            return DemoUiFactory.CreateScreenRoot("CollectionScreen", parent, 0.34f);
        }

        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public sealed class RosterFormationScreenView : DemoScreenView
    {
        private readonly RectTransform grid;
        private readonly List<Image> slotPortraits = new List<Image>();
        private readonly List<Text> slotLabels = new List<Text>();
        private readonly Text feedbackText;
        private readonly Button battleButton;

        public RosterFormationScreenView(Transform parent, Action<string> toggleCharacter, Action battle, Action back)
            : base(CreateRoot(parent))
        {
            ToggleCharacter = toggleCharacter;
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 30f);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            Button backButton = DemoUiFactory.CreateButton(
                "BackButton",
                safe,
                "Back",
                DemoUiFactory.SurfaceLight,
                () => back?.Invoke());
            SetAnchors(backButton.GetComponent<RectTransform>(), 0.02f, 0.885f, 0.12f, 0.965f);

            Text header = DemoUiFactory.CreateText(
                "Header",
                safe,
                "Formation",
                38,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            SetAnchors(header.rectTransform, 0.145f, 0.88f, 0.55f, 0.97f);

            Text rangeNote = DemoUiFactory.CreateText(
                "RangeNote",
                safe,
                "FRONTLINE 2 / 20     BACKLINE 10 / 20",
                15,
                TextAnchor.MiddleRight,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            SetAnchors(rangeNote.rectTransform, 0.62f, 0.895f, 0.97f, 0.955f);

            RectTransform slots = DemoUiFactory.CreateRect(
                "FormationSlots",
                safe,
                new Vector2(0.035f, 0.665f),
                new Vector2(0.965f, 0.845f),
                Vector2.zero,
                Vector2.zero);
            HorizontalLayoutGroup slotLayout = DemoUiFactory.AddHorizontalLayout(
                slots.gameObject,
                12f,
                new RectOffset(0, 0, 0, 0));
            slotLayout.childForceExpandWidth = true;

            for (int i = 0; i < TeamFormationState.RequiredMemberCount; i++)
            {
                Image slot = DemoUiFactory.CreateFramedPanel(
                    $"Slot_{i + 1}",
                    slots,
                    new Color(0.07f, 0.09f, 0.12f, 0.96f),
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero);
                DemoUiFactory.SetLayout(slot.gameObject, 250f, 175f, 1f, 1f);
                Image portrait = DemoUiFactory.CreatePortrait(
                    "Portrait",
                    slot.transform,
                    null,
                    new Vector2(0.03f, 0.08f),
                    new Vector2(0.34f, 0.92f),
                    Vector2.zero,
                    Vector2.zero);
                portrait.enabled = false;
                slotPortraits.Add(portrait);
                Text label = DemoUiFactory.CreateText(
                    "Label",
                    slot.transform,
                    $"SLOT {i + 1}\nEMPTY",
                    17,
                    TextAnchor.MiddleLeft,
                    DemoUiFactory.TextMuted,
                    FontStyle.Bold);
                SetAnchors(label.rectTransform, 0.38f, 0.12f, 0.96f, 0.88f);
                slotLabels.Add(label);
            }

            Image rosterPanel = DemoUiFactory.CreateFramedPanel(
                "RosterPanel",
                safe,
                new Color(0.055f, 0.072f, 0.10f, 0.94f),
                new Vector2(0.035f, 0.20f),
                new Vector2(0.965f, 0.635f),
                Vector2.zero,
                Vector2.zero);
            RectTransform rosterViewport = DemoUiFactory.CreateStretchRect(
                "RosterViewport",
                rosterPanel.transform,
                20f);
            Image rosterViewportImage = rosterViewport.gameObject.AddComponent<Image>();
            rosterViewportImage.color = new Color(0f, 0f, 0f, 0.01f);
            rosterViewport.gameObject.AddComponent<RectMask2D>();
            grid = DemoUiFactory.CreateRect(
                "FormationGrid",
                rosterViewport,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector2.zero);
            grid.pivot = new Vector2(0.5f, 1f);
            GridLayoutGroup gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.spacing = new Vector2(14f, 14f);
            gridLayout.padding = new RectOffset(8, 8, 8, 8);
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            grid.gameObject.AddComponent<ResponsiveGridLayout>().Configure(390f, 2.55f, 4, 4);
            ContentSizeFitter rosterContentFitter = grid.gameObject.AddComponent<ContentSizeFitter>();
            rosterContentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rosterContentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ScrollRect rosterScroll = rosterViewport.gameObject.AddComponent<ScrollRect>();
            rosterScroll.viewport = rosterViewport;
            rosterScroll.content = grid;
            rosterScroll.horizontal = false;
            rosterScroll.vertical = true;
            rosterScroll.movementType = ScrollRect.MovementType.Clamped;
            rosterScroll.scrollSensitivity = 34f;

            feedbackText = DemoUiFactory.CreateText(
                "Feedback",
                safe,
                string.Empty,
                17,
                TextAnchor.MiddleLeft,
                DemoUiFactory.TextSecondary);
            SetAnchors(feedbackText.rectTransform, 0.04f, 0.075f, 0.68f, 0.16f);

            battleButton = DemoUiFactory.CreateButton(
                "BattleButton",
                safe,
                "Start 5v5 Battle",
                DemoUiFactory.Action,
                () => battle?.Invoke());
            SetAnchors(battleButton.GetComponent<RectTransform>(), 0.72f, 0.06f, 0.96f, 0.16f);
        }

        private Action<string> ToggleCharacter { get; }

        public void Refresh(GameDatabase database, PlayerState state, IReadOnlyList<string> draftIds, string feedback = "")
        {
            DemoUiFactory.DestroyChildren(grid);
            for (int i = 0; i < slotLabels.Count; i++)
            {
                CharacterDefinition character = database != null && draftIds != null && i < draftIds.Count
                    ? database.GetCharacter(draftIds[i])
                    : null;
                slotPortraits[i].sprite = character == null ? null : character.Portrait;
                slotPortraits[i].enabled = slotPortraits[i].sprite != null;
                slotLabels[i].text = character == null
                    ? $"SLOT {i + 1}\nEMPTY"
                    : $"SLOT {i + 1}\n{character.DisplayName}\n{FormatRole(character.Role)}  /  RNG {character.AttackRange:0}";
                slotLabels[i].color = character == null ? DemoUiFactory.TextMuted : DemoUiFactory.TextPrimary;
            }

            feedbackText.text = string.IsNullOrEmpty(feedback)
                ? "Your five saved slots deploy directly into the local 5v5 Pixel PvP trial."
                : feedback;
            feedbackText.color = string.IsNullOrEmpty(feedback) ? DemoUiFactory.TextSecondary : DemoUiFactory.Warning;
            battleButton.interactable = draftIds != null && draftIds.Count == TeamFormationState.RequiredMemberCount;

            if (database == null)
            {
                return;
            }

            for (int i = 0; i < database.Characters.Count; i++)
            {
                CharacterDefinition character = database.Characters[i];
                if (character == null)
                {
                    continue;
                }

                bool unlocked = state != null && state.HasCharacter(character.Id);
                bool selected = draftIds != null && Contains(draftIds, character.Id);
                Color backgroundColor = !unlocked
                    ? new Color(0.06f, 0.07f, 0.09f, 1f)
                    : selected
                        ? new Color(0.10f, 0.25f, 0.22f, 1f)
                        : new Color(0.10f, 0.125f, 0.16f, 1f);
                string id = character.Id;
                Image card = DemoUiFactory.CreateFramedPanel(
                    $"Character_{id}",
                    grid,
                    backgroundColor,
                    Vector2.zero,
                    Vector2.one,
                    Vector2.zero,
                    Vector2.zero,
                    selected ? DemoUiFactory.Accent : DemoUiFactory.LineSubtle,
                    selected ? 2f : 1f);
                Button button = card.gameObject.AddComponent<Button>();
                button.targetGraphic = card;
                button.interactable = unlocked;
                button.onClick.AddListener(() => ToggleCharacter?.Invoke(id));

                Image portrait = DemoUiFactory.CreatePortrait(
                    "Portrait",
                    card.transform,
                    character.Portrait,
                    new Vector2(0.02f, 0.08f),
                    new Vector2(0.31f, 0.92f),
                    Vector2.zero,
                    Vector2.zero,
                    unlocked ? Color.white : new Color(0.33f, 0.36f, 0.40f, 1f));
                portrait.enabled = portrait.sprite != null;

                Text body = DemoUiFactory.CreateText(
                    "Body",
                    card.transform,
                    unlocked
                        ? $"{(selected ? "SELECTED  /  " : string.Empty)}{character.DisplayName}\n" +
                          $"{FormatRarity(character.Rarity)}  /  {FormatRole(character.Role)}\n" +
                          $"RANGE {character.AttackRange:0} / 20"
                        : character.IsLimited
                            ? "LIMITED SIGNAL\nARCHIVE ONLY"
                            : "LOCKED SIGNAL\nRECRUIT TO UNLOCK",
                    16,
                    TextAnchor.MiddleLeft,
                    unlocked ? DemoUiFactory.TextPrimary : DemoUiFactory.TextMuted,
                    FontStyle.Bold);
                SetAnchors(body.rectTransform, 0.34f, 0.08f, 0.96f, 0.92f);
            }
        }

        private static bool Contains(IReadOnlyList<string> ids, string id)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static GameObject CreateRoot(Transform parent)
        {
            return DemoUiFactory.CreateScreenRoot("FormationScreen", parent, 0.40f);
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
