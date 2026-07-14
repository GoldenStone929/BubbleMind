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

            ClickActiveButton("BackButton");
            yield return WaitForScreen("HomeScreen", StepTimeoutSeconds);
            AssertOnlyScreenActive("HomeScreen");

            ClickActiveButton("FormationButton");
            yield return WaitForScreen("FormationScreen", StepTimeoutSeconds);
            AssertOnlyScreenActive("FormationScreen");
            yield return WaitFor(
                () =>
                {
                    Button button = FindActiveButton("BattleButton", false);
                    return button != null && button.interactable;
                },
                "valid three-character formation and battle button",
                StepTimeoutSeconds);

            ClickActiveButton("BattleButton");
            yield return WaitForScreen("BattleScreen", StepTimeoutSeconds);
            AssertOnlyScreenActive("BattleScreen");
            yield return WaitFor(
                () => FindSceneObject("AbyssalObservatory_Backdrop") != null &&
                      FindSceneObject("P1_Abyssal Slime") != null,
                "Abyssal Observatory and authored Cosmic Slime",
                StepTimeoutSeconds);
            GameObject backdrop = FindSceneObject("AbyssalObservatory_Backdrop");
            Renderer backdropRenderer = backdrop == null ? null : backdrop.GetComponent<Renderer>();
            Require(backdropRenderer != null, "Abyssal Observatory backdrop has no Renderer.");
            Require(backdropRenderer.sharedMaterial != null,
                "Abyssal Observatory backdrop has no runtime material.");
            Require(backdropRenderer.sharedMaterial.shader != null,
                "Abyssal Observatory backdrop material has no runtime shader.");
            GameObject cosmicSlime = FindSceneObject("P1_Abyssal Slime");
            Require(cosmicSlime != null && cosmicSlime.GetComponent<CosmicSlimeVisualController>() != null,
                "Player Cosmic Slime did not instantiate from the authored prefab.");
            Require(cosmicSlime.GetComponentsInChildren<Renderer>(true).Length > 0,
                "Player Cosmic Slime prefab has no visible renderer.");

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
