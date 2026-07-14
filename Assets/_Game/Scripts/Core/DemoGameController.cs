using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GenericGachaRPG
{
    [DisallowMultipleComponent]
    public sealed class DemoGameController : MonoBehaviour
    {
        [SerializeField] private GameDatabase database;
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private int gachaSeed = 20260713;
        [SerializeField] private int battleSeed = 7312026;

        private readonly List<string> draftFormation = new List<string>();

        private GameStateService gameState;
        private IGachaService gachaService;
        private IFormationService formationService;
        private DemoBattlePresenter battlePresenter;
        private HomeScreenView homeScreen;
        private GachaScreenView gachaScreen;
        private CollectionScreenView collectionScreen;
        private FormationScreenView formationScreen;
        private BattleScreenView battleScreen;
        private DemoScreen currentScreen;
        private string formationFeedback = string.Empty;

        public GameDatabase Database => database;

        public void Configure(GameDatabase gameDatabase, Camera cameraToUse)
        {
            database = gameDatabase;
            sceneCamera = cameraToUse;
        }

        private void Start()
        {
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (database == null)
            {
                BuildFatalError("GameDatabase is missing.\nRun Tools > Generic Gacha RPG > Generate or Repair Demo.");
                Debug.LogError("[GenericGachaRPG] Demo cannot start because GameDatabase is missing.", this);
                return;
            }

            gameState = new GameStateService(database);
            gachaService = new LocalGachaService(database, gameState, new SeededRandomService(gachaSeed));
            formationService = new LocalFormationService(database, gameState);
            gameState.StateChanged += OnStateChanged;

            EnsureSceneCamera();
            BuildScreens();
            ResetDraftFromSavedFormation();
            ShowScreen(DemoScreen.Home);
        }

        private void OnDestroy()
        {
            if (gameState != null)
            {
                gameState.StateChanged -= OnStateChanged;
            }

            if (battlePresenter != null)
            {
                battlePresenter.BattleCompleted -= OnBattleCompleted;
                battlePresenter.StopBattle();
            }
        }

        private void BuildScreens()
        {
            Canvas canvas = DemoUiFactory.CreateCanvas(transform);
            RectTransform root = DemoUiFactory.CreateStretchRect("ScreenRoot", canvas.transform);

            homeScreen = new HomeScreenView(
                root,
                OpenGacha,
                OpenCollection,
                OpenFormation,
                BeginBattleFromSavedFormation,
                ResetDemoData);
            gachaScreen = new GachaScreenView(root, DrawSingle, ReturnHome);
            collectionScreen = new CollectionScreenView(root, ReturnHome);
            formationScreen = new FormationScreenView(root, ToggleFormationCharacter, BeginBattleFromDraft, ReturnHome);
            battleScreen = new BattleScreenView(root, RestartBattle, ReturnHomeFromBattle);

            battlePresenter = GetComponent<DemoBattlePresenter>();
            if (battlePresenter == null)
            {
                battlePresenter = gameObject.AddComponent<DemoBattlePresenter>();
            }

            battlePresenter.Configure(sceneCamera);
            battlePresenter.BattleCompleted += OnBattleCompleted;
        }

        private void ShowScreen(DemoScreen screen)
        {
            currentScreen = screen;
            homeScreen?.SetVisible(screen == DemoScreen.Home);
            gachaScreen?.SetVisible(screen == DemoScreen.Gacha);
            collectionScreen?.SetVisible(screen == DemoScreen.Collection);
            formationScreen?.SetVisible(screen == DemoScreen.Formation);
            battleScreen?.SetVisible(screen == DemoScreen.Battle);
            RefreshScreen(screen);
            SelectFirstInteractableButton(screen);
        }

        private void SelectFirstInteractableButton(DemoScreen screen)
        {
            DemoScreenView activeView = screen switch
            {
                DemoScreen.Gacha => gachaScreen,
                DemoScreen.Collection => collectionScreen,
                DemoScreen.Formation => formationScreen,
                DemoScreen.Battle => battleScreen,
                _ => homeScreen
            };

            if (activeView == null || EventSystem.current == null)
            {
                return;
            }

            Button[] buttons = activeView.Root.GetComponentsInChildren<Button>(false);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].IsInteractable())
                {
                    EventSystem.current.SetSelectedGameObject(buttons[i].gameObject);
                    return;
                }
            }
        }

        private void OpenGacha()
        {
            gachaScreen.ClearResult();
            ShowScreen(DemoScreen.Gacha);
        }

        private void OpenCollection()
        {
            ShowScreen(DemoScreen.Collection);
        }

        private void OpenFormation()
        {
            ResetDraftFromSavedFormation();
            formationFeedback = string.Empty;
            ShowScreen(DemoScreen.Formation);
        }

        private void ReturnHome()
        {
            ShowScreen(DemoScreen.Home);
        }

        private void ReturnHomeFromBattle()
        {
            battlePresenter.StopBattle();
            ShowScreen(DemoScreen.Home);
        }

        private void DrawSingle()
        {
            GachaResult result = gachaService.DrawSingle(database.DefaultBanner);
            CharacterDefinition character = result == null || !result.Success
                ? null
                : database.GetCharacter(result.CharacterId);
            gachaScreen.ShowResult(result, character);
        }

        private void ToggleFormationCharacter(string characterId)
        {
            int existingIndex = draftFormation.FindIndex(id => string.Equals(id, characterId, StringComparison.Ordinal));
            if (existingIndex >= 0)
            {
                draftFormation.RemoveAt(existingIndex);
                formationFeedback = string.Empty;
            }
            else if (draftFormation.Count >= TeamFormationState.RequiredMemberCount)
            {
                formationFeedback = "Remove one selected character before adding another.";
            }
            else if (gameState.State.HasCharacter(characterId))
            {
                draftFormation.Add(characterId);
                formationFeedback = string.Empty;
            }

            if (draftFormation.Count == TeamFormationState.RequiredMemberCount)
            {
                if (formationService.TrySetFormation(draftFormation, out string reason))
                {
                    formationFeedback = "Formation saved. Ready for battle.";
                }
                else
                {
                    formationFeedback = reason;
                }
            }

            formationScreen.Refresh(database, gameState.State, draftFormation, formationFeedback);
            homeScreen.Refresh(gameState.State);
        }

        private void BeginBattleFromSavedFormation()
        {
            ResetDraftFromSavedFormation();
            BeginBattleFromDraft();
        }

        private void BeginBattleFromDraft()
        {
            if (!formationService.TrySetFormation(draftFormation, out string reason))
            {
                formationFeedback = reason;
                ShowScreen(DemoScreen.Formation);
                return;
            }

            List<CharacterDefinition> playerTeam = ResolveCharacters(draftFormation);
            List<CharacterDefinition> enemyTeam = BuildEnemyTeam();
            if (playerTeam.Count != BattleTeam.RequiredMemberCount || enemyTeam.Count != BattleTeam.RequiredMemberCount)
            {
                formationFeedback = "Battle content is incomplete. Run Generate or Repair Demo.";
                ShowScreen(DemoScreen.Formation);
                return;
            }

            ShowScreen(DemoScreen.Battle);
            battlePresenter.StartBattle(playerTeam, enemyTeam, battleScreen, battleSeed);
        }

        private void RestartBattle()
        {
            ResetDraftFromSavedFormation();
            BeginBattleFromDraft();
        }

        private void ResetDemoData()
        {
            if (battlePresenter != null)
            {
                battlePresenter.StopBattle();
            }

            gameState.Reset();
            ResetDraftFromSavedFormation();
            formationFeedback = "Demo data reset to its original state.";
            gachaScreen.ClearResult();
            ShowScreen(DemoScreen.Home);
        }

        private void OnStateChanged(PlayerState state)
        {
            RefreshScreen(currentScreen);
        }

        private void OnBattleCompleted(BattleResult result)
        {
            // P0 keeps rewards presentation-only. Production rewards would be
            // committed by a backend-authoritative IBattleRewardService.
            RefreshScreen(currentScreen);
        }

        private void RefreshScreen(DemoScreen screen)
        {
            if (gameState == null)
            {
                return;
            }

            PlayerState state = gameState.State;
            switch (screen)
            {
                case DemoScreen.Gacha:
                    gachaScreen?.Refresh(state, database.DefaultBanner, database);
                    break;
                case DemoScreen.Collection:
                    collectionScreen?.Refresh(database, state);
                    break;
                case DemoScreen.Formation:
                    formationScreen?.Refresh(database, state, draftFormation, formationFeedback);
                    break;
                case DemoScreen.Home:
                    homeScreen?.Refresh(state);
                    break;
            }
        }

        private void ResetDraftFromSavedFormation()
        {
            draftFormation.Clear();
            TeamFormationState saved = gameState == null || gameState.State == null
                ? null
                : gameState.State.TeamFormation;
            if (saved == null)
            {
                return;
            }

            for (int i = 0; i < saved.CharacterIds.Count; i++)
            {
                draftFormation.Add(saved.CharacterIds[i]);
            }
        }

        private List<CharacterDefinition> ResolveCharacters(IReadOnlyList<string> ids)
        {
            var result = new List<CharacterDefinition>();
            if (ids == null)
            {
                return result;
            }

            for (int i = 0; i < ids.Count; i++)
            {
                CharacterDefinition character = database.GetCharacter(ids[i]);
                if (character != null)
                {
                    result.Add(character);
                }
            }

            return result;
        }

        private List<CharacterDefinition> BuildEnemyTeam()
        {
            var result = new List<CharacterDefinition>(BattleTeam.RequiredMemberCount);
            for (int i = database.Characters.Count - 1;
                 i >= 0 && result.Count < BattleTeam.RequiredMemberCount;
                 i--)
            {
                CharacterDefinition definition = database.Characters[i];
                if (definition != null && !definition.IsLimited)
                {
                    result.Add(definition);
                }
            }

            result.Reverse();
            return result;
        }

        private void EnsureSceneCamera()
        {
            if (sceneCamera == null)
            {
                sceneCamera = Camera.main;
            }

            if (sceneCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                sceneCamera = cameraObject.GetComponent<Camera>();
            }

            sceneCamera.clearFlags = CameraClearFlags.SolidColor;
            sceneCamera.backgroundColor = new Color(0.025f, 0.045f, 0.085f, 1f);
        }

        private void BuildFatalError(string message)
        {
            Canvas canvas = DemoUiFactory.CreateCanvas(transform);
            Image background = DemoUiFactory.CreatePanel(
                "FatalError",
                canvas.transform,
                DemoUiFactory.Background,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            Text label = DemoUiFactory.CreateText(
                "Message",
                background.transform,
                message,
                34,
                TextAnchor.MiddleCenter,
                DemoUiFactory.Danger,
                FontStyle.Bold);
            label.rectTransform.offsetMin = new Vector2(120f, 120f);
            label.rectTransform.offsetMax = new Vector2(-120f, -120f);
        }
    }
}
