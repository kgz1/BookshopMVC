﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookShopByKg;

[Authorize(Roles = SD.Role_Admin)]
public class ProductController : Controller
{
private readonly IUnitOfWork _unitOfWork;
private readonly IWebHostEnvironment _webHostEnvironment;

    public ProductController(IUnitOfWork unitOfWork, IWebHostEnvironment webHostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _webHostEnvironment = webHostEnvironment;
    }
    public IActionResult Index()
    {
        List<Product> objProductList = _unitOfWork.ProductRepo.GetAll(includeProperties:"Category").ToList();
        return View(objProductList);
    }

    public IActionResult Upsert(int? id)
    {
        ProductVM productVM = new ()
        {
           CategoryList = _unitOfWork.CategoryRepo.GetAll().Select(u => new SelectListItem
        {
             Text = u.Name,
             Value = u.Id.ToString()
        }),
        Product = new Product()
        }; 
        if(id == null || id == 0)
        {
             return View(productVM);
        }else
        {
            productVM.Product = _unitOfWork.ProductRepo.Get(u => u.Id == id);
            return View(productVM);
        }
    }
    [HttpPost]
     public IActionResult Upsert(ProductVM productVM, IFormFile? file)
    {
        if(ModelState.IsValid)
        { 
        string wwwRootPath = _webHostEnvironment.WebRootPath;
        if(file != null)
        {
            string fileName = Guid.NewGuid().ToString()+Path.GetExtension(file.FileName);
            string productPath = Path.Combine(wwwRootPath, @"images\product");

            if(!string.IsNullOrEmpty(productVM.Product.ImageUrl))
            {
               var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));
               if(System.IO.File.Exists(oldImagePath))
               {
                System.IO.File.Delete(oldImagePath);
               }
            }

            using(var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
            {
                file.CopyTo(fileStream);
            }   
            productVM.Product.ImageUrl = @"\images\product\" + fileName;
        } 
        if(productVM.Product.Id == 0)
        {
             _unitOfWork.ProductRepo.Add(productVM.Product);
        }else
        {
            _unitOfWork.ProductRepo.Update(productVM.Product);
        }
        _unitOfWork.Save();
        TempData["success"] = "Product created successfully";
        return RedirectToAction("Index");
        }
        else
        {
            productVM.CategoryList = _unitOfWork.CategoryRepo.GetAll().Select(u => new SelectListItem
            {
                Text = u.Name,
                Value = u.Id.ToString()
            });
              return View(productVM);
        }
    }
   
     #region API CALLS
       
       [HttpGet]
       public IActionResult GetAll()
       {
          List<Product> objProductList = _unitOfWork.ProductRepo.GetAll(includeProperties:"Category").ToList();
          return Json(new {data = objProductList});
       }

        [HttpDelete]
        public IActionResult Delete(int? id)
       {
        var productToBeDeleted = _unitOfWork.ProductRepo.Get(u => u.Id == id);
        if(productToBeDeleted == null)
        {
            return Json(new { success=false, Message="Error while deleting"});
        }
        
        var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl.TrimStart('\\'));
             
               if(System.IO.File.Exists(oldImagePath))
               {
                System.IO.File.Delete(oldImagePath);
               }

               _unitOfWork.ProductRepo.Remove(productToBeDeleted);
               _unitOfWork.Save();

         return Json(new { success=true, Message="Delete Successful"});
       }

     #endregion
}
