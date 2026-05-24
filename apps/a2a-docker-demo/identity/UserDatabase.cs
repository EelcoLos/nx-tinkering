namespace A2ADemo.Identity;

public sealed class UserDatabase
{
    private readonly List<User> users = [];

    public void SeedDemoUsers()
    {
        if (users.Count > 0)
        {
            return;
        }

        users.AddRange(
        [
            new User(
                "user-1",
                Environment.GetEnvironmentVariable("DEMO_USER_USERNAME") ?? "admin",
                Environment.GetEnvironmentVariable("DEMO_USER_PASSWORD") ?? "demo123"),
            new User(
                "user-2",
                Environment.GetEnvironmentVariable("DEMO_USER2_USERNAME") ?? "user",
                Environment.GetEnvironmentVariable("DEMO_USER2_PASSWORD") ?? "user456")
        ]);
    }

    public User? GetByUsername(string username) => users.FirstOrDefault(user => user.Username == username);
}

public sealed record User(string UserId, string Username, string PasswordHash);