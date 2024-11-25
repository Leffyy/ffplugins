using Dalamud.Plugin;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.Game;
using Dalamud.Lua;
using Dalamud.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

public class AutoRepairPlugin : IDalamudPlugin
{
    private const string PluginName = "AutoRepairPlugin";
    private readonly DalamudPluginInterface pluginInterface;
    private readonly ClientState clientState;
    private readonly GameGui gameGui;
    private readonly Lua lua;
    private readonly uint repairThreshold = 50;  // Default repair threshold (percent)
    private readonly uint darkMatterID = 33917;  // Dark Matter 8 item ID

    private bool isRunning;

    public AutoRepairPlugin(DalamudPluginInterface pluginInterface, ClientState clientState, GameGui gameGui, Lua lua)
    {
        this.pluginInterface = pluginInterface;
        this.clientState = clientState;
        this.gameGui = gameGui;
        this.lua = lua;
        this.isRunning = false;
    }

    // Initialize the plugin (called on plugin load)
    public void Initialize()
    {
        PluginLog.Information($"[{PluginName}] Initialized.");
    }

    // Clean up when the plugin is unloaded
    public void Dispose()
    {
        PluginLog.Information($"[{PluginName}] Disposed.");
    }

    // Start the autorepair loop
    public void StartAutoRepairLoop()
    {
        if (isRunning)
        {
            PluginLog.Information($"[{PluginName}] AutoRepair loop already running.");
            return;
        }

        isRunning = true;
        Task.Run(() => AutoRepairLoop());
    }

    // Stop the autorepair loop
    public void StopAutoRepairLoop()
    {
        isRunning = false;
        PluginLog.Information($"[{PluginName}] AutoRepair loop stopped.");
    }

    // The autorepair loop that runs continuously in the background
    private async Task AutoRepairLoop()
    {
        while (isRunning)
        {
            // Wait for a while before checking the durability again
            await Task.Delay(5000);  // Delay in milliseconds (5 seconds)

            if (NeedsRepair(repairThreshold))
            {
                await RepairGear();
            }
        }
    }

    // Function to check if any gear needs repair based on the threshold
    private bool NeedsRepair(uint threshold)
    {
        var inventory = Dalamud.GameData.GetInventory();
        
        foreach (var slot in inventory)
        {
            if (slot?.Item?.Durability != null)
            {
                var durabilityPercent = (float)slot.Item.Durability / slot.Item.MaxDurability * 100;
                if (durabilityPercent < threshold)
                {
                    PluginLog.Information($"Item {slot.Item.Name} needs repair (Durability: {durabilityPercent}%).");
                    return true;
                }
            }
        }

        return false;
    }

    // Function to trigger repair
    private async Task RepairGear()
    {
        if (IsInZone(129)) // Check if we are in Limsa Lominsa Lower Decks
        {
            PluginLog.Information($"[{PluginName}] Repairing gear...");
            var darkMatterCount = GetItemCount(darkMatterID);
            if (darkMatterCount > 0)
            {
                PluginLog.Information($"Using Dark Matter 8 for repair.");
                await UseDarkMatterRepair();
            }
            else
            {
                PluginLog.Information($"No Dark Matter 8 available, attempting repair at NPC.");
                await RepairAtNPC();
            }
        }
        else
        {
            PluginLog.Information($"[{PluginName}] Not in Limsa Lominsa Lower Decks, teleporting...");
            TeleportTo("Limsa Lominsa Lower Decks");
        }
    }

    // Function to repair using Dark Matter 8
    private async Task UseDarkMatterRepair()
    {
        // Your logic for Dark Matter repair goes here
        PluginLog.Information($"Using Dark Matter 8 to repair gear.");
        await Task.Delay(1000);  // Simulate a delay for using dark matter
    }

    // Function to repair at an NPC (e.g., Alistair in Limsa Lominsa Lower Decks)
    private async Task RepairAtNPC()
    {
        // NPC's position for repair
        var repairNPC = new { npcName = "Alistair", x = -246.87f, y = 16.19f, z = 49.83f };

        if (GetDistanceToPoint(repairNPC.x, repairNPC.y, repairNPC.z) > 5)
        {
            // Move to the NPC if not close
            await MoveToPoint(repairNPC.x, repairNPC.y, repairNPC.z);
        }
        else
        {
            if (!HasTarget() || GetTargetName() != repairNPC.npcName)
            {
                TargetNPC(repairNPC.npcName);
            }

            await InteractWithNPC();
        }
    }

    // Helper method to interact with an NPC
    private async Task InteractWithNPC()
    {
        if (!GetCharacterCondition(CharacterCondition.occupiedInQuestEvent))
        {
            // Interact with NPC
            await ExecuteCommand("/interact");
        }
    }

    // Function to move to a specific point (use pathfinding)
    private async Task MoveToPoint(float x, float y, float z)
    {
        while (GetDistanceToPoint(x, y, z) > 5)
        {
            if (!PathfindInProgress() && !PathIsRunning())
            {
                // Move to the target point
                PathfindAndMoveTo(x, y, z);
            }
            await Task.Delay(500);  // Wait a moment before checking the distance again
        }
    }

    // Utility function to check if the player is in a specific zone
    private bool IsInZone(uint zoneId)
    {
        return clientState.TerritoryType == zoneId;
    }

    // Utility function to teleport to a specific location
    private void TeleportTo(string locationName)
    {
        ExecuteCommand($"/teleport {locationName}");
    }

    // Helper function to execute a command
    private void ExecuteCommand(string command)
    {
        lua.RunString(command);
    }

    // Helper function to get the count of a specific item in the inventory
    private int GetItemCount(uint itemId)
    {
        var inventory = Dalamud.GameData.GetInventory();
        var item = inventory.FirstOrDefault(slot => slot?.Item?.Id == itemId);
        return item != null ? 1 : 0;
    }

    // Helper function to check if we have a target
    private bool HasTarget()
    {
        return clientState.Target != null;
    }

    // Helper function to get the target name
    private string GetTargetName()
    {
        return clientState.Target?.Name;
    }

    // Helper function to check the distance to a point
    private float GetDistanceToPoint(float x, float y, float z)
    {
        return (float)Math.Sqrt(Math.Pow(clientState.LocalPlayer.Position.X - x, 2) +
                                Math.Pow(clientState.LocalPlayer.Position.Y - y, 2) +
                                Math.Pow(clientState.LocalPlayer.Position.Z - z, 2));
    }

    // Helper function to check if pathfinding is running
    private bool PathfindInProgress()
    {
        return false;  // Add pathfinding checks here as needed
    }

    // Helper function to check if a path is running
    private bool PathIsRunning()
    {
        return false;  // Add pathrunning checks here as needed
    }

    // Helper function to initiate pathfinding and moving to the target point
    private void PathfindAndMoveTo(float x, float y, float z)
    {
        // Add pathfinding logic here (may require additional Dalamud API calls)
    }
}
