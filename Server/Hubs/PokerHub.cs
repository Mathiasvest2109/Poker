using Microsoft.AspNetCore.SignalR;
using Server.Services;

namespace Server.Hubs;

public class PokerHub : Hub
{
    private readonly TableManager _tableManager;

    // Constructor to inject TableManager (dependency injection)
    public PokerHub(TableManager tableManager)
    {
        _tableManager = tableManager;
    }

    public async Task JoinTable(string tableId, string playerName)
    {
        var connectionId = Context.ConnectionId;

        var joined = _tableManager.TryJoinTable(tableId, connectionId, playerName);
        if (!joined)
        {
            await Clients.Caller.SendAsync("TableJoinFailed", tableId, "Table is full or does not exist.");
            return; //Stop here if join fails
        }

        await Groups.AddToGroupAsync(connectionId, tableId);
        await Clients.Group(tableId).SendAsync("PlayerJoined", playerName);
    }


    public async Task SendTableMessage(string tableId, string message)
    {
        var playerName = _tableManager.GetPlayerName(Context.ConnectionId) ?? "Unknown";

        await Clients.Group(tableId)
            .SendAsync("ReceiveTableMessage", playerName, message, DateTime.UtcNow);
    }


    // Called automatically when someone disconnects (e.g. browser closes)
    public override async Task OnDisconnectedAsync(Exception exception)
    {
        foreach (var tableId in _tableManager.GetAllTableIds())
        {
            _tableManager.LeaveTable(tableId, Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, tableId);
        }

        await base.OnDisconnectedAsync(exception);
    }    
}
