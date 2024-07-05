namespace BookShopByKg;

public interface ICategoryRepository : IRepository<Category>
{
    void Update(Category obj); 
}
