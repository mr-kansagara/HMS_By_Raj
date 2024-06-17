﻿using HMS.ConHelper;
using HMS.Data;
using HMS.Helpers;
using HMS.Models;
using HMS.Models.CommonViewModel;
using HMS.Models.DashboardViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using static HMS.Pages.MainMenu;
using DoctorsInfo = HMS.Models.DoctorsInfo;
using PatientInfo = HMS.Models.PatientInfo;
using UserProfile = HMS.Models.UserProfile;

namespace HMS.Services
{
    public class Functional : IFunctional
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IRoles _roles;
        private readonly SuperAdminDefaultOptions _superAdminDefaultOptions;
        private readonly ApplicationInfo _applicationInfo;
        private readonly IAccount _iAccount;
        private UserRoleCountsModel userRolesCountsModel;
        private readonly ILogger<Functional> _logger;

        public Functional(UserManager<ApplicationUser> userManager,
           ApplicationDbContext context,
           IRoles roles,
           IOptions<SuperAdminDefaultOptions> superAdminDefaultOptions,
           IOptions<ApplicationInfo> applicationInfo,
           IOptions<UserRoleCountsModel> userRoleCountsModel,
           IAccount iAccount,
           ILogger<Functional> logger)
        {
            _userManager = userManager;
            _context = context;
            _roles = roles;
            _superAdminDefaultOptions = superAdminDefaultOptions.Value;
            _applicationInfo = applicationInfo.Value;
            userRolesCountsModel = userRoleCountsModel.Value;
            _iAccount = iAccount;
            _logger = logger;
        }

        public async Task SendEmailBySendGridAsync(string apiKey,
            string fromEmail,
            string fromFullName,
            string subject,
            string message,
            string email)
        {
            var client = new SendGridClient(apiKey);
            var msg = new SendGridMessage()
            {
                From = new EmailAddress(fromEmail, fromFullName),
                Subject = subject,
                PlainTextContent = message,
                HtmlContent = message
            };
            msg.AddTo(new EmailAddress(email, email));
            await client.SendEmailAsync(msg);

        }

        public async Task SendEmailByGmailAsync(string fromEmail,
            string fromFullName,
            string subject,
            string messageBody,
            string toEmail,
            string toFullName,
            string smtpUser,
            string smtpPassword,
            string smtpHost,
            int smtpPort,
            bool smtpSSL)
        {
            var body = messageBody;
            var message = new MailMessage();
            message.To.Add(new MailAddress(toEmail, toFullName));
            message.From = new MailAddress(fromEmail, fromFullName);
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;

            using (var smtp = new SmtpClient())
            {
                smtp.UseDefaultCredentials = false;
                var credential = new NetworkCredential
                {
                    UserName = smtpUser,
                    Password = smtpPassword
                };
                smtp.Credentials = credential;
                smtp.Host = smtpHost;
                smtp.Port = smtpPort;
                smtp.EnableSsl = smtpSSL;
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                await smtp.SendMailAsync(message);

            }
        }

