namespace MABamlai.Services;
public class UserAccountStore
{
    private readonly DatabaseService databaseService;
    public UserAccountStore(DatabaseService databaseService)
    {
        this.databaseService = databaseService;
    }
    public bool ValidateCredentials(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return false;
        return databaseService.CanLogIn(username.Trim(), password.Trim()) != null;
    }
    public bool Register(string username, string password, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            errorMessage = "Username and password are required.";
            return false;
        }
        if (password.Trim().Length < 6)
        {
            errorMessage = "Password must be at least 6 characters.";
            return false;
        }
        bool created = databaseService.TryCreateUser(username.Trim(), password.Trim());
        if (!created)
            errorMessage = "This username already exists.";
        return created;
    }
}