using System.Net.Http.Json;
using PriceTrace.Contracts.Models.SearchRequestModels;

namespace PriceTrace.Web;

public class SearchRequestApiClient(HttpClient httpClient)
{
    public async Task<List<SearchRequestResponse>> GetAllSearchRequestsAsync(CancellationToken cancellationToken = default)
    {
        var requests = await httpClient.GetFromJsonAsync<List<SearchRequestResponse>>("/api/searchrequests", cancellationToken);
        return requests ?? [];
    }

    public async Task<SearchRequestResponse?> GetSearchRequestByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<SearchRequestResponse>($"/api/searchrequests/{id}", cancellationToken);
    }

    public async Task<List<SearchRequestResponse>> GetSearchRequestsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var requests = await httpClient.GetFromJsonAsync<List<SearchRequestResponse>>($"/api/searchrequests/by-user/{userId}", cancellationToken);
        return requests ?? [];
    }

    public async Task<SearchRequestResponse?> CreateSearchRequestAsync(CreateSearchRequestRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/searchrequests", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SearchRequestResponse>(cancellationToken);
    }

    public async Task<SearchRequestResponse?> UpdateSearchRequestAsync(Guid id, UpdateSearchRequestRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/searchrequests/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<SearchRequestResponse>(cancellationToken);
    }

    public async Task DeleteSearchRequestAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/searchrequests/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
