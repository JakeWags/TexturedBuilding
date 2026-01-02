using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace TexturedBuilding
{
    public class TexturedBuildingModSystem : ModSystem
    {
        public static TBConfig Config { get; private set; }
        private ICoreClientAPI clientApi;
        private RandomMode randomMode;
        private const string ConfigName = "TexturedBuildingConfig.json";

        public override void Start(ICoreAPI api)
        {
            // Load Config
            try
            {
                Config = api.LoadModConfig<TBConfig>(ConfigName);
            }
            catch
            {
                Config = null;
            }

            if (Config == null)
            {
                Config = new TBConfig();
                api.StoreModConfig(Config, ConfigName);
            }
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            clientApi = api;
            randomMode = new RandomMode(api);

            // 1. Register Keybinding (R key default)
            api.Input.RegisterHotKey(
                "texturedbuilding-toggle",
                "Textured Building: Toggle Mode",
                GlKeys.R,
                HotkeyType.CharacterControls
            );
            api.Input.SetHotKeyHandler("texturedbuilding-toggle", OnToggleKey);

            // 2. Register the Mouse Event
            // FIX: Use api.Event.MouseDown instead of api.Input.InWorldMouseEvent
            api.Event.MouseDown += OnMouseDown;
        }

        private void OnMouseDown(MouseEvent e)
        {
            // We only care about Right Clicks (Building/Interaction)
            if (e.Button != EnumMouseButton.Right) return;

            // Only run if our mode is enabled
            if (Config == null || !Config.RandomModeEnabled) return;

            // Optional: Ensure we are looking at something in the world (prevents swapping when clicking UI)
            if (clientApi.World.Player.CurrentBlockSelection == null) return;

            // Get a random slot index
            int newSlotIndex = randomMode.GetRandomizedHotbarSlot();

            if (newSlotIndex != -1)
            {
                // Switch the active hotbar slot immediately
                clientApi.World.Player.InventoryManager.ActiveHotbarSlotNumber = newSlotIndex;

                // The game continues to process this click, now using the new item
            }
        }

        private bool OnToggleKey(KeyCombination t1)
        {
            if (Config == null) return false;

            Config.RandomModeEnabled = !Config.RandomModeEnabled;
            clientApi.ShowChatMessage("Random Mode: " + (Config.RandomModeEnabled ? "ON" : "OFF"));

            // Save the state so it persists across restarts
            clientApi.StoreModConfig(Config, ConfigName);

            return true;
        }

        // Cleanup events when the mod unloads/game closes
        public override void Dispose()
        {
            if (clientApi != null)
            {
                // FIX: Unsubscribe from the correct event
                clientApi.Event.MouseDown -= OnMouseDown;
            }
            base.Dispose();
        }
    }
}