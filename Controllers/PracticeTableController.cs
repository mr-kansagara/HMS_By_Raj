using HMS.Data;
using HMS.Models;
using HMS.Models.PracticeTable;
using HMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using static HMS.Pages.MainMenu;

namespace HMS.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class PracticeTableController : Controller
    {
        private readonly IWebHostEnvironment _iHostingEnvironment;
        private readonly ApplicationDbContext _context;
        private readonly ICommon _iCommon;
        private readonly ILogger<PracticeTableController> _logger;
        private string _hospitalId;
        private string _role;

        public PracticeTableController(ApplicationDbContext context, ICommon iCommon, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IRoles roles, ILogger<PracticeTableController> logger, IWebHostEnvironment iHostingEnvironment)
        {
            _iHostingEnvironment = iHostingEnvironment;
            _context = context;
            _iCommon = iCommon;
            _logger = logger;
            _iHostingEnvironment = iHostingEnvironment;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _hospitalId = HttpContext.Session.GetString("HospitalId");
            _role = HttpContext.Session.GetString("Role");
            base.OnActionExecuting(context);
        }

        //[Authorize(Roles = Pages.MainMenu.PracticeTable.RoleName)]
        [HttpGet]
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

                //var _getGridItem = _context.PracticeTable.ToList();
                var data = _hospitalId;
                var _getGridItem = GetGridItem(Convert.ToInt64(_hospitalId));
                //Sorting
                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(sortColumnAscDesc)))
                {
                    _getGridItem = _getGridItem.OrderBy(sortColumn + " " + sortColumnAscDesc);
                }

                //Search
                if (!string.IsNullOrEmpty(searchValue))
                {
                    searchValue = searchValue.ToLower();
                    _getGridItem = _getGridItem.Where(obj => obj.Id.ToString().Contains(searchValue)
                   || obj.Title.ToLower().Contains(searchValue)
                   || obj.Description.ToLower().Contains(searchValue)
                   || obj.ShortDescription.ToString().Contains(searchValue));
                }

                resultTotal = _getGridItem.Count();
                _logger.LogInformation("Error in getting Successfully.");
                var result = _getGridItem.Skip(skip).Take(pageSize).ToList();

                return Json(new { draw = draw, recordsFiltered = resultTotal, recordsTotal = resultTotal, data = result });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in getting User Role.");
                throw;
            }
        }


        private IQueryable<PracticeTableCRUDModel> GetGridItem(long hospitalId)
        {
            try
            {
                if(_role == "SuperAdmin")
                {
                    return (from _practiceTable in _context.PracticeTable
                            join _practiceTableImage in _context.PracticeTableImage on _practiceTable.ImageId equals _practiceTableImage.ImageId
                            join _hospital in _context.Hospital on _practiceTable.HospitalId equals _hospital.Id 
                            into hospitalGroup
                            from _hospital in hospitalGroup.DefaultIfEmpty()
                            select new PracticeTableCRUDModel
                            {
                                Id = _practiceTable.Id,
                                Title = _practiceTable.Title,
                                Description = _practiceTable.Description,
                                ShortDescription = _practiceTable.ShortDescription,
                                AddedOn = _practiceTable.AddedOn,
                                ModifiedOn = _practiceTable.ModifiedOn,
                                IsActive = _practiceTable.IsActive,
                                ImageId = _practiceTable.ImageId,
                                ProfilePath = _practiceTableImage.ImagePath ?? "/upload/blank-person.png",
                                Hospital = _hospital.HospitalName
                            }).OrderByDescending(p => p.Id);
                }
                else
                {
                    return (from _practiceTable in _context.PracticeTable
                            join _practiceTableImage in _context.PracticeTableImage on _practiceTable.ImageId equals _practiceTableImage.ImageId
                            join _hospital in _context.Hospital on _practiceTable.HospitalId equals _hospital.Id into hospitalGroup
                            from hospital in hospitalGroup.DefaultIfEmpty() where _practiceTable.HospitalId == hospitalId
                            select new PracticeTableCRUDModel
                            {
                                Id = _practiceTable.Id,
                                Title = _practiceTable.Title,
                                Description = _practiceTable.Description,
                                ShortDescription = _practiceTable.ShortDescription,
                                AddedOn = _practiceTable.AddedOn,
                                ModifiedOn = _practiceTable.ModifiedOn,
                                IsActive = _practiceTable.IsActive,
                                ImageId = _practiceTable.ImageId,
                                ProfilePath = _context.PracticeTableImage.FirstOrDefault(p => p.ImageId == _practiceTable.ImageId).ImagePath ?? "/upload/blank-person.png",
                                Hospital  = string.Empty

                            }).OrderByDescending(p => p.Id);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in the profile picture.");
                throw;
            }
        }
        

        [HttpGet]
        public async Task<IActionResult> AddEdit(Guid id)
        {
            PracticeTableCRUDModel model = new();
            if (id != Guid.Empty)
            {
                model = await _context.PracticeTable.Where(p=>p.Id == id).SingleOrDefaultAsync();

                var profilePicture = await _context.PracticeTableImage.Where(p => p.ImageId == model.ImageId).FirstOrDefaultAsync();
                if (profilePicture != null)
                {
                    model.ProfilePath = profilePicture.ImagePath;
                }
                return PartialView("_AddEdit", model);
            }
            return PartialView("_AddEdit", model);
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(PracticeTableCRUDModel addData)
        {
            try
            {
                if (addData.Id == Guid.Empty)
                {
                    PracticeTableModel add = new PracticeTableModel();


                    add.ImageId = GetImageFileDetails(addData.ProfileImage, "PracticeTableImage", add.Id);
                    add.Id = Guid.NewGuid();
                    add.Title = addData.Title;
                    add.Description = addData.Description;
                    add.ShortDescription = addData.ShortDescription;
                    add.AddedOn = DateTime.Now;
                    add.IsActive = "1";
                    if (_role == "SuperAdmin")
                    {
                        add.HospitalId = addData.HospitalId;
                    }
                    else
                    {
                        add.HospitalId = Convert.ToInt64(_hospitalId);
                    }


                    _context.PracticeTable.Add(add);
                    await _context.SaveChangesAsync();
                    var _AlertMessage = "Data has been Successfully Added. ID: " + add.Title;
                    return new JsonResult(_AlertMessage);
                }
                else
                {
                    var modelData = await _context.PracticeTable.FirstOrDefaultAsync(x => x.Id == addData.Id);

                    modelData.ImageId = GetImageFileDetails(addData.ProfileImage, "PracticeTableImage", modelData.ImageId);
                    modelData.Title = addData.Title;
                    modelData.Description = addData.Description;
                    modelData.ShortDescription = addData.ShortDescription;
                    modelData.ModifiedOn = DateTime.Now;
                    modelData.IsActive = "1";
                    if (_role == "SuperAdmin")
                    {
                        modelData.HospitalId = modelData.HospitalId;
                    }
                    else
                    {
                        modelData.HospitalId = Convert.ToInt64(_hospitalId);
                    }

                    _context.PracticeTable.Update(modelData);
                    await _context.SaveChangesAsync();
                    var _AlertMessage = "Data has been added successfully. Title: " + addData.Title;
                    return new JsonResult(_AlertMessage);
                }
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Error in Add Or Update Data in the practice table.");
                return new JsonResult(ex.Message);
                throw;
            }
        }

        [HttpPost]
        public async Task<JsonResult> Delete(Guid id)
        {
            try
            {
                var _practiceTable = await _context.PracticeTable.FindAsync(id);
                _context.Remove(_practiceTable);
                await _context.SaveChangesAsync();
                return new JsonResult(_practiceTable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in delete data from the practice table.");
                throw;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            PracticeTableCRUDModel model = await _context.PracticeTable.FirstOrDefaultAsync(x => x.Id == id);
            
            var ProfilePicture = await _context.PracticeTableImage.Where(p => p.ImageId == model.ImageId).FirstOrDefaultAsync();
            if (ProfilePicture != null)
            {
                model.ProfilePath = ProfilePicture.ImagePath;
            }

            return PartialView("_Info", model);
        }


        public Guid GetImageFileDetails(IFormFile ProfileImage, string folderName, Guid ImageId)
        {
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                string uploadsFolder = Path.Combine(_iHostingEnvironment.WebRootPath, "images", folderName); // Use the provided folder name
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate a unique file name
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ProfileImage.FileName).Replace(" ", "_");
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the uploaded file to the specified folder
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    ProfileImage.CopyTo(fileStream);
                }

                // Extract relative path after wwwroot
                var updatedFilePath = filePath.Split("wwwroot");
                string relativePath = updatedFilePath.Length > 1 ? updatedFilePath[1] : filePath;

                //if (ImageId.HasValue && ImageId.Value != 0)
                if (ImageId != Guid.Empty)
                {
                    // Find the existing image record
                    var existingImage = _context.PracticeTableImage.FirstOrDefault(img => img.ImageId == ImageId);

                    if (existingImage != null)
                    {
                        // Delete the old file from the folder
                        string oldFilePath = Path.Combine(_iHostingEnvironment.WebRootPath,"images",folderName,existingImage.PictureName);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }

                        // Update existing image record with new details
                        existingImage.PictureName = uniqueFileName;
                        existingImage.ImagePath = relativePath.Replace("\\", "/");
                        // Save changes to the database
                        _context.SaveChanges();

                        // Return the ID of the updated record
                        return existingImage.ImageId;
                    }
                }
                else
                {
                    // Create UserImages object and populate properties
                    var practiceTableImage = new PracticeTableImage()
                    {
                        ImageId = ImageId,
                        PictureName = uniqueFileName,
                        ImagePath = relativePath.Replace("\\", "/"),
                    };
                    // Add UserImages object to context
                    _context.PracticeTableImage.Add(practiceTableImage);
                    _context.SaveChanges();

                    // Return the ID of the inserted record
                    return practiceTableImage.ImageId;
                }
            }
            // Return the ImageId if no new image is uploaded
            return ImageId;
        }
    }
}






