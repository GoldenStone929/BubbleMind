using System;
using System.Collections.Generic;

namespace GenericGachaRPG
{
    public enum DemoMissionObjective
    {
        DrawCharacters,
        OwnCharacters,
        WinBattles,
        ClearStage
    }

    public sealed class DemoMissionDefinition
    {
        public DemoMissionDefinition(
            string id,
            string title,
            string description,
            DemoMissionObjective objective,
            int target,
            int crystalReward,
            int goldReward,
            string stageId = "")
        {
            Id = id ?? string.Empty;
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            Objective = objective;
            Target = Math.Max(1, target);
            CrystalReward = Math.Max(0, crystalReward);
            GoldReward = Math.Max(0, goldReward);
            StageId = stageId ?? string.Empty;
        }

        public string Id { get; }
        public string Title { get; }
        public string Description { get; }
        public DemoMissionObjective Objective { get; }
        public int Target { get; }
        public int CrystalReward { get; }
        public int GoldReward { get; }
        public string StageId { get; }

        public int GetProgress(PlayerState state)
        {
            if (state == null)
            {
                return 0;
            }

            switch (Objective)
            {
                case DemoMissionObjective.DrawCharacters:
                    return state.TotalDraws;
                case DemoMissionObjective.OwnCharacters:
                    return state.OwnedCharacters.Count;
                case DemoMissionObjective.WinBattles:
                    return state.BattleWins;
                case DemoMissionObjective.ClearStage:
                    return state.IsStageCleared(StageId) ? Target : 0;
                default:
                    return 0;
            }
        }

        public bool IsComplete(PlayerState state)
        {
            return GetProgress(state) >= Target;
        }
    }

    public static class DemoMissionCatalog
    {
        private static readonly DemoMissionDefinition[] Definitions =
        {
            new DemoMissionDefinition(
                "mission_first_signal",
                "First Signal",
                "Complete one recruitment.",
                DemoMissionObjective.DrawCharacters,
                1,
                120,
                0),
            new DemoMissionDefinition(
                "mission_growing_roster",
                "Growing Roster",
                "Own six different characters.",
                DemoMissionObjective.OwnCharacters,
                6,
                0,
                400),
            new DemoMissionDefinition(
                "mission_first_victory",
                "First Victory",
                "Win one stage battle.",
                DemoMissionObjective.WinBattles,
                1,
                160,
                250),
            new DemoMissionDefinition(
                "mission_observatory_clear",
                "Observatory Secured",
                "Clear Chapter 1-3.",
                DemoMissionObjective.ClearStage,
                1,
                300,
                800,
                "stage_1_3")
        };

        public static IReadOnlyList<DemoMissionDefinition> All => Definitions;

        public static DemoMissionDefinition Get(string missionId)
        {
            for (int index = 0; index < Definitions.Length; index++)
            {
                if (string.Equals(Definitions[index].Id, missionId, StringComparison.Ordinal))
                {
                    return Definitions[index];
                }
            }

            return null;
        }

        public static int CountClaimable(PlayerState state)
        {
            int count = 0;
            for (int index = 0; index < Definitions.Length; index++)
            {
                DemoMissionDefinition definition = Definitions[index];
                if (definition.IsComplete(state) && !state.IsMissionClaimed(definition.Id))
                {
                    count++;
                }
            }

            return count;
        }
    }

    public sealed class DemoInventoryDefinition
    {
        public DemoInventoryDefinition(string id, string name, string category, string description)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Category = category ?? string.Empty;
            Description = description ?? string.Empty;
        }

        public string Id { get; }
        public string Name { get; }
        public string Category { get; }
        public string Description { get; }
    }

    public static class DemoInventoryCatalog
    {
        private static readonly DemoInventoryDefinition[] Definitions =
        {
            new DemoInventoryDefinition(
                "standard_ticket",
                "Signal Ticket",
                "RECRUITMENT",
                "A local-demo recruitment voucher reserved for future banner rules."),
            new DemoInventoryDefinition(
                "echo_gel",
                "Echo Gel",
                "MATERIAL",
                "Condensed field residue awarded by stage battles."),
            new DemoInventoryDefinition(
                "void_fragment",
                "Void Fragment",
                "RARE MATERIAL",
                "A stable fragment recovered from the Abyssal Observatory."),
            new DemoInventoryDefinition(
                "universal_shard",
                "Universal Shard",
                "CHARACTER SHARD",
                "Duplicate-signal residue for a future awakening system.")
        };

        public static IReadOnlyList<DemoInventoryDefinition> All => Definitions;

        public static DemoInventoryDefinition Get(string itemId)
        {
            for (int index = 0; index < Definitions.Length; index++)
            {
                if (string.Equals(Definitions[index].Id, itemId, StringComparison.Ordinal))
                {
                    return Definitions[index];
                }
            }

            return null;
        }
    }
}
