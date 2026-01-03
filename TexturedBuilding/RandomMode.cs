using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace TexturedBuilding
{
    public class RandomMode
    {
        private readonly ICoreClientAPI capi;
        private readonly Random rand;
        private readonly TexturedBuildingModSystem modSystem;

        public RandomMode(ICoreClientAPI api)
        {
            this.capi = api;
            this.rand = new Random();
            this.modSystem = api.ModLoader.GetModSystem<TexturedBuildingModSystem>();
        }

        public bool IsItemAllowed(ItemSlot slot)
        {
            if (slot.Empty) return false;
            if (slot.Itemstack.Class != EnumItemClass.Block) return false;

            Block block = slot.Itemstack.Block;

            if (modSystem.Settings.DebugMode)
            {
                capi.Logger.Notification($"[TB] Checking item: {block.Code}");
            }

            // Blacklist
            if (modSystem.Settings.Blacklist.Length > 0)
            {
                String[] blacklist = modSystem.Settings.Blacklist.Split(','); // optimize this by saving the split string in Settings rather than doing it here

                if (blacklist.Contains(block.Code.ToString()))
                {
                    if (modSystem.Settings.DebugMode)
                    {
                        capi.Logger.Notification($"[TB] Item blocked by blacklist: {block.Code.Path}");
                    }
                    return false;
                }
            }

            // Whitelist
            if (modSystem.Settings.Whitelist.Length > 0)
            {
                String[] whitelist = modSystem.Settings.Whitelist.Split(','); // optimize this by saving the split string in Settings rather than doing it here

                if (whitelist.Contains(block.Code.ToString()))
                {
                    if (modSystem.Settings.DebugMode)
                    {
                        capi.Logger.Notification($"[TB] Item allowed by whitelist: {block.Code.Path}");
                    }
                    return true;
                }
            }

            // Food Check
            if (!modSystem.Settings.AllowFood && slot.Itemstack.Collectible.NutritionProps != null)
                return false;

            // Block Entity Check
            if (!modSystem.Settings.AllowBlockEntities && block.EntityClass != null)
                return false;

            // Plant Check
            if (!modSystem.Settings.AllowPlants && block.BlockMaterial == EnumBlockMaterial.Plant)
                return false;

            // Liquid Check
            if (!modSystem.Settings.AllowLiquids && block.BlockMaterial == EnumBlockMaterial.Liquid)
                return false;

            // Clay & Pottery Check
            if (!modSystem.Settings.AllowClay)
            {
                string path = block.Code.Path;

                if (string.IsNullOrEmpty(path))
                    return true;

                if (path.Contains("strawbedding"))
                    return true;

                if (path.Contains("rawclay"))
                    return true;

                if (path.Contains("raw") ||
                    path.Contains("fired") ||
                    path.Contains("crock") ||
                    path.Contains("bowl") ||
                    path.Contains("planter") ||
                    path.Contains("flowerpot") ||
                    path.Contains("storagevessel") ||
                    path.Contains("jug") ||
                    path.Contains("watering") ||
                    path.StartsWith("item-shingle") ||
                    path.Contains("mold"))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetRandomizedHotbarSlot()
        {
            IClientPlayer player = capi.World.Player;
            IInventory hotbar = player.InventoryManager.GetHotbarInventory();
            List<int> validSlotIndices = new List<int>();

            if (modSystem.Settings.DebugMode)
            {
                capi.Logger.Notification($"[TB] Whitelist: {modSystem.Settings.Whitelist}");
                capi.Logger.Notification($"[TB] Blacklist: {modSystem.Settings.Blacklist}");
            }

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

            if (validSlotIndices.Count > 0)
            {
                int randomIndex = rand.Next(validSlotIndices.Count);
                return validSlotIndices[randomIndex];
            }

            return -1;
        }
    }
}