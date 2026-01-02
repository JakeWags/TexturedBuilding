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

            api.Input.RegisterHotKey(
                "texturedbuilding-toggle",
                "Textured Building: Toggle Mode",
                GlKeys.R,
                HotkeyType.CharacterControls
            );
            api.Input.SetHotKeyHandler("texturedbuilding-toggle", OnToggleKey);

            api.Event.MouseDown += OnMouseDown;

            Config.RandomModeEnabled = false; // Start disabled
        }

        private void OnMouseDown(MouseEvent e)
        {
            // Basic checks
            if (e.Button != EnumMouseButton.Right) return;
            if (clientApi.IsGamePaused) return;
            if (!Config.RandomModeEnabled) return;
            if (Config == null) return;

            IClientPlayer player = clientApi.World.Player;
            ItemSlot heldSlot = player.InventoryManager.ActiveHotbarSlot;

            // Check if holding anything at all
            if (heldSlot.Empty) return;

            // If the item currently in hand is NOT allowed in the random rotation (e.g. Chest, Flower),
            // we assume the user wants to place THAT specific item. 
            // We exit here, letting the default game behavior take over.
            if (!randomMode.IsItemAllowed(heldSlot))
            {
                if (Config.DebugMode)
                    clientApi.Logger.Notification($"[TB] Ignored randomization. Holding excluded item: {heldSlot.Itemstack.Collectible.Code}");
                return;
            }

            // Ensure we are actually aiming at a block
            if (player.CurrentBlockSelection == null) return;

            // Perform the Swap
            int newSlotIndex = randomMode.GetRandomizedHotbarSlot();

            if (newSlotIndex != -1)
            {
                player.InventoryManager.ActiveHotbarSlotNumber = newSlotIndex;

                if (Config.DebugMode)
                    clientApi.Logger.Notification($"[TB] Swapped to slot {newSlotIndex}");
            }
        }

        private bool OnToggleKey(KeyCombination t1)
        {
            Config.RandomModeEnabled = !Config.RandomModeEnabled;
            string status = Config.RandomModeEnabled ? "ON" : "OFF";
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