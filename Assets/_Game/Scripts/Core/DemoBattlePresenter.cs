using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace GenericGachaRPG
{
    /// <summary>
    /// Replays presentation-neutral battle events using procedural characters.
    /// It never changes battle calculations; it only visualizes the completed,
    /// deterministic simulation result.
    /// </summary>
    public sealed class DemoBattlePresenter : MonoBehaviour
    {
        private const float PlaybackSpeed = 1.6f;
        private const string ArenaBackdropResource = "AbyssalObservatory_Concept";
        private const string ArenaBackdropMaterialResource = "MAT_AbyssalObservatoryBackdrop";

        private readonly Dictionary<string, UnitPresentation> units = new Dictionary<string, UnitPresentation>();
        private readonly List<Material> runtimeMaterials = new List<Material>();

        private Transform worldRoot;
        private Camera battleCamera;
        private BattleScreenView screen;
        private Coroutine replayRoutine;
        private BattleResult lastResult;

        public bool IsRunning => replayRoutine != null;
        public BattleResult LastResult => lastResult;

        public event Action<BattleResult> BattleCompleted;

        public void Configure(Camera cameraToUse)
        {
            battleCamera = cameraToUse;
        }

        public void StartBattle(
            IReadOnlyList<CharacterDefinition> playerCharacters,
            IReadOnlyList<CharacterDefinition> enemyCharacters,
            BattleScreenView battleScreen,
            int seed)
        {
            if (playerCharacters == null || playerCharacters.Count != BattleTeam.RequiredMemberCount)
            {
                throw new ArgumentException("Player battle team must contain exactly five characters.", nameof(playerCharacters));
            }

            if (enemyCharacters == null || enemyCharacters.Count != BattleTeam.RequiredMemberCount)
            {
                throw new ArgumentException("Enemy battle team must contain exactly five characters.", nameof(enemyCharacters));
            }

            StopBattle();
            screen = battleScreen;
            EnsureCamera();
            BuildWorld(playerCharacters, enemyCharacters);

            var context = new BattleContext(
                new BattleTeam(playerCharacters),
                new BattleTeam(enemyCharacters),
                seed,
                BattleContext.DefaultTickDuration,
                72f);
            lastResult = new BattleSimulation(context).Run();
            screen?.HideResult();
            screen?.SetBattleStatus(0f, "AUTO BATTLE START");
            replayRoutine = StartCoroutine(Replay(lastResult));
        }

        public void StopBattle()
        {
            if (replayRoutine != null)
            {
                StopCoroutine(replayRoutine);
                replayRoutine = null;
            }

            if (worldRoot != null)
            {
                Destroy(worldRoot.gameObject);
                worldRoot = null;
            }

            units.Clear();
            DestroyRuntimeMaterials();
            screen = null;
            lastResult = null;
        }

        private IEnumerator Replay(BattleResult result)
        {
            float presentedSimulationTime = 0f;
            IReadOnlyList<BattleEvent> events = result.Events;

            for (int i = 0; i < events.Count; i++)
            {
                BattleEvent battleEvent = events[i];
                float waitSimulationTime = Mathf.Max(0f, battleEvent.Time - presentedSimulationTime);
                float waitRealTime = waitSimulationTime / PlaybackSpeed;
                float elapsedRealTime = 0f;

                while (elapsedRealTime < waitRealTime)
                {
                    elapsedRealTime += Time.deltaTime;
                    float fraction = waitRealTime <= 0f ? 1f : Mathf.Clamp01(elapsedRealTime / waitRealTime);
                    float displayedTime = Mathf.Lerp(presentedSimulationTime, battleEvent.Time, fraction);
                    screen?.SetBattleStatus(displayedTime, "AUTO BATTLE");
                    yield return null;
                }

                presentedSimulationTime = battleEvent.Time;
                PresentEvent(battleEvent);
            }

            replayRoutine = null;
            bool playerWon = result.Outcome == BattleOutcome.PlayerVictory;
            screen?.SetBattleStatus(result.ElapsedTime, "BATTLE COMPLETE");
            screen?.ShowResult(playerWon, result.IsTimeout, result.ElapsedTime);
            BattleCompleted?.Invoke(result);
        }

        private void PresentEvent(BattleEvent battleEvent)
        {
            UnitPresentation actor = FindUnit(battleEvent.ActorRuntimeId);
            UnitPresentation target = FindUnit(battleEvent.TargetRuntimeId);

            switch (battleEvent.Type)
            {
                case BattleEventType.UnitMoved:
                    if (actor != null)
                    {
                        actor.View.MoveRootTo(
                            battleEvent.ActorPositionAfter,
                            battleEvent.Duration / PlaybackSpeed);
                    }

                    break;

                case BattleEventType.BasicAttackStarted:
                    if (actor != null && target != null)
                    {
                        actor.View.FaceTarget(target.View.transform.position);
                        actor.View.PlayBasicAttack(
                            target.View.transform,
                            BattleRules.BasicAttackHitDelay / PlaybackSpeed);
                        screen?.SetBattleStatus(battleEvent.Time, $"{actor.Definition.DisplayName} attacks");
                    }

                    break;

                case BattleEventType.SkillCastStarted:
                    if (actor != null)
                    {
                        Vector3 targetPosition = target == null ? actor.View.transform.position : target.View.transform.position;
                        actor.View.FaceTarget(targetPosition);
                        actor.View.PlaySkill(targetPosition);
                        SpawnSkillPulse(actor, target);
                        string skillName = actor.Definition.Skill == null
                            ? "Skill"
                            : actor.Definition.Skill.DisplayName;
                        screen?.SetBattleStatus(battleEvent.Time, $"{actor.Definition.DisplayName} • {skillName}");
                    }

                    break;

                case BattleEventType.DamageApplied:
                    if (target != null)
                    {
                        target.Health = battleEvent.HealthAfter;
                        target.Bar.SetHealth(target.Health, target.Definition.MaxHealth);
                        Vector3 source = actor == null ? target.View.transform.position - target.View.transform.forward : actor.View.transform.position;
                        target.View.PlayHit(source);
                        DamageNumberView.SpawnDamage(GetNumberPosition(target), battleEvent.Amount, false, worldRoot);
                    }

                    break;

                case BattleEventType.HealingApplied:
                    if (target != null)
                    {
                        target.Health = battleEvent.HealthAfter;
                        target.Bar.SetHealth(target.Health, target.Definition.MaxHealth);
                        DamageNumberView.SpawnHealing(GetNumberPosition(target), battleEvent.Amount, worldRoot);
                        SpawnHealPulse(target);
                    }

                    break;

                case BattleEventType.EnergyChanged:
                    if (target != null)
                    {
                        target.Energy = battleEvent.EnergyAfter;
                        target.Bar.SetEnergy(target.Energy, target.Definition.MaxEnergy);
                    }

                    break;

                case BattleEventType.UnitDefeated:
                    if (target != null)
                    {
                        target.Health = 0f;
                        target.Bar.SetHealthNormalized(0f);
                        target.View.PlayDeath();
                        if (target.View.HealthBar != null)
                        {
                            target.View.HealthBar.gameObject.SetActive(false);
                        }

                        screen?.SetBattleStatus(battleEvent.Time, $"{target.Definition.DisplayName} defeated");
                    }

                    break;
            }
        }

        private void BuildWorld(
            IReadOnlyList<CharacterDefinition> playerCharacters,
            IReadOnlyList<CharacterDefinition> enemyCharacters)
        {
            GameObject rootObject = new GameObject("BattleWorld");
            rootObject.transform.SetParent(transform, false);
            worldRoot = rootObject.transform;

            BuildEnvironment();
            for (int i = 0; i < BattleTeam.RequiredMemberCount; i++)
            {
                CreateUnit(
                    $"P{i}",
                    playerCharacters[i],
                    BattleRules.GetSlotPosition(BattleTeamSide.Player, i),
                    Quaternion.Euler(0f, 90f, 0f),
                    i,
                    false);
                CreateUnit(
                    $"E{i}",
                    enemyCharacters[i],
                    BattleRules.GetSlotPosition(BattleTeamSide.Enemy, i),
                    Quaternion.Euler(0f, -90f, 0f),
                    i + 20,
                    true);
            }
        }

        private void BuildEnvironment()
        {
            Texture2D backdrop = Resources.Load<Texture2D>(ArenaBackdropResource);
            Material backdropMaterial = Resources.Load<Material>(ArenaBackdropMaterialResource);
            if (backdrop != null && backdropMaterial != null)
            {
                BuildAbyssalObservatory(backdrop, backdropMaterial);
                return;
            }

            Debug.LogWarning(
                $"[GenericGachaRPG] Arena backdrop resources are unavailable; using the procedural fallback. " +
                $"Texture={backdrop != null}, Material={backdropMaterial != null}.",
                this);
            BuildFallbackEnvironment();
        }

        private void BuildAbyssalObservatory(Texture2D backdropTexture, Material backdropTemplate)
        {
            Material stone = CreateMaterial("ObservatoryStone", new Color(0.055f, 0.065f, 0.085f, 1f), 0.18f, 0.42f);
            Material trim = CreateMaterial("ObservatoryTrim", new Color(0.33f, 0.23f, 0.11f, 1f), 0.72f, 0.52f);
            Material energy = CreateMaterial("ObservatoryEnergy", new Color(0.06f, 0.68f, 0.86f, 0.76f), 0.12f, 0.82f, true);
            Material backdrop = CreateBackdropMaterial(backdropTexture, backdropTemplate);

            GameObject backdropObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backdropObject.name = "AbyssalObservatory_Backdrop";
            backdropObject.transform.SetParent(worldRoot, false);
            if (battleCamera != null)
            {
                const float distance = 22f;
                backdropObject.transform.position =
                    battleCamera.transform.position + battleCamera.transform.forward * distance;
                backdropObject.transform.rotation = battleCamera.transform.rotation;
                float height = 2f * distance * Mathf.Tan(battleCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.08f;
                backdropObject.transform.localScale = new Vector3(height * battleCamera.aspect, height, 1f);
            }
            else
            {
                backdropObject.transform.position = new Vector3(0f, 3.2f, 6.6f);
                backdropObject.transform.localScale = new Vector3(29.6f, 16.65f, 1f);
            }

            Renderer backdropRenderer = backdropObject.GetComponent<Renderer>();
            backdropRenderer.sharedMaterial = backdrop;
            backdropRenderer.shadowCastingMode = ShadowCastingMode.Off;
            backdropRenderer.receiveShadows = false;
            RemoveCollider(backdropObject);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                BattleTeamSide side = sideIndex == 0 ? BattleTeamSide.Player : BattleTeamSide.Enemy;
                for (int slot = 0; slot < BattleRules.TeamSize; slot++)
                {
                    Vector3 position = BattleRules.GetSlotPosition(side, slot);
                    position.y = 0.035f;
                    GameObject marker = CreateArenaCylinder(
                        side == BattleTeamSide.Player ? $"PlayerMarker_{slot}" : $"EnemyMarker_{slot}",
                        position,
                        new Vector3(0.78f, 0.014f, 0.78f),
                        energy);
                    marker.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            Vector3[] ruinPositions =
            {
                new Vector3(-6.15f, 0.72f, 3.25f),
                new Vector3(-4.85f, 0.48f, 3.72f),
                new Vector3(4.85f, 0.48f, 3.72f),
                new Vector3(6.15f, 0.72f, 3.25f)
            };

            for (int i = 0; i < ruinPositions.Length; i++)
            {
                GameObject ruin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ruin.name = $"RuinMonolith_{i}";
                ruin.transform.SetParent(worldRoot, false);
                ruin.transform.position = ruinPositions[i];
                ruin.transform.localScale = new Vector3(0.52f, i % 2 == 0 ? 1.65f : 1.15f, 0.52f);
                ruin.transform.rotation = Quaternion.Euler(0f, i < 2 ? -12f : 12f, i % 2 == 0 ? 3f : -3f);
                ruin.GetComponent<Renderer>().sharedMaterial = i % 2 == 0 ? trim : stone;
                RemoveCollider(ruin);
            }
        }

        private GameObject CreateArenaCylinder(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(worldRoot, false);
            cylinder.transform.position = position;
            cylinder.transform.localScale = scale;
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            RemoveCollider(cylinder);
            return cylinder;
        }

        private static void RemoveCollider(GameObject gameObject)
        {
            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }
        }

        private Material CreateBackdropMaterial(Texture2D texture, Material template)
        {
            Material material = new Material(template) { name = "AbyssalObservatoryBackdrop" };
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }
            else
            {
                material.mainTexture = texture;
            }

            runtimeMaterials.Add(material);
            return material;
        }

        private void BuildFallbackEnvironment()
        {
            Material floorMaterial = CreateMaterial("ArenaFloor", new Color(0.07f, 0.11f, 0.17f, 1f), 0.15f, 0.4f);
            Material markerPlayer = CreateMaterial("PlayerMarker", new Color(0.08f, 0.55f, 0.93f, 0.7f), 0f, 0.7f, true);
            Material markerEnemy = CreateMaterial("EnemyMarker", new Color(0.95f, 0.18f, 0.28f, 0.7f), 0f, 0.7f, true);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(worldRoot, false);
            floor.transform.position = new Vector3(0f, -0.16f, 0f);
            floor.transform.localScale = new Vector3(13.5f, 0.25f, 8f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            RemoveCollider(floor);

            for (int sideIndex = 0; sideIndex < 2; sideIndex++)
            {
                BattleTeamSide side = sideIndex == 0 ? BattleTeamSide.Player : BattleTeamSide.Enemy;
                for (int slot = 0; slot < BattleRules.TeamSize; slot++)
                {
                    Vector3 position = BattleRules.GetSlotPosition(side, slot);
                    position.y = 0.005f;
                    GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    marker.name = side == BattleTeamSide.Player
                        ? $"PlayerMarker_{slot}"
                        : $"EnemyMarker_{slot}";
                    marker.transform.SetParent(worldRoot, false);
                    marker.transform.position = position;
                    marker.transform.localScale = new Vector3(0.85f, 0.018f, 0.85f);
                    marker.GetComponent<Renderer>().sharedMaterial = side == BattleTeamSide.Player
                        ? markerPlayer
                        : markerEnemy;
                    RemoveCollider(marker);
                }
            }

            for (int i = 0; i < 7; i++)
            {
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"BackdropPillar_{i}";
                pillar.transform.SetParent(worldRoot, false);
                pillar.transform.position = new Vector3(-6f + i * 2f, 1.25f + (i % 2) * 0.45f, 4.5f);
                pillar.transform.localScale = new Vector3(1.3f, 2.5f + (i % 2) * 0.9f, 0.45f);
                pillar.GetComponent<Renderer>().sharedMaterial = floorMaterial;
                RemoveCollider(pillar);
            }
        }

        private void CreateUnit(
            string runtimeId,
            CharacterDefinition definition,
            Vector3 position,
            Quaternion rotation,
            int styleSeed,
            bool enemy)
        {
            CharacterView view = CreateAuthoredCharacter(runtimeId, definition, position, rotation);
            if (view == null)
            {
                Color primary = enemy
                    ? Color.Lerp(definition.DisplayColor, new Color(0.45f, 0.04f, 0.08f, 1f), 0.34f)
                    : definition.DisplayColor;
                Color accent = enemy
                    ? new Color(1f, 0.22f, 0.18f, 1f)
                    : Color.Lerp(definition.DisplayColor, Color.white, 0.48f);
                view = ProceduralCharacterBuilder.Create(
                    $"{runtimeId}_{definition.DisplayName}",
                    worldRoot,
                    position,
                    rotation,
                    primary,
                    accent,
                    styleSeed,
                    GetArchetype(definition.Role),
                    definition.Role == CharacterRole.Guardian ? 1.12f : 1f);
            }

            Transform barAnchor = view.HealthBar != null ? view.HealthBar : view.transform;
            GameObject barObject = new GameObject("WorldBars");
            barObject.transform.SetParent(barAnchor, false);
            WorldBarView bar = barObject.AddComponent<WorldBarView>();
            bar.SetTargetCamera(battleCamera);
            bar.SetHealth(definition.MaxHealth, definition.MaxHealth, true);
            bar.SetEnergy(0f, definition.MaxEnergy, true);
            bar.SetColors(
                enemy ? new Color(1f, 0.30f, 0.25f, 1f) : new Color(0.22f, 0.94f, 0.45f, 1f),
                new Color(0.22f, 0.66f, 1f, 1f));

            CreateWorldName(barAnchor, definition.DisplayName, enemy ? DemoUiFactory.Danger : DemoUiFactory.Accent);
            units[runtimeId] = new UnitPresentation(definition, view, bar);
        }

        private CharacterView CreateAuthoredCharacter(
            string runtimeId,
            CharacterDefinition definition,
            Vector3 position,
            Quaternion rotation)
        {
            if (definition.CharacterPrefab == null)
            {
                return null;
            }

            GameObject instance = Instantiate(definition.CharacterPrefab, position, rotation, worldRoot);
            instance.name = $"{runtimeId}_{definition.DisplayName}";
            CharacterView view = instance.GetComponent<CharacterView>();
            if (view == null)
            {
                Debug.LogWarning(
                    $"[GenericGachaRPG] Character prefab '{definition.CharacterPrefab.name}' has no root CharacterView; using fallback.",
                    definition.CharacterPrefab);
                Destroy(instance);
                return null;
            }

            view.ResetView();
            return view;
        }

        private void CreateWorldName(Transform parent, string displayName, Color color)
        {
            GameObject host = new GameObject("Nameplate");
            host.transform.SetParent(parent, false);
            host.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            WorldNameplateView view = host.AddComponent<WorldNameplateView>();
            view.Initialize(displayName.ToUpperInvariant(), color, battleCamera);
        }

        private void SpawnSkillPulse(UnitPresentation actor, UnitPresentation target)
        {
            Vector3 position = target == null
                ? actor.View.transform.position + Vector3.up * 1.1f
                : target.View.transform.position + Vector3.up * 1.0f;
            StartCoroutine(AnimatePulse(position, actor.Definition.DisplayColor, 0.72f, 1.8f));
        }

        private void SpawnHealPulse(UnitPresentation target)
        {
            StartCoroutine(AnimatePulse(
                target.View.transform.position + Vector3.up * 0.95f,
                new Color(0.22f, 1f, 0.45f, 1f),
                0.46f,
                1.15f));
        }

        private IEnumerator AnimatePulse(Vector3 position, Color color, float duration, float maximumScale)
        {
            GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pulse.name = "SkillPulse";
            pulse.transform.SetParent(worldRoot, true);
            pulse.transform.position = position;
            pulse.transform.localScale = Vector3.one * 0.12f;
            Material material = CreateMaterial("Pulse", color, 0f, 0.9f, true);
            pulse.GetComponent<Renderer>().sharedMaterial = material;
            Collider collider = pulse.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            float elapsed = 0f;
            while (elapsed < duration && pulse != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                pulse.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, maximumScale, 1f - Mathf.Pow(1f - t, 3f));
                color.a = 1f - t;
                SetMaterialColor(material, color);
                yield return null;
            }

            if (pulse != null)
            {
                Destroy(pulse);
            }
        }

        private UnitPresentation FindUnit(string runtimeId)
        {
            if (string.IsNullOrEmpty(runtimeId))
            {
                return null;
            }

            units.TryGetValue(runtimeId, out UnitPresentation unit);
            return unit;
        }

        private static Vector3 GetNumberPosition(UnitPresentation unit)
        {
            Transform target = unit.View.Target != null ? unit.View.Target : unit.View.transform;
            return target.position + Vector3.up * 0.38f;
        }

        private static ProceduralCharacterArchetype GetArchetype(CharacterRole role)
        {
            switch (role)
            {
                case CharacterRole.Guardian:
                    return ProceduralCharacterArchetype.Vanguard;
                case CharacterRole.Support:
                    return ProceduralCharacterArchetype.Mystic;
                default:
                    return ProceduralCharacterArchetype.Striker;
            }
        }

        private void EnsureCamera()
        {
            if (battleCamera == null)
            {
                battleCamera = Camera.main;
            }

            if (battleCamera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                battleCamera = cameraObject.GetComponent<Camera>();
            }

            battleCamera.transform.position = new Vector3(0f, 5.6f, -12.8f);
            battleCamera.transform.rotation = Quaternion.LookRotation(
                new Vector3(0f, 1.15f, 0f) - battleCamera.transform.position,
                Vector3.up);
            battleCamera.fieldOfView = 46f;
            battleCamera.clearFlags = CameraClearFlags.SolidColor;
            battleCamera.backgroundColor = new Color(0.025f, 0.045f, 0.085f, 1f);
        }

        private Material CreateMaterial(
            string materialName,
            Color color,
            float metallic,
            float smoothness,
            bool emission = false)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader) { name = materialName };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            if (color.a < 0.999f)
            {
                if (material.HasProperty("_Surface"))
                {
                    material.SetFloat("_Surface", 1f);
                    material.SetFloat("_Blend", 0f);
                    material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                    material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                    material.SetFloat("_ZWrite", 0f);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.renderQueue = (int)RenderQueue.Transparent;
                }
                else
                {
                    material.SetFloat("_Mode", 3f);
                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                }
            }

            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * 1.6f);
            }

            runtimeMaterials.Add(material);
            return material;
        }

        private static void SetMaterialColor(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
        }

        private void DestroyRuntimeMaterials()
        {
            for (int i = 0; i < runtimeMaterials.Count; i++)
            {
                if (runtimeMaterials[i] != null)
                {
                    Destroy(runtimeMaterials[i]);
                }
            }

            runtimeMaterials.Clear();
        }

        private sealed class UnitPresentation
        {
            public UnitPresentation(CharacterDefinition definition, CharacterView view, WorldBarView bar)
            {
                Definition = definition;
                View = view;
                Bar = bar;
                Health = definition.MaxHealth;
                Energy = 0;
            }

            public CharacterDefinition Definition { get; }
            public CharacterView View { get; }
            public WorldBarView Bar { get; }
            public float Health { get; set; }
            public int Energy { get; set; }
        }
    }
}
