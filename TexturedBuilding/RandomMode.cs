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

        public int GetRandomizedHotbarSlot()
        {
            IClientPlayer player = capi.World.Player;

            // List to hold indices of slots that contain blocks
            List<int> validSlotIndices = new List<int>();

            // Iterate through the standard hotbar (slots 0-9)
            for (int i = 0; i < 10; i++)
            {
                IInventory hotbar = player.InventoryManager.GetHotbarInventory();
                ItemSlot checkSlot = hotbar[i];

                // Check if the slot is not empty AND contains a Block (not an Item like a tool/food)
                if (!checkSlot.Empty && checkSlot.Itemstack.Class == EnumItemClass.Block)
                {
                    validSlotIndices.Add(i);
                }
            }

            // If we found valid blocks, pick one at random
            if (validSlotIndices.Count > 0)
            {
                int randomIndex = rand.Next(validSlotIndices.Count);
                return validSlotIndices[randomIndex];
            }

            return -1;
        }
    }
}