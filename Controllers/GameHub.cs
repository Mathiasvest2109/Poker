using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Poker.Hubs
{
    public class GameHub : Hub
    {
        private static ConcurrentDictionary<string, List<string>> Rooms = new();

        public async Task CreateRoom()
        {
            string roomId = Guid.NewGuid().ToString().Substring(0, 6);
            Rooms.TryAdd(roomId, new List<string>());
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            Rooms[roomId].Add(Context.ConnectionId);

            Console.WriteLine($"[GameHub] Room Created: {roomId} by {Context.ConnectionId}");
            await Clients.Caller.SendAsync("ReceiveRoomId", roomId);
        }

        public async Task JoinRoom(string roomId)
        {
            if (Rooms.ContainsKey(roomId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                Rooms[roomId].Add(Context.ConnectionId);
                Console.WriteLine($"[GameHub] User {Context.ConnectionId} joined Room: {roomId}");
                await Clients.Caller.SendAsync("JoinSuccess", roomId);
            }
            else
            {
                Console.WriteLine($"[GameHub] Join failed: Room {roomId} not found");
                await Clients.Caller.SendAsync("JoinFailed", "Room ID not found.");
            }
        }

        public async Task SendMessage(string roomId, string user, string message)
        {
            if (Rooms.ContainsKey(roomId))
            {
                Console.WriteLine($"[GameHub] Message in Room {roomId}: {user} -> {message}");
                await Clients.Group(roomId).SendAsync("ReceiveMessage", user, message);
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            foreach (var room in Rooms)
            {
                if (room.Value.Contains(Context.ConnectionId))
                {
                    room.Value.Remove(Context.ConnectionId);
                    Console.WriteLine($"[GameHub] User {Context.ConnectionId} left Room {room.Key}");

                    if (!room.Value.Any())
                    {
                        Rooms.TryRemove(room.Key, out _);
                        Console.WriteLine($"[GameHub] Room {room.Key} deleted (empty)");
                    }
                    break;
                }
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
