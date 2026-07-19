using ConfigLib;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace TexturedBuilding
{
    public class TexturedBuildingModSystem : ModSystem
    {
        public TexturedBuildingSettings Settings { get; set; } = new();
        public bool RandomModeEnabled { get; set; } = false;
        public bool ServerModAvailable { get; private set; } = false;

        private ICoreClientAPI? clientApi;
        private PlacementMode? currentMode;
        private IClientNetworkChannel? networkChannel;
        private const string NetworkChannelName = "texturedbuilding";

        // Track last placement for click-and-hold
        private BlockPos? lastPlacedPos;
        private long lastPlacementTime;
        private bool isRightMouseDown = false;
        private long gameTickListenerId;

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

                if (clientApi != null)
                {
                    clientApi.Logger.Notification($"[TB] Setting changed - AllowClay: {Settings.AllowClay}, AllowFood: {Settings.AllowFood}, UseEntireInventory: {Settings.UseEntireInventory}, ClickAndHold: {Settings.EnableClickAndHold}");
                }
            };

            system.ConfigsLoaded += () =>
            {
                system.GetConfig("texturedbuilding")?.AssignSettingsValues(Settings);

                if (clientApi != null)
                {
                    clientApi.Logger.Notification($"[TB] Settings loaded - AllowClay: {Settings.AllowClay}, AllowFood: {Settings.AllowFood}, UseEntireInventory: {Settings.UseEntireInventory}, ClickAndHold: {Settings.EnableClickAndHold}");
                }
            };
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            clientApi = api;

            // Initialize with RandomMode as the default
            currentMode = new RandomMode(api);

            // Set up network channel for server communication
            networkChannel = api.Network.RegisterChannel(NetworkChannelName)
                .RegisterMessageType<InventorySwapRequest>()
                .RegisterMessageType<ServerModAvailableMessage>()
                .SetMessageHandler<ServerModAvailableMessage>(OnServerModAvailable);

            // Delay server mod check to ensure connection is established
            api.Event.PlayerJoin += (player) =>
            {
                api.Event.RegisterCallback((dt) =>
                {
                    CheckServerModAvailability();
                }, 1000); // Wait 1 second after join
            };

            // Register debug command using ChatCommands API
            api.ChatCommands.Create("tbstatus")
                .WithDescription("Check TexturedBuilding mod status")
                .HandleWith((args) =>
                {
                    return TextCommandResult.Success($"TexturedBuilding Status:\n" +
                        $"  Server Mod Available: {ServerModAvailable}\n" +
                        $"  Channel Connected: {networkChannel?.Connected ?? false}\n" +
                        $"  Is Single Player: {api.IsSinglePlayer}\n" +
                        $"  UseEntireInventory: {Settings.UseEntireInventory}\n" +
                        $"  Random Mode Enabled: {RandomModeEnabled}\n" +
                        $"  Click and Hold: {Settings.EnableClickAndHold}\n" +
                        $"  Right Mouse Down: {isRightMouseDown}\n" +
                        $"  Debug Mode: {Settings.DebugMode}");
                });

            // Register hotkeys
            api.Input.RegisterHotKey("texturedbuilding-toggle", "Textured Building: Toggle Random Mode", GlKeys.R, HotkeyType.CharacterControls);
            api.Input.SetHotKeyHandler("texturedbuilding-toggle", OnToggleRandomMode);

            api.Input.RegisterHotKey("texturedbuilding-inventory", "Textured Building: Toggle Inventory Mode", GlKeys.T, HotkeyType.CharacterControls, ctrlPressed: true);
            api.Input.SetHotKeyHandler("texturedbuilding-inventory", OnToggleInventoryMode);

            api.Event.MouseDown += OnMouseDown;
            api.Event.MouseUp += OnMouseUp;

            // Register game tick listener for click-and-hold detection
            gameTickListenerId = api.Event.RegisterGameTickListener(OnGameTick, 50); // Check every 50ms

            RandomModeEnabled = false;
            lastPlacedPos = null;
            lastPlacementTime = 0;

            clientApi.Logger.Notification("[TexturedBuilding] Client system loaded");
        }

        private void CheckServerModAvailability()
        {
            if (clientApi == null) return;

            // In single player, we control both client and server
            if (clientApi.IsSinglePlayer)
            {
                ServerModAvailable = clientApi.ModLoader.IsModEnabled("texturedbuilding");

                if (Settings.DebugMode)
                {
                    clientApi.Logger.Notification($"[TB] SinglePlayer - Server mod available: {ServerModAvailable}");
                }
            }
            else
            {
                // In multiplayer, check if channel is connected
                bool channelConnected = networkChannel != null && networkChannel.Connected;

                if (Settings.DebugMode)
                {
                    clientApi.Logger.Notification($"[TB] Multiplayer - Channel connected: {channelConnected}");
                }

                if (channelConnected)
                {
                    // Channel is connected, server has the mod
                    ServerModAvailable = true;

                    if (Settings.DebugMode)
                    {
                        clientApi.Logger.Notification("[TB] Server has mod detected via connected channel");
                    }
                }
                else
                {
                    // Channel not connected means server doesn't have the mod
                    ServerModAvailable = false;

                    if (Settings.DebugMode)
                    {
                        clientApi.Logger.Notification("[TB] Server does not have mod (channel not connected)");
                    }
                }
            }
        }

        private void OnServerModAvailable(ServerModAvailableMessage message)
        {
            ServerModAvailable = message.Available;

            if (Settings.DebugMode && clientApi != null)
            {
                clientApi.Logger.Notification($"[TB] Received server mod status: {ServerModAvailable}");
            }
        }

        // Send inventory swap request to server
        public void RequestInventorySwap(string sourceInventoryId, int sourceSlotId, string targetInventoryId, int targetSlotId)
        {
            if (networkChannel == null || !networkChannel.Connected)
            {
                if (Settings.DebugMode && clientApi != null)
                {
                    clientApi.Logger.Warning("[TB] Cannot request inventory swap - channel not connected");
                }
                return;
            }

            if (!ServerModAvailable)
            {
                if (Settings.DebugMode && clientApi != null)
                {
                    clientApi.Logger.Warning("[TB] Cannot request inventory swap - server mod not available");
                }
                return;
            }

            var request = new InventorySwapRequest
            {
                SourceInventoryId = sourceInventoryId,
                SourceSlotId = sourceSlotId,
                TargetInventoryId = targetInventoryId,
                TargetSlotId = targetSlotId
            };

            networkChannel.SendPacket(request);

            if (Settings.DebugMode && clientApi != null)
            {
                clientApi.Logger.Notification($"[TB] Sent swap request to server");
            }
        }

        private void OnGameTick(float dt)
        {
            // Only run click-and-hold logic if enabled and conditions are met
            if (!Settings.EnableClickAndHold) return;
            if (clientApi == null || clientApi.IsGamePaused) return;
            if (!RandomModeEnabled) return;
            if (!isRightMouseDown) return;
            if (currentMode == null) return;

            IClientPlayer player = clientApi.World.Player;

            // Check if player is targeting a block
            if (player.CurrentBlockSelection == null) return;

            BlockPos currentPos = player.CurrentBlockSelection.Position;

            // Check if we're looking at a different position than last placement
            if (lastPlacedPos != null && currentPos.Equals(lastPlacedPos))
            {
                // Still looking at the same block, don't randomize yet
                return;
            }

            // If we have no last placement, or we're targeting a different block, randomize
            // This means every time the player targets a new block position, we randomize
            if (lastPlacedPos == null || !currentPos.Equals(lastPlacedPos))
            {
                if (Settings.DebugMode)
                {
                    long currentTime = clientApi.World.ElapsedMilliseconds;
                    long timeSince = lastPlacementTime > 0 ? currentTime - lastPlacementTime : 0;
                    clientApi.Logger.Notification($"[TB] Click-and-hold: New block target. Time since last: {timeSince}ms. Pos: {currentPos}");
                }

                PerformRandomization();

                // Reuse BlockPos object to reduce allocations
                if (lastPlacedPos == null)
                {
                    lastPlacedPos = currentPos.Copy();
                }
                else
                {
                    lastPlacedPos.Set(currentPos);
                }

                lastPlacementTime = clientApi.World.ElapsedMilliseconds;
            }
        }

        private void OnMouseDown(MouseEvent e)
        {
            if (e.Button != EnumMouseButton.Right) return;

            isRightMouseDown = true;

            if (clientApi == null || clientApi.IsGamePaused) return;
            if (!RandomModeEnabled) return;

            if (Settings.DebugMode)
            {
                clientApi.Logger.Notification($"[TB] Right mouse down - ClickAndHold: {Settings.EnableClickAndHold}");
            }

            // Perform randomization on initial click
            PerformRandomization();

            // Record this as the first placement
            IClientPlayer player = clientApi.World.Player;
            if (player.CurrentBlockSelection != null)
            {
                // Reuse or create BlockPos object
                BlockPos currentPos = player.CurrentBlockSelection.Position;
                if (lastPlacedPos == null)
                {
                    lastPlacedPos = currentPos.Copy();
                }
                else
                {
                    lastPlacedPos.Set(currentPos);
                }

                lastPlacementTime = clientApi.World.ElapsedMilliseconds;

                if (Settings.DebugMode)
                {
                    clientApi.Logger.Notification($"[TB] Initial placement at {lastPlacedPos}");
                }
            }
        }

        private void OnMouseUp(MouseEvent e)
        {
            if (e.Button != EnumMouseButton.Right) return;

            isRightMouseDown = false;
            lastPlacedPos = null;
            lastPlacementTime = 0;

            if (Settings.DebugMode && clientApi != null)
            {
                clientApi.Logger.Notification("[TB] Right mouse up - reset tracking");
            }
        }

        private void PerformRandomization()
        {
            if (currentMode == null || clientApi == null) return;

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
                    clientApi.Logger.Notification($"[TB] Randomized to slot {newSlotIndex}");
            }
            else if (Settings.DebugMode)
            {
                clientApi.Logger.Notification("[TB] No valid slot found for randomization");
            }
        }

        private bool OnToggleRandomMode(KeyCombination t1)
        {
            if (clientApi == null) return false;

            RandomModeEnabled = !RandomModeEnabled;
            string status = RandomModeEnabled ? "ON" : "OFF";

            // Show current mode status
            if (RandomModeEnabled)
            {
                string holdStatus = Settings.EnableClickAndHold ? "Click & Hold" : "Single Click";
                if (Settings.UseEntireInventory && ServerModAvailable)
                {
                    clientApi.ShowChatMessage($"Random Mode: {status} (Full Inventory, {holdStatus})");
                }
                else
                {
                    clientApi.ShowChatMessage($"Random Mode: {status} (Hotbar Only, {holdStatus})");
                }
            }
            else
            {
                clientApi.ShowChatMessage($"Random Mode: {status}");
            }

            return true;
        }

        private bool OnToggleInventoryMode(KeyCombination t1)
        {
            if (clientApi == null) return false;

            Settings.UseEntireInventory = !Settings.UseEntireInventory;

            string status = Settings.UseEntireInventory ? "ON" : "OFF";

            if (Settings.UseEntireInventory && !ServerModAvailable)
            {
                clientApi.ShowChatMessage($"Inventory Mode: {status} (Server mod required - falling back to Hotbar Only)");
            }
            else
            {
                clientApi.ShowChatMessage($"Inventory Mode: {status}");
            }

            return true;
        }

        public override void Dispose()
        {
            if (clientApi != null)
            {
                clientApi.Event.MouseDown -= OnMouseDown;
                clientApi.Event.MouseUp -= OnMouseUp;
                clientApi.Event.UnregisterGameTickListener(gameTickListenerId);
            }
            base.Dispose();
        }
    }
}