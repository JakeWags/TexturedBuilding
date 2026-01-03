using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TexturedBuilding
{
    // Random placement mode - selects a random valid block from the hotbar
    public class RandomMode : PlacementMode
    {
        private readonly Random rand;

        public RandomMode(ICoreClientAPI api) : base(api)
        {
            this.rand = new Random();
        }

        // Returns a random hotbar slot containing a valid block, or -1 if none found
        public override int GetPlacementSlot()
        {
            IClientPlayer player = capi.World.Player;
            IInventory hotbar = player.InventoryManager.GetHotbarInventory();
            List<int> validSlotIndices = new List<int>();

            if (modSystem.Settings.DebugMode)
            {
                capi.Logger.Notification($"[TB] Whitelist: {modSystem.Settings.Whitelist}");
                capi.Logger.Notification($"[TB] WhitelistOnly: {modSystem.Settings.WhitelistOnly}");
                capi.Logger.Notification($"[TB] Blacklist: {modSystem.Settings.Blacklist}");
            }

            // Scan hotbar for valid items
            for (int i = 0; i < 10; i++)
            {
                ItemSlot checkSlot = hotbar[i];

                if (IsItemAllowed(checkSlot))
                {
                    validSlotIndices.Add(i);
                }
                else if (modSystem.Settings.DebugMode && !checkSlot.Empty)
                {
                    capi.Logger.Notification($"[TB] Slot {i} skipped: {checkSlot.Itemstack.Collectible.Code}");
                }
            }

            // Return a random valid slot, or -1 if none found
            if (validSlotIndices.Count > 0)
            {
                int randomIndex = rand.Next(validSlotIndices.Count);
                return validSlotIndices[randomIndex];
            }

            return -1;
        }
    }
}