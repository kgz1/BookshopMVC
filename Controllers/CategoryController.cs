using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShopByKg;

[Authorize(Roles = SD.Role_Admin)]
public class CategoryController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public CategoryController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public IActionResult Index()
    {
        List<Category> objCategoryList = _unitOfWork.CategoryRepo.GetAll().ToList();
        return View(objCategoryList);
    }

    public IActionResult Create()
    {
        return View();
    }
    [HttpPost]
     public IActionResult Create(Category obj)
    {
        if(obj.Name == obj.DisplayOrder.ToString())
        {
            ModelState.AddModelError("name", "The DisplayOrder cannot match Name!");
        }
        if(ModelState.IsValid)
        { 
        _unitOfWork.CategoryRepo.Add(obj);
        _unitOfWork.Save();
        TempData["success"] = "Category created successfully";
          return RedirectToAction("Index");
        }
        return View();
    }
    public IActionResult Edit(int? id)
    {
        if(id==0 || id==null)
        {
            return NotFound();
        }
        Category? categoryFromDb = _unitOfWork.CategoryRepo.Get(u => u.Id == id);
        if(categoryFromDb==null)
        {
            return NotFound();
        }
        return View(categoryFromDb);
    }
    [HttpPost]
     public IActionResult Edit(Category obj)
    {
        if(ModelState.IsValid)
        { 
        _unitOfWork.CategoryRepo.Update(obj);
        _unitOfWork.Save();
          TempData["success"] = "Category updated successfully";
          return RedirectToAction("Index");
        }
        return View();
    }
      public IActionResult Delete(int? id)
    {
        if(id==0 || id==null)
        {
            return NotFound();
        }
        Category? categoryFromDb = _unitOfWork.CategoryRepo.Get(u => u.Id == id);
        if(categoryFromDb==null)
        {
            return NotFound();
        }
        return View(categoryFromDb);
    }
    [HttpPost, ActionName("Delete")]
     public IActionResult DeletePOST(int? id)
    {
        Category? obj = _unitOfWork.CategoryRepo.Get(u => u.Id == id);
        if(obj==null)
        {
            return NotFound();
        }
         _unitOfWork.CategoryRepo.Remove(obj);
         _unitOfWork.Save();
           TempData["success"] = "Category deleted successfully";
          return RedirectToAction("Index");
    }
}
