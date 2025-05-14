
public class UserState
{
    public bool IsLoggedIn { get; private set; }
    public string Username { get; private set; }
    public string DisplayName { get; private set; }
    public int UserId { get; private set; }

    public void SetUser(string username, string displayName, int userId)
    {
        Username = username;
        DisplayName = displayName;
        UserId = userId;
        IsLoggedIn = true;
    }

    public void Logout()
    {
        Username = null;
        DisplayName = null;
        UserId = 0;
        IsLoggedIn = false;
    }
}
