using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GenericGachaRPG
{
    public enum DemoScreen
    {
        Home,
        Gacha,
        Collection,
        Formation,
        Battle
    }

    public abstract class DemoScreenView
    {
        protected DemoScreenView(GameObject root)
        {
            Root = root;
        }

        public GameObject Root { get; }

        public void SetVisible(bool visible)
        {
            Root.SetActive(visible);
        }

        protected static string FormatRarity(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Epic:
                    return "SSR";
                case Rarity.Rare:
                    return "SR";
                default:
                    return "R";
            }
        }

        protected static Color RarityColor(Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Epic:
                    return new Color(0.92f, 0.53f, 1f, 1f);
                case Rarity.Rare:
                    return new Color(1f, 0.78f, 0.24f, 1f);
                default:
                    return new Color(0.52f, 0.78f, 1f, 1f);
            }
        }
    }

    public sealed class HomeScreenView : DemoScreenView
    {
        private readonly Text currencyText;
        private readonly Text statusText;
        private readonly Button battleButton;

        public HomeScreenView(
            Transform parent,
            Action openGacha,
            Action openCollection,
            Action openFormation,
            Action startBattle,
            Action resetData)
            : base(CreateRoot(parent))
        {
            RectTransform safeArea = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 36f);
            safeArea.gameObject.AddComponent<SafeAreaFitter>();

            Text title = DemoUiFactory.CreateText(
                "Title",
                safeArea,
                "GENERIC GACHA RPG",
                68,
                TextAnchor.UpperCenter,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            title.rectTransform.anchorMin = new Vector2(0.12f, 0.72f);
            title.rectTransform.anchorMax = new Vector2(0.88f, 0.94f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            Text subtitle = DemoUiFactory.CreateText(
                "Subtitle",
                safeArea,
                "ORIGINAL CLEAN-ROOM VERTICAL SLICE",
                24,
                TextAnchor.UpperCenter,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            subtitle.rectTransform.anchorMin = new Vector2(0.12f, 0.67f);
            subtitle.rectTransform.anchorMax = new Vector2(0.88f, 0.75f);
            subtitle.rectTransform.offsetMin = Vector2.zero;
            subtitle.rectTransform.offsetMax = Vector2.zero;

            Image currencyCard = DemoUiFactory.CreatePanel(
                "CurrencyCard",
                safeArea,
                DemoUiFactory.Surface,
                new Vector2(0.73f, 0.87f),
                new Vector2(0.97f, 0.97f),
                Vector2.zero,
                Vector2.zero);
            currencyText = DemoUiFactory.CreateText(
                "Currency",
                currencyCard.transform,
                "CRYSTALS 0",
                27,
                TextAnchor.MiddleCenter,
                DemoUiFactory.Warning,
                FontStyle.Bold);

            RectTransform buttonRow = DemoUiFactory.CreateRect(
                "MainActions",
                safeArea,
                new Vector2(0.12f, 0.34f),
                new Vector2(0.88f, 0.62f),
                Vector2.zero,
                Vector2.zero);
            DemoUiFactory.AddHorizontalLayout(
                buttonRow.gameObject,
                28f,
                new RectOffset(18, 18, 18, 18));

            Button gachaButton = DemoUiFactory.CreateButton(
                "GachaButton",
                buttonRow,
                "GACHA\nSUMMON",
                new Color(0.44f, 0.24f, 0.72f, 1f),
                () => openGacha?.Invoke());
            DemoUiFactory.SetLayout(gachaButton.gameObject, 300f, 180f, 1f, 1f);

            Button collectionButton = DemoUiFactory.CreateButton(
                "CollectionButton",
                buttonRow,
                "CHARACTER\nCOLLECTION",
                new Color(0.12f, 0.46f, 0.64f, 1f),
                () => openCollection?.Invoke());
            DemoUiFactory.SetLayout(collectionButton.gameObject, 300f, 180f, 1f, 1f);

            Button formationButton = DemoUiFactory.CreateButton(
                "FormationButton",
                buttonRow,
                "TEAM\nFORMATION",
                new Color(0.11f, 0.55f, 0.39f, 1f),
                () => openFormation?.Invoke());
            DemoUiFactory.SetLayout(formationButton.gameObject, 300f, 180f, 1f, 1f);

            battleButton = DemoUiFactory.CreateButton(
                "BattleButton",
                buttonRow,
                "START\nBATTLE",
                new Color(0.78f, 0.24f, 0.23f, 1f),
                () => startBattle?.Invoke());
            DemoUiFactory.SetLayout(battleButton.gameObject, 300f, 180f, 1f, 1f);

            statusText = DemoUiFactory.CreateText(
                "Status",
                safeArea,
                string.Empty,
                25,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextMuted);
            statusText.rectTransform.anchorMin = new Vector2(0.18f, 0.22f);
            statusText.rectTransform.anchorMax = new Vector2(0.82f, 0.32f);
            statusText.rectTransform.offsetMin = Vector2.zero;
            statusText.rectTransform.offsetMax = Vector2.zero;

            Button resetButton = DemoUiFactory.CreateButton(
                "ResetButton",
                safeArea,
                "RESET DEMO DATA",
                DemoUiFactory.SurfaceLight,
                () => resetData?.Invoke());
            RectTransform resetRect = resetButton.GetComponent<RectTransform>();
            resetRect.anchorMin = new Vector2(0.38f, 0.07f);
            resetRect.anchorMax = new Vector2(0.62f, 0.16f);
            resetRect.offsetMin = Vector2.zero;
            resetRect.offsetMax = Vector2.zero;
        }

        public void Refresh(PlayerState state)
        {
            int currency = state == null ? 0 : state.Currency;
            bool validFormation = state != null && state.TeamFormation != null && state.TeamFormation.IsComplete;
            currencyText.text = $"CRYSTALS  {currency:N0}";
            battleButton.interactable = validFormation;
            statusText.text = validFormation
                ? "Your three-character team is ready."
                : "Choose exactly three unlocked characters in Formation.";
            statusText.color = validFormation ? DemoUiFactory.Positive : DemoUiFactory.Warning;
        }

        private static GameObject CreateRoot(Transform parent)
        {
            return DemoUiFactory.CreatePanel(
                "HomeScreen",
                parent,
                DemoUiFactory.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero).gameObject;
        }
    }

    public sealed class GachaScreenView : DemoScreenView
    {
        private readonly Text currencyText;
        private readonly Text bannerText;
        private readonly Text oddsText;
        private readonly Text resultText;
        private readonly Image resultColor;
        private readonly Button pullButton;

        public GachaScreenView(Transform parent, Action pull, Action back)
            : base(CreateRoot(parent))
        {
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 34f);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            BuildHeader(safe, "GACHA LAB", back, out currencyText);

            Image bannerCard = DemoUiFactory.CreatePanel(
                "BannerCard",
                safe,
                new Color(0.18f, 0.09f, 0.29f, 1f),
                new Vector2(0.06f, 0.15f),
                new Vector2(0.58f, 0.82f),
                Vector2.zero,
                Vector2.zero);
            bannerText = DemoUiFactory.CreateText(
                "BannerText",
                bannerCard.transform,
                "STANDARD SIGNAL",
                44,
                TextAnchor.UpperCenter,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            bannerText.rectTransform.offsetMin = new Vector2(38f, 210f);
            bannerText.rectTransform.offsetMax = new Vector2(-38f, -42f);

            oddsText = DemoUiFactory.CreateText(
                "Odds",
                bannerCard.transform,
                string.Empty,
                25,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextMuted);
            oddsText.rectTransform.anchorMin = new Vector2(0.08f, 0.22f);
            oddsText.rectTransform.anchorMax = new Vector2(0.92f, 0.56f);
            oddsText.rectTransform.offsetMin = Vector2.zero;
            oddsText.rectTransform.offsetMax = Vector2.zero;

            pullButton = DemoUiFactory.CreateButton(
                "PullButton",
                bannerCard.transform,
                "SINGLE SUMMON",
                new Color(0.50f, 0.27f, 0.77f, 1f),
                () => pull?.Invoke());
            RectTransform pullRect = pullButton.GetComponent<RectTransform>();
            pullRect.anchorMin = new Vector2(0.24f, 0.07f);
            pullRect.anchorMax = new Vector2(0.76f, 0.22f);
            pullRect.offsetMin = Vector2.zero;
            pullRect.offsetMax = Vector2.zero;

            Image resultCard = DemoUiFactory.CreatePanel(
                "ResultCard",
                safe,
                DemoUiFactory.Surface,
                new Vector2(0.62f, 0.15f),
                new Vector2(0.94f, 0.82f),
                Vector2.zero,
                Vector2.zero);
            resultColor = DemoUiFactory.CreatePanel(
                "ResultColor",
                resultCard.transform,
                DemoUiFactory.SurfaceLight,
                new Vector2(0.26f, 0.52f),
                new Vector2(0.74f, 0.88f),
                Vector2.zero,
                Vector2.zero);
            resultText = DemoUiFactory.CreateText(
                "ResultText",
                resultCard.transform,
                "Summon a new signal.",
                29,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextMuted,
                FontStyle.Bold);
            resultText.rectTransform.anchorMin = new Vector2(0.08f, 0.08f);
            resultText.rectTransform.anchorMax = new Vector2(0.92f, 0.48f);
            resultText.rectTransform.offsetMin = Vector2.zero;
            resultText.rectTransform.offsetMax = Vector2.zero;

            Text disclaimer = DemoUiFactory.CreateText(
                "Disclaimer",
                safe,
                "DEMO / NOT SERVER AUTHORITATIVE • NO REAL-MONEY PURCHASES",
                20,
                TextAnchor.MiddleCenter,
                DemoUiFactory.Warning,
                FontStyle.Bold);
            disclaimer.rectTransform.anchorMin = new Vector2(0.18f, 0.04f);
            disclaimer.rectTransform.anchorMax = new Vector2(0.82f, 0.11f);
            disclaimer.rectTransform.offsetMin = Vector2.zero;
            disclaimer.rectTransform.offsetMax = Vector2.zero;
        }

        public void Refresh(PlayerState state, GachaBannerDefinition banner, GameDatabase database)
        {
            int balance = state == null ? 0 : state.Currency;
            currencyText.text = $"CRYSTALS  {balance:N0}";
            if (banner == null)
            {
                bannerText.text = "NO BANNER CONFIGURED";
                oddsText.text = "Run the demo generator to repair content.";
                pullButton.interactable = false;
                return;
            }

            bannerText.text = $"{banner.DisplayName.ToUpperInvariant()}\n\n{banner.Description}";
            pullButton.interactable = banner.TotalWeight > 0f && balance >= banner.SingleDrawCost;
            Text label = pullButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = $"SINGLE SUMMON  •  {banner.SingleDrawCost}";
            }

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

                float percent = entry.Weight / banner.TotalWeight * 100f;
                lines.Add($"{FormatRarity(character.Rarity)}  {character.DisplayName}  {percent:0.#}%");
            }

            oddsText.text = string.Join("\n", lines);
        }

        public void ShowResult(GachaResult result, CharacterDefinition character)
        {
            if (result == null || !result.Success || character == null)
            {
                resultColor.color = DemoUiFactory.Danger;
                resultText.color = DemoUiFactory.Danger;
                resultText.text = result == null ? "Summon failed." : result.ErrorMessage;
                return;
            }

            resultColor.color = character.DisplayColor;
            resultText.color = RarityColor(character.Rarity);
            resultText.text =
                $"{FormatRarity(character.Rarity)}\n{character.DisplayName}\n" +
                (result.IsNewCharacter ? "NEW CHARACTER UNLOCKED" : "DUPLICATE SIGNAL REGISTERED");
        }

        public void ClearResult()
        {
            resultColor.color = DemoUiFactory.SurfaceLight;
            resultText.color = DemoUiFactory.TextMuted;
            resultText.text = "Summon a new signal.";
        }

        private static GameObject CreateRoot(Transform parent)
        {
            return DemoUiFactory.CreatePanel(
                "GachaScreen",
                parent,
                DemoUiFactory.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero).gameObject;
        }

        private static void BuildHeader(Transform parent, string title, Action back, out Text rightText)
        {
            Button backButton = DemoUiFactory.CreateButton(
                "BackButton",
                parent,
                "← HOME",
                DemoUiFactory.SurfaceLight,
                () => back?.Invoke());
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.02f, 0.87f);
            backRect.anchorMax = new Vector2(0.16f, 0.97f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;

            Text header = DemoUiFactory.CreateText(
                "Header",
                parent,
                title,
                48,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            header.rectTransform.anchorMin = new Vector2(0.22f, 0.86f);
            header.rectTransform.anchorMax = new Vector2(0.78f, 0.98f);
            header.rectTransform.offsetMin = Vector2.zero;
            header.rectTransform.offsetMax = Vector2.zero;

            rightText = DemoUiFactory.CreateText(
                "RightHeader",
                parent,
                string.Empty,
                26,
                TextAnchor.MiddleCenter,
                DemoUiFactory.Warning,
                FontStyle.Bold);
            rightText.rectTransform.anchorMin = new Vector2(0.80f, 0.87f);
            rightText.rectTransform.anchorMax = new Vector2(0.98f, 0.97f);
            rightText.rectTransform.offsetMin = Vector2.zero;
            rightText.rectTransform.offsetMax = Vector2.zero;
        }
    }

    public sealed class CollectionScreenView : DemoScreenView
    {
        private readonly Transform grid;
        private readonly Text ownedText;

        public CollectionScreenView(Transform parent, Action back)
            : base(CreateRoot(parent))
        {
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 34f);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            BuildSimpleHeader(safe, "CHARACTER COLLECTION", back, out ownedText);

            Image gridPanel = DemoUiFactory.CreatePanel(
                "GridPanel",
                safe,
                DemoUiFactory.Surface,
                new Vector2(0.05f, 0.08f),
                new Vector2(0.95f, 0.83f),
                Vector2.zero,
                Vector2.zero);
            grid = DemoUiFactory.CreateStretchRect("CharacterGrid", gridPanel.transform, 28f);
            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(490f, 270f);
            layout.spacing = new Vector2(24f, 24f);
            layout.padding = new RectOffset(30, 30, 24, 24);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.childAlignment = TextAnchor.MiddleCenter;
        }

        public void Refresh(GameDatabase database, PlayerState state)
        {
            DemoUiFactory.DestroyChildren(grid);
            int owned = 0;
            if (database != null)
            {
                for (int i = 0; i < database.Characters.Count; i++)
                {
                    CharacterDefinition character = database.Characters[i];
                    if (character == null)
                    {
                        continue;
                    }

                    bool unlocked = state != null && state.HasCharacter(character.Id);
                    if (unlocked)
                    {
                        owned++;
                    }

                    CreateCharacterCard(character, unlocked);
                }
            }

            ownedText.text = database == null ? "0 / 0" : $"OWNED  {owned} / {database.Characters.Count}";
        }

        private void CreateCharacterCard(CharacterDefinition character, bool unlocked)
        {
            Image card = DemoUiFactory.CreatePanel(
                $"Card_{character.Id}",
                grid,
                unlocked ? DemoUiFactory.SurfaceLight : new Color(0.06f, 0.07f, 0.09f, 1f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);

            Image swatch = DemoUiFactory.CreatePanel(
                "Swatch",
                card.transform,
                unlocked ? character.DisplayColor : Color.black,
                new Vector2(0.05f, 0.15f),
                new Vector2(0.29f, 0.85f),
                Vector2.zero,
                Vector2.zero);

            Text body = DemoUiFactory.CreateText(
                "Body",
                card.transform,
                unlocked
                    ? $"{character.DisplayName}\n{FormatRarity(character.Rarity)} • {character.Role}\n" +
                      $"LV 1  |  HP {character.MaxHealth:0}\nATK {character.Attack:0}  DEF {character.Defense:0}"
                    : "LOCKED SIGNAL\nAcquire from Gacha",
                24,
                TextAnchor.MiddleLeft,
                unlocked ? DemoUiFactory.TextPrimary : DemoUiFactory.TextMuted,
                unlocked ? FontStyle.Bold : FontStyle.Italic);
            body.rectTransform.anchorMin = new Vector2(0.34f, 0.12f);
            body.rectTransform.anchorMax = new Vector2(0.96f, 0.88f);
            body.rectTransform.offsetMin = Vector2.zero;
            body.rectTransform.offsetMax = Vector2.zero;
            swatch.raycastTarget = false;
        }

        private static GameObject CreateRoot(Transform parent)
        {
            return DemoUiFactory.CreatePanel(
                "CollectionScreen",
                parent,
                DemoUiFactory.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero).gameObject;
        }

        private static void BuildSimpleHeader(Transform parent, string title, Action back, out Text rightText)
        {
            Button backButton = DemoUiFactory.CreateButton(
                "BackButton",
                parent,
                "← HOME",
                DemoUiFactory.SurfaceLight,
                () => back?.Invoke());
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.02f, 0.87f);
            backRect.anchorMax = new Vector2(0.16f, 0.97f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;

            Text header = DemoUiFactory.CreateText(
                "Header",
                parent,
                title,
                48,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            header.rectTransform.anchorMin = new Vector2(0.22f, 0.86f);
            header.rectTransform.anchorMax = new Vector2(0.78f, 0.98f);
            header.rectTransform.offsetMin = Vector2.zero;
            header.rectTransform.offsetMax = Vector2.zero;

            rightText = DemoUiFactory.CreateText(
                "RightHeader",
                parent,
                string.Empty,
                24,
                TextAnchor.MiddleCenter,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            rightText.rectTransform.anchorMin = new Vector2(0.80f, 0.87f);
            rightText.rectTransform.anchorMax = new Vector2(0.98f, 0.97f);
            rightText.rectTransform.offsetMin = Vector2.zero;
            rightText.rectTransform.offsetMax = Vector2.zero;
        }
    }

    public sealed class FormationScreenView : DemoScreenView
    {
        private readonly Transform grid;
        private readonly Text slotsText;
        private readonly Text feedbackText;
        private readonly Button battleButton;

        public FormationScreenView(Transform parent, Action<string> toggleCharacter, Action battle, Action back)
            : base(CreateRoot(parent))
        {
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 34f);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            Button backButton = DemoUiFactory.CreateButton(
                "BackButton",
                safe,
                "← HOME",
                DemoUiFactory.SurfaceLight,
                () => back?.Invoke());
            RectTransform backRect = backButton.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.02f, 0.87f);
            backRect.anchorMax = new Vector2(0.16f, 0.97f);
            backRect.offsetMin = Vector2.zero;
            backRect.offsetMax = Vector2.zero;

            Text header = DemoUiFactory.CreateText(
                "Header",
                safe,
                "THREE-UNIT FORMATION",
                48,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            header.rectTransform.anchorMin = new Vector2(0.22f, 0.86f);
            header.rectTransform.anchorMax = new Vector2(0.78f, 0.98f);
            header.rectTransform.offsetMin = Vector2.zero;
            header.rectTransform.offsetMax = Vector2.zero;

            slotsText = DemoUiFactory.CreateText(
                "Slots",
                safe,
                "SLOT 1  —  SLOT 2  —  SLOT 3",
                30,
                TextAnchor.MiddleCenter,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            slotsText.rectTransform.anchorMin = new Vector2(0.12f, 0.73f);
            slotsText.rectTransform.anchorMax = new Vector2(0.88f, 0.84f);
            slotsText.rectTransform.offsetMin = Vector2.zero;
            slotsText.rectTransform.offsetMax = Vector2.zero;

            Image gridPanel = DemoUiFactory.CreatePanel(
                "GridPanel",
                safe,
                DemoUiFactory.Surface,
                new Vector2(0.08f, 0.19f),
                new Vector2(0.92f, 0.71f),
                Vector2.zero,
                Vector2.zero);
            grid = DemoUiFactory.CreateStretchRect("FormationGrid", gridPanel.transform, 24f);
            GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(420f, 180f);
            layout.spacing = new Vector2(24f, 22f);
            layout.padding = new RectOffset(26, 26, 24, 24);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.childAlignment = TextAnchor.MiddleCenter;

            feedbackText = DemoUiFactory.CreateText(
                "Feedback",
                safe,
                string.Empty,
                23,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextMuted);
            feedbackText.rectTransform.anchorMin = new Vector2(0.18f, 0.10f);
            feedbackText.rectTransform.anchorMax = new Vector2(0.62f, 0.18f);
            feedbackText.rectTransform.offsetMin = Vector2.zero;
            feedbackText.rectTransform.offsetMax = Vector2.zero;

            battleButton = DemoUiFactory.CreateButton(
                "BattleButton",
                safe,
                "START 3v3 BATTLE",
                new Color(0.78f, 0.24f, 0.23f, 1f),
                () => battle?.Invoke());
            RectTransform battleRect = battleButton.GetComponent<RectTransform>();
            battleRect.anchorMin = new Vector2(0.66f, 0.07f);
            battleRect.anchorMax = new Vector2(0.92f, 0.17f);
            battleRect.offsetMin = Vector2.zero;
            battleRect.offsetMax = Vector2.zero;

            ToggleCharacter = toggleCharacter;
        }

        private Action<string> ToggleCharacter { get; }

        public void Refresh(GameDatabase database, PlayerState state, IReadOnlyList<string> draftIds, string feedback = "")
        {
            DemoUiFactory.DestroyChildren(grid);
            var names = new List<string>();
            if (draftIds != null && database != null)
            {
                for (int i = 0; i < draftIds.Count; i++)
                {
                    CharacterDefinition character = database.GetCharacter(draftIds[i]);
                    names.Add(character == null ? "EMPTY" : character.DisplayName.ToUpperInvariant());
                }
            }

            while (names.Count < TeamFormationState.RequiredMemberCount)
            {
                names.Add("EMPTY");
            }

            slotsText.text = $"SLOT 1  {names[0]}     •     SLOT 2  {names[1]}     •     SLOT 3  {names[2]}";
            feedbackText.text = string.IsNullOrEmpty(feedback)
                ? "Select exactly three unlocked characters. Click again to remove."
                : feedback;
            feedbackText.color = string.IsNullOrEmpty(feedback) ? DemoUiFactory.TextMuted : DemoUiFactory.Warning;
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
                Color color = !unlocked
                    ? new Color(0.11f, 0.12f, 0.14f, 1f)
                    : selected
                        ? DemoUiFactory.Positive
                        : new Color(character.DisplayColor.r * 0.62f, character.DisplayColor.g * 0.62f, character.DisplayColor.b * 0.62f, 1f);
                string label = unlocked
                    ? $"{(selected ? "✓ " : string.Empty)}{character.DisplayName}\n{FormatRarity(character.Rarity)} • {character.Role}"
                    : "LOCKED\nAcquire from Gacha";
                string id = character.Id;
                Button button = DemoUiFactory.CreateButton(
                    $"Character_{id}",
                    grid,
                    label,
                    color,
                    () => ToggleCharacter?.Invoke(id));
                button.interactable = unlocked;
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
            return DemoUiFactory.CreatePanel(
                "FormationScreen",
                parent,
                DemoUiFactory.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero).gameObject;
        }
    }

    public sealed class BattleScreenView : DemoScreenView
    {
        private readonly Text timerText;
        private readonly Text statusText;
        private readonly GameObject resultPanel;
        private readonly Text resultTitle;
        private readonly Text resultSummary;

        public BattleScreenView(Transform parent, Action restart, Action home)
            : base(CreateRoot(parent))
        {
            RectTransform safe = DemoUiFactory.CreateStretchRect("SafeArea", Root.transform, 24f);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            timerText = DemoUiFactory.CreateText(
                "Timer",
                safe,
                "00:00",
                34,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextPrimary,
                FontStyle.Bold);
            timerText.rectTransform.anchorMin = new Vector2(0.43f, 0.90f);
            timerText.rectTransform.anchorMax = new Vector2(0.57f, 0.98f);
            timerText.rectTransform.offsetMin = Vector2.zero;
            timerText.rectTransform.offsetMax = Vector2.zero;

            statusText = DemoUiFactory.CreateText(
                "Status",
                safe,
                "BATTLE INITIALIZING",
                22,
                TextAnchor.MiddleCenter,
                DemoUiFactory.Accent,
                FontStyle.Bold);
            statusText.rectTransform.anchorMin = new Vector2(0.24f, 0.83f);
            statusText.rectTransform.anchorMax = new Vector2(0.76f, 0.90f);
            statusText.rectTransform.offsetMin = Vector2.zero;
            statusText.rectTransform.offsetMax = Vector2.zero;

            Image panel = DemoUiFactory.CreatePanel(
                "ResultPanel",
                safe,
                new Color(0.035f, 0.055f, 0.10f, 0.96f),
                new Vector2(0.29f, 0.20f),
                new Vector2(0.71f, 0.80f),
                Vector2.zero,
                Vector2.zero);
            resultPanel = panel.gameObject;
            resultTitle = DemoUiFactory.CreateText(
                "ResultTitle",
                panel.transform,
                "VICTORY",
                64,
                TextAnchor.MiddleCenter,
                DemoUiFactory.Positive,
                FontStyle.Bold);
            resultTitle.rectTransform.anchorMin = new Vector2(0.08f, 0.62f);
            resultTitle.rectTransform.anchorMax = new Vector2(0.92f, 0.90f);
            resultTitle.rectTransform.offsetMin = Vector2.zero;
            resultTitle.rectTransform.offsetMax = Vector2.zero;

            resultSummary = DemoUiFactory.CreateText(
                "ResultSummary",
                panel.transform,
                string.Empty,
                25,
                TextAnchor.MiddleCenter,
                DemoUiFactory.TextPrimary);
            resultSummary.rectTransform.anchorMin = new Vector2(0.08f, 0.38f);
            resultSummary.rectTransform.anchorMax = new Vector2(0.92f, 0.62f);
            resultSummary.rectTransform.offsetMin = Vector2.zero;
            resultSummary.rectTransform.offsetMax = Vector2.zero;

            Button restartButton = DemoUiFactory.CreateButton(
                "RestartButton",
                panel.transform,
                "RESTART",
                DemoUiFactory.Accent,
                () => restart?.Invoke());
            RectTransform restartRect = restartButton.GetComponent<RectTransform>();
            restartRect.anchorMin = new Vector2(0.09f, 0.10f);
            restartRect.anchorMax = new Vector2(0.46f, 0.30f);
            restartRect.offsetMin = Vector2.zero;
            restartRect.offsetMax = Vector2.zero;

            Button homeButton = DemoUiFactory.CreateButton(
                "HomeButton",
                panel.transform,
                "RETURN HOME",
                DemoUiFactory.SurfaceLight,
                () => home?.Invoke());
            RectTransform homeRect = homeButton.GetComponent<RectTransform>();
            homeRect.anchorMin = new Vector2(0.54f, 0.10f);
            homeRect.anchorMax = new Vector2(0.91f, 0.30f);
            homeRect.offsetMin = Vector2.zero;
            homeRect.offsetMax = Vector2.zero;
            resultPanel.SetActive(false);
        }

        public void SetBattleStatus(float elapsed, string status)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(elapsed));
            timerText.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
            statusText.text = string.IsNullOrEmpty(status) ? "AUTO BATTLE" : status;
        }

        public void HideResult()
        {
            resultPanel.SetActive(false);
        }

        public void ShowResult(bool playerWon, bool timedOut, float duration)
        {
            resultPanel.SetActive(true);
            resultTitle.text = timedOut ? "TIME LIMIT" : playerWon ? "VICTORY" : "DEFEAT";
            resultTitle.color = timedOut
                ? DemoUiFactory.Warning
                : playerWon
                    ? DemoUiFactory.Positive
                    : DemoUiFactory.Danger;
            resultSummary.text =
                $"Battle duration: {duration:0.0}s\n" +
                (playerWon ? "Your formation held the line." : "Adjust your formation and try again.");
        }

        private static GameObject CreateRoot(Transform parent)
        {
            Image image = DemoUiFactory.CreatePanel(
                "BattleScreen",
                parent,
                new Color(0f, 0f, 0f, 0f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            image.raycastTarget = false;
            return image.gameObject;
        }
    }
}
