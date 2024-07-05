using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BookShopByKg.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BookShopByKg.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        IEnumerable<Product> productList = _unitOfWork.ProductRepo.GetAll(includeProperties:"Category");
        return View(productList);
    }
    public IActionResult Details(int productId)
    {
        ShoppingCart cart = new ()
        {
            Product = _unitOfWork.ProductRepo.Get(u => u.Id == productId, includeProperties:"Category"),
            Count = 1,
            ProductId = productId
        };
        return View(cart);
    }

[HttpPost]
[Authorize]
 public IActionResult Details(ShoppingCart shoppingCart)
    {
       var claimsIdentity = (ClaimsIdentity)User.Identity;
       var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
       shoppingCart.ApplicationUserId = userId;
       ShoppingCart cartFromDb = _unitOfWork.ShoppingRepo.Get(u => u.ApplicationUserId == userId 
       && u.ProductId == shoppingCart.ProductId);
       if(cartFromDb != null)
       {
         cartFromDb.Count += shoppingCart.Count;
         _unitOfWork.ShoppingRepo.Update(cartFromDb);
       }else
       {
        _unitOfWork.ShoppingRepo.Add(shoppingCart);
       }

       _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
