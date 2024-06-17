using HMS.Data;
using HMS.Models;
using HMS.Models.BedCategoriesViewModel;
using HMS.Models.UnitViewModel;
using HMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;


namespace HMS.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class UnitController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICommon _iCommon;
        private string _hospitalId;
        private string _role;
        private readonly ILogger<UnitController> _logger;

        public UnitController(ApplicationDbContext context, ICommon iCommon, ILogger<UnitController> logger)
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

        [Authorize(Roles = Pages.MainMenu.Unit.RoleName)]
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
                    || obj.Description.ToLower().Contains(searchValue)
                    || obj.CreatedDate.ToString().ToLower().Contains(searchValue));
                }

                resultTotal = _GetGridItem.Count();

                var result = _GetGridItem.Skip(skip).Take(pageSize).ToList();
                _logger.LogInformation("Error in getting Successfully.");
                return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in getting Unit.");
                throw;
            }

        }

        private IQueryable<UnitGridViewModel> GetGridItem(long hospitalId)
        {
            try
            {
                if (_role == "SuperAdmin")
                {
                    return (from _Unit in _context.Unit
                            join _hospital in _context.Hospital on _Unit.HospitalId equals _hospital.Id
                            into hospitalGroup
                            from _hospital in hospitalGroup.DefaultIfEmpty()
                            where _Unit.Cancelled == false
                            select new UnitGridViewModel
                            {
                                Id = _Unit.Id,
                                Name = _Unit.Name,
                                Description = _Unit.Description,
                                CreatedDate = _Unit.CreatedDate,
                                Hospital = _hospital.HospitalName

                            }).OrderByDescending(x => x.Id);
                }
                else
                {
                    return (from _Unit in _context.Unit
                            where _Unit.Cancelled == false && _Unit.HospitalId == hospitalId
                            select new UnitGridViewModel
                            {
                                Id = _Unit.Id,
                                Name = _Unit.Name,
                                Description = _Unit.Description,
                                CreatedDate = _Unit.CreatedDate,
                                Hospital = string.Empty
                            }).OrderByDescending(x => x.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Add Unit.");
                throw;
            }
        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();
            UnitCRUDViewModel vm = await _context.Unit.FirstOrDefaultAsync(m => m.Id == id);
            if (vm == null) return NotFound();
            return PartialView("_Details", vm);
        }

        public async Task<IActionResult> AddEdit(int id)
        {
            //UnitCRUDViewModel vm = new UnitCRUDViewModel();
            //if (id > 0) vm = await _context.Unit.Where(x => x.Id == id).SingleOrDefaultAsync();
            //return PartialView("_AddEdit", vm);

            ViewBag.ddlHospital = new SelectList(_iCommon.GetTableData<Hospital>(_context), "Id", "HospitalName");
            ViewBag.Role = _role;
            UnitCRUDViewModel vm = new UnitCRUDViewModel();

            var data = await _context.Unit.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (id > 0)
                vm = await _context.Unit.Where(x => x.Id == id).FirstOrDefaultAsync();
            if (data != null)
            {
                vm.HospitalId = data.HospitalId;
            }
            return PartialView("_AddEdit", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEdit(UnitCRUDViewModel vm)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        Unit _Unit = new Unit();
                        if (vm.Id > 0)
                        {
                            _Unit = await _context.Unit.FindAsync(vm.Id);

                            vm.CreatedDate = _Unit.CreatedDate;
                            vm.CreatedBy = _Unit.CreatedBy;
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
                            //vm.HospitalId = Convert.ToInt64(_hospitalId);
                            _context.Entry(_Unit).CurrentValues.SetValues(vm);
                            await _context.SaveChangesAsync();
                            TempData["successAlert"] = "Unit Updated Successfully. ID: " + _Unit.Id;
                            return RedirectToAction(nameof(Index));
                        }
                        else
                        {           
                            _Unit = vm;
                            _Unit.CreatedDate = DateTime.Now;
                            _Unit.ModifiedDate = DateTime.Now;
                            _Unit.CreatedBy = HttpContext.User.Identity.Name;
                            _Unit.ModifiedBy = HttpContext.User.Identity.Name;
                            if (_role == "SuperAdmin")
                            {
                                _Unit.HospitalId = vm.HospitalId;
                            }
                            else
                            {
                                _Unit.HospitalId = Convert.ToInt64(_hospitalId);
                            }
                           // _Unit.HospitalId = Convert.ToInt64(_hospitalId);
                            _context.Add(_Unit);
                            await _context.SaveChangesAsync();
                            TempData["successAlert"] = "Unit Created Successfully. ID: " + _Unit.Id;
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
                        _logger.LogError("Error in Add Or Update Unit.");
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
                var _Unit = await _context.Unit.FindAsync(id);
                _Unit.ModifiedDate = DateTime.Now;
                _Unit.ModifiedBy = HttpContext.User.Identity.Name;
                _Unit.Cancelled = true;

                _context.Update(_Unit);
                await _context.SaveChangesAsync();
                return new JsonResult(_Unit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Delete Unit.");
                throw;
            }
        }

        private bool IsExists(long id)
        {
            return _context.Unit.Any(e => e.Id == id);
        }
    }
}
