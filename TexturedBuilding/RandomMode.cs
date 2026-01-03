using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

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

        // Converts a wildcard pattern (with * for any characters) to a regex pattern
        private string WildcardToRegex(string pattern)
        {
            // Escape special regex characters except *
            string escaped = Regex.Escape(pattern);
            // Replace escaped \* with .* for regex matching
            return "^" + escaped.Replace("\\*", ".*") + "$";
        }

        // Checks if a block code matches any pattern in a comma-separated list
        // Supports wildcards using *
        private bool MatchesAnyPattern(string blockCode, string patternList)
        {
            if (string.IsNullOrEmpty(patternList)) return false;

            string[] patterns = patternList.Split(',');

            foreach (string pattern in patterns)
            {
                string trimmedPattern = pattern.Trim();
                if (string.IsNullOrEmpty(trimmedPattern)) continue;

                // Check if pattern contains wildcard
                if (trimmedPattern.Contains("*"))
                {
                    try
                    {
                        string regexPattern = WildcardToRegex(trimmedPattern);
                        if (Regex.IsMatch(blockCode, regexPattern))
                        {
                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (modSystem.Settings.DebugMode)
                        {
                            capi.Logger.Warning($"[TB] Invalid pattern '{trimmedPattern}': {ex.Message}");
                        }
                    }
                }
                else
                {
                    // Exact match for patterns without wildcards
                    if (blockCode == trimmedPattern)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // Checks if a block is a food block (pies, meals, cheese, etc.)
        private bool IsFoodBlock(Block block)
        {
            // Check for meal containers (BlockMeal includes pies, bowls, etc.)
            if (block is BlockMeal)
            {
                if (modSystem.Settings.DebugMode)
                {
                    capi.Logger.Notification($"[TB] Detected as BlockMeal: {block.Code}");
                }
                return true;
            }

            // Check for cheese blocks
            string path = block.Code.Path;
            if (path.Contains("cheese"))
            {
                if (modSystem.Settings.DebugMode)
                {
                    capi.Logger.Notification($"[TB] Detected as cheese: {block.Code}");
                }
                return true;
            }

            // Fall back to checking NutritionProps (for items that have it)
            if (block.NutritionProps != null)
            {
                if (modSystem.Settings.DebugMode)
                {
                    capi.Logger.Notification($"[TB] Detected as food via NutritionProps: {block.Code}");
                }
                return true;
            }

            return false;
        }

        // Checks if a block is a storage block entity (chests, vessels, etc.) but NOT food
        private bool IsStorageBlockEntity(Block block)
        {
            if (block.EntityClass == null) return false;

            string entityClass = block.EntityClass.ToLowerInvariant();
            string path = block.Code.Path.ToLowerInvariant();

            // Common storage block entities
            if (entityClass.Contains("chest") ||
                entityClass.Contains("container") ||
                entityClass.Contains("barrel") ||
                entityClass.Contains("vessel") ||
                path.Contains("chest") ||
                path.Contains("crate") ||
                path.Contains("storagevessel"))
            {
                return true;
            }

            return false;
        }

        public bool IsItemAllowed(ItemSlot slot)
        {
            if (slot.Empty) return false;
            if (slot.Itemstack.Class != EnumItemClass.Block) return false;

            Block block = slot.Itemstack.Block;
            string blockCode = block.Code.ToString();

            if (modSystem.Settings.DebugMode)
            {
                capi.Logger.Notification($"[TB] Checking item: {blockCode}");
            }

            // Blacklist ALWAYS takes priority - check first
            if (modSystem.Settings.Blacklist.Length > 0)
            {
                if (MatchesAnyPattern(blockCode, modSystem.Settings.Blacklist))
                {
                    if (modSystem.Settings.DebugMode)
                    {
                        capi.Logger.Notification($"[TB] Item blocked by blacklist: {blockCode}");
                    }
                    return false;
                }
            }

            // Whitelist logic depends on WhitelistOnly setting
            bool whitelistHasEntries = modSystem.Settings.Whitelist.Length > 0;
            bool inWhitelist = whitelistHasEntries && MatchesAnyPattern(blockCode, modSystem.Settings.Whitelist);

            if (whitelistHasEntries)
            {
                if (modSystem.Settings.WhitelistOnly)
                {
                    // STRICT MODE: Only items in whitelist are allowed
                    if (inWhitelist)
                    {
                        if (modSystem.Settings.DebugMode)
                        {
                            capi.Logger.Notification($"[TB] Item allowed by whitelist (strict mode): {blockCode}");
                        }
                        return true;
                    }
                    else
                    {
                        if (modSystem.Settings.DebugMode)
                        {
                            capi.Logger.Notification($"[TB] Item rejected - not in whitelist (strict mode): {blockCode}");
                        }
                        return false;
                    }
                }
                else
                {
                    // PERMISSIVE MODE: Whitelist items skip other filters
                    if (inWhitelist)
                    {
                        if (modSystem.Settings.DebugMode)
                        {
                            capi.Logger.Notification($"[TB] Item allowed by whitelist (permissive mode - skipping filters): {blockCode}");
                        }
                        return true;
                    }
                    // If not in whitelist, continue to regular filters below
                }
            }

            // Regular filter checks (only reached if not whitelisted in permissive mode, or no whitelist)

            // Food Check - uses IsFoodBlock helper which detects BlockMeal (pies, meals) and cheese
            if (!modSystem.Settings.AllowFood && IsFoodBlock(block))
            {
                if (modSystem.Settings.DebugMode)
                {
                    capi.Logger.Notification($"[TB] Item rejected - is food: {blockCode}");
                }
                return false;
            }

            // Block Entity Check - now specifically for storage/machines, NOT food
            if (!modSystem.Settings.AllowBlockEntities)
            {
                if (IsStorageBlockEntity(block))
                {
                    if (modSystem.Settings.DebugMode)
                    {
                        capi.Logger.Notification($"[TB] Item rejected - is storage block entity: {blockCode}");
                    }
                    return false;
                }

                // Catch-all for other block entities (signs, etc.)
                if (block.EntityClass != null && !IsFoodBlock(block))
                {
                    if (modSystem.Settings.DebugMode)
                    {
                        capi.Logger.Notification($"[TB] Item rejected - is block entity: {blockCode}");
                    }
                    return false;
                }
            }

            // Plant Check
            if (!modSystem.Settings.AllowPlants && block.BlockMaterial == EnumBlockMaterial.Plant)
            {
                if (modSystem.Settings.DebugMode)
                {
                    capi.Logger.Notification($"[TB] Item rejected - is plant: {blockCode}");
                }
                return false;
            }

            // Liquid Check
            if (!modSystem.Settings.AllowLiquids && block.BlockMaterial == EnumBlockMaterial.Liquid)
            {
                if (modSystem.Settings.DebugMode)
                {
                    capi.Logger.Notification($"[TB] Item rejected - is liquid: {blockCode}");
                }
                return false;
            }

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
                    if (modSystem.Settings.DebugMode)
                    {
                        capi.Logger.Notification($"[TB] Item rejected - is clay/pottery: {blockCode}");
                    }
                    return false;
                }
            }

            if (modSystem.Settings.DebugMode)
            {
                capi.Logger.Notification($"[TB] Item allowed - passed all filters: {blockCode}");
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
                capi.Logger.Notification($"[TB] WhitelistOnly: {modSystem.Settings.WhitelistOnly}");
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