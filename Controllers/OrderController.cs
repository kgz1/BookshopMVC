using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace BookShopByKg;

[Authorize]

public class OrderController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    [BindProperty]
    public OrderVM OrderVM{ get; set; }

    public OrderController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
   public IActionResult Index()
   {
    return View();
   }

 public IActionResult Details(int orderId)
   {
    OrderVM = new ()
    {
      OrderHeader = _unitOfWork.OrderHeaderRepo.Get(u => u.Id == orderId, includeProperties:"ApplicationUser"),
      OrderDetail = _unitOfWork.OrderDetailRepo.GetAll(u => u.OrderHeaderId==orderId, includeProperties:"Product")
    };
    return View(OrderVM);
   }
   
   [HttpPost]
   [Authorize(Roles =SD.Role_Admin+","+SD.Role_Employee)]
   public IActionResult UpdateOrderDetail()
   {
   var orderHeaderFromDb = _unitOfWork.OrderHeaderRepo.Get(u => u.Id == OrderVM.OrderHeader.Id);
   orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
   orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
   orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
   orderHeaderFromDb.City = OrderVM.OrderHeader.City;
   orderHeaderFromDb.State = OrderVM.OrderHeader.State;
   orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

   if(!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier)){
    orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
   }
   if(!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber)){
    orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
   }
   _unitOfWork.OrderHeaderRepo.Update(orderHeaderFromDb);
   _unitOfWork.Save();

   TempData["Success"] = "Order Details Updated Successfully";
    return RedirectToAction(nameof(Details), new {orderId = orderHeaderFromDb.Id});
   }

   [HttpPost]
   [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
   public IActionResult StartProcessing()
   {
      _unitOfWork.OrderHeaderRepo.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
      _unitOfWork.Save();
      TempData["Success"] = "Order Details Updated Successfully";
       return RedirectToAction(nameof(Details), new {orderId = OrderVM.OrderHeader.Id});
   }
   [HttpPost]
   [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
   public IActionResult ShipOrder()
   {

    var orderHeader = _unitOfWork.OrderHeaderRepo.Get(u => u.Id == OrderVM.OrderHeader.Id);
    orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
    orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
    orderHeader.OrderStatus = OrderVM.OrderHeader.OrderStatus;
    orderHeader.ShippingDate = DateTime.Now;
    if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
    {
         orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
    }
    _unitOfWork.OrderHeaderRepo.Update(orderHeader);
    _unitOfWork.Save();
    TempData["Success"] = "Order Shipped Successfully";
    return RedirectToAction(nameof(Details), new {orderId = OrderVM.OrderHeader.Id});
   }

   [HttpPost]
   [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
   public IActionResult CancelOrder()
   {
      var orderHeader = _unitOfWork.OrderHeaderRepo.Get(u => u.Id == OrderVM.OrderHeader.Id);
      if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
      {
        var options = new RefundCreateOptions{
          Reason = RefundReasons.RequestedByCustomer,
          PaymentIntent = orderHeader.PaymentIntentId,
        };
        var service = new RefundService();
        Refund refund = service.Create(options);
        _unitOfWork.OrderHeaderRepo.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
      }else{
           _unitOfWork.OrderHeaderRepo.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
      }
      _unitOfWork.Save();
      TempData["Success"] = "Order Canceled Successfully";
      return RedirectToAction(nameof(Details), new {orderId = OrderVM.OrderHeader.Id});
   }

   [ActionName("Details")]
   [HttpPost]
   public IActionResult Details_PAY_NOW()
   {
     OrderVM.OrderHeader = _unitOfWork.OrderHeaderRepo.Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties:"ApplicationUser");
     OrderVM.OrderDetail = _unitOfWork.OrderDetailRepo.GetAll(u => u.OrderHeaderId==OrderVM.OrderHeader.Id, includeProperties:"Product"); 
      var domain  = "http://localhost:5132/";
      var options = new Stripe.Checkout.SessionCreateOptions
      {
      SuccessUrl = domain+ $"order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
      CancelUrl = domain+ $"order/details?orderId={OrderVM.OrderHeader.Id}",
      LineItems = new List<Stripe.Checkout.SessionLineItemOptions>(),
      Mode = "payment",
      };

      foreach(var item in OrderVM.OrderDetail)
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
      _unitOfWork.OrderHeaderRepo.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
      _unitOfWork.Save();
      Response.Headers.Add("Location", session.Url);
      return new StatusCodeResult(303); 
   }

   public IActionResult PaymentConfirmation(int orderHeaderId)
    {
        OrderHeader orderHeader = _unitOfWork.OrderHeaderRepo.Get(u => u.Id == orderHeaderId);
        if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment){
            var service  = new SessionService();
            Session session = service.Get(orderHeader.SessionId);
               if (session.PaymentStatus.ToLower() == "paid") {
					_unitOfWork.OrderHeaderRepo.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unitOfWork.OrderHeaderRepo.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unitOfWork.Save();
        }
        }
        return View(orderHeaderId);
    }
    


     #region API CALLS

    [HttpGet]
       public IActionResult GetAll(string status)
       {
          IEnumerable<OrderHeader> objOrderHeaders;
          if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
          {
            objOrderHeaders = _unitOfWork.OrderHeaderRepo.GetAll(includeProperties:"ApplicationUser").ToList();
          }else{
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            
            objOrderHeaders = _unitOfWork.OrderHeaderRepo.GetAll(u => u.ApplicationUserId == userId, includeProperties:"ApplicationUser");
          }
           switch (status) {
        case "pending":
            objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
            break;
        case "inprocess":
            objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
            break;
        case "completed":
            objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.StatusShipped);
            break;
        case "approved":
             objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.StatusApproved);
            break;
        default:
            break;

    }
          return Json(new {data = objOrderHeaders});
       }
       #endregion
}
