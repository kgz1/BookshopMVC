namespace BookShopByKg;

public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
{
    private ApplicationDbContext _db;
    public ShoppingCartRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }
    public void Update(ShoppingCart obj)
    {
       _db.ShoppingCarts.Update(obj);
    }

}
