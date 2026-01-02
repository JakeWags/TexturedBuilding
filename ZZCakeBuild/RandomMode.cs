using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace RNGBuilder
{
    public class RNGBuilderMod : ModSystem
    {
        // Only load this mod on the client side
        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        private ICoreClientAPI capi;
        private bool isRandomModeActive = false;
        private Random random = new Random();

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;

            // 1. Register the Hotkey (Default: Ctrl + B)
            // arguments: name, description, default key, type (modifier keys allowed)
            capi.Input.RegisterHotKey("togglernbuilder", "Toggle Random Placement", GlKeys.B, HotkeyType.GUIOrOtherControls, false, true, false);

            // 2. Add the handler for when the key is pressed
            capi.Input.SetHotKeyHandler("togglernbuilder", OnToggleKey);

            // 3. Listen for Mouse Events (Right Click)
            capi.Input.InWorldMouseEvent += OnMouseDown;
        }

        private bool OnToggleKey(KeyCombination comb)
        {
            isRandomModeActive = !isRandomModeActive;

            // Give the user visual feedback
            if (isRandomModeActive)
            {
                capi.ShowChatMessage("Random Placement Mode: [ON]");
                // Optional: Play a small sound
                capi.World.PlaySoundAt(new AssetLocation("game:sounds/effect/tick"), capi.World.Player.Entity, null, true, 32, 1f);
            }
            else
            {
                capi.ShowChatMessage("Random Placement Mode: [OFF]");
            }

            return true; // Consume the event
        }

        private void OnMouseDown(MouseEvent e, float dt)
        {
            // We only care if:
            // 1. The mod is active
            // 2. It is a Right Mouse Button click (Interaction/Placing)
            // 3. The mouse event is "Down" (pressed), not released
            if (!isRandomModeActive || e.Button != EnumMouseButton.Right) return;

            // Safety check: Ensure player and inventory exist
            var player = capi.World.Player;
            if (player == null || player.InventoryManager == null) return;

            // We only want to switch blocks if we are actually looking at something to place ON
            // Otherwise, we might switch items just by right-clicking the air to eat food
            if (player.CurrentBlockSelection == null) return;

            // Get the hotbar inventory
            IInventory hotbar = player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
            if (hotbar == null) return;

            // FIND VALID SLOTS
            // We want slots that:
            // 1. Are not empty
            // 2. Contain a Block (not a tool, not food, etc.)
            List<int> validSlotIndices = new List<int>();

            for (int i = 0; i < 10; i++) // Hotbar is slots 0-9
            {
                ItemSlot slot = hotbar[i];
                if (!slot.Empty && slot.Itemstack.Class == EnumItemClass.Block)
                {
                    validSlotIndices.Add(i);
                }
            }

            // If we have valid blocks to choose from...
            if (validSlotIndices.Count > 0)
            {
                // Pick a random index from our list of valid slots
                int randomListIndex = random.Next(validSlotIndices.Count);
                int newHotbarSlotIndex = validSlotIndices[randomListIndex];

                // Perform the switch immediately
                player.InventoryManager.ActiveHotbarSlotIndex = newHotbarSlotIndex;

                // Note: The game engine processes the "Place" action immediately after this event.
                // Since we swapped the active slot, the engine will use the new item.
            }
        }
    }
}