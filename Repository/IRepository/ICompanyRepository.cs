namespace BookShopByKg;

public interface ICompanyRepository : IRepository<Company>
{
   void Update(Company obj);
}
