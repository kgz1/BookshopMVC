using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
namespace BookShopByKg;

[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    [BindProperty]
    public ShoppingCartVM ShoppingCartVM{ get; set; }
    public CartController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public IActionResult Index()
    {
       var claimsIdentity = (ClaimsIdentity)User.Identity;
       var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
       ShoppingCartVM = new ()
       {
          ShoppingCartList = _unitOfWork.ShoppingRepo.GetAll(u => u.ApplicationUserId == userId, includeProperties:"Product"),
          OrderHeader = new()
       };
       foreach (var cart in ShoppingCartVM.ShoppingCartList)
       {
        cart.Price = GetPriceBasedOnQuantity(cart);
        ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
       }
        return View(ShoppingCartVM);
    }

    public IActionResult Summary()
    {
       var claimsIdentity = (ClaimsIdentity)User.Identity;
       var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
       ShoppingCartVM = new ()
       {
          ShoppingCartList = _unitOfWork.ShoppingRepo.GetAll(u => u.ApplicationUserId == userId, includeProperties:"Product"),
          OrderHeader = new()
       };
       ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationRepo.Get(u => u.Id == userId);

        ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
        ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
        ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
        ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
        ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
        ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

       foreach (var cart in ShoppingCartVM.ShoppingCartList)
       {
        cart.Price = GetPriceBasedOnQuantity(cart);
        ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
       }
        return View(ShoppingCartVM);
    }

[HttpPost]
[ActionName("Summary")]
public IActionResult SummaryPOST()
{
       var claimsIdentity = (ClaimsIdentity)User.Identity;
       var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
       ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingRepo.GetAll(u => u.ApplicationUserId == userId, includeProperties:"Product");
       ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
       ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

       ApplicationUser applicationUser = _unitOfWork.ApplicationRepo.Get(u => u.Id == userId);

       foreach (var cart in ShoppingCartVM.ShoppingCartList)
       {
        cart.Price = GetPriceBasedOnQuantity(cart);
        ShoppingCartVM.OrderHeader.OrderTotal += (cart.Price * cart.Count);
       }
       if(applicationUser.CompanyId.GetValueOrDefault()==0){
           ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
           ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
       }else{
            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
            ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
       }
       _unitOfWork.OrderHeaderRepo.Add(ShoppingCartVM.OrderHeader);
       _unitOfWork.Save();
       foreach(var cart in ShoppingCartVM.ShoppingCartList){
        OrderDetail orderDetail = new (){
            ProductId = cart.ProductId,
            OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
            Price = cart.Price,
            Count=cart.Count
        };
        _unitOfWork.OrderDetailRepo.Add(orderDetail);
        _unitOfWork.Save();
       }
       if(applicationUser.CompanyId.GetValueOrDefault()==0){
        var domain  = "http://localhost:5132/";
      var options = new Stripe.Checkout.SessionCreateOptions
      {
      SuccessUrl = domain+ $"cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
      CancelUrl = domain+ "cart/index",
      LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
      Mode = "payment",
      };

      foreach(var item in ShoppingCartVM.ShoppingCartList)
      {
          var sessionLineItems = new SessionLineItemOptions{
            PriceData = new SessionLineItemPriceDataOptions{
             UnitAmount = (long)(item.Price*100),
             Currency = "usd",
             ProductData = new SessionLineItemPriceDataProductDataOptions{
                Name = item.Product.Title
             }
            },
            Quantity = item.Count
          };
          options.LineItems.Add(sessionLineItems);
      }
      var service = new Stripe.Checkout.SessionService();
      Session session = service.Create(options);
      _unitOfWork.OrderHeaderRepo.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
      _unitOfWork.Save();
      Response.Headers.Add("Location", session.Url);
      return new StatusCodeResult(303);
       }
        return RedirectToAction(nameof(OrderConfirmation), new {id = ShoppingCartVM.OrderHeader.Id});
    }

    public IActionResult OrderConfirmation(int id)
    {
        OrderHeader orderHeader = _unitOfWork.OrderHeaderRepo.Get(u => u.Id == id, includeProperties:"ApplicationUser");
        if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment){
            var service  = new SessionService();
            Session session = service.Get(orderHeader.SessionId);
               if (session.PaymentStatus.ToLower() == "paid") {
					_unitOfWork.OrderHeaderRepo.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeaderRepo.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
        }
        }
          List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingRepo
                .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
            _unitOfWork.ShoppingRepo.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
        return View(id);
    }
    

    public IActionResult Plus(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingRepo.Get(u => u.Id == cartId);
        cartFromDb.Count += 1;
        _unitOfWork.ShoppingRepo.Update(cartFromDb);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Minus(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingRepo.Get(u => u.Id == cartId);
        if(cartFromDb.Count <= 1)
        {
           _unitOfWork.ShoppingRepo.Remove(cartFromDb);
        }else{
            cartFromDb.Count -= 1;
             _unitOfWork.ShoppingRepo.Update(cartFromDb);
        }
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }
 public IActionResult Remove(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingRepo.Get(u => u.Id == cartId);
        _unitOfWork.ShoppingRepo.Remove(cartFromDb);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart) {
            if (shoppingCart.Count <= 50) {
                return shoppingCart.Product.Price;
            }
            else {
                if (shoppingCart.Count <= 100) {
                    return shoppingCart.Product.Price50;
                }
                else {
                    return shoppingCart.Product.Price100;
                }
            }
}
}
