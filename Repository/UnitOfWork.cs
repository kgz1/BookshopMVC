namespace BookShopByKg;

public class UnitOfWork : IUnitOfWork
{
    private ApplicationDbContext _db;
    public ICategoryRepository CategoryRepo { get; private set; }
    public IProductRepository ProductRepo { get; private set; }
    public ICompanyRepository CompanyRepo { get; private set; }
    public IShoppingCartRepository ShoppingRepo { get; private set; }
    public IApplicationUserRepository ApplicationRepo { get; private set; }
    public IOrderDetailRepository OrderDetailRepo { get; private set; }
    public IOrderHeaderRepository OrderHeaderRepo { get; private set; }
    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;
        CategoryRepo = new CategoryRepository(_db);
        ProductRepo = new ProductRepository(_db);
        CompanyRepo = new CompanyRepository(_db);
        ShoppingRepo = new ShoppingCartRepository(_db);
        ApplicationRepo = new ApplicationUserRepository(_db);
        OrderDetailRepo = new OrderDetailRepository(_db);
        OrderHeaderRepo = new OrderHeaderRepository(_db);
    }
    public void Save()
    {
        _db.SaveChanges();
    }
}
