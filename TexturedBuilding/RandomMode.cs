using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace TexturedBuilding
{
    // Random placement mode - selects a random valid block from the hotbar or entire inventory
    public class RandomMode : PlacementMode
    {
        private readonly Random rand;

        public RandomMode(ICoreClientAPI api) : base(api)
        {
            this.rand = new Random();
        }

        // Returns a random slot containing a valid block, or -1 if none found
        public override int GetPlacementSlot()
        {
            IClientPlayer player = capi.World.Player;
            List<InventorySlotInfo> validSlots = new List<InventorySlotInfo>();

            if (modSystem.Settings.DebugMode)
            {
                capi.Logger.Notification($"[TB] UseEntireInventory: {modSystem.Settings.UseEntireInventory}");
                capi.Logger.Notification($"[TB] Server has mod: {modSystem.ServerModAvailable}");
                capi.Logger.Notification($"[TB] Whitelist: {modSystem.Settings.Whitelist}");
                capi.Logger.Notification($"[TB] WhitelistOnly: {modSystem.Settings.WhitelistOnly}");
                capi.Logger.Notification($"[TB] Blacklist: {modSystem.Settings.Blacklist}");
            }

            // Check if UseEntireInventory is enabled AND server has the mod
            if (modSystem.Settings.UseEntireInventory)
            {
                if (!modSystem.ServerModAvailable)
                {
                    if (modSystem.Settings.DebugMode)
                    {
                        capi.Logger.Warning("[TB] UseEntireInventory requires server-side mod. Falling back to hotbar only.");
                    }
                    // Fall through to hotbar-only mode
                }
                else
                {
                    // Scan all inventories
                    ScanInventoryForValidItems(player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName), validSlots);
                    ScanInventoryForValidItems(player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName), validSlots);
                    ScanInventoryForValidItems(player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName), validSlots);

                    if (validSlots.Count > 0)
                    {
                        int randomIndex = rand.Next(validSlots.Count);
                        InventorySlotInfo selected = validSlots[randomIndex];
                        return SwapToHotbar(player, selected);
                    }

                    return -1;
                }
            }

            // Hotbar-only mode (default or fallback)
            IInventory hotbar = player.InventoryManager.GetHotbarInventory();
            for (int i = 0; i < 10; i++)
            {
                ItemSlot checkSlot = hotbar[i];
                if (IsItemAllowed(checkSlot))
                {
                    validSlots.Add(new InventorySlotInfo(hotbar, i));
                }
                else if (modSystem.Settings.DebugMode && !checkSlot.Empty)
                {
                    capi.Logger.Notification($"[TB] Slot {i} skipped: {checkSlot.Itemstack.Collectible.Code}");
                }
            }

            if (validSlots.Count > 0)
            {
                int randomIndex = rand.Next(validSlots.Count);
                return validSlots[randomIndex].SlotIndex;
            }

            return -1;
        }

        // Scans an inventory and adds valid items to the list
        private void ScanInventoryForValidItems(IInventory inventory, List<InventorySlotInfo> validSlots)
        {
            if (inventory == null) return;

            for (int i = 0; i < inventory.Count; i++)
            {
                ItemSlot checkSlot = inventory[i];
                if (IsItemAllowed(checkSlot))
                {
                    validSlots.Add(new InventorySlotInfo(inventory, i));

                    if (modSystem.Settings.DebugMode)
                    {
                        capi.Logger.Notification($"[TB] Found valid item in {inventory.ClassName}[{i}]: {checkSlot.Itemstack.Collectible.Code}");
                    }
                }
            }
        }

        // Swaps an item from any inventory into the active hotbar slot
        // Returns the hotbar slot number that now contains the item
        private int SwapToHotbar(IClientPlayer player, InventorySlotInfo sourceSlot)
        {
            IInventory hotbar = player.InventoryManager.GetHotbarInventory();
            int activeHotbarSlot = player.InventoryManager.ActiveHotbarSlotNumber;

            // If the source is already in the hotbar, just return that slot number
            if (sourceSlot.Inventory.ClassName == GlobalConstants.hotBarInvClassName)
            {
                return sourceSlot.SlotIndex;
            }

            ItemSlot source = sourceSlot.Inventory[sourceSlot.SlotIndex];
            ItemSlot target = hotbar[activeHotbarSlot];

            if (modSystem.Settings.DebugMode)
            {
                string sourceName = source.Empty ? "empty" : source.Itemstack.Collectible.Code.ToString();
                string targetName = target.Empty ? "empty" : target.Itemstack.Collectible.Code.ToString();
                capi.Logger.Notification($"[TB] Requesting swap: {sourceName} from {sourceSlot.Inventory.InventoryID}[{sourceSlot.SlotIndex}] with {targetName} in hotbar[{activeHotbarSlot}]");
            }

            // Send swap request to server
            modSystem.RequestInventorySwap(
                sourceSlot.Inventory.InventoryID,
                sourceSlot.SlotIndex,
                hotbar.InventoryID,
                activeHotbarSlot
            );

            return activeHotbarSlot;
        }

        // Helper class to track inventory slot locations
        private class InventorySlotInfo
        {
            public IInventory Inventory { get; }
            public int SlotIndex { get; }

            public InventorySlotInfo(IInventory inventory, int slotIndex)
            {
                Inventory = inventory;
                SlotIndex = slotIndex;
            }
        }
    }
}