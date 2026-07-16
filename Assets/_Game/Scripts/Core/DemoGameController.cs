using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
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
        private DemoUiRouter router;
        private AppShellView appShell;
        private HomeHubScreenView homeScreen;
        private WorldStageScreenView worldScreen;
        private SummonScreenView gachaScreen;
        private CharacterPageScreenView collectionScreen;
        private RosterFormationScreenView formationScreen;
        private InventoryScreenView inventoryScreen;
        private MissionsScreenView missionsScreen;
        private SettingsScreenView settingsScreen;
        private LockedFeatureScreenView lockedFeatureScreen;
        private BattleScreenView battleScreen;
        private StageDefinition activeStage;
        private string selectedStageId = string.Empty;
        private string formationFeedback = string.Empty;
        private string missionFeedback = string.Empty;
        private bool battleResultCommitted;

        public GameDatabase Database => database;
        public PlayerState CurrentPlayerState => gameState?.State;
        public AppRoute CurrentRoute => router == null ? AppRoute.Home : router.CurrentRoute;

        public void Configure(GameDatabase gameDatabase, Camera cameraToUse)
        {
            database = gameDatabase;
            sceneCamera = cameraToUse;
        }

        private void Start()
        {
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
            ApplyRuntimeSettings();
            router.ResetTo(AppRoute.Home);
            ApplyLaunchRouteOption();
        }

        private void OnDestroy()
        {
            if (gameState != null)
            {
                gameState.StateChanged -= OnStateChanged;
            }

            if (router != null)
            {
                router.RouteChanged -= OnRouteChanged;
            }

            if (battlePresenter != null)
            {
                battlePresenter.BattleCompleted -= OnBattleCompleted;
                battlePresenter.StopBattle();
            }
        }

        private void Update()
        {
            bool cancelPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            cancelPressed |= Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            if (!cancelPressed || router == null)
            {
                return;
            }

            if (router.CurrentRoute == AppRoute.Settings && settingsScreen != null && settingsScreen.TryCloseModal())
            {
                return;
            }

            if (router.CurrentRoute == AppRoute.Battle)
            {
                ReturnWorldFromBattle();
                return;
            }

            router.Back(AppRoute.Home);
        }

        private void BuildScreens()
        {
            Canvas canvas = DemoUiFactory.CreateCanvas(transform);
            RectTransform root = DemoUiFactory.CreateStretchRect("ScreenRoot", canvas.transform);

            homeScreen = new HomeHubScreenView(
                root,
                OpenCurrentStageWorld,
                OpenGacha,
                OpenCollection,
                OpenFormation,
                OpenLockedFeature);
            worldScreen = new WorldStageScreenView(
                root,
                database,
                SelectStage,
                OpenSelectedStageFormation,
                NavigateBack);
            gachaScreen = new SummonScreenView(root, DrawSingle, NavigateBack);
            collectionScreen = new CharacterPageScreenView(root, NavigateBack);
            formationScreen = new RosterFormationScreenView(
                root,
                ToggleFormationCharacter,
                BeginBattleFromDraft,
                NavigateBack);
            inventoryScreen = new InventoryScreenView(root, NavigateBack);
            missionsScreen = new MissionsScreenView(root, ClaimMission, NavigateForMission, NavigateBack);
            settingsScreen = new SettingsScreenView(
                root,
                SetMusicVolume,
                SetEffectsVolume,
                SetFullscreen,
                SetSixtyFps,
                ResetDemoData,
                NavigateBack);
            lockedFeatureScreen = new LockedFeatureScreenView(root, NavigateBack);
            battleScreen = new BattleScreenView(
                root,
                RestartBattle,
                ReturnWorldFromBattle,
                ReturnHomeFromBattle);

            router = new DemoUiRouter();
            router.Register(AppRoute.Home, homeScreen);
            router.Register(AppRoute.World, worldScreen);
            router.Register(AppRoute.Gacha, gachaScreen);
            router.Register(AppRoute.Collection, collectionScreen);
            router.Register(AppRoute.Formation, formationScreen);
            router.Register(AppRoute.Inventory, inventoryScreen);
            router.Register(AppRoute.Missions, missionsScreen);
            router.Register(AppRoute.Settings, settingsScreen);
            router.Register(AppRoute.LockedFeature, lockedFeatureScreen);
            router.Register(AppRoute.Battle, battleScreen);
            router.RouteChanged += OnRouteChanged;

            appShell = new AppShellView(
                canvas.transform,
                NavigateFromShell,
                OpenFormation,
                OpenSettings,
                OpenLockedFeature);
            appShell.SetVisible(false);

            battlePresenter = GetComponent<DemoBattlePresenter>();
            if (battlePresenter == null)
            {
                battlePresenter = gameObject.AddComponent<DemoBattlePresenter>();
            }

            battlePresenter.Configure(sceneCamera);
            battlePresenter.BattleCompleted += OnBattleCompleted;
        }

        private void OnRouteChanged(AppRoute route, string context)
        {
            bool showShell = UsesAppShell(route);
            appShell?.SetVisible(showShell);
            appShell?.SetActiveRoute(route);
            RefreshScreen(route, context);
            SelectFirstInteractableButton(route);
        }

        private void NavigateFromShell(AppRoute route)
        {
            switch (route)
            {
                case AppRoute.Home:
                    router.ResetTo(AppRoute.Home);
                    break;
                case AppRoute.World:
                    OpenWorld();
                    break;
                case AppRoute.Gacha:
                    OpenGacha();
                    break;
                case AppRoute.Collection:
                    OpenCollection();
                    break;
                case AppRoute.Formation:
                    OpenFormation();
                    break;
                case AppRoute.Inventory:
                    router.Navigate(AppRoute.Inventory);
                    break;
                case AppRoute.Missions:
                    missionFeedback = string.Empty;
                    router.Navigate(AppRoute.Missions);
                    break;
                case AppRoute.Settings:
                    OpenSettings();
                    break;
                default:
                    router.Navigate(route);
                    break;
            }
        }

        private void NavigateBack()
        {
            router?.Back(AppRoute.Home);
        }

        private void OpenWorld()
        {
            StageDefinition current = database.GetCurrentStage(gameState.State) ?? database.FirstStage;
            if (current != null && string.IsNullOrEmpty(selectedStageId))
            {
                selectedStageId = current.Id;
            }

            router.Navigate(AppRoute.World);
        }

        private void OpenCurrentStageWorld()
        {
            StageDefinition current = database.GetCurrentStage(gameState.State) ?? database.FirstStage;
            selectedStageId = current == null ? string.Empty : current.Id;
            router.Navigate(AppRoute.World);
        }

        private void SelectStage(string stageId)
        {
            if (database.GetStage(stageId) == null)
            {
                return;
            }

            selectedStageId = stageId;
            worldScreen.Refresh(gameState.State, selectedStageId);
        }

        private void OpenSelectedStageFormation()
        {
            string stageId = worldScreen == null ? selectedStageId : worldScreen.SelectedStageId;
            StageDefinition stage = database.GetStage(stageId) ?? database.GetCurrentStage(gameState.State);
            OpenFormationForStage(stage);
        }

        private void OpenGacha()
        {
            gachaScreen.ClearResult();
            router.Navigate(AppRoute.Gacha);
        }

        private void OpenCollection()
        {
            router.Navigate(AppRoute.Collection);
        }

        private void OpenFormation()
        {
            StageDefinition stage = database.GetCurrentStage(gameState.State) ?? database.FirstStage;
            OpenFormationForStage(stage);
        }

        private void OpenFormationForStage(StageDefinition stage)
        {
            activeStage = stage;
            if (stage != null)
            {
                selectedStageId = stage.Id;
            }

            ResetDraftFromSavedFormation();
            formationFeedback = stage == null
                ? string.Empty
                : $"Preparing {FormatStageId(stage.Id)} {stage.DisplayName}.";
            router.Navigate(AppRoute.Formation);
        }

        private void OpenSettings()
        {
            router.Navigate(AppRoute.Settings);
        }

        private void OpenLockedFeature(string feature)
        {
            router.Navigate(AppRoute.LockedFeature, feature ?? string.Empty);
        }

        private void ReturnWorldFromBattle()
        {
            battlePresenter.StopBattle();
            if (activeStage != null)
            {
                selectedStageId = activeStage.Id;
            }

            router.ResetTo(AppRoute.World);
        }

        private void ReturnHomeFromBattle()
        {
            battlePresenter.StopBattle();
            router.ResetTo(AppRoute.Home);
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
                    formationFeedback = activeStage == null
                        ? "Formation saved. Ready for battle."
                        : $"Formation saved for {FormatStageId(activeStage.Id)}.";
                }
                else
                {
                    formationFeedback = reason;
                }
            }

            formationScreen.Refresh(database, gameState.State, draftFormation, formationFeedback);
            appShell?.Refresh(gameState.State, database);
        }

        private void BeginBattleFromDraft()
        {
            if (!formationService.TrySetFormation(draftFormation, out string reason))
            {
                formationFeedback = reason;
                router.Replace(AppRoute.Formation);
                return;
            }

            StageDefinition stage = activeStage ?? database.GetCurrentStage(gameState.State) ?? database.FirstStage;
            IReadOnlyList<string> enemyIds = stage != null && stage.EnemyCharacterIds.Count > 0
                ? stage.EnemyCharacterIds
                : database.DemoEnemyBattleCharacterIds;
            List<CharacterDefinition> playerTeam = ResolveCharacters(draftFormation);
            List<CharacterDefinition> enemyTeam = ResolveCharacters(enemyIds);
            if (playerTeam.Count != BattleRules.DemoPlayerTeamSize ||
                enemyTeam.Count != BattleRules.DemoEnemyTeamSize)
            {
                formationFeedback = "Battle content is incomplete. Run Generate or Repair Demo.";
                router.Replace(AppRoute.Formation);
                return;
            }

            if (stage != null && !gameState.TryStartStage(stage, out reason))
            {
                formationFeedback = reason;
                router.Replace(AppRoute.Formation);
                return;
            }

            activeStage = stage;
            battleResultCommitted = false;
            router.Navigate(AppRoute.Battle);
            battlePresenter.StartBattle(playerTeam, enemyTeam, battleScreen, battleSeed);
        }

        private void RestartBattle()
        {
            battlePresenter.StopBattle();
            ResetDraftFromSavedFormation();
            BeginBattleFromDraft();
        }

        private void ClaimMission(string missionId)
        {
            bool claimed = gameState.TryClaimMission(missionId, out string reason);
            missionFeedback = claimed ? reason : $"Unable to claim: {reason}";
            missionsScreen.Refresh(gameState.State, missionFeedback);
            appShell?.Refresh(gameState.State, database);
        }

        private void NavigateForMission(string missionId)
        {
            DemoMissionDefinition mission = DemoMissionCatalog.Get(missionId);
            if (mission == null)
            {
                missionFeedback = "Mission route is unavailable.";
                missionsScreen.Refresh(gameState.State, missionFeedback);
                return;
            }

            switch (mission.Objective)
            {
                case DemoMissionObjective.DrawCharacters:
                    OpenGacha();
                    break;
                case DemoMissionObjective.OwnCharacters:
                    OpenCollection();
                    break;
                case DemoMissionObjective.ClearStage:
                    if (!string.IsNullOrEmpty(mission.StageId))
                    {
                        selectedStageId = mission.StageId;
                    }

                    router.Navigate(AppRoute.World);
                    break;
                case DemoMissionObjective.WinBattles:
                    StageDefinition stage = database.GetCurrentStage(gameState.State) ?? database.FirstStage;
                    if (stage != null)
                    {
                        selectedStageId = stage.Id;
                    }

                    router.Navigate(AppRoute.World);
                    break;
            }
        }

        private void ResetDemoData()
        {
            battlePresenter?.StopBattle();
            gameState.Reset();
            activeStage = null;
            battleResultCommitted = false;
            selectedStageId = string.Empty;
            formationFeedback = "Demo data reset to its original state.";
            missionFeedback = string.Empty;
            ResetDraftFromSavedFormation();
            gachaScreen.ClearResult();
            ApplyRuntimeSettings();
            router.ResetTo(AppRoute.Home);
        }

        private void SetMusicVolume(float value)
        {
            gameState.SetMusicVolume(value);
            ApplyRuntimeSettings();
        }

        private void SetEffectsVolume(float value)
        {
            gameState.SetEffectsVolume(value);
        }

        private void SetFullscreen(bool value)
        {
            gameState.SetFullscreen(value);
            ApplyRuntimeSettings();
        }

        private void SetSixtyFps(bool value)
        {
            gameState.SetSixtyFps(value);
            ApplyRuntimeSettings();
        }

        private void OnStateChanged(PlayerState state)
        {
            appShell?.Refresh(state, database);
            if (router != null)
            {
                RefreshScreen(router.CurrentRoute, router.Context);
            }
        }

        private void OnBattleCompleted(BattleResult result)
        {
            if (battleResultCommitted)
            {
                return;
            }

            battleResultCommitted = true;
            StageRewardGrant grant = gameState.CommitBattleResult(activeStage, result);
            battleScreen.SetRewardSummary(grant);
            RefreshScreen(AppRoute.Battle, string.Empty);
        }

        private void RefreshScreen(AppRoute route, string context = "")
        {
            if (gameState == null)
            {
                return;
            }

            PlayerState state = gameState.State;
            appShell?.Refresh(state, database);
            switch (route)
            {
                case AppRoute.Home:
                    homeScreen?.Refresh(state, database);
                    break;
                case AppRoute.World:
                    worldScreen?.Refresh(state, selectedStageId);
                    break;
                case AppRoute.Gacha:
                    gachaScreen?.Refresh(state, database.DefaultBanner, database);
                    break;
                case AppRoute.Collection:
                    collectionScreen?.Refresh(database, state);
                    break;
                case AppRoute.Formation:
                    formationScreen?.Refresh(database, state, draftFormation, formationFeedback);
                    break;
                case AppRoute.Inventory:
                    inventoryScreen?.Refresh(state);
                    break;
                case AppRoute.Missions:
                    missionsScreen?.Refresh(state, missionFeedback);
                    break;
                case AppRoute.Settings:
                    settingsScreen?.Refresh(state.Settings);
                    break;
                case AppRoute.LockedFeature:
                    lockedFeatureScreen?.ShowFeature(context, state);
                    break;
            }
        }

        private void SelectFirstInteractableButton(AppRoute route)
        {
            DemoScreenView activeView = route switch
            {
                AppRoute.World => worldScreen,
                AppRoute.Gacha => gachaScreen,
                AppRoute.Collection => collectionScreen,
                AppRoute.Formation => formationScreen,
                AppRoute.Inventory => inventoryScreen,
                AppRoute.Missions => missionsScreen,
                AppRoute.Settings => settingsScreen,
                AppRoute.LockedFeature => lockedFeatureScreen,
                AppRoute.Battle => battleScreen,
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

        private void ApplyRuntimeSettings()
        {
            PlayerSettingsState settings = gameState?.State?.Settings;
            Application.targetFrameRate = settings != null && !settings.SixtyFps ? 30 : 60;
            AudioListener.volume = settings == null ? 1f : Mathf.Clamp01(settings.MusicVolume);
            if (!Application.isEditor &&
                settings != null &&
                !HasCommandLineOption("-screen-fullscreen") &&
                Screen.fullScreen != settings.Fullscreen)
            {
                Screen.fullScreen = settings.Fullscreen;
            }
        }

        private static bool HasCommandLineOption(string option)
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (int index = 0; index < arguments.Length; index++)
            {
                if (string.Equals(arguments[index], option, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyLaunchRouteOption()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            const string prefix = "-app-route=";
            for (int index = 0; index < arguments.Length; index++)
            {
                string argument = arguments[index];
                if (string.IsNullOrEmpty(argument) ||
                    !argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string routeName = argument.Substring(prefix.Length);
                if (!Enum.TryParse(routeName, true, out AppRoute route) ||
                    route == AppRoute.Battle)
                {
                    return;
                }

                if (route == AppRoute.LockedFeature)
                {
                    OpenLockedFeature("ARENA");
                }
                else
                {
                    NavigateFromShell(route);
                }

                return;
            }
        }

        private static bool UsesAppShell(AppRoute route)
        {
            return route == AppRoute.Home ||
                   route == AppRoute.World ||
                   route == AppRoute.Inventory ||
                   route == AppRoute.Missions;
        }

        private static string FormatStageId(string stageId)
        {
            return string.IsNullOrEmpty(stageId)
                ? "STAGE"
                : stageId.Replace("stage_", string.Empty).Replace('_', '-');
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
