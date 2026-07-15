using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GenericGachaRPG
{
    [DisallowMultipleComponent]
    public sealed class DemoPlayModeSmokeRunner : MonoBehaviour
    {
        public const string PassMarker = "[P0_PLAY_SMOKE_PASS_20260713]";
        public const string FailMarker = "[P0_PLAY_SMOKE_FAIL_20260713]";
        public const string RuntimeActivationKey = "GenericGachaRPG.PlaySmoke.RuntimeActive";

        private const float StepTimeoutSeconds = 8f;
        private const float BattleTimeoutSeconds = 30f;
        private const float SmokeTimeScale = 12f;

        private static readonly string[] ScreenNames =
        {
            "HomeScreen",
            "GachaScreen",
            "CollectionScreen",
            "FormationScreen",
            "BattleScreen"
        };

        private bool finished;
        private float originalTimeScale;
        private bool originalRunInBackground;

        public static event Action<bool, string> Completed;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStartWhenRequested()
        {
            if (PlayerPrefs.GetInt(RuntimeActivationKey, 0) != 1 ||
                UnityEngine.Object.FindAnyObjectByType<DemoPlayModeSmokeRunner>() != null)
            {
                return;
            }

            Debug.Log("[P0_PLAY_SMOKE] Runtime activation detected; creating runner.");
            var runnerObject = new GameObject("P0_PlayModeSmokeRunner");
            runnerObject.AddComponent<DemoPlayModeSmokeRunner>();
        }
#endif

        private void Start()
        {
            Debug.Log("[P0_PLAY_SMOKE] Runner started.", this);
            originalTimeScale = Time.timeScale;
            originalRunInBackground = Application.runInBackground;
            Application.runInBackground = true;
            Time.timeScale = Mathf.Max(SmokeTimeScale, originalTimeScale);
            StartCoroutine(ExecuteSafely(RunSmoke()));
        }

        private IEnumerator RunSmoke()
        {
            yield return WaitFor(
                () => FindSceneObject("HomeScreen") != null
                      && FindSceneObject("HomeScreen").activeInHierarchy
                      && FindSceneObjectOfType<DemoGameController>() != null,
                "Home screen and DemoGameController initialization",
                StepTimeoutSeconds);

            AssertOnlyScreenActive("HomeScreen");
            DemoGameController controller = FindSceneObjectOfType<DemoGameController>();
            Require(controller != null && controller.Database != null,
                "DemoGameController has no GameDatabase.");
            Require(controller.Database.DefaultBanner != null,
                "GameDatabase has no default gacha banner.");
            RawImage homeEnvironment = FindDescendantComponent<RawImage>("HomeScreen", "EnvironmentBackdrop");
            Require(homeEnvironment != null && homeEnvironment.texture != null,
                "Home screen has no bound Abyssal Observatory artwork texture.");

            int homeCurrency = ReadIntegerText("HomeScreen", "Currency");
            ClickActiveButton("GachaButton");
            yield return WaitForScreen("GachaScreen", StepTimeoutSeconds);
            AssertOnlyScreenActive("GachaScreen");

            int currencyBeforeDraw = ReadIntegerText("GachaScreen", "RightHeader");
            Require(currencyBeforeDraw == homeCurrency,
                $"Home/Gacha currency mismatch: Home={homeCurrency}, Gacha={currencyBeforeDraw}.");
            ClickActiveButton("PullButton");

            yield return WaitFor(
                () =>
                {
                    Text result = FindDescendantComponent<Text>("GachaScreen", "ResultText");
                    return result != null
                           && !string.IsNullOrWhiteSpace(result.text)
                           && result.text.IndexOf("Summon a new signal", StringComparison.OrdinalIgnoreCase) < 0;
                },
                "single-summon result text",
                StepTimeoutSeconds);

            Text gachaResult = FindDescendantComponent<Text>("GachaScreen", "ResultText");
            Require(gachaResult != null
                    && (gachaResult.text.IndexOf("NEW CHARACTER UNLOCKED", StringComparison.Ordinal) >= 0
                        || gachaResult.text.IndexOf("DUPLICATE SIGNAL REGISTERED", StringComparison.Ordinal) >= 0),
                $"Single summon did not show a success result. Text='{gachaResult?.text ?? "<missing>"}'.");

            int currencyAfterDraw = ReadIntegerText("GachaScreen", "RightHeader");
            int expectedCost = controller.Database.DefaultBanner.SingleDrawCost;
            Require(currencyBeforeDraw - currencyAfterDraw == expectedCost,
                $"Single summon currency delta was {currencyBeforeDraw - currencyAfterDraw}; expected {expectedCost}.");
            Require(currencyAfterDraw >= 0, "Single summon produced a negative visible balance.");

            ClickActiveButton("BackButton");
            yield return WaitForScreen("HomeScreen", StepTimeoutSeconds);
            AssertOnlyScreenActive("HomeScreen");

            ClickActiveButton("CollectionButton");
            yield return WaitForScreen("CollectionScreen", StepTimeoutSeconds);
            AssertOnlyScreenActive("CollectionScreen");
            yield return WaitFor(
                () => CountDescendantsWithPrefix("CollectionScreen", "Card_") == 7,
                "seven collection cards",
                StepTimeoutSeconds);
            Require(CountDescendantsWithPrefix("CollectionScreen", "Card_") == 7,
                "Collection screen does not contain exactly seven character cards.");
            GameObject cosmicCard = FindSceneObject("Card_ur_cosmic_slime");
            Text cosmicCardBody = cosmicCard == null
                ? null
                : FindComponentInChildrenByName<Text>(cosmicCard, "Body");
            Require(cosmicCardBody != null &&
                    cosmicCardBody.text.IndexOf("UR", StringComparison.Ordinal) >= 0 &&
                    cosmicCardBody.text.IndexOf("TANK", StringComparison.Ordinal) >= 0 &&
                    cosmicCardBody.text.IndexOf("LIMITED", StringComparison.Ordinal) >= 0 &&
                    cosmicCardBody.text.IndexOf("RNG 2.0", StringComparison.Ordinal) >= 0 &&
                    cosmicCardBody.text.IndexOf("MOVE 3.3", StringComparison.Ordinal) >= 0,
                $"Cosmic Slime collection tags are incomplete. Text='{cosmicCardBody?.text ?? "<missing>"}'.");

            ClickActiveButton("BackButton");
            yield return WaitForScreen("HomeScreen", StepTimeoutSeconds);
            AssertOnlyScreenActive("HomeScreen");

            ClickActiveButton("FormationButton");
            yield return WaitForScreen("FormationScreen", StepTimeoutSeconds);
            AssertOnlyScreenActive("FormationScreen");
            yield return WaitFor(
                () => CountDescendantsWithPrefix("FormationScreen", "Slot_") == BattleRules.TeamSize,
                "five stable formation slots",
                StepTimeoutSeconds);
            Require(CountDescendantsWithPrefix("FormationScreen", "Slot_") == BattleRules.TeamSize,
                $"Formation screen does not contain exactly {BattleRules.TeamSize} slot units.");
            yield return WaitFor(
                () =>
                {
                    Button button = FindActiveButton("BattleButton", false);
                    return button != null && button.interactable;
                },
                "valid five-character formation and battle button",
                StepTimeoutSeconds);

            ClickActiveButton("BattleButton");
            yield return WaitForScreen("BattleScreen", StepTimeoutSeconds);
            AssertOnlyScreenActive("BattleScreen");
            yield return WaitFor(
                () => FindSceneObject("AbyssalObservatory_Backdrop") != null &&
                      FindSceneObjectOfType<CosmicSlimeVisualController>() != null &&
                      CountBattleComponents<CosmicSlimeVisualController>() == 1 &&
                      CountBattleComponents<BasicSlimeVisualController>() == 7 &&
                      CountBattleUnits("P") == BattleRules.DemoPlayerTeamSize &&
                      CountBattleUnits("E") == BattleRules.DemoEnemyTeamSize,
                "Abyssal Observatory, one authored UR, and seven Basic Slimes in 3v5 combat",
                StepTimeoutSeconds);
            GameObject backdrop = FindSceneObject("AbyssalObservatory_Backdrop");
            Renderer backdropRenderer = backdrop == null ? null : backdrop.GetComponent<Renderer>();
            Require(backdropRenderer != null, "Abyssal Observatory backdrop has no Renderer.");
            Require(backdropRenderer.sharedMaterial != null,
                "Abyssal Observatory backdrop has no runtime material.");
            Require(backdropRenderer.sharedMaterial.shader != null,
                "Abyssal Observatory backdrop material has no runtime shader.");
            Texture runtimeBackdropTexture = backdropRenderer.sharedMaterial.HasProperty("_BaseMap")
                ? backdropRenderer.sharedMaterial.GetTexture("_BaseMap")
                : backdropRenderer.sharedMaterial.mainTexture;
            Require(runtimeBackdropTexture != null,
                "Abyssal Observatory runtime material has no bound artwork texture.");
            CosmicSlimeVisualController cosmicSlimeController = FindSceneObjectOfType<CosmicSlimeVisualController>();
            GameObject cosmicSlime = cosmicSlimeController == null ? null : cosmicSlimeController.gameObject;
            Require(cosmicSlime != null,
                "Player Cosmic Slime did not instantiate from the authored prefab.");
            Require(cosmicSlime.name.StartsWith("P0_", StringComparison.Ordinal),
                $"Cosmic Slime must occupy player slot 0; found '{cosmicSlime.name}'.");
            Require(cosmicSlime.GetComponentsInChildren<Renderer>(true).Length > 0,
                "Player Cosmic Slime prefab has no visible renderer.");
            Require(cosmicSlimeController.HasRequiredBlendShapes,
                "Catherine Yuki runtime model is missing required Idle/Squash/Stretch/Ultimate blend shapes.");
            CatherineSkillVfxController catherineVfx =
                cosmicSlime.GetComponent<CatherineSkillVfxController>();
            Require(catherineVfx != null,
                "Catherine Yuki runtime prefab has no CatherineSkillVfxController.");
            Require(Shader.Find("BubbleMind/Slime Toon") != null &&
                    Shader.Find("BubbleMind/Black Hole VFX") != null,
                "Catherine Yuki runtime shaders are unavailable.");
            Require(GameObjectUsesShader(cosmicSlime, "BubbleMind/Slime Toon"),
                "Catherine Yuki runtime shell does not use BubbleMind/Slime Toon.");
            Require(CountBattleUnits("P") == BattleRules.DemoPlayerTeamSize &&
                    CountBattleUnits("E") == BattleRules.DemoEnemyTeamSize,
                "Battle did not instantiate the complete 3-versus-5 teams.");
            Require(CountBattleComponents<CosmicSlimeVisualController>() == 1,
                "Battle must contain exactly one authored UR Cosmic Slime.");
            Require(CountBattleComponents<BasicSlimeVisualController>() == 7,
                "Battle must contain exactly seven authored Basic Slimes.");
            Require(CountBattleComponents<ProceduralCharacterBuilder>() == 0,
                "Battle unexpectedly used procedural humanoid fallback units.");
            RequireVisibleBasicSlimes();
            VerifyRageWorldBars();

            CharacterView cosmicSlimeView = cosmicSlime.GetComponent<CharacterView>();
            Require(cosmicSlimeView != null, "Player Cosmic Slime has no CharacterView.");
            DemoBattlePresenter presenter = FindSceneObjectOfType<DemoBattlePresenter>();
            Require(presenter != null && presenter.LastResult != null,
                "Battle presenter has no deterministic result to replay.");
            Require(presenter.LastResult.PlayerUnits.Count == BattleRules.DemoPlayerTeamSize &&
                    presenter.LastResult.EnemyUnits.Count == BattleRules.DemoEnemyTeamSize &&
                    string.Equals(
                        presenter.LastResult.PlayerUnits[0].CharacterId,
                        CatherineYukiBattleKit.CharacterId,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        presenter.LastResult.PlayerUnits[1].CharacterId,
                        "gold_ranger",
                        StringComparison.Ordinal) &&
                    string.Equals(
                        presenter.LastResult.PlayerUnits[2].CharacterId,
                        AssassinBattleKit.CharacterId,
                        StringComparison.Ordinal),
                "Playable battle roster must be P0 Catherine, P1 Gold Ranger, P2 Ember Striker versus five enemies.");
            string[] expectedEnemyIds =
            {
                "cyan_warden",
                "azure_vanguard",
                "violet_arcanist",
                "gold_ranger",
                "verdant_medic"
            };
            CharacterRole[] expectedEnemyRoles =
            {
                CharacterRole.Tank,
                CharacterRole.Tank,
                CharacterRole.Mage,
                CharacterRole.Ranged,
                CharacterRole.Support
            };
            for (int index = 0; index < expectedEnemyIds.Length; index++)
            {
                Require(string.Equals(
                            presenter.LastResult.EnemyUnits[index].CharacterId,
                            expectedEnemyIds[index],
                            StringComparison.Ordinal) &&
                        presenter.LastResult.EnemyUnits[index].Role == expectedEnemyRoles[index],
                    $"Runtime enemy slot E{index} has the wrong character or role.");
            }
            Require(presenter.TryGetPresentedRage("P0", out int presentedRage, out int presentedMaxRage) &&
                    presentedRage >= 0 && presentedRage <= BattleRules.MaxRage &&
                    presentedMaxRage == BattleRules.MaxRage,
                "Presenter has no valid 0..1000 Rage model for Catherine.");
            VerifyDemoEnemyScalingAndCatherineKit(presenter);
            VerifyAssassinBacklineShift(presenter.LastResult);
            BattleEvent firstTankAction = VerifyTankTargetLockAndRetarget(presenter.LastResult);
            Vector3 tankSpawnPosition = BattleRules.GetSlotPosition(BattleTeamSide.Player, 0);
            yield return WaitFor(
                () => cosmicSlimeView.HasPerformedApproach &&
                      cosmicSlimeView.MaximumRootTravelDistance > 0.5f &&
                      Vector3.Distance(cosmicSlimeView.transform.position, firstTankAction.ActorPositionAfter) < 0.12f,
                "player slot 0 tank reaches its first locked target",
                BattleTimeoutSeconds);
            Require(cosmicSlimeView.HasPerformedApproach &&
                    cosmicSlimeView.MaximumRootTravelDistance > 0.5f,
                "Player slot 0 tank never advanced toward its target.");

            Vector3 heldFrontlinePosition = cosmicSlimeView.transform.position;
            yield return new WaitForSecondsRealtime(0.015f);
            Require(Vector3.Distance(cosmicSlimeView.transform.position, tankSpawnPosition) > 0.5f,
                "Player slot 0 tank returned to its formation spawn after attacking.");
            Require(Vector3.Distance(cosmicSlimeView.transform.position, heldFrontlinePosition) < 0.12f,
                "Player slot 0 tank did not hold position while its locked target remained alive.");

            yield return WaitFor(
                () =>
                {
                    GameObject panel = FindSceneObject("ResultPanel");
                    return panel != null && panel.activeInHierarchy;
                },
                "battle result panel",
                BattleTimeoutSeconds);

            GameObject resultPanel = FindSceneObject("ResultPanel");
            Require(resultPanel != null && resultPanel.activeInHierarchy,
                "Battle result panel is not active.");
            Text resultTitle = FindComponentInChildrenByName<Text>(resultPanel, "ResultTitle");
            Require(resultTitle != null
                    && (string.Equals(resultTitle.text, "VICTORY", StringComparison.Ordinal)
                        || string.Equals(resultTitle.text, "DEFEAT", StringComparison.Ordinal)
                        || string.Equals(resultTitle.text, "TIME LIMIT", StringComparison.Ordinal)),
                $"Battle result title is invalid: '{resultTitle?.text ?? "<missing>"}'.");

            ClickActiveButton("HomeButton");
            yield return WaitForScreen("HomeScreen", StepTimeoutSeconds);
            AssertOnlyScreenActive("HomeScreen");
        }

        private IEnumerator ExecuteSafely(IEnumerator rootRoutine)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(rootRoutine);

            while (stack.Count > 0)
            {
                IEnumerator currentRoutine = stack.Peek();
                bool movedNext = false;
                object yielded = null;
                Exception error = null;

                try
                {
                    movedNext = currentRoutine.MoveNext();
                    if (movedNext)
                    {
                        yielded = currentRoutine.Current;
                    }
                }
                catch (Exception exception)
                {
                    error = exception;
                }

                if (error != null)
                {
                    Finish(false, error.Message, error);
                    yield break;
                }

                if (!movedNext)
                {
                    stack.Pop();
                    continue;
                }

                if (yielded is IEnumerator nestedRoutine)
                {
                    stack.Push(nestedRoutine);
                    continue;
                }

                yield return yielded;
            }

            Finish(true, "Automated UI flow completed.", null);
        }

        private static IEnumerator WaitForScreen(string screenName, float timeoutSeconds)
        {
            return WaitFor(
                () =>
                {
                    GameObject screen = FindSceneObject(screenName);
                    if (screen == null || !screen.activeInHierarchy)
                    {
                        return false;
                    }

                    for (int i = 0; i < ScreenNames.Length; i++)
                    {
                        if (string.Equals(ScreenNames[i], screenName, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        GameObject other = FindSceneObject(ScreenNames[i]);
                        if (other != null && other.activeInHierarchy)
                        {
                            return false;
                        }
                    }

                    return true;
                },
                $"active {screenName}",
                timeoutSeconds);
        }

        private static IEnumerator WaitFor(Func<bool> condition, string description, float timeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, timeoutSeconds);
            while (!condition())
            {
                if (Time.realtimeSinceStartup >= deadline)
                {
                    throw new TimeoutException(
                        $"Timed out after {timeoutSeconds:0.#}s waiting for {description}.");
                }

                yield return null;
            }
        }

        private static void AssertOnlyScreenActive(string expectedScreenName)
        {
            for (int i = 0; i < ScreenNames.Length; i++)
            {
                GameObject screen = FindSceneObject(ScreenNames[i]);
                Require(screen != null, $"Screen object '{ScreenNames[i]}' is missing.");
                bool shouldBeActive = string.Equals(
                    ScreenNames[i],
                    expectedScreenName,
                    StringComparison.Ordinal);
                Require(screen.activeInHierarchy == shouldBeActive,
                    $"Screen '{ScreenNames[i]}' active={screen.activeInHierarchy}; expected {shouldBeActive}.");
            }
        }

        private static void ClickActiveButton(string buttonName)
        {
            Button button = FindActiveButton(buttonName, true);
            Require(button != null, $"Active button '{buttonName}' was not found.");
            Require(button.interactable, $"Active button '{buttonName}' is not interactable.");
            button.onClick.Invoke();
            Canvas.ForceUpdateCanvases();
        }

        private static Button FindActiveButton(string buttonName, bool requireInteractable)
        {
            Button[] buttons = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];
                if (button != null
                    && button.gameObject.scene.IsValid()
                    && button.gameObject.activeInHierarchy
                    && string.Equals(button.name, buttonName, StringComparison.Ordinal)
                    && (!requireInteractable || button.interactable))
                {
                    return button;
                }
            }

            return null;
        }

        private static int ReadIntegerText(string screenName, string textObjectName)
        {
            Text text = FindDescendantComponent<Text>(screenName, textObjectName);
            Require(text != null, $"Text '{textObjectName}' is missing from {screenName}.");

            long value = 0;
            bool foundDigit = false;
            for (int i = 0; i < text.text.Length; i++)
            {
                char character = text.text[i];
                if (character < '0' || character > '9')
                {
                    continue;
                }

                foundDigit = true;
                value = checked(value * 10L + (character - '0'));
                Require(value <= int.MaxValue,
                    $"Text '{textObjectName}' contains a number larger than Int32.");
            }

            Require(foundDigit,
                $"Text '{textObjectName}' contains no integer. Text='{text.text}'.");
            return (int)value;
        }

        private static int CountDescendantsWithPrefix(string rootName, string prefix)
        {
            GameObject root = FindSceneObject(rootName);
            if (root == null)
            {
                return 0;
            }

            int count = 0;
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform descendant = descendants[i];
                if (descendant != null
                    && descendant.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountBattleUnits(string runtimeIdPrefix)
        {
            CharacterView[] views = UnityEngine.Object.FindObjectsByType<CharacterView>(FindObjectsInactive.Include);
            int count = 0;
            for (int i = 0; i < views.Length; i++)
            {
                CharacterView view = views[i];
                string objectName = view == null ? string.Empty : view.gameObject.name;
                if (view != null &&
                    view.gameObject.scene.IsValid() &&
                    objectName.StartsWith(runtimeIdPrefix, StringComparison.Ordinal) &&
                    objectName.Length > runtimeIdPrefix.Length &&
                    char.IsDigit(objectName[runtimeIdPrefix.Length]))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountBattleComponents<T>()
            where T : Component
        {
            T[] components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            int count = 0;
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component == null || !component.gameObject.scene.IsValid())
                {
                    continue;
                }

                string objectName = component.gameObject.name;
                if (objectName.Length >= 2 &&
                    (objectName[0] == 'P' || objectName[0] == 'E') &&
                    char.IsDigit(objectName[1]))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool GameObjectUsesShader(GameObject root, string shaderName)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Material[] materials = renderers[rendererIndex].sharedMaterials;
                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];
                    if (material != null &&
                        material.shader != null &&
                        string.Equals(material.shader.name, shaderName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void RequireVisibleBasicSlimes()
        {
            BasicSlimeVisualController[] controllers =
                UnityEngine.Object.FindObjectsByType<BasicSlimeVisualController>(FindObjectsInactive.Include);
            int verified = 0;
            for (int i = 0; i < controllers.Length; i++)
            {
                BasicSlimeVisualController controller = controllers[i];
                if (controller == null || !controller.gameObject.scene.IsValid())
                {
                    continue;
                }

                string objectName = controller.gameObject.name;
                if (objectName.Length < 2 ||
                    (objectName[0] != 'P' && objectName[0] != 'E') ||
                    !char.IsDigit(objectName[1]))
                {
                    continue;
                }

                Require(controller.AnimatedDecorationCount > 0,
                    $"Basic Slime '{objectName}' has no animated element decorations.");
                Renderer[] renderers = controller.GetComponentsInChildren<Renderer>(false);
                bool hasVisibleBounds = false;
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                {
                    Renderer renderer = renderers[rendererIndex];
                    if (renderer != null && renderer.enabled && renderer.bounds.size.sqrMagnitude > 0.01f)
                    {
                        hasVisibleBounds = true;
                        break;
                    }
                }

                Require(hasVisibleBounds,
                    $"Basic Slime '{objectName}' has no enabled renderer with visible bounds.");
                verified++;
            }

            Require(verified == 7, $"Expected to visually verify 7 Basic Slimes; found {verified}.");
        }

        private static void VerifyRageWorldBars()
        {
            WorldBarView[] bars = UnityEngine.Object.FindObjectsByType<WorldBarView>(FindObjectsInactive.Include);
            int verifiedLabels = 0;
            for (int barIndex = 0; barIndex < bars.Length; barIndex++)
            {
                WorldBarView bar = bars[barIndex];
                if (bar == null)
                {
                    continue;
                }

                Text[] labels = bar.GetComponentsInChildren<Text>(true);
                Text rageLabel = null;
                for (int labelIndex = 0; labelIndex < labels.Length; labelIndex++)
                {
                    if (string.Equals(labels[labelIndex].name, "RageLabel", StringComparison.Ordinal))
                    {
                        rageLabel = labels[labelIndex];
                        break;
                    }
                }

                Require(rageLabel != null,
                    $"World Rage bar '{bar.name}' has no numeric RageLabel.");
                Require(rageLabel.text.StartsWith("RAGE ", StringComparison.Ordinal) &&
                        rageLabel.text.EndsWith($"/{BattleRules.MaxRage}", StringComparison.Ordinal),
                    $"World Rage label has unexpected text '{rageLabel.text}'.");
                verifiedLabels += 1;
            }

            Require(verifiedLabels == BattleRules.DemoPlayerTeamSize + BattleRules.DemoEnemyTeamSize,
                $"Expected eight numeric Rage labels; found {verifiedLabels}.");
        }

        private static void VerifyDemoEnemyScalingAndCatherineKit(DemoBattlePresenter presenter)
        {
            BattleResult result = presenter.LastResult;
            Require(result != null, "Cannot verify Catherine kit without a battle result.");
            VerifyWindWheelBreakDisplacement(result);
            Require(result.PlayerUnits.Count == BattleRules.DemoPlayerTeamSize,
                "Demo player runtime snapshot does not contain three units.");
            Require(result.EnemyUnits.Count == BattleRules.DemoEnemyTeamSize,
                "Demo enemy runtime snapshot does not contain five units.");
            for (int index = 0; index < result.EnemyUnits.Count; index++)
            {
                BattleUnitState enemy = result.EnemyUnits[index];
                Require(Mathf.Approximately(
                            enemy.MaxHealth,
                            enemy.Definition.MaxHealth * CatherineYukiBattleKit.DemoEnemyHealthMultiplier),
                    $"Enemy slot {index} is missing the demo 10x HP multiplier.");
                Require(Mathf.Approximately(
                            enemy.Attack,
                            enemy.Definition.Attack * CatherineYukiBattleKit.DemoEnemyAttackMultiplier),
                    $"Enemy slot {index} is missing the demo 0.1x ATK multiplier.");
            }

            Require(presenter.TryGetPresentedHealth("E0", out _, out float presentedEnemyMaxHealth),
                "Presenter has no runtime health model for enemy slot 0.");
            Require(Mathf.Approximately(presentedEnemyMaxHealth, result.EnemyUnits[0].MaxHealth),
                "Enemy world-bar health model does not use the simulated 10x maximum HP.");

            var timedSkill2Casts = new List<BattleEvent>();
            var timedSkill3Casts = new List<BattleEvent>();
            var skill2Ticks = new HashSet<int>();
            int pullCount = 0;
            int ultimateDamageCount = 0;
            bool skill2KnockUp = false;
            bool skill3Healing = false;
            bool skill3Taunt = false;
            bool activeStarRage = false;
            bool passiveMassGain = false;
            bool rageFromAttack = false;
            bool rageFromDamage = false;
            bool fullRage = false;
            bool rageSpent = false;
            bool ultimateCast = false;
            bool transformMatchesMass = false;
            bool collapsed = false;
            bool ultimateKnockUp = false;
            int ultimateChargeStacks = CatherineYukiBattleKit.InitialImaginaryMassStacks;
            for (int index = 0; index < result.Events.Count; index++)
            {
                BattleEvent battleEvent = result.Events[index];
                bool catherineActor = string.Equals(
                    battleEvent.ActorRuntimeId,
                    "P0",
                    StringComparison.Ordinal);

                if (battleEvent.Type == BattleEventType.SkillCastStarted && catherineActor)
                {
                    if (string.Equals(
                            battleEvent.SkillId,
                            CatherineYukiBattleKit.TimedSkill2Id,
                            StringComparison.Ordinal))
                    {
                        timedSkill2Casts.Add(battleEvent);
                        skill2Ticks.Add(battleEvent.Tick);
                    }
                    else if (string.Equals(
                                 battleEvent.SkillId,
                                 CatherineYukiBattleKit.TimedSkill3Id,
                                 StringComparison.Ordinal))
                    {
                        timedSkill3Casts.Add(battleEvent);
                    }
                    else if (string.Equals(
                                 battleEvent.SkillId,
                                 CatherineYukiBattleKit.StarRagePassiveId,
                                 StringComparison.Ordinal))
                    {
                        activeStarRage = true;
                    }
                    else if (string.Equals(
                                 battleEvent.SkillId,
                                 CatherineYukiBattleKit.RageUltimateSkillId,
                                 StringComparison.Ordinal))
                    {
                        ultimateCast = true;
                    }
                }

                if (battleEvent.Type == BattleEventType.StatusApplied &&
                    catherineActor &&
                    battleEvent.Amount > CatherineYukiBattleKit.InitialImaginaryMassStacks &&
                    battleEvent.Amount <= CatherineYukiBattleKit.AwakenedImaginaryMassStackCap)
                {
                    passiveMassGain = true;
                }

                if (battleEvent.Type == BattleEventType.RageChanged &&
                    string.Equals(battleEvent.TargetRuntimeId, "P0", StringComparison.Ordinal))
                {
                    rageFromAttack |= catherineActor &&
                                      Mathf.Approximately(
                                          battleEvent.Amount,
                                          BattleRules.RagePerBasicAttackHit);
                    rageFromDamage |= !catherineActor &&
                                      Mathf.Approximately(
                                          battleEvent.Amount,
                                          BattleRules.RagePerDamageReceived);
                    fullRage |= battleEvent.RageAfter == BattleRules.MaxRage;
                    rageSpent |= catherineActor && battleEvent.RageAfter == 0 &&
                                 Mathf.Approximately(battleEvent.Amount, -BattleRules.MaxRage) &&
                                 string.Equals(
                                     battleEvent.SkillId,
                                     CatherineYukiBattleKit.RageUltimateSkillId,
                                     StringComparison.Ordinal);
                }

                skill2KnockUp |= catherineActor && battleEvent.Type == BattleEventType.UnitKnockedUp &&
                                  string.Equals(
                                      battleEvent.SkillId,
                                      CatherineYukiBattleKit.TimedSkill2Id,
                                      StringComparison.Ordinal) &&
                                  Mathf.Approximately(
                                      battleEvent.Amount,
                                      CatherineYukiBattleKit.WindWheelBreakKnockbackDistance);
                skill3Healing |= catherineActor && battleEvent.Type == BattleEventType.HealingApplied &&
                                 string.Equals(
                                     battleEvent.SkillId,
                                     CatherineYukiBattleKit.TimedSkill3Id,
                                     StringComparison.Ordinal);
                skill3Taunt |= catherineActor && battleEvent.Type == BattleEventType.DebuffApplied &&
                               string.Equals(
                                   battleEvent.SkillId,
                                   CatherineYukiBattleKit.TauntDebuffId,
                                   StringComparison.Ordinal);

                if (catherineActor && battleEvent.Type == BattleEventType.UltimatePhase)
                {
                    if (string.Equals(
                            battleEvent.SkillId,
                            CatherineYukiBattleKit.UltimateChargePhaseId,
                            StringComparison.Ordinal))
                    {
                        ultimateChargeStacks = Mathf.RoundToInt(battleEvent.Amount);
                    }

                    if (string.Equals(
                            battleEvent.SkillId,
                            CatherineYukiBattleKit.UltimateTransformPhaseId,
                            StringComparison.Ordinal))
                    {
                        transformMatchesMass |= Mathf.Approximately(
                            battleEvent.Amount,
                            CatherineYukiBattleKit.GetUltimateScaling(ultimateChargeStacks, false));
                    }
                }

                collapsed |= catherineActor && battleEvent.Type == BattleEventType.UltimatePhase &&
                             string.Equals(
                                 battleEvent.SkillId,
                                 CatherineYukiBattleKit.UltimateCollapsePhaseId,
                                 StringComparison.Ordinal);
                pullCount += catherineActor && battleEvent.Type == BattleEventType.UnitPulled &&
                             string.Equals(
                                 battleEvent.SkillId,
                                 CatherineYukiBattleKit.RageUltimateSkillId,
                                 StringComparison.Ordinal)
                    ? 1
                    : 0;
                ultimateDamageCount += catherineActor && battleEvent.Type == BattleEventType.DamageApplied &&
                                       string.Equals(
                                           battleEvent.SkillId,
                                           CatherineYukiBattleKit.RageUltimateSkillId,
                                           StringComparison.Ordinal)
                    ? 1
                    : 0;
                ultimateKnockUp |= catherineActor && battleEvent.Type == BattleEventType.UnitKnockedUp &&
                                   string.Equals(
                                       battleEvent.SkillId,
                                       CatherineYukiBattleKit.RageUltimateSkillId,
                                       StringComparison.Ordinal);
            }

            int expectedSkill2Casts = ExpectedRecurringCastCount(
                result.ElapsedTime,
                BattleRules.Skill2InitialCastTime);
            int expectedSkill3Casts = ExpectedRecurringCastCount(
                result.ElapsedTime,
                BattleRules.Skill3InitialCastTime);
            Require(timedSkill2Casts.Count == expectedSkill2Casts &&
                    timedSkill3Casts.Count == expectedSkill3Casts,
                $"Catherine timed skill counts do not match the completed battle window " +
                $"(actual={timedSkill2Casts.Count}/{timedSkill3Casts.Count}, " +
                $"expected={expectedSkill2Casts}/{expectedSkill3Casts}).");
            Require(timedSkill2Casts.Count > 0 && timedSkill3Casts.Count > 0,
                "Catherine battle ended before both timed active skills could be observed.");
            float tickTolerance = BattleContext.DefaultTickDuration + 0.001f;
            Require(Mathf.Abs(timedSkill2Casts[0].Time - BattleRules.Skill2InitialCastTime) <= tickTolerance &&
                    Mathf.Abs(timedSkill3Casts[0].Time - BattleRules.Skill3InitialCastTime) <= tickTolerance,
                "Catherine timed skills did not first cast at 5 and 10 seconds.");
            for (int index = 1; index < timedSkill2Casts.Count; index++)
            {
                Require(Mathf.Abs(
                            timedSkill2Casts[index].Time - timedSkill2Casts[index - 1].Time -
                            BattleRules.ActiveSkillCooldown) <= tickTolerance,
                    "Catherine Skill 2 did not repeat after its 10-second cooldown.");
            }

            for (int index = 0; index < timedSkill3Casts.Count; index++)
            {
                Require(!skill2Ticks.Contains(timedSkill3Casts[index].Tick),
                    "Catherine timed Skill 2 and Skill 3 overlapped on one tick.");
                if (index > 0)
                {
                    Require(Mathf.Abs(
                                timedSkill3Casts[index].Time - timedSkill3Casts[index - 1].Time -
                                BattleRules.ActiveSkillCooldown) <= tickTolerance,
                        "Catherine Skill 3 did not repeat after its 10-second cooldown.");
                }
            }

            Require(!activeStarRage && passiveMassGain,
                "Star Rage must remain passive and gain mass from enemy active skills.");
            Require(skill2KnockUp && skill3Healing && skill3Taunt,
                "Catherine timed skills are missing five-grid knockback, healing, or Taunt events.");
            Require(rageFromAttack && rageFromDamage && fullRage && rageSpent && ultimateCast,
                "Catherine Rage did not build from attacks/damage, reach 1000, clear, and cast the ultimate.");
            Require(transformMatchesMass && collapsed && pullCount >= BattleRules.DemoEnemyTeamSize &&
                    ultimateDamageCount >= CatherineYukiBattleKit.UltimateHitCount && ultimateKnockUp,
                "Catherine ultimate is missing dynamic mass scaling, five pulls, continuous hits, collapse, or knock-up.");
        }

        private static void VerifyWindWheelBreakDisplacement(BattleResult result)
        {
            var positions = new Dictionary<string, Vector3>(StringComparer.Ordinal);
            for (int slot = 0; slot < result.PlayerUnits.Count; slot++)
            {
                positions[$"P{slot}"] = BattleRules.GetSlotPosition(BattleTeamSide.Player, slot);
            }

            for (int slot = 0; slot < result.EnemyUnits.Count; slot++)
            {
                positions[$"E{slot}"] = BattleRules.GetSlotPosition(BattleTeamSide.Enemy, slot);
            }

            bool verified = false;
            for (int index = 0; index < result.Events.Count; index++)
            {
                BattleEvent battleEvent = result.Events[index];
                if ((battleEvent.Type == BattleEventType.UnitMoved ||
                     battleEvent.Type == BattleEventType.UnitTeleported) &&
                    !string.IsNullOrEmpty(battleEvent.ActorRuntimeId))
                {
                    positions[battleEvent.ActorRuntimeId] = battleEvent.ActorPositionAfter;
                    continue;
                }

                if (battleEvent.Type != BattleEventType.UnitPulled &&
                    battleEvent.Type != BattleEventType.UnitKnockedUp)
                {
                    continue;
                }

                if (battleEvent.Type == BattleEventType.UnitKnockedUp &&
                    string.Equals(
                        battleEvent.SkillId,
                        CatherineYukiBattleKit.TimedSkill2Id,
                        StringComparison.Ordinal))
                {
                    Vector3 actorPosition = default;
                    Vector3 targetPosition = default;
                    bool hasActorPosition = positions.TryGetValue(
                        battleEvent.ActorRuntimeId,
                        out actorPosition);
                    bool hasTargetPosition = positions.TryGetValue(
                        battleEvent.TargetRuntimeId,
                        out targetPosition);
                    Require(hasActorPosition &&
                            hasTargetPosition &&
                            battleEvent.TargetSide.HasValue,
                        "Wind Wheel: Break has incomplete pre-event position data.");
                    Vector3 expectedDestination = BattleRules.CalculateKnockbackDestination(
                        actorPosition,
                        targetPosition,
                        battleEvent.TargetSide.Value,
                        battleEvent.TargetSlot,
                        CatherineYukiBattleKit.WindWheelBreakKnockbackDistance);
                    float actualDistance = Vector3.Distance(
                        targetPosition,
                        battleEvent.TargetPositionAfter);
                    Require(Vector3.Distance(
                                expectedDestination,
                                battleEvent.TargetPositionAfter) <= 0.001f &&
                            actualDistance > BattleRules.RangeEpsilon &&
                            actualDistance <=
                            CatherineYukiBattleKit.WindWheelBreakKnockbackDistance +
                            BattleRules.RangeEpsilon,
                        "Wind Wheel: Break did not apply its five-grid boundary-clamped displacement.");
                    verified = true;
                }

                if (!string.IsNullOrEmpty(battleEvent.TargetRuntimeId))
                {
                    positions[battleEvent.TargetRuntimeId] = battleEvent.TargetPositionAfter;
                }
            }

            Require(verified,
                "PlayMode observed no Wind Wheel: Break displacement to verify.");
        }

        private static void VerifyAssassinBacklineShift(BattleResult result)
        {
            Vector3 boundaryTarget = new Vector3(
                BattleRules.BattlefieldHalfLength,
                0f,
                BattleRules.BattlefieldHalfDepth);
            Vector3 boundaryLanding = AssassinBattleKit.CalculateBacklineDestination(
                Vector3.zero,
                boundaryTarget,
                BattleTeamSide.Enemy);
            Require(Mathf.Abs(
                        Vector3.Distance(boundaryLanding, boundaryTarget) -
                        AssassinBattleKit.TeleportDistance) <= 0.001f &&
                    boundaryLanding.x <= BattleRules.BattlefieldHalfLength &&
                    boundaryLanding.z <= BattleRules.BattlefieldHalfDepth,
                "Backline Shift boundary fallback must keep two-grid separation inside the battlefield.");

            var defeatedUnits = new HashSet<string>(StringComparer.Ordinal);
            BattleEvent firstTeleport = null;
            BattleUnitState teleportTarget = null;
            bool continuedBasicAttacksOnLockedTarget = false;
            bool firstTeleportTargetAlive = true;
            bool secondShiftRetainedLivingTarget = false;
            int teleportCount = 0;

            for (int index = 0; index < result.Events.Count; index++)
            {
                BattleEvent battleEvent = result.Events[index];
                if (battleEvent.Type == BattleEventType.UnitDefeated)
                {
                    defeatedUnits.Add(battleEvent.TargetRuntimeId);
                    if (firstTeleport != null &&
                        string.Equals(
                            battleEvent.TargetRuntimeId,
                            firstTeleport.TargetRuntimeId,
                            StringComparison.Ordinal))
                    {
                        firstTeleportTargetAlive = false;
                    }

                    continue;
                }

                if (battleEvent.Type == BattleEventType.UnitRevived)
                {
                    defeatedUnits.Remove(battleEvent.TargetRuntimeId);
                    if (firstTeleport != null &&
                        string.Equals(
                            battleEvent.TargetRuntimeId,
                            firstTeleport.TargetRuntimeId,
                            StringComparison.Ordinal))
                    {
                        firstTeleportTargetAlive = true;
                    }

                    continue;
                }

                bool isBacklineTeleport = battleEvent.Type == BattleEventType.UnitTeleported &&
                                          string.Equals(
                                              battleEvent.ActorRuntimeId,
                                              "P2",
                                              StringComparison.Ordinal) &&
                                          string.Equals(
                                              battleEvent.SkillId,
                                              AssassinBattleKit.BacklineShiftSkillId,
                                              StringComparison.Ordinal);
                if (isBacklineTeleport)
                {
                    teleportCount++;
                    Require(!defeatedUnits.Contains(battleEvent.TargetRuntimeId),
                        "P2 Backline Shift targeted a defeated enemy.");

                    if (firstTeleport == null)
                    {
                        firstTeleport = battleEvent;
                        for (int targetIndex = 0; targetIndex < result.EnemyUnits.Count; targetIndex++)
                        {
                            BattleUnitState candidate = result.EnemyUnits[targetIndex];
                            if (string.Equals(
                                    candidate.RuntimeId,
                                    battleEvent.TargetRuntimeId,
                                    StringComparison.Ordinal))
                            {
                                teleportTarget = candidate;
                                break;
                            }
                        }

                        Require(teleportTarget != null &&
                                (teleportTarget.Role == CharacterRole.Ranged ||
                                 teleportTarget.Role == CharacterRole.Mage ||
                                 teleportTarget.Role == CharacterRole.Support),
                            "P2 Backline Shift did not target a living enemy backline role.");
                        Require(Mathf.Abs(
                                    battleEvent.Time - BattleRules.Skill2InitialCastTime) <=
                                BattleContext.DefaultTickDuration + 0.001f,
                            $"P2 first Backline Shift occurred at {battleEvent.Time:0.###}s instead of 5s.");
                    }
                    else if (firstTeleportTargetAlive)
                    {
                        Require(string.Equals(
                                battleEvent.TargetRuntimeId,
                                firstTeleport.TargetRuntimeId,
                                StringComparison.Ordinal),
                            "P2 changed Backline Shift targets before the first target was defeated.");
                        secondShiftRetainedLivingTarget = true;
                    }

                    if (teleportCount == 2)
                    {
                        float expectedSecondTime = BattleRules.Skill2InitialCastTime +
                                                   BattleRules.ActiveSkillCooldown;
                        Require(Mathf.Abs(battleEvent.Time - expectedSecondTime) <=
                                BattleContext.DefaultTickDuration + 0.001f,
                            $"P2 second Backline Shift occurred at {battleEvent.Time:0.###}s instead of 15s.");
                    }

                    float teleportDistance = Vector3.Distance(
                        battleEvent.ActorPositionAfter,
                        battleEvent.TargetPositionAfter);
                    Require(Mathf.Approximately(
                                battleEvent.Amount,
                                AssassinBattleKit.TeleportDistance) &&
                            Mathf.Abs(teleportDistance - AssassinBattleKit.TeleportDistance) <= 0.001f,
                        "P2 Backline Shift did not preserve two-grid separation from its target.");
                    Require(battleEvent.ActorPositionAfter.x >=
                            battleEvent.TargetPositionAfter.x - BattleRules.RangeEpsilon,
                        "P2 Backline Shift did not land toward the enemy base side of its target.");
                    continue;
                }

                if (firstTeleport == null ||
                    !firstTeleportTargetAlive ||
                    !string.Equals(battleEvent.ActorRuntimeId, "P2", StringComparison.Ordinal))
                {
                    continue;
                }

                if ((battleEvent.Type == BattleEventType.UnitMoved ||
                     battleEvent.Type == BattleEventType.BasicAttackStarted) &&
                    !string.IsNullOrEmpty(battleEvent.TargetRuntimeId))
                {
                    Require(string.Equals(
                            battleEvent.TargetRuntimeId,
                            firstTeleport.TargetRuntimeId,
                            StringComparison.Ordinal),
                        $"P2 abandoned living Backline Shift target '{firstTeleport.TargetRuntimeId}'.");
                }

                if (battleEvent.Type == BattleEventType.BasicAttackStarted &&
                    string.Equals(
                        battleEvent.TargetRuntimeId,
                        firstTeleport.TargetRuntimeId,
                        StringComparison.Ordinal))
                {
                    Require(BattleRules.IsWithinAttackRange(
                            battleEvent.ActorPositionAfter,
                            battleEvent.TargetPositionAfter,
                            BattleRules.MeleeAttackRange),
                        "P2 did not continue attacking its Backline Shift target from melee range.");
                    continuedBasicAttacksOnLockedTarget = true;
                }
            }

            Require(firstTeleport != null,
                "P2 emitted no Backline Shift teleport event at its first Skill 2 window.");
            Require(continuedBasicAttacksOnLockedTarget,
                "P2 did not keep its teleported backline target locked for a following basic attack.");
            Require(teleportCount >= 2 && secondShiftRetainedLivingTarget,
                "P2 did not retain its living backline target through the second Skill 2 window.");
        }

        private static BattleEvent VerifyTankTargetLockAndRetarget(BattleResult result)
        {
            var defeatedUnits = new HashSet<string>(StringComparer.Ordinal);
            string lockedTarget = null;
            BattleEvent firstAction = null;

            for (int i = 0; i < result.Events.Count; i++)
            {
                BattleEvent battleEvent = result.Events[i];
                if (battleEvent.Type == BattleEventType.UnitDefeated)
                {
                    defeatedUnits.Add(battleEvent.TargetRuntimeId);
                    continue;
                }

                if (!string.Equals(battleEvent.ActorRuntimeId, "P0", StringComparison.Ordinal) ||
                    (battleEvent.Type != BattleEventType.UnitMoved &&
                     battleEvent.Type != BattleEventType.BasicAttackStarted &&
                     battleEvent.Type != BattleEventType.SkillCastStarted))
                {
                    continue;
                }

                if (lockedTarget != null &&
                    !string.Equals(lockedTarget, battleEvent.TargetRuntimeId, StringComparison.Ordinal))
                {
                    Require(defeatedUnits.Contains(lockedTarget),
                        $"Player tank changed from living target '{lockedTarget}' to '{battleEvent.TargetRuntimeId}'.");
                }

                lockedTarget = battleEvent.TargetRuntimeId;
                if (firstAction == null &&
                    (battleEvent.Type == BattleEventType.BasicAttackStarted ||
                     battleEvent.Type == BattleEventType.SkillCastStarted))
                {
                    firstAction = battleEvent;
                }
            }

            Require(firstAction != null, "Player tank has no action event after approaching.");
            Require(firstAction.Type == BattleEventType.BasicAttackStarted,
                "Player tank did not begin combat with a basic attack at its range boundary.");
            float firstAttackDistance = Vector3.Distance(
                firstAction.ActorPositionAfter,
                firstAction.TargetPositionAfter);
            Require(Mathf.Abs(firstAttackDistance - BattleRules.MeleeAttackRange) <= 0.02f,
                $"Player tank first attacked at distance {firstAttackDistance:0.###}; " +
                $"expected maximum range {BattleRules.MeleeAttackRange:0.###}.");
            Require(Vector3.Distance(
                    result.PlayerUnits[0].CurrentPosition,
                    BattleRules.GetSlotPosition(BattleTeamSide.Player, 0)) > 0.5f,
                "Player tank simulation ended back at its formation spawn.");
            return firstAction;
        }

        private static int ExpectedRecurringCastCount(float elapsedTime, float initialCastTime)
        {
            if (elapsedTime + BattleRules.RangeEpsilon < initialCastTime)
            {
                return 0;
            }

            return 1 + Mathf.FloorToInt(
                (elapsedTime - initialCastTime + BattleRules.RangeEpsilon) /
                BattleRules.ActiveSkillCooldown);
        }

        private static T FindDescendantComponent<T>(string rootName, string objectName)
            where T : Component
        {
            GameObject root = FindSceneObject(rootName);
            return root == null ? null : FindComponentInChildrenByName<T>(root, objectName);
        }

        private static T FindComponentInChildrenByName<T>(GameObject root, string objectName)
            where T : Component
        {
            T[] components = root.GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component != null
                    && string.Equals(component.name, objectName, StringComparison.Ordinal))
                {
                    return component;
                }
            }

            return null;
        }

        private static T FindSceneObjectOfType<T>()
            where T : Component
        {
            T[] components = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component != null && component.gameObject.scene.IsValid())
                {
                    return component;
                }
            }

            return null;
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null
                    && candidate.gameObject.scene.IsValid()
                    && string.Equals(candidate.name, objectName, StringComparison.Ordinal))
                {
                    return candidate.gameObject;
                }
            }

            return null;
        }

        private void Finish(bool success, string message, Exception exception)
        {
            if (finished)
            {
                return;
            }

            finished = true;
            Time.timeScale = originalTimeScale;
            Application.runInBackground = originalRunInBackground;
            if (success)
            {
                Debug.Log($"{PassMarker} {message}", this);
            }
            else
            {
                Debug.LogError(
                    $"{FailMarker} {message}{(exception == null ? string.Empty : $"\n{exception}")}",
                    this);
            }

            Completed?.Invoke(success, message);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }

        private void OnDestroy()
        {
            if (!finished)
            {
                Time.timeScale = originalTimeScale;
                Application.runInBackground = originalRunInBackground;
            }
        }
    }
}
