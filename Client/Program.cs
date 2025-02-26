using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

internal class RoomClient
{
    private static readonly string SERVER_IP = "127.0.0.1"; // Server IP address
    private static readonly int SERVER_PORT = 5000; // Server port

    static async Task Main(string[] args)
    {
        TcpClient client = new TcpClient();

        try
        {
            await client.ConnectAsync(SERVER_IP, SERVER_PORT);
            Console.WriteLine("Connected to server");

            NetworkStream stream = client.GetStream();

            // Start a task to continuously read messages from the server
            _ = ReceiveMessagesAsync(stream);

            // Read messages from the user and send them to the server
            string userInput;
            while ((userInput = Console.ReadLine()) != null)
            {
                byte[] buffer = Encoding.UTF8.GetBytes(userInput);
                await stream.WriteAsync(buffer, 0, buffer.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            client.Close();
        }
    }

    // Method to receive messages from the server
    private static async Task ReceiveMessagesAsync(NetworkStream stream)
    {
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0) break; // Server disconnected

                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received: {message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving message: {ex.Message}");
        }
    }
}
