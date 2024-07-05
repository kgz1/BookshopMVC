namespace BookShopByKg;

public interface IOrderDetailRepository : IRepository<OrderDetail>
{
    void Update(OrderDetail obj); 
}
