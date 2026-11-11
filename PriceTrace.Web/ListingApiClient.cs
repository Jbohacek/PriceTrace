using System.Net.Http.Json;
using PriceTrace.Contracts.Models.ListingModels;

namespace PriceTrace.Web;

public class ListingApiClient(HttpClient httpClient)
{
    public async Task<List<ListingResponse>> GetAllListingsAsync(CancellationToken cancellationToken = default)
    {
        var listings = await httpClient.GetFromJsonAsync<List<ListingResponse>>("/api/listings", cancellationToken);
        return listings ?? [];
    }

    public async Task<ListingResponse?> GetListingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ListingResponse>($"/api/listings/{id}", cancellationToken);
    }

    public async Task<FilteredListingsResponse?> FilterListingsAsync(ListingFilterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/listings/filter", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FilteredListingsResponse>(cancellationToken);
    }

    public async Task<ListingResponse?> CreateListingAsync(CreateListingRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/listings", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ListingResponse>(cancellationToken);
    }

    public async Task<ListingResponse?> UpdateListingAsync(Guid id, UpdateListingRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/listings/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ListingResponse>(cancellationToken);
    }

    public async Task DeleteListingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/listings/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
