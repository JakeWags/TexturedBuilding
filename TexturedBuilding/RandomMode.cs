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

        public RandomMode(ICoreClientAPI api)
        {
            this.capi = api;
            this.rand = new Random();
        }

        // Checks if a specific item slot is allowed to be used in Random Mode
        public bool IsItemAllowed(ItemSlot slot)
        {
            if (slot.Empty) return false;
            if (slot.Itemstack.Class != EnumItemClass.Block) return false;

            var config = TexturedBuildingModSystem.Config;
            if (config == null) return true;

            Block block = slot.Itemstack.Block;

            // Food Check
            if (!config.AllowFood && slot.Itemstack.Collectible.NutritionProps != null)
                return false;

            // Block Entity Check (Chests, Signs, etc.)
            if (!config.AllowBlockEntities && block.EntityClass != null)
                return false;

            // Plant Check
            if (!config.AllowPlants && block.BlockMaterial == EnumBlockMaterial.Plant)
                return false;

            // Liquid Check
            if (!config.AllowLiquids && block.BlockMaterial == EnumBlockMaterial.Liquid)
                return false;

            // Clay & Pottery Check
            // Filters out "rawclay" and specific pottery keywords.
            // We do NOT filter "bricks" or "shingles" so users can still build with those.
            if (!config.AllowClay)
            {
                string path = block.Code.Path;

                if (string.IsNullOrEmpty(path))
                    return true;

                if (config.DebugMode)
                {
                    capi.Logger.Notification($"[TB] Checking clay/pottery filter for item: {block.Code}");
                    capi.Logger.Notification($"[TB] Item path: {path}");
                }

                // Strewn Straw contains 'raw' so we explicitly allow it here
                if (path.Contains("strawbedding"))
                    return true;

                // rawclay BLOCK should be allowed
                if (path.Contains("rawclay"))
                    return true;

                if (path.Contains("raw") ||
                    path.Contains("fired") ||
                    // explicit blocks
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
                    if (config.DebugMode)
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

            // Find all valid candidates
            for (int i = 0; i < 10; i++)
            {
                ItemSlot checkSlot = hotbar[i];

                if (IsItemAllowed(checkSlot))
                {
                    validSlotIndices.Add(i);
                }
                else if (TexturedBuildingModSystem.Config.DebugMode && !checkSlot.Empty)
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