namespace A2ADemo.Identity;

using Microsoft.Extensions.Options;

public sealed class UserDatabase(IOptions<AuthSettings> settingsOptions)
{
    private readonly AuthSettings settings = settingsOptions.Value;
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
                settings.DemoUserUsername,
                settings.DemoUserPassword),
            new User(
                "user-2",
                settings.DemoUser2Username,
                settings.DemoUser2Password)
        ]);
    }

    public User? GetByUsername(string username) => users.FirstOrDefault(user => user.Username == username);
}

public sealed record User(string UserId, string Username, string PasswordHash);