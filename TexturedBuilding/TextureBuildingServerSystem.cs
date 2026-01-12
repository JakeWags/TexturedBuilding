using ProtoBuf;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace TexturedBuilding
{
    // Server-side system to handle inventory swaps
    // This enables the UseEntireInventory feature when mod is on both client and server
    public class TexturedBuildingServerSystem : ModSystem
    {
        private ICoreServerAPI? serverApi;
        private const string NetworkChannelName = "texturedbuilding";

        public override void StartServerSide(ICoreServerAPI api)
        {
            serverApi = api;

            serverApi.Logger.Notification("========================================");
            serverApi.Logger.Notification("[TexturedBuilding] Starting server system...");
            serverApi.Logger.Notification("========================================");

            try
            {
                // Register network channel for inventory swap requests
                var channel = api.Network.RegisterChannel(NetworkChannelName)
                    .RegisterMessageType<InventorySwapRequest>()
                    .RegisterMessageType<ServerModAvailableMessage>()
                    .SetMessageHandler<InventorySwapRequest>(OnInventorySwapRequest)
                    .SetMessageHandler<ServerModAvailableMessage>(OnServerModCheckRequest);

                serverApi.Logger.Notification("[TexturedBuilding] Network channel registered successfully");
                serverApi.Logger.Notification("[TexturedBuilding] Server system loaded - UseEntireInventory feature enabled");
            }
            catch (System.Exception ex)
            {
                serverApi.Logger.Error("[TexturedBuilding] Failed to initialize server system:");
                serverApi.Logger.Error(ex.ToString());
            }
        }

        // Handle requests from clients checking if server has the mod
        private void OnServerModCheckRequest(IServerPlayer player, ServerModAvailableMessage request)
        {
            if (serverApi == null) return;

            // Send back confirmation that server has the mod
            var response = new ServerModAvailableMessage { Available = true };
            serverApi.Network.GetChannel(NetworkChannelName).SendPacket(response, player);

            serverApi.Logger.Debug($"[TexturedBuilding] Sent mod availability to {player.PlayerName}");
        }

        // Handle inventory swap requests from clients
        private void OnInventorySwapRequest(IServerPlayer player, InventorySwapRequest request)
        {
            if (serverApi == null) return;

            serverApi.Logger.Debug($"[TexturedBuilding] Processing swap request from {player.PlayerName}: {request.SourceInventoryId}[{request.SourceSlotId}] <-> {request.TargetInventoryId}[{request.TargetSlotId}]");

            // Get the inventories
            IInventory sourceInventory = player.InventoryManager.GetInventory(request.SourceInventoryId);
            IInventory targetInventory = player.InventoryManager.GetInventory(request.TargetInventoryId);

            if (sourceInventory == null)
            {
                serverApi.Logger.Warning($"[TexturedBuilding] Source inventory not found: {request.SourceInventoryId}");
                return;
            }

            if (targetInventory == null)
            {
                serverApi.Logger.Warning($"[TexturedBuilding] Target inventory not found: {request.TargetInventoryId}");
                return;
            }

            // Validate slot indices
            if (request.SourceSlotId < 0 || request.SourceSlotId >= sourceInventory.Count)
            {
                serverApi.Logger.Warning($"[TexturedBuilding] Invalid source slot: {request.SourceSlotId}");
                return;
            }

            if (request.TargetSlotId < 0 || request.TargetSlotId >= targetInventory.Count)
            {
                serverApi.Logger.Warning($"[TexturedBuilding] Invalid target slot: {request.TargetSlotId}");
                return;
            }

            // Get the slots
            ItemSlot sourceSlot = sourceInventory[request.SourceSlotId];
            ItemSlot targetSlot = targetInventory[request.TargetSlotId];

            if (sourceSlot == null || targetSlot == null)
            {
                serverApi.Logger.Warning($"[TexturedBuilding] Slot is null");
                return;
            }

            // Perform the swap on the main thread to avoid threading issues
            serverApi.Event.EnqueueMainThreadTask(() =>
            {
                try
                {
                    ItemStack sourceStack = sourceSlot.TakeOutWhole();
                    ItemStack targetStack = targetSlot.TakeOutWhole();

                    if (sourceStack != null)
                    {
                        targetSlot.Itemstack = sourceStack;
                        targetSlot.MarkDirty();
                    }

                    if (targetStack != null)
                    {
                        sourceSlot.Itemstack = targetStack;
                        sourceSlot.MarkDirty();
                    }

                    // Mark inventories as modified
                    player.InventoryManager.BroadcastHotbarSlot();

                    serverApi.Logger.Debug($"[TexturedBuilding] Swap completed successfully for {player.PlayerName}");
                }
                catch (System.Exception ex)
                {
                    serverApi.Logger.Error($"[TexturedBuilding] Error during swap: {ex.Message}");
                }
            }, "inventory-swap");
        }
    }

    // Network message for inventory swap requests
    [ProtoContract]
    public class InventorySwapRequest
    {
        [ProtoMember(1)]
        public string SourceInventoryId { get; set; } = "";

        [ProtoMember(2)]
        public int SourceSlotId { get; set; }

        [ProtoMember(3)]
        public string TargetInventoryId { get; set; } = "";

        [ProtoMember(4)]
        public int TargetSlotId { get; set; }
    }

    // Message to check/confirm server has the mod
    [ProtoContract]
    public class ServerModAvailableMessage
    {
        [ProtoMember(1)]
        public bool Available { get; set; }
    }
}