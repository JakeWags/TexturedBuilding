using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

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
                capi.Logger.Notification($"[TB] AllowClay setting: {modSystem.Settings.AllowClay}");
                capi.Logger.Notification($"[TB] AllowFood setting: {modSystem.Settings.AllowFood}");
                capi.Logger.Notification($"[TB] AllowPlants setting: {modSystem.Settings.AllowPlants}");
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

                if (modSystem.Settings.DebugMode)
                {
                    capi.Logger.Notification($"[TB] Checking clay/pottery filter for item: {block.Code}");
                    capi.Logger.Notification($"[TB] Item path: {path}");
                }

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
                    if (modSystem.Settings.DebugMode)
                    {
                        capi.Logger.Notification($"[TB] Block filtered out by clay/pottery rule: {block.Code}");
                    }

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