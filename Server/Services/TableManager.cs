namespace Server.Services;

public class TablePlayer
{
    public string Name { get; set; }
    public string ConnectionId { get; set; }
}

public class TableManager
{
    private readonly Dictionary<string, string> _connectionToName = new();
    private readonly Dictionary<string, List<TablePlayer>> _tables = new();

    public string CreateTable()
    {
        var code = GenerateTableCode();
        _tables[code] = new List<TablePlayer>();
        return code;
    }

    public bool TableExists(string tableId) => _tables.ContainsKey(tableId);

    public bool TryJoinTable(string tableId, string connectionId, string playerName)
    {
        if (!_tables.ContainsKey(tableId))
            return false;

        var players = _tables[tableId];

        if (players.Count >= 4)
        {
            Console.WriteLine("too many players detected");
            return false;
        }

        _connectionToName[connectionId] = playerName;

        // Check if player already exists by name
        var existing = players.FirstOrDefault(p => p.Name == playerName);
        if (existing != null)
        {
            // Update their ConnectionId to the latest
            existing.ConnectionId = connectionId;
        }
        else
        {
            players.Add(new TablePlayer { ConnectionId = connectionId, Name = playerName });
        }
        return true;
    }

    public string? GetPlayerName(string connectionId)
    {
        return _connectionToName.TryGetValue(connectionId, out var name) ? name : null;
    }      

    public void LeaveTable(string tableId, string connectionId)
    {
        if (_tables.TryGetValue(tableId, out var players))
        {
            players.RemoveAll(player => player.ConnectionId == connectionId);
            _connectionToName.Remove(connectionId);
        }
    }

    public IEnumerable<string> GetAllTableIds() => _tables.Keys;

    public bool IsHost(string tableId, string connectionId)
    {
        if (!_tables.ContainsKey(tableId)) return false;

        // Assume the first player to join is the host
        return _tables[tableId].FirstOrDefault()?.ConnectionId == connectionId;
    }

    public List<string> GetPlayersInTable(string tableId)
    {
        if (!_tables.ContainsKey(tableId)) return new List<string>();
        return _tables[tableId].Select(player => player.Name).ToList();
    }

    public List<TablePlayer> GetTablePlayers(string tableId)
    {
        if (_tables.TryGetValue(tableId, out var players))
            return players;
        return new List<TablePlayer>();
    }

    private string GenerateTableCode()
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
