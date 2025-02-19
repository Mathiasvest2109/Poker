using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Poker.Server
{
    internal class Server
    {
        private static TcpListener? server;
        private static readonly int PORT = 5000;

        // Dictionary of rooms (Room ID → List of player connections)
        private static ConcurrentDictionary<string, List<TcpClient>> rooms = new();

        static async Task Main()
        {
            server = new TcpListener(IPAddress.Any, PORT);
            server.Start();
            Console.WriteLine($"Server started on port {PORT}");

            while (true)
            {
                var client = await server.AcceptTcpClientAsync();
                Console.WriteLine("Client Connected");

                // Assign the player to a room
                string roomId = AssignPlayerToRoom(client);
                Console.WriteLine($"Player assigned to Room: {roomId}");

                _ = HandleClientAsync(client, roomId);
            }
        }

        // Assign player to a room (creates a new one if needed)
        private static string AssignPlayerToRoom(TcpClient client)
        {
            lock (rooms)
            {
                // Try to find a room that is not full
                foreach (var room in rooms)
                {
                    if (room.Value.Count < 4)
                    {
                        room.Value.Add(client);
                        return room.Key; // Return the existing room ID
                    }
                }

                // If no available room, create a new one
                string newRoomId = Guid.NewGuid().ToString().Substring(0, 6);
                rooms[newRoomId] = new List<TcpClient> { client };
                return newRoomId;
            }
        }

        // Handles client communication
        private static async Task HandleClientAsync(TcpClient client, string roomId)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (true)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Client disconnected

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received from Room {roomId}: {message}");

                    // Broadcast message to all players in the same room
                    await BroadcastToRoom(roomId, message);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}");
            }
            finally
            {
                client.Close();
                RemovePlayerFromRoom(client, roomId);
            }
        }

        // Broadcast a message to all players in a room
        private static async Task BroadcastToRoom(string roomId, string message)
        {
            if (rooms.TryGetValue(roomId, out var players))
            {
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                foreach (var player in players)
                {
                    if (player.Connected)
                    {
                        await player.GetStream().WriteAsync(buffer, 0, buffer.Length);
                    }
                }
            }
        }

        // Remove player from a room
        private static void RemovePlayerFromRoom(TcpClient client, string roomId)
        {
            if (rooms.TryGetValue(roomId, out var players))
            {
                players.Remove(client);
                if (players.Count == 0)
                {
                    rooms.TryRemove(roomId, out _); // Remove empty room
                }
            }
        }
    }
}
