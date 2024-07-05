using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShopByKg;

[Authorize(Roles = SD.Role_Admin)]
public class CompanyController : Controller
{
private readonly IUnitOfWork _unitOfWork;

    public CompanyController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public IActionResult Index()
    {
        List<Company> objCompanyList = _unitOfWork.CompanyRepo.GetAll().ToList();
        return View(objCompanyList);
    }

    public IActionResult Upsert(int? id)
    {
        if(id == null || id == 0)
        {
             return View(new Company());
        }else
        {
            Company companyObj = _unitOfWork.CompanyRepo.Get(u => u.Id == id);
            return View(companyObj);
        }
    }
    [HttpPost]
     public IActionResult Upsert(Company CompanyObj)
    {
        if(ModelState.IsValid)
        { 
        if(CompanyObj.Id == 0)
        {
             _unitOfWork.CompanyRepo.Add(CompanyObj);
        }else
        {
            _unitOfWork.CompanyRepo.Update(CompanyObj);
        }
        _unitOfWork.Save();
        TempData["success"] = "Company created successfully";
        return RedirectToAction("Index");
        }
        else
        {
              return View(CompanyObj);
        }
    }
   
     #region API CALLS
       
       [HttpGet]
       public IActionResult GetAll()
       {
          List<Company> objCompanyList = _unitOfWork.CompanyRepo.GetAll().ToList();
          return Json(new {data = objCompanyList});
       }

        [HttpDelete]
        public IActionResult Delete(int? id)
       {
        var companyToBeDeleted = _unitOfWork.CompanyRepo.Get(u => u.Id == id);
        if(companyToBeDeleted == null)
        {
            return Json(new { success=false, Message="Error while deleting"});
        }
        
               _unitOfWork.CompanyRepo.Remove(companyToBeDeleted);
               _unitOfWork.Save();

         return Json(new { success=true, Message="Delete Successful"});
       }

     #endregion

}
