using Microsoft.AspNetCore.SignalR;
using Server.Services;
using System.Collections.Concurrent;

namespace Server.Hubs;

public class PokerHub : Hub
{
    private readonly TableManager _tableManager;
    private readonly IHubContext<PokerHub> _hubContext;
    // Store game state per table
    private static ConcurrentDictionary<string, Gamecontroller> games = new();

    public PokerHub(TableManager tableManager, IHubContext<PokerHub> hubContext)
    {
        _tableManager = tableManager;
        _hubContext = hubContext;
    }

    public async Task JoinTable(string tableId, string playerName)
    {
        var connectionId = Context.ConnectionId;
        var joined = _tableManager.TryJoinTable(tableId, connectionId, playerName);
        if (!joined)
        {
            await Clients.Caller.SendAsync("TableJoinFailed", tableId, "Table is full or does not exist.");
            return;
        }
        await Groups.AddToGroupAsync(connectionId, tableId);

        // 1. Notify all clients that a player joined (already present)
        await Clients.Group(tableId).SendAsync("PlayerJoined", playerName);

        // 2. Send the full player list to the newly joined client
        var allPlayers = _tableManager.GetTablePlayers(tableId).Select(p => p.Name).ToList();
        await Clients.Caller.SendAsync("PlayerList", allPlayers);
    }

    public async Task SendMessage(string tableId, string sender, string message)
    {
        if (message.Trim().ToLower() == "!start")
        {
            if (!_tableManager.IsHost(tableId, Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("GameError", "Only the host can start the game.");
                return;
            }
            var tablePlayers = _tableManager.GetTablePlayers(tableId); // returns List<TablePlayer>
            if (tablePlayers.Count < 2)
            {
                await Clients.Caller.SendAsync("GameError", "At least 2 players are required to start the game.");
                return;
            }
            var game = new Gamecontroller(tablePlayers, _hubContext, tableId, () => games.TryRemove(tableId, out _));
            games[tableId] = game;
            await game.PlayRoundAsync();
            return;
        }
        await Clients.Group(tableId).SendAsync("ReceiveTableMessage", sender, message, DateTime.UtcNow);
    }

    public async Task PlayerAction(string tableId, string playerName, string action, int raiseAmount = 0)
    {
        if (!games.TryGetValue(tableId, out var game)) return;
        await game.HandlePlayerActionAsync(playerName, action, raiseAmount);
    }

    public async Task UpdatePot(string tableId, int pot)
    {
        await Clients.Group(tableId).SendAsync("UpdatePot", pot);
    }

    public async Task PlayAgain(string tableId)
    {
        if (!games.TryGetValue(tableId, out var game)) return;
        await game.DealNewHandAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        foreach (var tableId in _tableManager.GetAllTableIds())
        {
            _tableManager.LeaveTable(tableId, Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, tableId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
