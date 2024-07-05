namespace BookShopByKg;

public interface IUnitOfWork
{
    ICategoryRepository CategoryRepo { get; }
    IProductRepository ProductRepo { get; }
    ICompanyRepository CompanyRepo { get; }
    IShoppingCartRepository ShoppingRepo{ get; }
    IApplicationUserRepository ApplicationRepo { get; }
    IOrderDetailRepository OrderDetailRepo { get; }
    IOrderHeaderRepository OrderHeaderRepo { get; }
    void Save();
}
