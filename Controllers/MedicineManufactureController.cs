using HMS.Data;
using HMS.Models;
using HMS.Models.BedCategoriesViewModel;
using HMS.Models.MedicineManufactureViewModel;
using HMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;


namespace HMS.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class MedicineManufactureController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICommon _iCommon;
        private string _hospitalId;
        private readonly ILogger<MedicineManufactureController> _logger;
        private string _role;

        public MedicineManufactureController(ApplicationDbContext context, ICommon iCommon, ILogger<MedicineManufactureController> logger)
        {
            _context = context;
            _iCommon = iCommon;
            _logger = logger;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _hospitalId = HttpContext.Session.GetString("HospitalId");
            _role = HttpContext.Session.GetString("Role");
            base.OnActionExecuting(context);
        }

        [Authorize(Roles = Pages.MainMenu.MedicineManufacture.RoleName)]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult GetDataTabelData()
        {
            try
            {
                var draw = HttpContext.Request.Form["draw"].FirstOrDefault();
                var start = Request.Form["start"].FirstOrDefault();
                var length = Request.Form["length"].FirstOrDefault();

                var sortColumn = Request.Form["columns[" + Request.Form["order[0][column]"].FirstOrDefault() + "][name]"].FirstOrDefault();
                var sortColumnAscDesc = Request.Form["order[0][dir]"].FirstOrDefault();
                var searchValue = Request.Form["search[value]"].FirstOrDefault();

                int pageSize = length != null ? Convert.ToInt32(length) : 0;
                int skip = start != null ? Convert.ToInt32(start) : 0;
                int resultTotal = 0;

                var _GetGridItem = GetGridItem(Convert.ToInt64(_hospitalId));

                //Sorting
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnAscDesc)))
                {
                    _GetGridItem = _GetGridItem.OrderBy(sortColumn + " " + sortColumnAscDesc);
                }

                //Search
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToLower();
                    _GetGridItem = _GetGridItem.Where(obj => obj.Id.ToString().Contains(searchValue)
                    || obj.Name.ToLower().Contains(searchValue)
                    || obj.Address.ToLower().Contains(searchValue)
                    || obj.Description.ToLower().Contains(searchValue)
                    || obj.CreatedDate.ToString().Contains(searchValue));
                }

                resultTotal = _GetGridItem.Count();

                var result = _GetGridItem.Skip(skip).Take(pageSize).ToList();
                _logger.LogInformation("Error in getting Successfully.");
                return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in getting Medicine Manufacture.");
                throw;
            }

        }

        private IQueryable<MedicineManufactureGridViewModel> GetGridItem(long hospitalId)
        {
            try
            {
                if (_role == "SuperAdmin")
                {
                    return (from _MedicineManufacture in _context.MedicineManufacture
                            join _hospital in _context.Hospital on _MedicineManufacture.HospitalId equals _hospital.Id
                            into hospitalGroup
                            from _hospital in hospitalGroup.DefaultIfEmpty()
                            where _MedicineManufacture.Cancelled == false
                            select new MedicineManufactureGridViewModel

                            {
                                Id = _MedicineManufacture.Id,
                                Name = _MedicineManufacture.Name,
                                Address = _MedicineManufacture.Address,
                                Description = _MedicineManufacture.Description,
                                CreatedDate = _MedicineManufacture.CreatedDate,
                                ModifiedDate = _MedicineManufacture.ModifiedDate,
                                CreatedBy = _MedicineManufacture.CreatedBy,
                                Hospital = _hospital.HospitalName

                            }).OrderByDescending(x => x.Id);
                }
                else
                {
                    return (from _MedicineManufacture in _context.MedicineManufacture
                            where _MedicineManufacture.Cancelled == false && _MedicineManufacture.HospitalId == hospitalId
                            select new MedicineManufactureGridViewModel
                            {
                                Id = _MedicineManufacture.Id,
                                Name = _MedicineManufacture.Name,
                                Address = _MedicineManufacture.Address,
                                Description = _MedicineManufacture.Description,
                                CreatedDate = _MedicineManufacture.CreatedDate,
                                ModifiedDate = _MedicineManufacture.ModifiedDate,
                                CreatedBy = _MedicineManufacture.CreatedBy,
                                Hospital = string.Empty

                            }).OrderByDescending(x => x.Id);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Add Medicine Manufacture.");
                throw;
            }
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();
            MedicineManufactureCRUDViewModel vm = await _context.MedicineManufacture.FirstOrDefaultAsync(m => m.Id == id);
            if (vm == null) return NotFound();
            return PartialView("_Details", vm);
        }

        public async Task<IActionResult> AddEdit(int id)
        {
            
            ViewBag.ddlHospital = new SelectList(_iCommon.GetTableData<Hospital>(_context), "Id", "HospitalName");
            ViewBag.Role = _role;
            MedicineManufactureCRUDViewModel vm = new MedicineManufactureCRUDViewModel();
            var data = await _context.MedicineManufacture.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (id > 0)
                vm = await _context.MedicineManufacture.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (data != null)
            {
                vm.HospitalId = data.HospitalId;
            }
            return PartialView("_AddEdit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEdit(MedicineManufactureCRUDViewModel vm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        MedicineManufacture _MedicineManufacture = new MedicineManufacture();
                        if (vm.Id > 0)
                        {
                            _MedicineManufacture = await _context.MedicineManufacture.FindAsync(vm.Id);

                            vm.CreatedDate = _MedicineManufacture.CreatedDate;
                            vm.CreatedBy = _MedicineManufacture.CreatedBy;
                            vm.ModifiedDate = DateTime.Now;
                            vm.ModifiedBy = HttpContext.User.Identity.Name;
                            if (_role == "SuperAdmin")
                            {
                                vm.HospitalId = vm.HospitalId;
                            }
                            else
                            {
                                vm.HospitalId = Convert.ToInt64(_hospitalId);
                            }
                           
                            _context.Entry(_MedicineManufacture).CurrentValues.SetValues(vm);
                            await _context.SaveChangesAsync();
                            TempData["successAlert"] = "Medicine Manufacture Updated Successfully. ID: " + _MedicineManufacture.Id;
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {
                            _MedicineManufacture = vm;
                            _MedicineManufacture.CreatedDate = DateTime.Now;
                            _MedicineManufacture.ModifiedDate = DateTime.Now;
                            _MedicineManufacture.CreatedBy = HttpContext.User.Identity.Name;
                            _MedicineManufacture.ModifiedBy = HttpContext.User.Identity.Name;
                            if (_role == "SuperAdmin")
                            {
                                _MedicineManufacture.HospitalId = vm.HospitalId;
                            }
                            else
                            {
                                _MedicineManufacture.HospitalId = Convert.ToInt64(_hospitalId);
                            }
                           
                            _context.Add(_MedicineManufacture);
                            await _context.SaveChangesAsync();
                            TempData["successAlert"] = "Medicine Manufacture Created Successfully. ID: " + _MedicineManufacture.Id;
                            return RedirectToAction(nameof(Index));
                        }
                    }
                    TempData["errorAlert"] = "Operation failed.";
                    return View("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IsExists(vm.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError( "Error in Add Or Update Medicine Manufacture.");
                        throw;
                    }
                }
            }
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Int64 id)
        {
            try
            {
                var _MedicineManufacture = await _context.MedicineManufacture.FindAsync(id);
                _MedicineManufacture.ModifiedDate = DateTime.Now;
                _MedicineManufacture.ModifiedBy = HttpContext.User.Identity.Name;
                _MedicineManufacture.Cancelled = true;

                _context.Update(_MedicineManufacture);
                await _context.SaveChangesAsync();
                return new JsonResult(_MedicineManufacture);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Delete Medicine Manufacture.");
                throw;
            }
        }

        private bool IsExists(long id)
        {
            return _context.MedicineManufacture.Any(e => e.Id == id);
        }
    }
}
