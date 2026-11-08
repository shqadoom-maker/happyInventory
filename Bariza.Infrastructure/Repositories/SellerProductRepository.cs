using Bariza.Application.Interfaces.Repositories;
using Bariza.Core.Entities;
using Bariza.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Bariza.Infrastructure.Repositories;

public class SellerProductRepository : ISellerProductRepository
{
    private readonly AppDbContext _db;

    public SellerProductRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<SellerProfile?> GetSellerProfileByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _db.SellerProfiles.FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

    public Task AddSellerProfileAsync(SellerProfile seller, CancellationToken cancellationToken = default) =>
        _db.SellerProfiles.AddAsync(seller, cancellationToken).AsTask();

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
        await _db.Categories.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Product>> GetProductsAsync(int sellerProfileId, CancellationToken cancellationToken = default) =>
        await _db.Products
            .Include(p => p.Category)
            .Where(p => p.SellerProfileId == sellerProfileId)
            .OrderByDescending(p => p.CreatedUtc)
            .ToListAsync(cancellationToken);

    public async Task<Product?> GetProductAsync(int sellerProfileId, int productId, CancellationToken cancellationToken = default) =>
        await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == productId && p.SellerProfileId == sellerProfileId, cancellationToken);

    public Task AddProductAsync(Product product, CancellationToken cancellationToken = default) =>
        _db.Products.AddAsync(product, cancellationToken).AsTask();

    public Task UpdateProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        _db.Products.Update(product);
        return Task.CompletedTask;
    }

    public Task DeleteProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        _db.Products.Remove(product);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
