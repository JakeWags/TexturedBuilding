using ConfigLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace TexturedBuilding
{
    public class TexturedBuildingModSystem : ModSystem
    {
        public TexturedBuildingSettings Settings { get; set; } = new();
        public bool RandomModeEnabled { get; set; } = false;

        private ICoreClientAPI? clientApi;
        private PlacementMode? currentMode;

        public override void Start(ICoreAPI api)
        {
            if (api.ModLoader.IsModEnabled("configlib"))
            {
                SubscribeToConfigChange(api);
            }
        }

        private void SubscribeToConfigChange(ICoreAPI api)
        {
            ConfigLibModSystem system = api.ModLoader.GetModSystem<ConfigLibModSystem>();

            system.SettingChanged += (domain, config, setting) =>
            {
                if (domain != "texturedbuilding") return;

                setting.AssignSettingValue(Settings);

                if (Settings.DebugMode && clientApi != null)
                {
                    clientApi.Logger.Notification($"[TB] Setting changed - AllowClay: {Settings.AllowClay}, AllowFood: {Settings.AllowFood}");
                }
            };

            system.ConfigsLoaded += () =>
            {
                system.GetConfig("texturedbuilding")?.AssignSettingsValues(Settings);

                if (clientApi != null)
                {
                    clientApi.Logger.Notification($"[TB] Settings loaded - AllowClay: {Settings.AllowClay}, AllowFood: {Settings.AllowFood}");
                }
            };
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            clientApi = api;

            // Initialize with RandomMode as the default
            currentMode = new RandomMode(api);

            api.Input.RegisterHotKey("texturedbuilding-toggle", "Textured Building: Toggle Mode", GlKeys.R, HotkeyType.CharacterControls);
            api.Input.SetHotKeyHandler("texturedbuilding-toggle", OnToggleKey);
            api.Event.MouseDown += OnMouseDown;

            RandomModeEnabled = false;
        }

        private void OnMouseDown(MouseEvent e)
        {
            if (e.Button != EnumMouseButton.Right) return;
            if (clientApi == null || clientApi.IsGamePaused) return;
            if (!RandomModeEnabled) return;
            if (currentMode == null) return;

            IClientPlayer player = clientApi.World.Player;
            ItemSlot heldSlot = player.InventoryManager.ActiveHotbarSlot;

            if (heldSlot.Empty) return;

            if (!currentMode.IsItemAllowed(heldSlot))
            {
                if (Settings.DebugMode)
                    clientApi.Logger.Notification($"[TB] Ignored randomization. Holding excluded item: {heldSlot.Itemstack.Collectible.Code}");
                return;
            }

            if (player.CurrentBlockSelection == null) return;

            int newSlotIndex = currentMode.GetPlacementSlot();

            if (newSlotIndex != -1)
            {
                player.InventoryManager.ActiveHotbarSlotNumber = newSlotIndex;
                if (Settings.DebugMode)
                    clientApi.Logger.Notification($"[TB] Swapped to slot {newSlotIndex}");
            }
        }

        private bool OnToggleKey(KeyCombination t1)
        {
            if (clientApi == null) return false;

            RandomModeEnabled = !RandomModeEnabled;
            string status = RandomModeEnabled ? "ON" : "OFF";
            clientApi.ShowChatMessage($"Random Placement Mode: {status}");
            return true;
        }

        public override void Dispose()
        {
            if (clientApi != null)
            {
                clientApi.Event.MouseDown -= OnMouseDown;
            }
            base.Dispose();
        }
    }
}