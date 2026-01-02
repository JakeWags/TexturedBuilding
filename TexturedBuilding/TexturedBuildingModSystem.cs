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

            Config.RandomModeEnabled = false;
        }

        private void OnMouseDown(MouseEvent e)
        {
            if (e.Button != EnumMouseButton.Right) return;
            if (Config == null || !Config.RandomModeEnabled) return;

            IClientPlayer player = clientApi.World.Player;

            // Check if the player is currently holding a BLOCK
            ItemSlot heldSlot = player.InventoryManager.ActiveHotbarSlot;
            if (heldSlot.Empty || heldSlot.Itemstack.Class != EnumItemClass.Block)
            {
                return;
            }

            // Optional: Check if we are actually aiming at a block to place against
            if (player.CurrentBlockSelection == null) return;

            int newSlotIndex = randomMode.GetRandomizedHotbarSlot();

            if (newSlotIndex != -1)
            {
                player.InventoryManager.ActiveHotbarSlotNumber = newSlotIndex;
            }
        }

        private bool OnToggleKey(KeyCombination t1)
        {
            if (Config == null) return false;

            Config.RandomModeEnabled = !Config.RandomModeEnabled;
            clientApi.ShowChatMessage("Random Mode: " + (Config.RandomModeEnabled ? "ON" : "OFF"));
            clientApi.StoreModConfig(Config, ConfigName);

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