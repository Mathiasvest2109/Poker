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

            // Read the initial prompt from the server
            byte[] buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine(serverMessage);

            // Send choice to the server
            Console.Write("Enter your choice (1 to create, 2 to join): ");
            string choice = Console.ReadLine();
            byte[] choiceBytes = Encoding.UTF8.GetBytes(choice);
            await stream.WriteAsync(choiceBytes, 0, choiceBytes.Length);

            if (choice == "1")
            {
                // Read the room creation response
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine(serverMessage);
            }
            else if (choice == "2")
            {
                // Read the room PIN prompt
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine(serverMessage);

                // Send room PIN to the server
                Console.Write("Enter room PIN: ");
                string pin = Console.ReadLine();
                byte[] pinBytes = Encoding.UTF8.GetBytes(pin);
                await stream.WriteAsync(pinBytes, 0, pinBytes.Length);

                // Read the room joining response
                bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                serverMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine(serverMessage);
            }

            // Start a task to continuously read messages from the server
            _ = ReceiveMessagesAsync(stream);

            // Read messages from the user and send them to the server
            string userInput;
            while ((userInput = Console.ReadLine()) != null)
            {
                byte[] userInputBytes = Encoding.UTF8.GetBytes(userInput);
                await stream.WriteAsync(userInputBytes, 0, userInputBytes.Length);
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
