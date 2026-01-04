using System.Net.Http.Json;
using PriceTrace.ApiService.Models;

namespace PriceTrace.Web;

public class UserApiClient(HttpClient httpClient)
{
    public async Task<List<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await httpClient.GetFromJsonAsync<List<UserResponse>>("/api/users", cancellationToken);
        return users ?? [];
    }

    public async Task<UserResponse?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<UserResponse>($"/api/users/{id}", cancellationToken);
    }
}