        public async Task CreateDefaultSuperAdmin()
        {
            try
            {
                await _roles.GenerateRolesFromPageList();

                ApplicationUser superAdmin = new ApplicationUser();
                SeedData _SeedData = new();
                superAdmin.Email = _superAdminDefaultOptions.Email;
                superAdmin.UserName = superAdmin.Email;
                superAdmin.EmailConfirmed = true;
                var hospitalId = _context.Hospital.SingleOrDefault().Id;
                var result = await _userManager.CreateAsync(superAdmin, _superAdminDefaultOptions.Password);

                if (result.Succeeded)
                {
                    UserProfile _UserProfile = new()
                    {
                        //UserType = UserType.General,
                        EmployeeId = StaticData.RandomDigits(6),
                        RoleId = 1,
                        ApplicationUserId = superAdmin.Id,
                        FirstName = "Super",
                        LastName = "Admin",
                        PhoneNumber = "+8801674411603",
                        Email = superAdmin.Email,
                        Address = "R/A, Dhaka",
                        Country = "Bangladesh",
                        ProfilePicture = "/images/UserIcon/Admin.png",

                        Designation = 1,
                        Department = 1,
                        SubDepartment = 1,
                        JoiningDate = DateTime.Now.AddYears(-1),
                        LeavingDate = DateTime.Now,

                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        CreatedBy = "Admin",
                        ModifiedBy = "Admin",
                        HospitalId = hospitalId
                    };

                    await _context.UserProfile.AddAsync(_UserProfile);
                    await _context.SaveChangesAsync();

                    await _roles.AddToRoles(superAdmin);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Create Super Admin.");
                throw;
            }
        }


        public async Task CreateDefaultHospital()
        {
            try
            {
                SeedData _SeedData = new();
                var _getHospitalList = _SeedData.GetHospitalList();
                foreach (var item in _getHospitalList)
                {
                    Hospital _hospital = new Hospital
                    {
                        HospitalName = item.HospitalName,
                        Description = item.Description,
                        Address = item.Address,
                        ImageId = (long)item.ImageId,
                        CreatedDate = item.CreatedDate,
                        ModifiedDate = item.ModifiedDate,
                        CreatedBy = "Admin",
                        ModifiedBy = "Admin"
                    };
                    _context.Hospital.Add(_hospital);
                }
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Create Default Hospital.");
                throw;
            }
        }

        public async Task<string> UploadFile(List<IFormFile> files, IWebHostEnvironment env, string uploadFolder)
        {
            var result = "";

            var webRoot = env.WebRootPath;
            var uploads = Path.Combine(webRoot, uploadFolder);
            var extension = "";
            var filePath = "";
            var fileName = "";


            foreach (var formFile in files)
            {
                if (formFile.Length > 0)
                {
                    extension = Path.GetExtension(formFile.FileName);
                    fileName = Guid.NewGuid().ToString() + extension;
                    filePath = Path.Combine(uploads, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await formFile.CopyToAsync(stream);
                    }
                    result = fileName;
                }
            }
            return result;
        }

        public async Task InitAppData()
        {
            SeedData _SeedData = new();

            var _GetMedicineCategoriesList = _SeedData.GetMedicineCategoriesList();
            var hospitalId = _context.Hospital.SingleOrDefault().Id;
            foreach (var item in _GetMedicineCategoriesList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                item.HospitalId = hospitalId;
                _context.MedicineCategories.Add(item);
                await _context.SaveChangesAsync();
            }
            var _GetUnitList = _SeedData.GetUnitList();
            foreach (var item in _GetUnitList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                item.HospitalId = hospitalId;
                _context.Unit.Add(item);
                await _context.SaveChangesAsync();
            }
            var _GetMedicineManufactureList = _SeedData.GetMedicineManufactureList();
            foreach (var item in _GetMedicineManufactureList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                item.HospitalId = hospitalId;
                _context.MedicineManufacture.Add(item);
                await _context.SaveChangesAsync();
            }
            var _GetMedicinesList = _SeedData.GetMedicinesList();
            foreach (var item in _GetMedicinesList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                item.HospitalId = hospitalId;
                _context.Medicines.Add(item);
                await _context.SaveChangesAsync();
            }

            var _GetBedCategoriesList = _SeedData.GetBedCategoriesList();
            foreach (var item in _GetBedCategoriesList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                _context.BedCategories.Add(item);
                item.HospitalId = hospitalId;
                await _context.SaveChangesAsync();
            }

            var _GetBedList = _SeedData.GetBedList();
            foreach (var item in _GetBedList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                item.HospitalId = hospitalId;
                _context.Bed.Add(item);
                await _context.SaveChangesAsync();
            }

            var _GetExpenseCategoriesList = _SeedData.GetExpenseCategoriesList();
            foreach (var item in _GetExpenseCategoriesList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                item.HospitalId = hospitalId;
                _context.ExpenseCategories.Add(item);
                await _context.SaveChangesAsync();
            }

            var _GetPaymentCategoriesList = _SeedData.GetPaymentCategoriesList();
            foreach (var item in _GetPaymentCategoriesList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                item.HospitalId = hospitalId;
                _context.PaymentCategories.Add(item);
                await _context.SaveChangesAsync();
            }

            var _GetLabTestCategoriesList = _SeedData.GetLabTestCategoriesList();
            foreach (var item in _GetLabTestCategoriesList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                item.HospitalId = hospitalId;
                _context.LabTestCategories.Add(item);
                await _context.SaveChangesAsync();
            }
            var _GetLabTestsList = _SeedData.GetLabTestsList();
            foreach (var item in _GetLabTestsList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                item.HospitalId = hospitalId;
                _context.LabTests.Add(item);
                await _context.SaveChangesAsync();
            }

            var _GetCurrencyList = _SeedData.GetCurrencyList();
            foreach (var item in _GetCurrencyList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                item.HospitalId = hospitalId;
                _context.Currency.Add(item);
                await _context.SaveChangesAsync();
            }
            var _GetInsuranceCompanyInfoList = _SeedData.GetInsuranceCompanyInfoList();
            foreach (var item in _GetInsuranceCompanyInfoList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                item.HospitalId = hospitalId;
                _context.InsuranceCompanyInfo.Add(item);
                await _context.SaveChangesAsync();
            }
            var _GetManageUserRolesList = _SeedData.GetManageUserRolesList();
            foreach (var item in _GetManageUserRolesList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                item.HospitalId = hospitalId;
                _context.ManageUserRoles.Add(item);
                await _context.SaveChangesAsync();
            }
            var _GetDesignationList = _SeedData.GetDesignationList();
            foreach (var item in _GetDesignationList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                item.HospitalId = hospitalId;
                _context.Designation.Add(item);
                await _context.SaveChangesAsync();
            }
            var _GetDepartmentList = _SeedData.GetDepartmentList();
            foreach (var item in _GetDepartmentList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                _context.Department.Add(item);
                item.HospitalId = hospitalId;
                await _context.SaveChangesAsync();
            }
            var _GetSubDepartmentList = _SeedData.GetSubDepartmentList();
            foreach (var item in _GetSubDepartmentList)
            {
                item.CreatedDate = DateTime.Now;
                item.ModifiedDate = DateTime.Now;
                item.CreatedBy = "Admin";
                item.ModifiedBy = "Admin";
                _context.SubDepartment.Add(item);
                item.HospitalId = hospitalId;
                await _context.SaveChangesAsync();
            }

            var _GetCompanyInfo = _SeedData.GetCompanyInfo();
            _GetCompanyInfo.CreatedDate = DateTime.Now;
            _GetCompanyInfo.ModifiedDate = DateTime.Now;
            _GetCompanyInfo.CreatedBy = "Admin";
            _GetCompanyInfo.ModifiedBy = "Admin";
            _GetCompanyInfo.HospitalId = hospitalId;
            _context.CompanyInfo.Add(_GetCompanyInfo);
            await _context.SaveChangesAsync();
        }

        public async Task GenerateUserUserRole()
        {
            var _ManageRole = await _context.ManageUserRoles.ToListAsync();
            var _GetRoleList = await _roles.GetRoleList();

            foreach (var role in _ManageRole)
            {
                foreach (var item in _GetRoleList)
                {
                    ManageUserRolesDetails _ManageRoleDetails = new()
                    {
                        ManageRoleId = role.Id,
                        RoleId = item.RoleId,
                        RoleName = item.RoleName
                    };
                    if (role.Name == "Admin" || role.Name == "HospitalAdmin")
                    {
                        if (role.Name == "Admin") // Super Admin
                        {
                            _ManageRoleDetails.IsAllowed = true;
                        }
                        else if (role.Name == "HospitalAdmin") // Hospital Admin
                        {
                            if (item.RoleName == "Hospital Management" || item.RoleName == "System Role" || item.RoleName == "Manage User Roles")
                                _ManageRoleDetails.IsAllowed = false;
                            else
                                _ManageRoleDetails.IsAllowed = true;
                        }
                    }
                    else if (role.Name == "General")
                    {
                        if (item.RoleName == "User Profile" || item.RoleName == "Dashboard")
                            _ManageRoleDetails.IsAllowed = true;
                    }
                    else if (role.Name == "Doctor")
                    {
                        if (item.RoleName == "User Profile" || item.RoleName == "Dashboard" || item.RoleName == "Checkup" || item.RoleName == "Manage Clinic")
                            _ManageRoleDetails.IsAllowed = true;
                    }
                    else if (role.Name == "Patient")
                    {
                        if (item.RoleName == "User Profile" || item.RoleName == "Dashboard" || item.RoleName == "Manage Clinic"
                        || item.RoleName == "Patient Prescriptions" || item.RoleName == "Patient Appointment")
                            _ManageRoleDetails.IsAllowed = true;
                    }
                    else if (role.Name == "Nurse")
                    {
                        if (item.RoleName == "User Profile" || item.RoleName == "Dashboard" || item.RoleName == "Manage Clinic"
                        || item.RoleName == "Patient Info" || item.RoleName == "Patient Test" || item.RoleName == "Doctors Info"
                        || item.RoleName == "Bed" || item.RoleName == "Bed Allotments" || item.RoleName == "Bed Categories"
                        || item.RoleName == "Checkup" || item.RoleName == "Manage Medicine" || item.RoleName == "Medicine Categories"
                        || item.RoleName == "Medicine History" || item.RoleName == "Medicines")
                            _ManageRoleDetails.IsAllowed = true;
                    }
                    else if (role.Name == "Pharmacist")
                    {
                        if (item.RoleName == "User Profile" || item.RoleName == "Dashboard" || item.RoleName == "Manage Clinic"
                        || item.RoleName == "Manage Medicine" || item.RoleName == "Medicine Categories" || item.RoleName == "Medicine Label"
                        || item.RoleName == "Medicine History" || item.RoleName == "Medicines" || item.RoleName == "Medicine Manufacture")
                            _ManageRoleDetails.IsAllowed = true;
                    }
                    else if (role.Name == "Laboratries")
                    {
                        if (item.RoleName == "User Profile" || item.RoleName == "Dashboard" || item.RoleName == "Lab Test Categories"
                        || item.RoleName == "Lab Tests" || item.RoleName == "Patient Test")
                            _ManageRoleDetails.IsAllowed = true;
                    }
                    else if (role.Name == "Accountant")
                    {
                        if (item.RoleName == "User Profile" || item.RoleName == "Dashboard" || item.RoleName == "Expense Categories"
                        || item.RoleName == "Expenses" || item.RoleName == "Insurance Company Info" || item.RoleName == "Patient Payments"
                        || item.RoleName == "Payment Categories" || item.RoleName == "Payments")
                            _ManageRoleDetails.IsAllowed = true;
                    }
                    else
                    {
                        _ManageRoleDetails.IsAllowed = false;
                    }

                    _ManageRoleDetails.CreatedDate = DateTime.Now;
                    _ManageRoleDetails.ModifiedDate = DateTime.Now;
                    _ManageRoleDetails.CreatedBy = "Admin";
                    _ManageRoleDetails.ModifiedBy = "Admin";
                    _context.Add(_ManageRoleDetails);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task CreateDefaultEmailSettings()
        {
            //SMTP
            var CountSMTPEmailSetting = _context.SMTPEmailSetting.Count();
            if (CountSMTPEmailSetting < 1)
            {
                SMTPEmailSetting _SMTPEmailSetting = new SMTPEmailSetting
                {
                    UserName = "devmlbd@gmail.com",
                    Password = "",
                    Host = "smtp.gmail.com",
                    Port = 587,
                    IsSSL = true,
                    FromEmail = "devmlbd@gmail.com",
                    FromFullName = "Web Admin Notification",
                    IsDefault = true,

                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    CreatedBy = "Admin",
                    ModifiedBy = "Admin",
                };
                _context.Add(_SMTPEmailSetting);
                await _context.SaveChangesAsync();
            }
            //SendGridOptions
            var CountSendGridSetting = _context.SendGridSetting.Count();
            if (CountSendGridSetting < 1)
            {
                SendGridSetting _SendGridOptions = new SendGridSetting
                {
                    SendGridUser = "",
                    SendGridKey = "",
                    FromEmail = "devmlbd@gmail.com",
                    FromFullName = "Web Admin Notification",
                    IsDefault = false,

                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    CreatedBy = "Admin",
                    ModifiedBy = "Admin",
                };
                _context.Add(_SendGridOptions);
                await _context.SaveChangesAsync();
            }
        }

        public async Task CreateDefaultDoctorUser()
        {
            SeedData _SeedData = new();
            var _GetDoctorsInfoList = _SeedData.GetDoctorsInfoList();
            var hospitalId = _context.Hospital.SingleOrDefault().Id;
            foreach (var item in _GetDoctorsInfoList)
            {
                item.RoleId = 3;
                var _ApplicationUser = await _iAccount.CreateUserProfile(item, "Admin");
                if (_ApplicationUser.Item2 == "Success")
                {
                    DoctorsInfo _DoctorsInfo = new DoctorsInfo
                    {
                        ApplicationUserId = _ApplicationUser.Item1.Id,
                        DesignationId = 1,
                        DoctorsID = "MB" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                        DoctorFee = 500,
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        CreatedBy = "Admin",
                        ModifiedBy = "Admin",
                        HospitalId = hospitalId
                    };
                    _context.DoctorsInfo.Add(_DoctorsInfo);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task CreateDefaultOtherUser()
        {
            SeedData _SeedData = new();
            var _GetUserProfileList = _SeedData.GetUserProfileList();
            var hospitalId = _context.Hospital.SingleOrDefault().Id;
            foreach (var item in _GetUserProfileList)
            {
                item.RoleId = 2;
                item.EmployeeId = StaticData.RandomDigits(6);
                item.DateOfBirth = DateTime.Now.AddYears(-25);
                item.Designation = 1;
                item.Department = 1;
                item.SubDepartment = 1;
                item.JoiningDate = DateTime.Now.AddYears(-1);
                item.LeavingDate = DateTime.Now;
                item.HospitalId = hospitalId;
                await _iAccount.CreateUserProfile(item, "Admin");
            }
        }

        public async Task CreateDefaultHospitalUser()
        {
            SeedData _SeedData = new();
            var _GetUserProfileList = _SeedData.GetUserProfileList();
            var hospitalId = _context.Hospital.SingleOrDefault().Id;
            foreach (var item in _GetUserProfileList)
            {
                item.RoleId = 2;
                item.EmployeeId = StaticData.RandomDigits(6);
                item.DateOfBirth = DateTime.Now.AddYears(-25);
                item.Designation = 1;
                item.Department = 1;
                item.SubDepartment = 1;
                item.JoiningDate = DateTime.Now.AddYears(-1);
                item.LeavingDate = DateTime.Now;
                item.HospitalId = hospitalId;
                await _iAccount.CreateUserProfile(item, "HospitalAdmin");
            }
        }
        public async Task CreateDefaultPatientUser()
        {
            SeedData _SeedData = new();
            var _GetPatientInfoList = _SeedData.GetPatientInfoList();
            var hospitalId = _context.Hospital.SingleOrDefault().Id;
            foreach (var item in _GetPatientInfoList)
            {
                //item.UserType = UserType.Patient;
                item.RoleId = 4;
                item.PasswordHash = "123";
                item.ConfirmPassword = "123";
                var _ApplicationUser = await _iAccount.CreateUserProfile(item, "Admin");
                if (_ApplicationUser.Item2 == "Success")
                {
                    PatientInfo _PatientInfo = item;
                    Task.Delay(1000).Wait();
                    _PatientInfo.ApplicationUserId = _ApplicationUser.Item1.Id;
                    _PatientInfo.PatientCode = "P" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    _PatientInfo.CreatedDate = DateTime.Now;
                    _PatientInfo.ModifiedDate = DateTime.Now;
                    _PatientInfo.CreatedBy = "Admin";
                    _PatientInfo.ModifiedBy = "Admin";
                    _PatientInfo.HospitalId = hospitalId;
                    _context.PatientInfo.Add(_PatientInfo);
                    await _context.SaveChangesAsync();
                }
            }
        }
        public async Task<SharedUIDataViewModel> GetSharedUIData(ClaimsPrincipal claimsPrincipal)
        {
            SharedUIDataViewModel sharedUIDataViewModel = new SharedUIDataViewModel();
            ApplicationUser applicationUser = await _userManager.GetUserAsync(claimsPrincipal);
            sharedUIDataViewModel.UserProfile = _context.UserProfile.SingleOrDefault(x => x.ApplicationUserId.Equals(applicationUser.Id));
            sharedUIDataViewModel.MainMenuViewModel = await _roles.RolebaseMenuLoad(applicationUser);
            sharedUIDataViewModel.ApplicationInfo = _applicationInfo;
            sharedUIDataViewModel.userRolesCountsModel = new List<UserRoleCountsModel>();

            List<HMS.Models.ManageUserRoles> _manageUserRolesList = _context.ManageUserRoles.Where(x => x.Cancelled == false).ToList();            
            foreach (var userRole in _manageUserRolesList)
            {
                if (userRole.Name.ToLower() != "admin")
                {
                    try
                    {
                        sharedUIDataViewModel.userRolesCountsModel.Add(new UserRoleCountsModel()
                        {
                            RoleId = userRole.Id,
                            RoleName = userRole.Name,
                            UserCounts = _context.UserProfile.Where(x => x.RoleId == userRole.Id && x.Cancelled == false).Count(),
                            LeftMenuImage = _context.UserImages.Where(x => x.Id == userRole.ImageId).FirstOrDefault()?.ImagePath,
                            DashboardImage = _context.UserImages.Where(x => x.Id == userRole.DashboardImageId).FirstOrDefault()?.ImagePath,
                        });
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                }
            }
            return sharedUIDataViewModel;
        }
        public async Task<DefaultIdentityOptions> GetDefaultIdentitySettings()
        {
            return await _context.DefaultIdentityOptions.Where(x => x.Id == 1).SingleOrDefaultAsync();
        }
        public async Task CreateDefaultIdentitySettings()
        {
            if (_context.DefaultIdentityOptions.Count() < 1)
            {
                DefaultIdentityOptions _DefaultIdentityOptions = new DefaultIdentityOptions
                {
                    PasswordRequireDigit = false,
                    PasswordRequiredLength = 3,
                    PasswordRequireNonAlphanumeric = false,
                    PasswordRequireUppercase = false,
                    PasswordRequireLowercase = false,
                    PasswordRequiredUniqueChars = 0,
                    LockoutDefaultLockoutTimeSpanInMinutes = 30,
                    LockoutMaxFailedAccessAttempts = 5,
                    LockoutAllowedForNewUsers = false,
                    UserRequireUniqueEmail = true,
                    SignInRequireConfirmedEmail = false,

                    CookieHttpOnly = true,
                    CookieExpiration = 150,
                    CookieExpireTimeSpan = 120,
                    LoginPath = "/Account/Login",
                    LogoutPath = "/Account/Logout",
                    AccessDeniedPath = "/Account/AccessDenied",
                    SlidingExpiration = true,

                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    CreatedBy = "Admin",
                    ModifiedBy = "Admin",
                };
                _context.Add(_DefaultIdentityOptions);
                await _context.SaveChangesAsync();
            }
        }
    }
}
