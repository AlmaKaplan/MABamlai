using System.Text.Json;
using MABamlai.Model;
using Microsoft.JSInterop;

namespace MABamlai.Services;

public sealed class UserSessionStorageService
{
    private const string StorageKey = "mabamlai.currentUser";
    private readonly IJSRuntime jsRuntime;

    public UserSessionStorageService(IJSRuntime jsRuntime)
    {
        this.jsRuntime = jsRuntime;
    }

    public async Task SaveUserAsync(User user)
    {
        StoredUser storedUser = new StoredUser(
            user.GetId(),
            user.GetFullName(),
            user.GetUserName(),
            user.GetRole() ? 1 : 0);

        string json = JsonSerializer.Serialize(storedUser);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public async Task<User?> TryLoadUserAsync()
    {
        string? json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        StoredUser? storedUser;
        try
        {
            storedUser = JsonSerializer.Deserialize<StoredUser>(json);
        }
        catch
        {
            await ClearUserAsync();
            return null;
        }

        if (storedUser is null || string.IsNullOrWhiteSpace(storedUser.UserName))
        {
            await ClearUserAsync();
            return null;
        }

        if (storedUser.UserName.Equals("guest", StringComparison.OrdinalIgnoreCase))
        {
            await ClearUserAsync();
            return null;
        }

        string fullName = string.IsNullOrWhiteSpace(storedUser.FullName)
            ? storedUser.UserName
            : storedUser.FullName;

        return new User(storedUser.Id, fullName, storedUser.UserName, string.Empty, storedUser.Role);
    }

    public Task ClearUserAsync()
    {
        return jsRuntime.InvokeVoidAsync("localStorage.removeItem", StorageKey).AsTask();
    }

    private sealed record StoredUser(int Id, string FullName, string UserName, int Role);
}
