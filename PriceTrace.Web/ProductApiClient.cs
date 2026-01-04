using System.Net.Http.Json;
using PriceTrace.ApiService.Models;

namespace PriceTrace.Web;

public class ProductApiClient(HttpClient httpClient)
{
    public async Task<List<ProductResponse>> GetAllProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await httpClient.GetFromJsonAsync<List<ProductResponse>>("/api/products", cancellationToken);
        return products ?? [];
    }

    public async Task<ProductResponse?> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<ProductResponse>($"/api/products/{id}", cancellationToken);
    }

    public async Task<ProductResponse?> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("/api/products", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken);
    }

    public async Task<ProductResponse?> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/products/{id}", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.DeleteAsync($"/api/products/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
