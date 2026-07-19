using System;
using System.Linq;

namespace TexturedBuilding
{
    // Stores per-slot configuration for hotbar randomization
    public class HotbarSlotSettings
    {
        // Array of 10 slot configurations (one per hotbar slot)
        public SlotConfig[] Slots { get; set; }

        public HotbarSlotSettings()
        {
            // Initialize all slots as enabled with weight 1
            Slots = new SlotConfig[10];
            for (int i = 0; i < 10; i++)
            {
                Slots[i] = new SlotConfig
                {
                    Enabled = true,
                    Weight = 1
                };
            }
        }

        // Get total weight of enabled slots
        public int GetTotalWeight()
        {
            return Slots.Where(s => s.Enabled).Sum(s => s.Weight);
        }

        // Get enabled slot indices with their weights
        public (int slotIndex, int weight)[] GetEnabledSlots()
        {
            return Slots
                .Select((config, index) => (index, config))
                .Where(pair => pair.config.Enabled)
                .Select(pair => (pair.index, pair.config.Weight))
                .ToArray();
        }
    }

    public class SlotConfig
    {
        public bool Enabled { get; set; }
        public int Weight { get; set; } // 1-10 scale

        public SlotConfig()
        {
            Enabled = true;
            Weight = 1;
        }
    }
}