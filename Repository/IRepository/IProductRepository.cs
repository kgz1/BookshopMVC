namespace BookShopByKg;

public interface IProductRepository : IRepository<Product>
{
    void Update(Product obj);
}
