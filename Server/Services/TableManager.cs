namespace Server.Services;

public class TableManager
{
    private readonly Dictionary<string, string> _connectionToName = new();
    private readonly Dictionary<string, HashSet<string>> _tables = new();

    public string CreateTable()
    {
        var code = GenerateTableCode();
        _tables[code] = new HashSet<string>();
        return code;
    }

    public bool TableExists(string tableId) => _tables.ContainsKey(tableId);

    public bool TryJoinTable(string tableId, string connectionId, string playerName)
    {
        if (!_tables.ContainsKey(tableId))
            return false;

        var players = _tables[tableId];

        if (players.Count >= 4)
            return false;

        _connectionToName[connectionId] = playerName;
        return players.Add(connectionId); // Returns true if join was successful
    }

    public string? GetPlayerName(string connectionId)
    {
        return _connectionToName.TryGetValue(connectionId, out var name) ? name : null;
    }      

    public void LeaveTable(string tableId, string connectionId)
    {
        if (_tables.TryGetValue(tableId, out var players))
        {
            players.Remove(connectionId);
            _connectionToName.Remove(connectionId);
        }
    }

    public IEnumerable<string> GetAllTableIds() => _tables.Keys;

    private string GenerateTableCode()
    {
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
