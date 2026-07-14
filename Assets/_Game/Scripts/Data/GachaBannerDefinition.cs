using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenericGachaRPG
{
    [Serializable]
    public sealed class GachaPoolEntry
    {
        [SerializeField] private string characterId = string.Empty;
        [Min(0f), SerializeField] private float weight = 1f;

        public string CharacterId => characterId;
        public float Weight => weight;

        public GachaPoolEntry()
        {
        }

        public GachaPoolEntry(string id, float entryWeight)
        {
            characterId = id == null ? string.Empty : id.Trim();
            weight = Mathf.Max(0f, entryWeight);
        }
    }

    [CreateAssetMenu(fileName = "GachaBannerDefinition", menuName = "Generic Gacha RPG/Gacha Banner Definition")]
    public sealed class GachaBannerDefinition : ScriptableObject
    {
        [SerializeField] private string id = "standard_banner";
        [SerializeField] private string displayName = "Standard Summon";
        [TextArea, SerializeField] private string description = string.Empty;
        [Min(0), SerializeField] private int singleDrawCost = 100;
        [SerializeField] private List<GachaPoolEntry> entries = new List<GachaPoolEntry>();

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public int SingleDrawCost => singleDrawCost;
        public IReadOnlyList<GachaPoolEntry> Entries => entries;

        public float TotalWeight
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < entries.Count; i++)
                {
                    GachaPoolEntry entry = entries[i];
                    if (entry != null && entry.Weight > 0f)
                    {
                        total += entry.Weight;
                    }
                }

                return total;
            }
        }

        public void Configure(
            string bannerId,
            string bannerDisplayName,
            int drawCost,
            IEnumerable<GachaPoolEntry> poolEntries,
            string bannerDescription = "")
        {
            id = bannerId == null ? string.Empty : bannerId.Trim();
            displayName = string.IsNullOrWhiteSpace(bannerDisplayName) ? id : bannerDisplayName.Trim();
            description = bannerDescription ?? string.Empty;
            singleDrawCost = Mathf.Max(0, drawCost);
            entries = poolEntries == null
                ? new List<GachaPoolEntry>()
                : new List<GachaPoolEntry>(poolEntries);
        }

        private void OnValidate()
        {
            id = id == null ? string.Empty : id.Trim();
            displayName = string.IsNullOrWhiteSpace(displayName) ? id : displayName.Trim();
            singleDrawCost = Mathf.Max(0, singleDrawCost);
            if (entries == null)
            {
                entries = new List<GachaPoolEntry>();
            }
        }
    }
}
