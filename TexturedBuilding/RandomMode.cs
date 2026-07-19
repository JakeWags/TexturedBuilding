using System;
using System.Collections.Generic;
using System.Linq;
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
            List<WeightedSlotInfo> validSlots = new List<WeightedSlotInfo>();

            if (modSystem.Settings.DebugMode)
            {
                capi.Logger.Notification($"[TB] UseEntireInventory: {modSystem.Settings.UseEntireInventory}");
                capi.Logger.Notification($"[TB] Server has mod: {modSystem.ServerModAvailable}");
            }

            // Get enabled slot configurations
            var enabledSlots = modSystem.HotbarSettings.GetEnabledSlots();

            if (enabledSlots.Length == 0)
            {
                if (modSystem.Settings.DebugMode)
                {
                    capi.Logger.Warning("[TB] No hotbar slots enabled in configuration");
                }
                return -1;
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
                    // Scan all inventories, but only for enabled hotbar slots
                    ScanInventoryForValidItems(player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName), validSlots, enabledSlots);
                    ScanInventoryForValidItems(player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName), validSlots, null);
                    ScanInventoryForValidItems(player.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName), validSlots, null);

                    if (validSlots.Count > 0)
                    {
                        int selectedIndex = WeightedRandom(validSlots);
                        WeightedSlotInfo selected = validSlots[selectedIndex];
                        return SwapToHotbar(player, selected.SlotInfo);
                    }

                    return -1;
                }
            }

            // Hotbar-only mode (default or fallback)
            IInventory hotbar = player.InventoryManager.GetHotbarInventory();

            // Only check enabled slots
            foreach (var (slotIndex, weight) in enabledSlots)
            {
                ItemSlot checkSlot = hotbar[slotIndex];

                if (IsItemAllowed(checkSlot))
                {
                    validSlots.Add(new WeightedSlotInfo(
                        new InventorySlotInfo(hotbar, slotIndex),
                        weight
                    ));
                }
                else if (modSystem.Settings.DebugMode)
                {
                    string itemName = checkSlot.Empty ? "empty" : checkSlot.Itemstack.Collectible.Code.ToString();
                    capi.Logger.Notification($"[TB] Slot {slotIndex} skipped: {itemName}");
                }
            }

            if (validSlots.Count > 0)
            {
                int selectedIndex = WeightedRandom(validSlots);
                return validSlots[selectedIndex].SlotInfo.SlotIndex;
            }

            return -1;
        }

        private int WeightedRandom(List<WeightedSlotInfo> slots)
        {
            int totalWeight = slots.Sum(s => s.Weight);
            int randomValue = rand.Next(totalWeight);
            int cumulative = 0;

            for (int i = 0; i < slots.Count; i++)
            {
                cumulative += slots[i].Weight;
                if (randomValue < cumulative)
                {
                    return i;
                }
            }

            return slots.Count - 1; // Fallback
        }

        // Scans an inventory and adds valid items to the list
        private void ScanInventoryForValidItems(
            IInventory inventory,
            List<WeightedSlotInfo> validSlots,
            (int slotIndex, int weight)[]? enabledSlots)
        {
            if (inventory == null) return;

            // If this is the hotbar and we have enabled slot restrictions, only check those slots
            if (enabledSlots != null && inventory.ClassName == GlobalConstants.hotBarInvClassName)
            {
                foreach (var (slotIndex, weight) in enabledSlots)
                {
                    if (slotIndex >= inventory.Count) continue;

                    ItemSlot checkSlot = inventory[slotIndex];
                    if (checkSlot.Empty) continue;

                    if (IsItemAllowed(checkSlot))
                    {
                        validSlots.Add(new WeightedSlotInfo(
                            new InventorySlotInfo(inventory, slotIndex),
                            weight
                        ));

                        if (modSystem.Settings.DebugMode)
                        {
                            capi.Logger.Notification($"[TB] Found valid item in {inventory.ClassName}[{slotIndex}]: {checkSlot.Itemstack.Collectible.Code} (weight: {weight})");
                        }
                    }
                }
            }
            else
            {
                // For non-hotbar inventories, scan all slots with default weight
                for (int i = 0; i < inventory.Count; i++)
                {
                    ItemSlot checkSlot = inventory[i];
                    if (checkSlot.Empty) continue;

                    if (IsItemAllowed(checkSlot))
                    {
                        validSlots.Add(new WeightedSlotInfo(
                            new InventorySlotInfo(inventory, i),
                            1 // Default weight for non-hotbar items
                        ));

                        if (modSystem.Settings.DebugMode)
                        {
                            capi.Logger.Notification($"[TB] Found valid item in {inventory.ClassName}[{i}]: {checkSlot.Itemstack.Collectible.Code}");
                        }
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

        private class WeightedSlotInfo
        {
            public InventorySlotInfo SlotInfo { get; }
            public int Weight { get; }

            public WeightedSlotInfo(InventorySlotInfo slotInfo, int weight)
            {
                SlotInfo = slotInfo;
                Weight = weight;
            }
        }
    }
}