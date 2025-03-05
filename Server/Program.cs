    // See https://aka.ms/new-console-template for more information
    using System.Collections.Concurrent;
    using System.Net.Sockets;
    using System.Net;
    using System.Text;

   internal class RoomServer
{
    private static TcpListener? server;
    private static readonly int PORT = 5000;

    // Dictionary of rooms (Room PIN → List of player connections)
    private static ConcurrentDictionary<string, List<TcpClient>> rooms = new();

    // Dictionary to store nicknames
    private static ConcurrentDictionary<TcpClient, string> nicknames = new();

    static async Task Main()
    {
        server = new TcpListener(IPAddress.Any, PORT);
        server.Start();
        Console.WriteLine($"Server started on port {PORT}");

        while (true)
        {
            var client = await server.AcceptTcpClientAsync();
            Console.WriteLine("Client Connected");

            _ = HandleClientAsync(client);
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();

        // Prompt for nickname
        byte[] buffer = Encoding.UTF8.GetBytes("Enter your nickname:");
        await stream.WriteAsync(buffer, 0, buffer.Length);

        // Read nickname
        buffer = new byte[1024];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string nickname = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        // Store nickname
        nicknames[client] = nickname;

        // Send room options
        buffer = Encoding.UTF8.GetBytes("Type 1 to create a room, or type 2 to join an existing room.");
        await stream.WriteAsync(buffer, 0, buffer.Length);

        // Read client's choice
        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        string choice = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        string roomId;
        if (choice == "1")
        {
            // Create a new room
            roomId = CreateRoom(client);
            byte[] roomCreatedMessage = Encoding.UTF8.GetBytes($"Room created with PIN: {roomId}");
            await stream.WriteAsync(roomCreatedMessage, 0, roomCreatedMessage.Length);
        }
        else if (choice == "2")
        {
            // Join an existing room
            byte[] pinRequest = Encoding.UTF8.GetBytes("Enter room PIN to join:");
            await stream.WriteAsync(pinRequest, 0, pinRequest.Length);

            // Read room PIN
            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string pin = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

            roomId = JoinRoom(pin, client);

            if (roomId != null)
            {
                byte[] roomJoinedMessage = Encoding.UTF8.GetBytes($"Joined room with PIN: {roomId}");
                await stream.WriteAsync(roomJoinedMessage, 0, roomJoinedMessage.Length);
            }
            else
            {
                byte[] roomJoinFailedMessage = Encoding.UTF8.GetBytes("Failed to join room.");
                await stream.WriteAsync(roomJoinFailedMessage, 0, roomJoinFailedMessage.Length);
                client.Close();
                return;
            }
        }
        else
        {
            byte[] invalidChoiceMessage = Encoding.UTF8.GetBytes("Invalid choice.");
            await stream.WriteAsync(invalidChoiceMessage, 0, invalidChoiceMessage.Length);
            client.Close();
            return;
        }

        // Proceed with handling client communication
        await HandleClientCommunicationAsync(client, roomId);
    }

    private static string CreateRoom(TcpClient client)
    {
        lock (rooms)
        {
            string newRoomId = Guid.NewGuid().ToString().Substring(0, 6);
            rooms[newRoomId] = new List<TcpClient> { client };
            return newRoomId;
        }
    }

    private static string JoinRoom(string pin, TcpClient client)
    {
        lock (rooms)
        {
            if (rooms.TryGetValue(pin, out var room) && room.Count < 4)
            {
                room.Add(client);
                return pin;
            }
            return null;
        }
    }

    private static async Task HandleClientCommunicationAsync(TcpClient client, string roomId)
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
                string fullMessage = $"{nicknames[client]}: {message}";
                Console.WriteLine($"Received from Room {roomId}: {fullMessage}");

                // Broadcast message to all players in the same room
                await BroadcastToRoom(roomId, fullMessage);
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
        nicknames.TryRemove(client, out _); // Remove nickname
    }
}
