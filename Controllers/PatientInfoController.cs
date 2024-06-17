using HMS.ConHelper;
using HMS.Data;
using HMS.Models;
using HMS.Models.ManageUserRolesVM;
using HMS.Models.PatientInfoViewModel;
using HMS.Models.UserProfileViewModel;
using HMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace HMS.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class PatientInfoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ICommon _iCommon;
        private readonly IAccount _iAccount;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PatientInfoController> _logger;
        private string _UserName;

        public PatientInfoController(ApplicationDbContext context, ICommon iCommon, IAccount iAccount, UserManager<ApplicationUser> userManager, IEmailSender emailSender, ILogger<PatientInfoController> logger)
        {
            _context = context;
            _iCommon = iCommon;
            _iAccount = iAccount;
            _userManager = userManager;
            _emailSender = emailSender;
            _logger = logger;
        }

        [Authorize(Roles = Pages.MainMenu.PatientInfo.RoleName)]
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

                var _GetGridItem = _iCommon.GetPatientInfoGridItem();
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
                    || obj.FirstName.ToLower().Contains(searchValue)
                    || obj.LastName.ToLower().Contains(searchValue)
                    || obj.MaritalStatus.ToLower().Contains(searchValue)
                    || obj.Gender.ToLower().Contains(searchValue)
                    || obj.SpouseName.ToLower().Contains(searchValue)
                    || obj.BloodGroup.ToLower().Contains(searchValue)
                    || obj.DateOfBirth.ToString().ToLower().Contains(searchValue));
                }

                resultTotal = _GetGridItem.Count();

                var result = _GetGridItem.Skip(skip).Take(pageSize).ToList();
                _logger.LogInformation("Error in getting Successfully.");
                return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in getting Patient Info.");
                throw;
            }

        }

        public async Task<IActionResult> Details(long? id)
        {
            if (id == null) return NotFound();
            PatientInfoCRUDViewModel vm = await _iCommon.GetPatientInfoGridItem().Where(x => x.Id == id).SingleOrDefaultAsync();
            if (vm == null) return NotFound();
            return PartialView("_Details", vm);
        }

        public async Task<IActionResult> AddEdit(int id)
        {
            ViewBag.ddlManageUserRoles = new SelectList(_iCommon.GetTableData<ManageUserRoles>(_context).Where(x => x.Cancelled == false), "Id", "Name");
            if (id > 0)
            {
                try
                {
                    ManagePatientInfoViewModel vm = new();
                    vm.UserProfileCRUDViewModel = await _context.UserProfile.Where(x => x.UserProfileId == id).SingleOrDefaultAsync();
                    vm.PatientInfoEditViewModel = await _context.PatientInfo.Where(x => x.ApplicationUserId == vm.UserProfileCRUDViewModel.ApplicationUserId).SingleOrDefaultAsync();
                    return PartialView("_Edit", vm);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Error in AddEdit" + ex);
                    throw;
                }
            }
            return PartialView("_Add", new PatientInfoCRUDViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(PatientInfoCRUDViewModel _PatientInfoCRUDViewModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var _ApplicationUser = await _iAccount.CreateUserProfile(_PatientInfoCRUDViewModel, HttpContext.User.Identity.Name);
                    if (_ApplicationUser.Item2 == "Success")
                    {
                        PatientInfo _PatientInfo = new()
                        {
                            ApplicationUserId = _ApplicationUser.Item1.Id,

                            SpouseName = _PatientInfoCRUDViewModel?.SpouseName,
                            Gender = _PatientInfoCRUDViewModel?.Gender,
                            BloodGroup = _PatientInfoCRUDViewModel?.BloodGroup,
                            FatherName = _PatientInfoCRUDViewModel?.FatherName,
                            MotherName = _PatientInfoCRUDViewModel?.MotherName,
                            RegistrationFee = _PatientInfoCRUDViewModel?.RegistrationFee,
                            Agreement = _PatientInfoCRUDViewModel?.Agreement,
                            MaritalStatus = _PatientInfoCRUDViewModel?.MaritalStatus,
                            Remarks = _PatientInfoCRUDViewModel?.Remarks,
                            PatientCode = _PatientInfoCRUDViewModel.PatientCode,

                            CreatedDate = DateTime.Now,
                            ModifiedDate = DateTime.Now,
                            CreatedBy = HttpContext.User.Identity.Name,
                            ModifiedBy = HttpContext.User.Identity.Name
                        };
                        await _context.PatientInfo.AddAsync(_PatientInfo);
                        var result = await _context.SaveChangesAsync();

                        var _DefaultIdentityOptions = await _context.DefaultIdentityOptions.FirstOrDefaultAsync(m => m.Id == 1);
                        if (_DefaultIdentityOptions.SignInRequireConfirmedEmail)
                        {
                            var _ConfirmationToken = await _userManager.GenerateEmailConfirmationTokenAsync(_ApplicationUser.Item1);
                            var callbackUrl = Url.EmailConfirmationLink(_ApplicationUser.Item1.Id, _ConfirmationToken, Request.Scheme);
                            await _emailSender.SendEmailConfirmationAsync(_ApplicationUser.Item1.Email, callbackUrl);
                        }

                        TempData["successAlert"] = "Patient User Created Successfully. ID: " + _ApplicationUser.Item1.Email;
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        TempData["errorAlert"] = "Patient User Creation Failed." + _ApplicationUser.Item2;
                        return View("Index");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IsExists(_PatientInfoCRUDViewModel.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError("Error in Add Patient Info.");
                        throw;
                    }
                }
            }
            return View(_PatientInfoCRUDViewModel);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ManagePatientInfoViewModel vm)
        {
            if (ModelState.IsValid)
            {
                string errorMessage = string.Empty;
                PatientInfo _PatientInfo = new PatientInfo();
                try
                {
                    if (ModelState.IsValid)
                    {
                        _PatientInfo = await _context.PatientInfo.FindAsync(vm.PatientInfoEditViewModel.Id);

                        vm.UserProfileCRUDViewModel.ModifiedDate = DateTime.Now;
                        vm.UserProfileCRUDViewModel.ModifiedBy = HttpContext.User.Identity.Name;
                        _context.Entry(_PatientInfo).CurrentValues.SetValues(vm.PatientInfoEditViewModel);
                        await _context.SaveChangesAsync();

                        //UserProfile...
                        UserProfile _UserProfile = await _context.UserProfile.FindAsync(vm.UserProfileCRUDViewModel.UserProfileId);
                        if (vm.UserProfileCRUDViewModel.ProfilePictureDetails != null)
                            vm.UserProfileCRUDViewModel.ProfilePicture = "/upload/" + _iCommon.UploadedFile(vm.UserProfileCRUDViewModel.ProfilePictureDetails);

                        vm.UserProfileCRUDViewModel.ModifiedDate = DateTime.Now;
                        vm.UserProfileCRUDViewModel.ModifiedBy = HttpContext.User.Identity.Name;
                        _context.Entry(_UserProfile).CurrentValues.SetValues(vm.UserProfileCRUDViewModel);
                        await _context.SaveChangesAsync();

                        TempData["successAlert"] = "Patient Info Updated Successfully. ID: " + _PatientInfo.Id;
                        return RedirectToAction(nameof(Index));
                    }
                    TempData["errorAlert"] = "Patient User Update Failed." + errorMessage;
                    return View("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!IsExists(_PatientInfo.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        _logger.LogError("Error in Update Patient Info.");
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
                var _PatientInfo = await _context.PatientInfo.FindAsync(id);
                _PatientInfo.ModifiedDate = DateTime.Now;
                _PatientInfo.ModifiedBy = HttpContext.User.Identity.Name;
                _PatientInfo.Cancelled = true;

                _context.Update(_PatientInfo);
                await _context.SaveChangesAsync();
                return new JsonResult(_PatientInfo);
            }
            catch (Exception)
            {
                _logger.LogError("Error in Delete PatientInfo.");
                throw;
            }
        }
        private bool IsExists(long id)
        {
            return _context.PatientInfo.Any(e => e.Id == id);
        }
    }
}
