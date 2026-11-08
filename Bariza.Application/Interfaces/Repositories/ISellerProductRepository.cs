using Bariza.Core.Entities;

namespace Bariza.Application.Interfaces.Repositories;

public interface ISellerProductRepository
{
    Task<SellerProfile?> GetSellerProfileByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetProductsAsync(int sellerProfileId, CancellationToken cancellationToken = default);
    Task<Product?> GetProductAsync(int sellerProfileId, int productId, CancellationToken cancellationToken = default);
    Task AddProductAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateProductAsync(Product product, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Product product, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
