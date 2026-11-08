using Bariza.Application.DTOs.SellerProducts;

namespace Bariza.Application.Interfaces.Services;

public interface ISellerProductService
{
    Task<IReadOnlyList<SellerProductDto>> GetProductsAsync(int userId, CancellationToken cancellationToken = default);
    Task<SellerProductDto> CreateProductAsync(int userId, CreateSellerProductRequest request, CancellationToken cancellationToken = default);
    Task<SellerProductDto> UpdateProductAsync(int userId, int productId, UpdateSellerProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(int userId, int productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SellerProductDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);
}
