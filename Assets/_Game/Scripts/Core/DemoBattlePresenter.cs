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
        private const float PlaybackSpeed = 1.25f;
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

        public bool TryGetPresentedHealth(
            string runtimeId,
            out float currentHealth,
            out float maxHealth)
        {
            if (!string.IsNullOrEmpty(runtimeId) &&
                units.TryGetValue(runtimeId, out UnitPresentation unit))
            {
                currentHealth = unit.Health;
                maxHealth = unit.MaxHealth;
                return true;
            }

            currentHealth = 0f;
            maxHealth = 0f;
            return false;
        }

        public bool TryGetPresentedRage(
            string runtimeId,
            out int currentRage,
            out int maxRage)
        {
            if (!string.IsNullOrEmpty(runtimeId) &&
                units.TryGetValue(runtimeId, out UnitPresentation unit))
            {
                currentRage = unit.Rage;
                maxRage = unit.Definition.MaxRage;
                return true;
            }

            currentRage = 0;
            maxRage = 0;
            return false;
        }

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
                72f,
                CatherineYukiBattleKit.DemoEnemyHealthMultiplier,
                CatherineYukiBattleKit.DemoEnemyAttackMultiplier);
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
                        if (!PlayCatherineSkillVfx(actor, targetPosition, battleEvent.SkillId))
                        {
                            SpawnSkillPulse(actor, target);
                        }
                        string skillName = CatherineYukiBattleKit.GetDisplayName(battleEvent.SkillId);
                        if (string.IsNullOrEmpty(skillName))
                        {
                            skillName = ResolveSkillDisplayName(actor.Definition, battleEvent.SkillId);
                        }

                        bool isUltimate = actor.Definition.UltimateSkill != null &&
                                          string.Equals(
                                              actor.Definition.UltimateSkill.Id,
                                              battleEvent.SkillId,
                                              StringComparison.Ordinal);
                        string actionLabel = isUltimate ? $"ULTIMATE - {skillName}" : skillName;
                        screen?.SetBattleStatus(
                            battleEvent.Time,
                            $"{actor.Definition.DisplayName} - {actionLabel}");
                    }

                    break;

                case BattleEventType.DamageApplied:
                    if (target != null)
                    {
                        target.MaxHealth = Mathf.Max(target.MaxHealth, battleEvent.MaxHealthAfter);
                        target.Health = battleEvent.HealthAfter;
                        target.Bar.SetHealth(target.Health, target.MaxHealth);
                        Vector3 source = actor == null ? target.View.transform.position - target.View.transform.forward : actor.View.transform.position;
                        target.View.PlayHit(source);
                        DamageNumberView.SpawnDamage(GetNumberPosition(target), battleEvent.Amount, false, worldRoot);
                    }

                    break;

                case BattleEventType.HealingApplied:
                    if (target != null)
                    {
                        target.MaxHealth = Mathf.Max(target.MaxHealth, battleEvent.MaxHealthAfter);
                        target.Health = battleEvent.HealthAfter;
                        target.Bar.SetHealth(target.Health, target.MaxHealth);
                        DamageNumberView.SpawnHealing(GetNumberPosition(target), battleEvent.Amount, worldRoot);
                        SpawnHealPulse(target);
                    }

                    break;

                case BattleEventType.RageChanged:
                    if (target != null)
                    {
                        target.Rage = battleEvent.RageAfter;
                        target.Bar.SetRage(target.Rage, target.Definition.MaxRage);
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

                case BattleEventType.UnitPulled:
                    if (target != null)
                    {
                        target.View.MoveRootTo(
                            battleEvent.TargetPositionAfter,
                            battleEvent.Duration / PlaybackSpeed);
                        SpawnControlPulse(target, new Color(0.40f, 0.10f, 0.82f, 1f), 1.55f);
                    }

                    break;

                case BattleEventType.UnitKnockedUp:
                    if (target != null)
                    {
                        StartCoroutine(AnimateKnockUp(
                            target.View.transform,
                            battleEvent.TargetPositionAfter,
                            battleEvent.Duration / PlaybackSpeed));
                    }

                    break;

                case BattleEventType.DebuffApplied:
                    if (target != null)
                    {
                        SpawnControlPulse(target, new Color(0.76f, 0.20f, 0.92f, 1f), 1.15f);
                    }

                    break;

                case BattleEventType.StatusApplied:
                    if (actor != null &&
                        CatherineYukiBattleKit.IsCatherine(actor.Definition.Id) &&
                        !string.Equals(
                            battleEvent.SkillId,
                            CatherineYukiBattleKit.SuperArmorStatusId,
                            StringComparison.Ordinal))
                    {
                        CatherineSkillVfxController vfx =
                            actor.View.GetComponent<CatherineSkillVfxController>();
                        vfx?.PlayStackGain(Mathf.RoundToInt(battleEvent.Amount));
                    }
                    else if (target != null &&
                        string.Equals(
                            battleEvent.SkillId,
                            CatherineYukiBattleKit.SuperArmorStatusId,
                            StringComparison.Ordinal))
                    {
                        SpawnControlPulse(target, new Color(1f, 0.78f, 0.24f, 1f), 1.28f);
                    }

                    break;

                case BattleEventType.UltimatePhase:
                    PresentUltimatePhase(battleEvent, actor, target);
                    break;

                case BattleEventType.UnitRevived:
                    if (target != null)
                    {
                        target.View.ResetView();
                        target.MaxHealth = Mathf.Max(target.MaxHealth, battleEvent.MaxHealthAfter);
                        target.Health = battleEvent.HealthAfter;
                        target.Bar.SetHealth(target.Health, target.MaxHealth, true);
                        if (target.View.HealthBar != null)
                        {
                            target.View.HealthBar.gameObject.SetActive(true);
                        }

                        SpawnControlPulse(target, new Color(0.90f, 0.70f, 1f, 1f), 1.8f);
                        screen?.SetBattleStatus(battleEvent.Time, "Catherine Yuki revived");
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
            Material stone = CreateMaterial("ObservatoryStone", new Color(0.34f, 0.45f, 0.48f, 1f), 0f, 0.16f);
            Material trim = CreateMaterial("ObservatoryTrim", new Color(0.76f, 0.62f, 0.34f, 1f), 0.04f, 0.22f);
            Material energy = CreateMaterial("ObservatoryEnergy", new Color(0.20f, 0.72f, 0.70f, 0.58f), 0f, 0.24f, true);
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
                    position.y = 0.018f;
                    GameObject marker = CreateArenaCylinder(
                        side == BattleTeamSide.Player ? $"PlayerMarker_{slot}" : $"EnemyMarker_{slot}",
                        position,
                        new Vector3(0.66f, 0.008f, 0.66f),
                        energy);
                    marker.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            Vector3[] ornamentPositions =
            {
                new Vector3(-7.20f, 0.26f, 3.80f),
                new Vector3(-6.55f, 0.16f, 4.55f),
                new Vector3(6.55f, 0.16f, 4.55f),
                new Vector3(7.20f, 0.26f, 3.80f)
            };

            for (int i = 0; i < ornamentPositions.Length; i++)
            {
                GameObject ornament = CreateArenaCylinder(
                    $"EdgePlinth_{i}",
                    ornamentPositions[i],
                    new Vector3(0.34f, i % 2 == 0 ? 0.26f : 0.16f, 0.34f),
                    i % 2 == 0 ? trim : stone);
                Renderer ornamentRenderer = ornament.GetComponent<Renderer>();
                ornamentRenderer.shadowCastingMode = ShadowCastingMode.Off;
                ornamentRenderer.receiveShadows = false;
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
                    definition.Role == CharacterRole.Tank ? 1.12f : 1f);
            }

            Transform barAnchor = view.HealthBar != null ? view.HealthBar : view.transform;
            GameObject barObject = new GameObject("WorldBars");
            barObject.transform.SetParent(barAnchor, false);
            WorldBarView bar = barObject.AddComponent<WorldBarView>();
            bar.SetTargetCamera(battleCamera);
            float runtimeMaxHealth = definition.MaxHealth *
                                     (enemy ? CatherineYukiBattleKit.DemoEnemyHealthMultiplier : 1f);
            bar.SetHealth(runtimeMaxHealth, runtimeMaxHealth, true);
            bar.SetRage(0f, definition.MaxRage, true);
            bar.SetColors(
                enemy ? new Color(1f, 0.30f, 0.25f, 1f) : new Color(0.22f, 0.94f, 0.45f, 1f),
                new Color(1f, 0.50f, 0.12f, 1f));

            CreateWorldName(barAnchor, definition.DisplayName, enemy ? DemoUiFactory.Danger : DemoUiFactory.Accent);
            units[runtimeId] = new UnitPresentation(definition, view, bar, runtimeMaxHealth);
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

        private void SpawnControlPulse(UnitPresentation target, Color color, float scale)
        {
            StartCoroutine(AnimatePulse(
                target.View.transform.position + Vector3.up * 0.88f,
                color,
                0.44f,
                scale));
        }

        private static string ResolveSkillDisplayName(
            CharacterDefinition definition,
            string skillId)
        {
            if (definition == null)
            {
                return "Skill";
            }

            SkillDefinition[] slots =
            {
                definition.UltimateSkill,
                definition.Skill2,
                definition.Skill3
            };
            for (int index = 0; index < slots.Length; index++)
            {
                SkillDefinition slot = slots[index];
                if (slot != null && string.Equals(slot.Id, skillId, StringComparison.Ordinal))
                {
                    return slot.DisplayName;
                }
            }

            return "Skill";
        }

        private static bool PlayCatherineSkillVfx(
            UnitPresentation actor,
            Vector3 targetPosition,
            string skillId)
        {
            if (actor == null || !CatherineYukiBattleKit.IsCatherine(actor.Definition.Id))
            {
                return false;
            }

            CatherineSkillVfxController vfx =
                actor.View.GetComponent<CatherineSkillVfxController>();
            if (vfx == null)
            {
                return false;
            }

            CosmicSlimeVisualController visual =
                actor.View.GetComponent<CosmicSlimeVisualController>();

            switch (skillId)
            {
                case CatherineYukiBattleKit.Skill1Id:
                    visual?.PlayStretch(0.58f);
                    vfx.PlaySkill1LineBreak(targetPosition);
                    return true;
                case CatherineYukiBattleKit.Skill2Id:
                    visual?.PlayDance(0.29f);
                    vfx.PlaySkill2Dance(targetPosition);
                    return true;
                case CatherineYukiBattleKit.Skill3Id:
                    visual?.PlaySquash(0.52f);
                    vfx.PlayDebuff(targetPosition);
                    return true;
                case CatherineYukiBattleKit.UltimateId:
                case CatherineYukiBattleKit.DeathUltimateId:
                    Action<CatherineUltimateVfxStage> stageCallback = visual == null
                        ? null
                        : visual.PresentUltimateStage;
                    vfx.PlayUltimatePull(
                        (IReadOnlyList<Transform>)null,
                        onStageChanged: stageCallback);
                    return true;
                default:
                    return false;
            }
        }

        private void PresentUltimatePhase(
            BattleEvent battleEvent,
            UnitPresentation actor,
            UnitPresentation target)
        {
            if (actor == null)
            {
                return;
            }

            bool hasAuthoredUltimateVfx =
                actor.View.GetComponent<CatherineSkillVfxController>() != null;

            if (string.Equals(
                    battleEvent.SkillId,
                    CatherineYukiBattleKit.UltimateChargePhaseId,
                    StringComparison.Ordinal))
            {
                if (!hasAuthoredUltimateVfx)
                {
                    SpawnControlPulse(actor, new Color(0.62f, 0.28f, 1f, 1f), 2.1f);
                }
                screen?.SetBattleStatus(battleEvent.Time, "Infinite Void charging");
            }
            else if (string.Equals(
                         battleEvent.SkillId,
                         CatherineYukiBattleKit.UltimateTransformPhaseId,
                         StringComparison.Ordinal))
            {
                if (!hasAuthoredUltimateVfx)
                {
                    SpawnControlPulse(actor, new Color(0.16f, 0.02f, 0.30f, 1f), 2.8f);
                }
                screen?.SetBattleStatus(battleEvent.Time, "Catherine becomes Infinite Void");
            }
            else if (string.Equals(
                         battleEvent.SkillId,
                         CatherineYukiBattleKit.UltimateCollapsePhaseId,
                         StringComparison.Ordinal))
            {
                if (!hasAuthoredUltimateVfx)
                {
                    SpawnControlPulse(actor, new Color(0.88f, 0.62f, 1f, 1f), 3.5f);
                }
                screen?.SetBattleStatus(battleEvent.Time, "Infinite Void collapse");
            }
        }

        private static IEnumerator AnimateKnockUp(
            Transform target,
            Vector3 destination,
            float duration)
        {
            if (target == null)
            {
                yield break;
            }

            Vector3 start = target.position;
            float safeDuration = Mathf.Max(0.08f, duration);
            float elapsed = 0f;
            while (elapsed < safeDuration && target != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                Vector3 position = Vector3.LerpUnclamped(start, destination, t);
                position.y += Mathf.Sin(t * Mathf.PI) * 1.15f;
                target.position = position;
                yield return null;
            }

            if (target != null)
            {
                target.position = destination;
            }
        }

        private IEnumerator AnimatePulse(Vector3 position, Color color, float duration, float maximumScale)
        {
            GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pulse.name = "SkillPulse";
            pulse.transform.SetParent(worldRoot, true);
            pulse.transform.position = position;
            pulse.transform.localScale = Vector3.one * 0.12f;
            if (battleCamera != null)
            {
                pulse.transform.rotation = Quaternion.LookRotation(
                    (battleCamera.transform.position - position).normalized,
                    Vector3.up);
            }

            Material material = CreatePulseMaterial(color);
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
                float readableScale = maximumScale * 0.72f;
                pulse.transform.localScale = Vector3.one * Mathf.Lerp(
                    0.12f,
                    readableScale,
                    1f - Mathf.Pow(1f - t, 3f));
                if (material.HasProperty("_Progress"))
                {
                    material.SetFloat("_Progress", t);
                    material.SetFloat("_Opacity", (1f - t) * 0.72f);
                }
                else
                {
                    color.a = (1f - t) * 0.5f;
                    SetMaterialColor(material, color);
                }
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
                case CharacterRole.Tank:
                    return ProceduralCharacterArchetype.Vanguard;
                case CharacterRole.Support:
                case CharacterRole.Mage:
                    return ProceduralCharacterArchetype.Mystic;
                case CharacterRole.Assassin:
                case CharacterRole.Ranged:
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

        private Material CreatePulseMaterial(Color color)
        {
            Shader shader = Shader.Find("BubbleMind/Black Hole VFX");
            if (shader == null)
            {
                color.a = 0.5f;
                return CreateMaterial("Pulse", color, 0f, 0.2f, true);
            }

            Material material = new Material(shader) { name = "Pulse" };
            material.SetFloat("_Mode", 2f);
            material.SetColor("_CoreColor", new Color(0f, 0f, 0f, 0f));
            material.SetColor("_InnerColor", color);
            material.SetColor("_OuterColor", Color.Lerp(color, Color.white, 0.28f));
            material.SetColor("_AccentColor", Color.Lerp(color, Color.white, 0.55f));
            material.SetFloat("_Radius", 0.34f);
            material.SetFloat("_RingWidth", 0.075f);
            material.SetFloat("_Softness", 0.065f);
            material.SetFloat("_Speed", 1.6f);
            material.SetFloat("_Intensity", 1.25f);
            material.SetFloat("_Progress", 0f);
            material.SetFloat("_Opacity", 0.72f);
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
            public UnitPresentation(
                CharacterDefinition definition,
                CharacterView view,
                WorldBarView bar,
                float maxHealth)
            {
                Definition = definition;
                View = view;
                Bar = bar;
                MaxHealth = maxHealth;
                Health = maxHealth;
                Rage = 0;
            }

            public CharacterDefinition Definition { get; }
            public CharacterView View { get; }
            public WorldBarView Bar { get; }
            public float MaxHealth { get; set; }
            public float Health { get; set; }
            public int Rage { get; set; }
        }
    }
}
