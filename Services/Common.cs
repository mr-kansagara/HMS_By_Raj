﻿using HMS.Data;
using HMS.Models;
using HMS.Models.CheckupMedicineDetailsViewModel;
using HMS.Models.CheckupSummaryViewModel;
using HMS.Models.CommonViewModel;
using HMS.Models.CompanyInfoViewModel;
using HMS.Models.DoctorsInfoViewModel;
using HMS.Models.ExpensesViewModel;
using HMS.Models.LabTestsViewModel;
using HMS.Models.MedicineHistoryViewModel;
using HMS.Models.MedicinesViewModel;
using HMS.Models.PatientAppointmentViewModel;
using HMS.Models.PatientInfoViewModel;
using HMS.Models.PatientTestDetailViewModel;
using HMS.Models.PatientTestViewModel;
using HMS.Models.PaymentModeHistoryViewModel;
using HMS.Models.PaymentsDetailsViewModel;
using HMS.Models.PaymentsViewModel;
using HMS.Models.ReportViewModel;
using HMS.Models.UserProfileViewModel;
using Microsoft.EntityFrameworkCore;
using UAParser;



namespace HMS.Services
{
    public class Common : ICommon
    {
        private readonly IWebHostEnvironment _iHostingEnvironment;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<Common> _logger;
        public Common(IWebHostEnvironment iHostingEnvironment, ApplicationDbContext context, ILogger<Common> logger)
        {
            _iHostingEnvironment = iHostingEnvironment;
            _context = context;
            _logger = logger;
        }
        public string UploadedFile(IFormFile ProfilePicture)
        {
            string ProfilePictureFileName = null;
            if (ProfilePicture != null)
            {
                string uploadsFolder = Path.Combine(_iHostingEnvironment.ContentRootPath, "wwwroot/upload");

                if (ProfilePicture.FileName == null)
                    ProfilePictureFileName = Guid.NewGuid().ToString() + "_" + "blank-person.png";
                else
                    ProfilePictureFileName = Guid.NewGuid().ToString() + "_" + ProfilePicture.FileName;
                string filePath = Path.Combine(uploadsFolder, ProfilePictureFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    ProfilePicture.CopyTo(fileStream);
                }
                return ProfilePictureFileName;
            }

            return "blank-person.png";
        }
        public Int64 GetImageFileDetails(String userName, IFormFile ProfilePicture, string folderName, long? ImageId = 0)
        {
            if (ProfilePicture != null && ProfilePicture.Length > 0)
            {
                string uploadsFolder = Path.Combine(_iHostingEnvironment.WebRootPath, "images", folderName); // Use the provided folder name
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate a unique file name
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ProfilePicture.FileName).Replace(" ", "_");
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save the uploaded file to the specified folder
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    ProfilePicture.CopyTo(fileStream);
                }

                // Extract relative path after wwwroot
                var updatedFilePath = filePath.Split("wwwroot");
                string relativePath = updatedFilePath.Length > 1 ? updatedFilePath[1] : filePath;

                if (ImageId.HasValue && ImageId.Value != 0)
                {
                    // Find the existing image record
                    var existingImage = _context.UserImages.FirstOrDefault(img => img.Id == ImageId);

                    if (existingImage != null)
                    {
                        // Delete the old file from the folder
                        string oldFilePath = Path.Combine(_iHostingEnvironment.WebRootPath, existingImage.ImagePath);
                        if (File.Exists(oldFilePath))
                        {
                            File.Delete(oldFilePath);
                        }

                        // Update existing image record with new details
                        existingImage.Name = uniqueFileName;
                        existingImage.ImagePath = relativePath.Replace("\\", "/");
                        existingImage.ModifiedDate = DateTime.Now;
                        existingImage.ModifiedBy = userName;

                        // Save changes to the database
                        _context.SaveChanges();

                        // Return the ID of the updated record
                        return existingImage.Id;
                    }
                }
                else
                {
                    // Create UserImages object and populate properties
                    var userImage = new UserImages
                    {
                        Name = uniqueFileName,
                        ImagePath = relativePath.Replace("\\", "/"),
                        CreatedBy = userName,
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now,
                        ModifiedBy = userName
                    };

                    // Add UserImages object to context
                    _context.UserImages.Add(userImage);
                    _context.SaveChanges();

                    // Return the ID of the inserted record
                    return userImage.Id;
                }
            }

            // Return the ImageId if no new image is uploaded
            return ImageId ?? 0;
        }


        public async Task<SMTPEmailSetting> GetSMTPEmailSetting()
        {
            return await _context.Set<SMTPEmailSetting>().Where(x => x.Id == 1).SingleOrDefaultAsync();
        }
        public async Task<SendGridSetting> GetSendGridEmailSetting()
        {
            return await _context.Set<SendGridSetting>().Where(x => x.Id == 1).SingleOrDefaultAsync();
        }

        public UserProfile GetByUserProfile(Int64 id)
        {
            return _context.UserProfile.Where(x => x.UserProfileId == id).SingleOrDefault();
        }
        public async Task<bool> InsertLoginHistory(LoginHistory _LoginHistory, ClientInfo _ClientInfo)
        {
            try
            {
                _LoginHistory.PublicIP = await GetPublicIP();
                _LoginHistory.CreatedDate = DateTime.Now;
                _LoginHistory.ModifiedDate = DateTime.Now;

                _context.Add(_LoginHistory);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Insert login History Successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into a Insert login History.");
                throw;
            }
        }
        public static async Task<string> GetPublicIP()
        {
            try
            {
                string url = "http://checkip.dyndns.org/";
                var _HttpClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true });
                var _GetAsync = await _HttpClient.GetAsync(url);
                var _Stream = await _GetAsync.Content.ReadAsStreamAsync();
                StreamReader _StreamReader = new StreamReader(_Stream);
                string result = _StreamReader.ReadToEnd();

                string[] a = result.Split(':');
                string a2 = a[1].Substring(1);
                string[] a3 = a2.Split('<');
                string a4 = a3[0];
                return a4;
            }
            catch (Exception ex)
            {
                return ex.Message;
                throw new Exception("No network adapters with an IPv4 address in the system!");
            }
        }
        public CompanyInfoCRUDViewModel GetCompanyInfo()
        {
            CompanyInfoCRUDViewModel vm = _context.CompanyInfo.FirstOrDefault(m => m.Id == 1);
            return vm;
        }


        public IQueryable<ItemDropdownListViewModel> LoadddlMedicineCategories()
        {
            return (from _MedicineCategories in _context.MedicineCategories.Where(x => x.Cancelled == false).OrderBy(x => x.Id)
                    select new ItemDropdownListViewModel
                    {
                        Id = _MedicineCategories.Id,
                        Name = _MedicineCategories.Name,
                    });
        }
        public IQueryable<ItemDropdownListViewModel> LoadddlUnit()
        {
            return (from _Unit in _context.Unit.Where(x => x.Cancelled == false).OrderBy(x => x.Id)
                    select new ItemDropdownListViewModel
                    {
                        Id = _Unit.Id,
                        Name = _Unit.Name,
                    });
        }
        public IQueryable<ItemDropdownListViewModel> LoadddlMedicineManufacture()
        {
            return (from _MedicineManufacture in _context.MedicineManufacture.Where(x => x.Cancelled == false).OrderBy(x => x.Id)
                    select new ItemDropdownListViewModel
                    {
                        Id = _MedicineManufacture.Id,
                        Name = _MedicineManufacture.Name,
                    });
        }
        public IQueryable<ItemDropdownListViewModel> LoadddlMedicines()
        {
            return (from _Medicines in _context.Medicines.Where(x => x.Cancelled == false).OrderBy(x => x.Id)
                    where _Medicines.MedicineName != null && _Medicines.Quantity > 0 && _Medicines.Cancelled == false
                    select new ItemDropdownListViewModel
                    {
                        Id = _Medicines.Id,
                        Name = _Medicines.MedicineName + ", Available Quantity: " + _Medicines.Quantity
                    });
        }
        public IQueryable<ItemDropdownListViewModel> LoadddlBedNo(HMS.Models.BedAllotments _BedAllotments, bool showPrice = false)
        {
            var _GetAvailableBedList = _context.Bed.Where(x => x.Cancelled == false && x.BedCategoryId == _BedAllotments.BedCategoryId).ToList();

            var _BookedBedList = _context.BedAllotments.Where(x => x.Cancelled == false
                        && x.BedCategoryId == _BedAllotments.BedCategoryId && x.IsReleased == false).ToList();

            foreach (var item in _BookedBedList)
            {
                var itemToRemove = _GetAvailableBedList.Where(x => x.Id == item.BedId).SingleOrDefault();
                if (itemToRemove != null && itemToRemove.Id != _BedAllotments.BedId)
                    _GetAvailableBedList.Remove(itemToRemove);
            }

            return (from _Bed in _GetAvailableBedList.OrderBy(x => x.Id)
                    join _BedCategories in _context.BedCategories on _Bed.BedCategoryId equals _BedCategories.Id
                    where _Bed.Cancelled == false
                    select new ItemDropdownListViewModel
                    {
                        Id = _Bed.Id,
                        Name = showPrice ? _Bed.No + "<>" + _BedCategories.Description + " - " + _BedCategories.BedPrice + " INR (Per Day)" : _Bed.No + "<>" + _BedCategories.Description,
                    }).AsQueryable();
        }
        public IQueryable<ItemDropdownListViewModel> LoadddBedCategories()
        {
            return (from _BedCategories in _context.BedCategories.Where(x => x.Cancelled == false).OrderBy(x => x.CreatedDate)
                    select new ItemDropdownListViewModel
                    {
                        Id = _BedCategories.Id,
                        Name = _BedCategories.Name
                    });
        }
        public async Task<List<HMS.Models.BedCategories>> GetBedCategorieslist()
        {
            var result = await (from _BedCategories in _context.BedCategories
                                where _BedCategories.Cancelled == false
                                orderby _BedCategories.Id
                                select new HMS.Models.BedCategories
                                {
                                    Id = _BedCategories.Id,
                                    Name = _BedCategories.Name,
                                    BedPrice = _BedCategories.BedPrice
                                }).OrderBy(x => x.Name).ToListAsync();

            return result;
        }

        public IQueryable<ItemDropdownListViewModel> LoadddlPatientName()
        {
            return (from _UserProfile in _context.UserProfile.Where(x => x.Cancelled == false && x.RoleId == 4).OrderBy(x => x.CreatedDate)
                    select new ItemDropdownListViewModel
                    {
                        Id = _UserProfile.UserProfileId,
                        Name = _UserProfile.FirstName + " " + _UserProfile.LastName,
                    });
        }
        public IQueryable<ItemDropdownListViewModel> LoadddlVisitID(Int64 PatintID)
        {
            return (from _CheckupSummary in _context.CheckupSummary.Where(x => x.Cancelled == false).OrderBy(x => x.CreatedDate)
                    where _CheckupSummary.PatientId == PatintID
                    select new ItemDropdownListViewModel
                    {
                        Id = _CheckupSummary.Id,
                        Name = _CheckupSummary.VisitId,
                    });
        }
        public IQueryable<ItemDropdownListViewModel> LoadddlDoctorName()
        {
            return (from _UserProfile in _context.UserProfile.Where(x => x.Cancelled == false).OrderBy(x => x.CreatedDate)
                    join _DoctorsInfo in _context.DoctorsInfo on _UserProfile.ApplicationUserId equals _DoctorsInfo.ApplicationUserId
                    select new ItemDropdownListViewModel
                    {
                        Id = _DoctorsInfo.Id,
                        Name = _UserProfile.FirstName + " " + _UserProfile.LastName,
                    });
        }
        public IQueryable<ItemDropdownListViewModel> LoadddlLabTestCategories()
        {
            return (from _LabTestCategories in _context.LabTestCategories.Where(x => x.Cancelled == false).OrderBy(x => x.CreatedDate)
                    select new ItemDropdownListViewModel
                    {
                        Id = _LabTestCategories.Id,
                        Name = _LabTestCategories.Name,
                    });
        }
        public IQueryable<ItemDropdownListViewModel> LoadddlLabTests()
        {
            return (from _LabTests in _context.LabTests.Where(x => x.Cancelled == false).OrderBy(x => x.CreatedDate)
                    select new ItemDropdownListViewModel
                    {
                        Id = _LabTests.Id,
                        Name = _LabTests.LabTestName,
                    });
        }
        public IQueryable<ItemDropdownListViewModel> LoadddlExpenseCategories()
        {
            return (from _ExpenseCategories in _context.ExpenseCategories.Where(x => x.Cancelled == false).OrderBy(x => x.CreatedDate)
                    select new ItemDropdownListViewModel
                    {
                        Id = _ExpenseCategories.Id,
                        Name = _ExpenseCategories.Name,
                    });
        }
        public IQueryable<ItemDropdownListViewModel> LoadddlPaymentCategories()
        {
            var listPaymentCategories = (from _PaymentCategories in _context.PaymentCategories
                                         where _PaymentCategories.Cancelled == false
                                         select new ItemDropdownListViewModel
                                         {
                                             Code = _PaymentCategories.PaymentItemCode,
                                             Name = _PaymentCategories.Name + ", Unit Prize: " + _PaymentCategories.UnitPrice,
                                         });

            var listLabTests = (from _LabTests in _context.LabTests
                                where _LabTests.Cancelled == false
                                select new ItemDropdownListViewModel
                                {
                                    Code = _LabTests.PaymentItemCode,
                                    Name = _LabTests.LabTestName + ", Unit Prize: " + _LabTests.UnitPrice,
                                });

            var listMedicines = (from _Medicines in _context.Medicines
                                 where _Medicines.Quantity > 0 && _Medicines.Cancelled == false
                                 select new ItemDropdownListViewModel
                                 {
                                     Code = _Medicines.PaymentItemCode,
                                     Name = _Medicines.MedicineName + ", Unit Prize: " + _Medicines.SellPrice + ", Available Quantity: " + _Medicines.Quantity,
                                 });

            var result = listPaymentCategories.Concat(listLabTests);
            var result2 = result.Concat(listMedicines);

            return result2;
        }

        public IQueryable<ItemDropdownListViewModel> LoadddlCurrencyItem()
        {
            return (from _Currency in _context.Currency.Where(x => x.Cancelled == false).OrderByDescending(x => x.IsDefault)
                    select new ItemDropdownListViewModel
                    {
                        Id = _Currency.Id,
                        Name = _Currency.Name + " <> " + _Currency.Code + " <> " + _Currency.Symbol,
                    });
        }
        public IQueryable<ItemDropdownListViewModel> LoadddlInsuranceCompanyInfo()
        {
            return (from _InsuranceCompanyInfo in _context.InsuranceCompanyInfo.Where(x => x.Cancelled == false).OrderByDescending(x => x.CreatedDate)
                    select new ItemDropdownListViewModel
                    {
                        Id = _InsuranceCompanyInfo.Id,
                        Name = _InsuranceCompanyInfo.Name,
                    });
        }
        public IQueryable<DoctorsInfoCRUDViewModel> GetDoctorInfoList()
        {
            try
            {
                return (from _UserProfile in _context.UserProfile
                        join _DoctorsInfo in _context.DoctorsInfo on _UserProfile.ApplicationUserId equals _DoctorsInfo.ApplicationUserId
                        into _DoctorsInfo
                        from listDoctorsInfo in _DoctorsInfo.DefaultIfEmpty()
                        join _Designation in _context.Designation on listDoctorsInfo.DesignationId equals _Designation.Id
                        into _Designation
                        from listDesignation in _Designation.DefaultIfEmpty()
                        where listDoctorsInfo.Cancelled == false
                        select new DoctorsInfoCRUDViewModel
                        {
                            Id = _UserProfile.UserProfileId,
                            ApplicationUserId = _UserProfile.ApplicationUserId,
                            FirstName = _UserProfile.FirstName,
                            LastName = _UserProfile.LastName,
                            PhoneNumber = _UserProfile.PhoneNumber,
                            Email = _UserProfile.Email,

                            DesignationId = listDoctorsInfo.DesignationId,
                            DesignationDisplay = listDesignation.Name,
                            DoctorsID = listDoctorsInfo.DoctorsID,
                            DoctorFee = listDoctorsInfo.DoctorFee,

                            Address = _UserProfile.Address,
                            Country = _UserProfile.Country,
                            ProfilePicture = _UserProfile.ProfilePicture,
                            CreatedDate = _UserProfile.CreatedDate,
                            ModifiedDate = _UserProfile.ModifiedDate,
                            CreatedBy = _UserProfile.CreatedBy,
                            ModifiedBy = _UserProfile.ModifiedBy,
                            Cancelled = _UserProfile.Cancelled,
                        }).OrderByDescending(x => x.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting Doctor info List.");
                throw;
            }
        }
        public IQueryable<PatientInfoCRUDViewModel> GetPatientInfoGridItem()
        {
            try
            {
                return (from _UserProfile in _context.UserProfile
                        join _PatientInfo in _context.PatientInfo on _UserProfile.ApplicationUserId equals _PatientInfo.ApplicationUserId
                        where _PatientInfo.Cancelled == false

                        select new PatientInfoCRUDViewModel
                        {
                            Id = _UserProfile.UserProfileId,

                            FirstName = _UserProfile.FirstName,
                            LastName = _UserProfile.LastName,
                            FullName = _UserProfile.FirstName + " " + _UserProfile.LastName,
                            Address = _UserProfile.Address,
                            Country = _UserProfile.Country,
                            Phone = _UserProfile.PhoneNumber,
                            Email = _UserProfile.Email,
                            DateOfBirth = _UserProfile.DateOfBirth,
                            ProfilePicture = _UserProfile.ProfilePicture,

                            MaritalStatus = _PatientInfo.MaritalStatus,
                            MotherName = _PatientInfo.MotherName,
                            FatherName = _PatientInfo.FatherName,
                            Gender = _PatientInfo.Gender,
                            SpouseName = _PatientInfo.SpouseName,
                            BloodGroup = _PatientInfo.BloodGroup,

                            CreatedDate = _PatientInfo.CreatedDate,
                            ModifiedDate = _PatientInfo.ModifiedDate,
                            CreatedBy = _PatientInfo.CreatedBy,
                            ModifiedBy = _PatientInfo.ModifiedBy,
                            Cancelled = _PatientInfo.Cancelled,
                        }).OrderByDescending(x => x.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting Patient Grid List.");
                throw;
            }
        }
        public async Task<ManagePatientTestViewModel> GetByPatientTestDetails(Int64 id)
        {
            ManagePatientTestViewModel vm = new ManagePatientTestViewModel();
            vm.PatientTestCRUDViewModel = GetPatientTestGridItem().Where(x => x.Id == id).SingleOrDefault();
            vm.listPatientTestDetailCRUDViewModel = await GetPatientTestDetail().Where(x => x.PatientTestId == id).ToListAsync();
            return vm;
        }

        public IQueryable<PatientTestCRUDViewModel> GetPatientTestGridItem()
        {
            try
            {
                return (from _PatientTest in _context.PatientTest
                        join _UserProfile in _context.UserProfile on _PatientTest.PatientId equals _UserProfile.UserProfileId
                        //join _UserProfile in _context.UserProfile on _PatientTest.ApplicationUserId equals _UserProfile.ApplicationUserId
                        where _PatientTest.Cancelled == false
                        select new PatientTestCRUDViewModel
                        {
                            Id = _PatientTest.Id,
                            PatientId = _PatientTest.PatientId,
                            VisitId = _PatientTest.VisitId,
                            PatientName = _UserProfile.FirstName + " " + _UserProfile.LastName,
                            //ConsultantName = _UserProfile.FirstName + " " + _UserProfile.LastName,
                            TestDate = _PatientTest.TestDate,
                            DeliveryDate = _PatientTest.DeliveryDate,
                            PaymentStatus = _PatientTest.PaymentStatus,
                            CreatedDate = _PatientTest.CreatedDate,
                            ModifiedDate = _PatientTest.ModifiedDate,

                            CreatedBy = _PatientTest.CreatedBy,
                            ModifiedBy = _PatientTest.ModifiedBy,

                        }).OrderByDescending(x => x.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting Patient test List.");
                throw;
            }
        }

        public IQueryable<PatientTestDetailCRUDViewModel> GetPatientTestDetail()
        {
            try
            {
                return (from _PatientTestDetail in _context.PatientTestDetail
                        join _LabTests in _context.LabTests on _PatientTestDetail.LabTestsId equals _LabTests.Id
                        where _PatientTestDetail.Cancelled == false
                        select new PatientTestDetailCRUDViewModel
                        {
                            Id = _PatientTestDetail.Id,
                            PatientTestId = _PatientTestDetail.PatientTestId,
                            LabTestsId = _PatientTestDetail.LabTestsId,
                            LabTestsName = _LabTests.LabTestName,
                            Result = _PatientTestDetail.Result,
                            Quantity = _PatientTestDetail.Quantity,
                            UnitPrice = _PatientTestDetail.UnitPrice,
                            Remarks = _PatientTestDetail.Remarks,
                            CreatedDate = _PatientTestDetail.CreatedDate,
                            ModifiedDate = _PatientTestDetail.ModifiedDate,
                            CreatedBy = _PatientTestDetail.CreatedBy,
                            ModifiedBy = _PatientTestDetail.ModifiedBy,
                            Cancelled = _PatientTestDetail.Cancelled,

                        }).OrderByDescending(x => x.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting Patient Test Details.");
                throw;
            }
        }


        public async Task<ManagePaymentsViewModel> GetByPaymentsDetails(Int64 id)
        {
            try
            {
                ManagePaymentsViewModel vm = new ManagePaymentsViewModel();
                vm.PaymentsCRUDViewModel = await GetPaymentDetails().Where(x => x.Id == id).SingleOrDefaultAsync();
                vm.listPaymentsDetailsCRUDViewModel = GetPaymentsDetails2(vm.PaymentsCRUDViewModel.Id).OrderByDescending(x => x.CreatedDate).ToList();
                vm.listPaymentModeHistoryCRUDViewModel = GetPaymentModeHistory().Where(x => x.PaymentId == id).ToList();
                return vm;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting Payment Details.");
                throw;
            }
        }

        public async Task<ManagePaymentsViewModel> GetByPatientPayments(Int64 id)
        {
            ManagePaymentsViewModel vm = new ManagePaymentsViewModel();
            vm.PaymentsCRUDViewModel = await GetPaymentDetails().Where(x => x.Id == id).SingleOrDefaultAsync();
            vm.listPaymentsDetailsCRUDViewModel = GetPaymentsDetails2(vm.PaymentsCRUDViewModel.Id).OrderByDescending(x => x.CreatedDate).ToList();
            return vm;
        }

        public IQueryable<PaymentsCRUDViewModel> GetPaymentDetails()
        {
            try
            {
                var result = (from _Payments in _context.Payments
                              join _UserProfile in _context.UserProfile on _Payments.PatientId equals _UserProfile.UserProfileId
                              join _CheckupSummary in _context.CheckupSummary on _Payments.VisitId equals _CheckupSummary.VisitId
                              into listCheckupSummary
                              from _objlistlistCheckupSummary in listCheckupSummary.DefaultIfEmpty()
                              join _InsuranceCompanyInfo in _context.InsuranceCompanyInfo on _Payments.InsuranceCompanyId equals _InsuranceCompanyInfo.Id
                              into listInsuranceCompanyInfo
                              from _objlistInsuranceCompanyInfo in listInsuranceCompanyInfo.DefaultIfEmpty()
                              join _Currency in _context.Currency on _Payments.CurrencyId equals _Currency.Id
                              where _Payments.Cancelled == false
                              select new PaymentsCRUDViewModel
                              {
                                  Id = _Payments.Id,
                                  PatientId = _Payments.PatientId,
                                  VisitId = _Payments.VisitId,
                                  InvoiceNo = _Payments.InvoiceNo,
                                  PatientName = _UserProfile.FirstName + " " + _UserProfile.LastName,
                                  PatientType = _objlistlistCheckupSummary.PatientType,
                                  CommonCharge = _Payments.CommonCharge,
                                  Discount = _Payments.Discount,
                                  DiscountAmount = _Payments.DiscountAmount,
                                  Tax = _Payments.Tax,
                                  TaxAmount = _Payments.TaxAmount,
                                  SubTotal = _Payments.SubTotal,
                                  GrandTotal = _Payments.GrandTotal,
                                  PaidAmount = _Payments.PaidAmount,
                                  DueAmount = _Payments.DueAmount,
                                  InsuranceNo = _Payments.InsuranceNo,
                                  InsuranceCompanyId = _objlistInsuranceCompanyInfo.Id,
                                  InsuranceCompanyName = _objlistInsuranceCompanyInfo.Name,
                                  InsuranceCoverage = _Payments.InsuranceCoverage,
                                  InsuranceAmount = (_Payments.SubTotal / 100) * _Payments.InsuranceCoverage,
                                  CurrencyId = _Payments.CurrencyId,
                                  CurrencyName = _Currency.Name,
                                  PaymentStatus = _Payments.PaymentStatus,
                                  CreatedDate = _Payments.CreatedDate,
                                  CreatedBy = _Payments.CreatedBy,
                                  ModifiedBy = _Payments.ModifiedBy,

                              }).OrderByDescending(x => x.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting Payment Details.");
                throw;
            }
        }
        public IQueryable<UserRoleCountsModel> GetDashboardDetails()
        {
            try
            {
                var result = (from _UserRoleCountsModel in _context.UserRoleCountsModel
                              join _UserImages in _context.UserImages
                              on _UserRoleCountsModel.ImageId equals _UserImages.Id into userImageGroup
                              from userImage in userImageGroup.DefaultIfEmpty() // left join
                              join _DashboardImages in _context.UserImages
                              on _UserRoleCountsModel.DashboardImageId equals _DashboardImages.Id into dashboardImageGroup
                              from dashboardImage in dashboardImageGroup.DefaultIfEmpty() // left join
                              where _UserRoleCountsModel.Cancelled == false
                              select new UserRoleCountsModel
                              {
                                  RoleId = _UserRoleCountsModel.RoleId,
                                  RoleName = _UserRoleCountsModel.RoleName,
                                  UserCounts = _UserRoleCountsModel.UserCounts,
                                  LeftMenuImage = _context.UserImages.FirstOrDefault(x => x.Id == _UserRoleCountsModel.ImageId).ImagePath ?? "/upload/blank-person.png",
                                  DashboardImage = _context.UserImages.FirstOrDefault(x => x.Id == _UserRoleCountsModel.DashboardImageId).ImagePath ?? "/upload/blank-person.png",
                                  ModifiedBy = _UserRoleCountsModel.ModifiedBy
                              }).OrderByDescending(x => x.RoleId);

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }


        public IQueryable<PaymentsGridViewModel> GetPaymentGridList()
        {
            try
            {
                //var filterCheckupSummary = from _CheckupSummary in _context.CheckupSummary
                //                           where _CheckupSummary.Cancelled == false
                //                           select new PaymentsGridViewModel
                //                           {
                //                               VisitId = _CheckupSummary.VisitId,
                //                               PatientType = _CheckupSummary.PatientType
                //                           };

                var result = (from _Payments in _context.Payments
                              join _UserProfile in _context.UserProfile on _Payments.PatientId equals _UserProfile.UserProfileId
                              join _CheckupSummary in _context.CheckupSummary on _Payments.VisitId equals _CheckupSummary.VisitId
                              //into listCheckupSummary
                              //from _objlistlistCheckupSummary in listCheckupSummary.DefaultIfEmpty()
                              where _Payments.Cancelled == false
                              select new PaymentsGridViewModel
                              {
                                  Id = _Payments.Id,
                                  PatientName = _UserProfile.FirstName + " " + _UserProfile.LastName,
                                  PatientType = _CheckupSummary.PatientType,
                                  //PatientType = "Dummy",
                                  Discount = _Payments.Discount,
                                  Tax = _Payments.Tax,
                                  SubTotal = _Payments.SubTotal,
                                  GrandTotal = _Payments.GrandTotal,
                                  CreatedDate = _Payments.CreatedDate,
                              }).OrderByDescending(x => x.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting payment Grid List.");
                throw;
            }
        }
        public IQueryable<PaymentModeReportViewModel> GetPaymentModeReportData()
        {
            try
            {
                var result = (from _Payments in _context.Payments
                              join _PaymentModeHistory in _context.PaymentModeHistory on _Payments.Id equals _PaymentModeHistory.PaymentId
                              join _UserProfile in _context.UserProfile on _Payments.PatientId equals _UserProfile.UserProfileId

                              join _CheckupSummary in _context.CheckupSummary on _Payments.VisitId equals _CheckupSummary.VisitId
                              into listCheckupSummary
                              from _objlistlistCheckupSummary in listCheckupSummary.DefaultIfEmpty()
                              join _InsuranceCompanyInfo in _context.InsuranceCompanyInfo on _Payments.InsuranceCompanyId equals _InsuranceCompanyInfo.Id
                              into listInsuranceCompanyInfo
                              from _objlistInsuranceCompanyInfo in listInsuranceCompanyInfo.DefaultIfEmpty()
                              where _Payments.Cancelled == false && _PaymentModeHistory.Cancelled == false
                              select new PaymentModeReportViewModel
                              {
                                  Id = _PaymentModeHistory.Id,
                                  PaymentId = _Payments.Id,
                                  PatientName = _UserProfile.FirstName + " " + _UserProfile.LastName,
                                  PatientType = _objlistlistCheckupSummary.PatientType,
                                  Insurance = _objlistInsuranceCompanyInfo.Name,
                                  PaymentStatus = _Payments.PaymentStatus,
                                  ModeofPayment = _PaymentModeHistory.ModeOfPayment,
                                  Amount = _PaymentModeHistory.Amount,
                                  TotalAmount = _Payments.GrandTotal,
                                  TotalPaidAmount = _Payments.PaidAmount,
                                  TotalDueAmount = _Payments.DueAmount,
                                  CreatedDate = _PaymentModeHistory.CreatedDate,

                              }).OrderByDescending(x => x.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting payment Mode Report data.");
                throw;
            }
        }

        public IQueryable<PatientPaymentViewModel> GetPatientPaymentsGridItem()
        {
            try
            {
                return (from _CheckupSummary in _context.CheckupSummary
                        join _UserProfile in _context.UserProfile on _CheckupSummary.PatientId equals _UserProfile.UserProfileId
                        join _PatientVisitPaymentHistory in _context.PatientVisitPaymentHistory on _CheckupSummary.VisitId equals _PatientVisitPaymentHistory.VisitId
                        join _DoctorsInfo in _context.DoctorsInfo on _CheckupSummary.DoctorId equals _DoctorsInfo.Id
                        into list1
                        from _objDoctorsInfo in list1.DefaultIfEmpty()
                        join _PatientInfo in _context.PatientInfo on _objDoctorsInfo.ApplicationUserId equals _PatientInfo.ApplicationUserId

                        where _CheckupSummary.Cancelled == false
                        select new PatientPaymentViewModel
                        {
                            Id = _CheckupSummary.Id,
                            PatientId = _CheckupSummary.PatientId,
                            VisitId = _CheckupSummary.VisitId,
                            PaymentId = _PatientVisitPaymentHistory.PaymentId,
                            PatientName = _UserProfile.FirstName + " " + _UserProfile.LastName,
                            PatientType = _CheckupSummary.PatientType,
                            DoctorId = _CheckupSummary.DoctorId,
                            DoctorName = _UserProfile.FirstName + " " + _UserProfile.LastName,
                            CheckupDate = _CheckupSummary.CheckupDate,
                        }).OrderByDescending(x => x.CheckupDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting patient payment Grid List.");
                throw;
            }
        }

        public async Task<PaymentsReportViewModel> PrintPaymentInvoice(Int64 id)
        {
            PaymentsReportViewModel vm = new PaymentsReportViewModel();
            vm.PaymentsCRUDViewModel = GetPaymentDetails().Where(x => x.Id == id).SingleOrDefault();
            vm.listPaymentsDetailsCRUDViewModel = GetPaymentsDetails2(vm.PaymentsCRUDViewModel.Id).OrderByDescending(x => x.CreatedDate).ToList();
            vm.listPaymentModeHistoryCRUDViewModel = GetPaymentModeHistory().Where(x => x.PaymentId == id).ToList();

            vm.PatientInfoCRUDViewModel = await _context.PatientInfo.FirstOrDefaultAsync(m => m.Id == vm.PaymentsCRUDViewModel.PatientId);
            vm.CompanyInfoCRUDViewModel = GetCompanyInfo();
            return vm;
        }

        public IQueryable<PaymentsDetailsCRUDViewModel> GetPaymentsDetails(Int64 id)
        {
            try
            {
                var contextPaymentsDetails = _context.PaymentsDetails.Where(x => x.PaymentsId == id);

                var listPaymentCategories = (from _PaymentsDetails in contextPaymentsDetails
                                             join _PaymentCategories in _context.PaymentCategories on _PaymentsDetails.PaymentItemCode equals _PaymentCategories.PaymentItemCode
                                             where _PaymentsDetails.Cancelled == false
                                             select new PaymentsDetailsCRUDViewModel
                                             {
                                                 Id = _PaymentsDetails.Id,
                                                 PaymentsId = _PaymentsDetails.PaymentsId,
                                                 PaymentItemCode = _PaymentsDetails.PaymentItemCode,
                                                 ItemDetailId = _PaymentsDetails.ItemDetailId,
                                                 PaymentItemName = _PaymentCategories.Name,
                                                 Quantity = _PaymentsDetails.Quantity,
                                                 UnitPrize = _PaymentsDetails.UnitPrize,
                                                 TotalAmount = _PaymentsDetails.TotalAmount,
                                                 CreatedDate = _PaymentsDetails.CreatedDate,
                                                 ModifiedDate = _PaymentsDetails.ModifiedDate,
                                                 CreatedBy = _PaymentsDetails.CreatedBy,
                                                 ModifiedBy = _PaymentsDetails.ModifiedBy,
                                                 Cancelled = _PaymentsDetails.Cancelled,
                                             });

                var listLabTests = (from _PaymentsDetails in contextPaymentsDetails
                                    join _LabTests in _context.LabTests on _PaymentsDetails.PaymentItemCode equals _LabTests.PaymentItemCode
                                    where _PaymentsDetails.Cancelled == false
                                    select new PaymentsDetailsCRUDViewModel
                                    {
                                        Id = _PaymentsDetails.Id,
                                        PaymentsId = _PaymentsDetails.PaymentsId,
                                        PaymentItemCode = _PaymentsDetails.PaymentItemCode,
                                        ItemDetailId = _PaymentsDetails.ItemDetailId,
                                        PaymentItemName = _LabTests.LabTestName,
                                        Quantity = _PaymentsDetails.Quantity,
                                        UnitPrize = _PaymentsDetails.UnitPrize,
                                        TotalAmount = _PaymentsDetails.TotalAmount,
                                        CreatedDate = _PaymentsDetails.CreatedDate,
                                        ModifiedDate = _PaymentsDetails.ModifiedDate,
                                        CreatedBy = _PaymentsDetails.CreatedBy,
                                        ModifiedBy = _PaymentsDetails.ModifiedBy,
                                        Cancelled = _PaymentsDetails.Cancelled,
                                    });

                var listMedicines = (from _PaymentsDetails in contextPaymentsDetails
                                     join _Medicines in _context.Medicines on _PaymentsDetails.PaymentItemCode equals _Medicines.PaymentItemCode
                                     where _PaymentsDetails.Cancelled == false
                                     select new PaymentsDetailsCRUDViewModel
                                     {
                                         Id = _PaymentsDetails.Id,
                                         PaymentsId = _PaymentsDetails.PaymentsId,
                                         PaymentItemCode = _PaymentsDetails.PaymentItemCode,
                                         ItemDetailId = _PaymentsDetails.ItemDetailId,
                                         PaymentItemName = _Medicines.MedicineName,
                                         Quantity = _PaymentsDetails.Quantity,
                                         UnitPrize = _PaymentsDetails.UnitPrize,
                                         TotalAmount = _PaymentsDetails.TotalAmount,
                                         CreatedDate = _PaymentsDetails.CreatedDate,
                                         ModifiedDate = _PaymentsDetails.ModifiedDate,
                                         CreatedBy = _PaymentsDetails.CreatedBy,
                                         ModifiedBy = _PaymentsDetails.ModifiedBy,
                                         Cancelled = _PaymentsDetails.Cancelled,
                                     });

                var _Payments = _context.Payments.Where(x => x.Id == id).SingleOrDefault();
                var listCheckupMedicineDetails = _context.CheckupMedicineDetails.Where(x => x.VisitId == _Payments.VisitId).ToList();

                List<PaymentsDetailsCRUDViewModel> listPaymentsDetailsCRUDViewModel = new List<PaymentsDetailsCRUDViewModel>();


                var items = listMedicines.Where(x => x.ItemDetailId > 0).ToList();
                if (listCheckupMedicineDetails != null)
                {
                    for (int index = 0; index < items.Count(); index++)
                    {
                        PaymentsDetailsCRUDViewModel _PaymentsDetailsCRUDViewModel = items[index];
                        _PaymentsDetailsCRUDViewModel.PaymentItemName += " [When:" + listCheckupMedicineDetails[index].WhentoTake + ", Days: " + listCheckupMedicineDetails[index].NoofDays + " ]";
                        listPaymentsDetailsCRUDViewModel.Add(_PaymentsDetailsCRUDViewModel);
                    }
                }

                items = listMedicines.Where(x => x.ItemDetailId == 0).ToList();
                for (int index = 0; index < items.Count(); index++)
                {
                    PaymentsDetailsCRUDViewModel _PaymentsDetailsCRUDViewModel = items[index];
                    listPaymentsDetailsCRUDViewModel.Add(_PaymentsDetailsCRUDViewModel);
                }

                var result = listPaymentCategories.Concat(listLabTests).ToList();
                var result2 = result.Concat(listPaymentsDetailsCRUDViewModel).ToList();

                return result2.AsQueryable();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting payment Details.");
                throw;
            }
        }


        public IQueryable<PaymentsDetailsCRUDViewModel> GetPaymentsDetails2(Int64 id)
        {
            try
            {
                var _GetServicePaymentList = GetServicePaymentList().Where(x => x.PaymentsId == id);
                var listMedicines = _GetServicePaymentList.Where(x => x.PaymentItemCode.Contains("MED"));
                var listOtherService = _GetServicePaymentList.Where(x => !x.PaymentItemCode.Contains("MED"));

                var _Payments = _context.Payments.Where(x => x.Id == id).SingleOrDefault();
                var listCheckupMedicineDetails = _context.CheckupMedicineDetails.Where(x => x.VisitId == _Payments.VisitId).ToList();

                List<PaymentsDetailsCRUDViewModel> _PaymentsDetailsCRUDViewModel = new List<PaymentsDetailsCRUDViewModel>();
                if (listMedicines != null)
                {
                    CheckupMedicineDetails _CheckupMedicineDetails = new CheckupMedicineDetails();
                    foreach (var item in listMedicines)
                    {
                        if (item.ItemDetailId > 0)
                        {
                            _CheckupMedicineDetails = listCheckupMedicineDetails.Where(x => x.Id == item.ItemDetailId).SingleOrDefault();
                            item.PaymentItemName += " [When:" + _CheckupMedicineDetails.WhentoTake + ", Days: " + _CheckupMedicineDetails.NoofDays + " ]";
                        }
                        _PaymentsDetailsCRUDViewModel.Add(item);
                    }
                    var result = _PaymentsDetailsCRUDViewModel.Concat(listOtherService).ToList();
                    return result.AsQueryable();
                }
                else
                {
                    return listOtherService.AsQueryable();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting payment details2.");
                throw;
            }
        }

        public IQueryable<PaymentModeHistoryCRUDViewModel> GetPaymentModeHistory()
        {
            try
            {
                return (from _PaymentModeHistory in _context.PaymentModeHistory
                        where _PaymentModeHistory.Cancelled == false
                        select new PaymentModeHistoryCRUDViewModel
                        {
                            Id = _PaymentModeHistory.Id,
                            PaymentId = _PaymentModeHistory.PaymentId,
                            ModeOfPayment = _PaymentModeHistory.ModeOfPayment,
                            Amount = _PaymentModeHistory.Amount,
                            ReferenceNo = _PaymentModeHistory.ReferenceNo,
                            CreatedDate = _PaymentModeHistory.CreatedDate,
                            ModifiedDate = _PaymentModeHistory.ModifiedDate,
                            CreatedBy = _PaymentModeHistory.CreatedBy,
                            ModifiedBy = _PaymentModeHistory.ModifiedBy,
                            Cancelled = _PaymentModeHistory.Cancelled
                        }).OrderBy(x => x.CreatedDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting payment Mode History.");
                throw;
            }
        }
        public async Task<ManageCheckupViewModel> GetByCheckupDetails(Int64 id)
        {
            ManageCheckupViewModel vm = new ManageCheckupViewModel();
            vm.CheckupSummaryCRUDViewModel = GetCheckupGridItem().Where(x => x.Id == id).SingleOrDefault();
            vm.VitalSignsCRUDViewModel = vm.CheckupSummaryCRUDViewModel;

            vm.listCheckupMedicineDetailsCRUDViewModel = await GetCheckupMedicineDetails().Where(x => x.VisitId == vm.CheckupSummaryCRUDViewModel.VisitId).ToListAsync();

            //vm.ManagePatientTestViewModel = await GetByPatientTestDetails(vm.CheckupSummaryCRUDViewModel.VisitId);
            vm.PatientTestCRUDViewModel = GetPatientTestGridItem().Where(x => x.VisitId == vm.CheckupSummaryCRUDViewModel.VisitId).SingleOrDefault();

            if (vm.PatientTestCRUDViewModel != null)
                vm.listPatientTestDetailCRUDViewModel = await GetPatientTestDetail().Where(x => x.PatientTestId == vm.PatientTestCRUDViewModel.Id).ToListAsync();

            vm.PatientInfoCRUDViewModel = GetPatientInfoGridItem().Where(x => x.Id == vm.CheckupSummaryCRUDViewModel.PatientId).SingleOrDefault();
            vm.CompanyInfoCRUDViewModel = GetCompanyInfo();
            return vm;
        }

        public async Task<ManageCheckupHistoryViewModel> GetByPatientHistory(Int64 id)
        {
            try
            {
                ManageCheckupHistoryViewModel vm = new ManageCheckupHistoryViewModel();
                List<CheckupMedicineDetailsCRUDViewModel> listCheckupMedicineDetails = new List<CheckupMedicineDetailsCRUDViewModel>();

                vm.listCheckupMedicineDetailsCRUDViewModel = null;
                vm.listCheckupSummaryCRUDViewModel = GetCheckupGridItem().Where(x => x.PatientId == id).ToList();
                foreach (var item in vm.listCheckupSummaryCRUDViewModel)
                {
                    var result = await GetCheckupMedicineDetails().Where(x => x.VisitId == item.VisitId).ToListAsync();
                    listCheckupMedicineDetails.AddRange(result);
                }

                vm.listCheckupMedicineDetailsCRUDViewModel = listCheckupMedicineDetails;
                vm.PatientInfoCRUDViewModel = GetPatientInfoGridItem().Where(x => x.Id == id).SingleOrDefault();
                vm.CompanyInfoCRUDViewModel = GetCompanyInfo();
                return vm;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting patient History.");
                throw;
            }
        }

        public IQueryable<CheckupSummaryCRUDViewModel> GetCheckupGridItem()
        {
            try
            {
                var result = (from _CheckupSummary in _context.CheckupSummary
                              join _UserProfile in _context.UserProfile on _CheckupSummary.PatientId equals _UserProfile.UserProfileId
                              join _UserProfile1 in _context.UserProfile on _CheckupSummary.DoctorId equals _UserProfile1.UserProfileId into list1
                              from _objDoctorsInfo in list1.DefaultIfEmpty()
                              join _PatientInfo in _context.PatientInfo on _UserProfile.ApplicationUserId equals _PatientInfo.ApplicationUserId
                              where !_CheckupSummary.Cancelled
                              select new CheckupSummaryCRUDViewModel
                              {
                                  Id = _CheckupSummary.Id,
                                  PatientId = _CheckupSummary.PatientId,
                                  SerialNo = _CheckupSummary.SerialNo,
                                  VisitId = _CheckupSummary.VisitId,
                                  PaymentId = _CheckupSummary.PaymentId,
                                  // Retrieve patient's name from UserProfile
                                  PatientName = _UserProfile.FirstName + " " + _UserProfile.LastName,
                                  PatientType = _CheckupSummary.PatientType,
                                  DoctorId = _CheckupSummary.DoctorId,
                                  DoctorName = _objDoctorsInfo != null ? _objDoctorsInfo.FirstName + " " + _objDoctorsInfo.LastName : "",

                                  Symptoms = _CheckupSummary.Symptoms,
                                  Diagnosis = _CheckupSummary.Diagnosis,
                                  HPI = _CheckupSummary.HPI,
                                  VitalSigns = _CheckupSummary.VitalSigns,
                                  PhysicalExamination = _CheckupSummary.PhysicalExamination,
                                  Comments = _CheckupSummary.Comments,
                                  CheckupDate = _CheckupSummary.CheckupDate,
                                  NextVisitDate = _CheckupSummary.NextVisitDate,
                                  Advice = _CheckupSummary.Advice,
                                  BPSystolic = _CheckupSummary.BPSystolic,
                                  BPDiastolic = _CheckupSummary.BPDiastolic,
                                  RespirationRate = _CheckupSummary.RespirationRate,
                                  Temperature = _CheckupSummary.Temperature,
                                  NursingNotes = _CheckupSummary.NursingNotes,
                                  CreatedDate = _CheckupSummary.CreatedDate,

                              }).OrderByDescending(x => x.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting Checkup Details.");
                throw;
            }
        }
        public IQueryable<CheckupMedicineDetailsCRUDViewModel> GetCheckupMedicineDetails()
        {
            try
            {
                return (from _CheckupMedicineDetails in _context.CheckupMedicineDetails
                        join _Medicines in _context.Medicines on _CheckupMedicineDetails.MedicineId equals _Medicines.Id
                        where _CheckupMedicineDetails.Cancelled == false
                        select new CheckupMedicineDetailsCRUDViewModel
                        {
                            Id = _CheckupMedicineDetails.Id,
                            VisitId = _CheckupMedicineDetails.VisitId,
                            MedicineId = _CheckupMedicineDetails.MedicineId,
                            MedicineName = _Medicines.MedicineName,
                            NoofDays = _CheckupMedicineDetails.NoofDays,
                            WhentoTake = _CheckupMedicineDetails.WhentoTake,
                            IsBeforeMeal = _CheckupMedicineDetails.IsBeforeMeal,
                            CreatedDate = _CheckupMedicineDetails.CreatedDate,
                            ModifiedDate = _CheckupMedicineDetails.ModifiedDate,
                            CreatedBy = _CheckupMedicineDetails.CreatedBy,
                            ModifiedBy = _CheckupMedicineDetails.ModifiedBy,
                            Cancelled = _CheckupMedicineDetails.Cancelled,
                        }).OrderBy(x => x.CreatedDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting Checkup Medicine Details.");
                throw;
            }
        }

        public IQueryable<ExpensesGridViewModel> GetExpensesGridItem()
        {
            try
            {
                return (from _Expenses in _context.Expenses
                        join _ExpenseCategories in _context.ExpenseCategories on _Expenses.ExpenseCategoriesId equals _ExpenseCategories.Id
                        where _Expenses.Cancelled == false
                        select new ExpensesGridViewModel
                        {
                            Id = _Expenses.Id,
                            ExpenseCategoriesId = _Expenses.ExpenseCategoriesId,
                            ExpenseCategoriesName = _ExpenseCategories.Name,
                            Amount = _Expenses.Amount,
                            CreatedDate = _Expenses.CreatedDate,
                            ModifiedDate = _Expenses.ModifiedDate,
                            CreatedBy = _Expenses.CreatedBy,
                            ModifiedBy = _Expenses.ModifiedBy,
                        }).OrderByDescending(x => x.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting Expenses.");
                throw;
            }
        }

        public IQueryable<ExpensesGridViewModel> GetExpensesGridItemByHospitalId(long hospitalId)
        {
            try
            {

                return (from _Expenses in _context.Expenses
                        join _ExpenseCategories in _context.ExpenseCategories on _Expenses.ExpenseCategoriesId equals _ExpenseCategories.Id
                        where _Expenses.Cancelled == false && _Expenses.HospitalId == hospitalId
                        select new ExpensesGridViewModel
                        {
                            Id = _Expenses.Id,
                            ExpenseCategoriesId = _Expenses.ExpenseCategoriesId,
                            ExpenseCategoriesName = _ExpenseCategories.Name,
                            Amount = _Expenses.Amount,
                            CreatedDate = _Expenses.CreatedDate,
                            ModifiedDate = _Expenses.ModifiedDate,
                            CreatedBy = _Expenses.CreatedBy,
                            ModifiedBy = _Expenses.ModifiedBy,
                        }).OrderByDescending(x => x.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting Expenses.");
                throw;
            }
        }


        public IQueryable<PatientAppointmentCRUDViewModel> GetPatientAppointmentGridItem()
        {
            try
            {
                return (from _PatientAppointment in _context.PatientAppointment
                        join _UserProfile in _context.UserProfile on _PatientAppointment.PatientId equals _UserProfile.UserProfileId
                        join _DoctorsInfo in _context.DoctorsInfo on _PatientAppointment.DoctorId equals _DoctorsInfo.Id into list1
                        from _objDoctorsInfo in list1.DefaultIfEmpty()
                        join _PatientInfo in _context.PatientInfo on _UserProfile.ApplicationUserId equals _PatientInfo.ApplicationUserId
                        where !_PatientAppointment.Cancelled
                        select new PatientAppointmentCRUDViewModel
                        {
                            Id = _PatientAppointment.Id,
                            PatientId = _PatientAppointment.PatientId,
                            VisitId = _PatientAppointment.VisitId,
                            PatientName = _UserProfile.FirstName + " " + _UserProfile.LastName,
                            DoctorId = _PatientAppointment.DoctorId,
                            DoctorName = _UserProfile.FirstName + " " + _UserProfile.LastName,


                            SerialNo = _PatientAppointment.SerialNo,
                            AppointmentDate = _PatientAppointment.AppointmentDate,
                            AppointmentTimeDisplay = String.Format("{0:t}", _PatientAppointment.AppointmentTime),
                            Note = _PatientAppointment.Note,
                            CreatedDate = _PatientAppointment.CreatedDate,

                        }).OrderByDescending(x => x.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting patient appointment Grid.");
                throw;
            }
        }

        public IQueryable<LabTestsCRUDViewModel> GetAllLabTests()
        {
            try
            {
                return (from _LabTests in _context.LabTests
                        join _LabTestCategories in _context.LabTestCategories on _LabTests.LabTestCategoryId equals _LabTestCategories.Id
                        where _LabTests.Cancelled == false
                        select new LabTestsCRUDViewModel
                        {
                            Id = _LabTests.Id,
                            PaymentItemCode = _LabTests.PaymentItemCode,
                            LabTestCategoryName = _LabTestCategories.Name,
                            LabTestName = _LabTests.LabTestName,
                            Unit = _LabTests.Unit,
                            UnitPrice = _LabTests.UnitPrice,
                            ReferenceRange = _LabTests.ReferenceRange,
                            Status = _LabTests.Status,
                            CreatedDate = _LabTests.CreatedDate,

                        }).OrderByDescending(x => x.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting lab test list.");
                throw;
            }
        }

        public IQueryable<LabTestsCRUDViewModel> GetAllLabTestsByHospital(long hospitalId, string roleName)
        {
            try
            {
                if (roleName == "SuperAdmin")
                {

                    return (from _LabTests in _context.LabTests
                            join _LabTestCategories in _context.LabTestCategories on _LabTests.LabTestCategoryId equals _LabTestCategories.Id
                            join _hospital in _context.Hospital on _LabTests.HospitalId equals _hospital.Id
                             into hospitalGroup
                            from _hospital in hospitalGroup.DefaultIfEmpty()
                            where _LabTests.Cancelled == false
                            select new LabTestsCRUDViewModel
                            {
                                Id = _LabTests.Id,
                                PaymentItemCode = _LabTests.PaymentItemCode,
                                LabTestCategoryName = _LabTestCategories.Name,
                                LabTestName = _LabTests.LabTestName,
                                Unit = _LabTests.Unit,
                                UnitPrice = _LabTests.UnitPrice,
                                ReferenceRange = _LabTests.ReferenceRange,
                                Status = _LabTests.Status,
                                CreatedDate = _LabTests.CreatedDate,
                                Hospital = _hospital.HospitalName

                            }).OrderByDescending(x => x.Id);
                }
                else
                {
                    return (from _LabTests in _context.LabTests
                            join _LabTestCategories in _context.LabTestCategories on _LabTests.LabTestCategoryId equals _LabTestCategories.Id
                            where _LabTests.Cancelled == false && _LabTests.HospitalId == hospitalId
                            select new LabTestsCRUDViewModel
                            {
                                Id = _LabTests.Id,
                                PaymentItemCode = _LabTests.PaymentItemCode,
                                LabTestCategoryName = _LabTestCategories.Name,
                                LabTestName = _LabTests.LabTestName,
                                Unit = _LabTests.Unit,
                                UnitPrice = _LabTests.UnitPrice,
                                ReferenceRange = _LabTests.ReferenceRange,
                                Status = _LabTests.Status,
                                CreatedDate = _LabTests.CreatedDate,
                                Hospital = string.Empty

                            }).OrderByDescending(x => x.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting All Lab tests.");
                throw;
            }
        }

        public IQueryable<MedicinesCRUDViewModel> GetAllMedicines()
        {
            try
            {
                return (from _Medicines in _context.Medicines
                        join _MedicineCategories in _context.MedicineCategories on _Medicines.MedicineCategoryId equals _MedicineCategories.Id
                        join _MedicineManufacture in _context.MedicineManufacture on _Medicines.ManufactureId equals _MedicineManufacture.Id
                        join _Unit in _context.Unit on _Medicines.UnitId equals _Unit.Id
                        where _Medicines.Cancelled == false
                        select new MedicinesCRUDViewModel
                        {
                            Id = _Medicines.Id,
                            Code = _Medicines.Code,
                            MedicineCategoryId = _Medicines.MedicineCategoryId,
                            MedicineCategoryName = _MedicineCategories.Name,
                            PaymentItemCode = _Medicines.PaymentItemCode,
                            MedicineName = _Medicines.MedicineName,
                            UnitName = _Unit.Name,
                            ManufactureId = _Medicines.ManufactureId,
                            ManufactureName = _MedicineManufacture.Name,
                            UnitPrice = _Medicines.UnitPrice,
                            SellPrice = _Medicines.SellPrice,
                            OldUnitPrice = _Medicines.OldUnitPrice,
                            OldSellPrice = _Medicines.OldSellPrice,
                            Quantity = _Medicines.Quantity,
                            Description = _Medicines.Description,
                            ExpiryDate = _Medicines.ExpiryDate,
                            CreatedDate = _Medicines.CreatedDate,
                            ModifiedDate = _Medicines.ModifiedDate,
                            CreatedBy = _Medicines.CreatedBy,
                            ModifiedBy = _Medicines.ModifiedBy

                        }).OrderByDescending(x => x.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting All Medicine List.");
                throw;
            }
        }
        public IQueryable<MedicinesCRUDViewModel> GetAllMedicinesByHospital(long hospitalId, string roleName)
        {
            try
            {
                if (roleName == "SuperAdmin")
                {
                    return (from _Medicines in _context.Medicines
                            join _MedicineCategories in _context.MedicineCategories on _Medicines.MedicineCategoryId equals _MedicineCategories.Id
                            join _MedicineManufacture in _context.MedicineManufacture on _Medicines.ManufactureId equals _MedicineManufacture.Id
                            join _Unit in _context.Unit on _Medicines.UnitId equals _Unit.Id
                            join _hospital in _context.Hospital on _Medicines.HospitalId equals _hospital.Id
                            into hospitalGroup
                            from _hospital in hospitalGroup.DefaultIfEmpty()
                            where _Medicines.Cancelled == false
                            select new MedicinesCRUDViewModel
                            {
                                Id = _Medicines.Id,
                                Code = _Medicines.Code,
                                MedicineCategoryId = _Medicines.MedicineCategoryId,
                                MedicineCategoryName = _MedicineCategories.Name,
                                PaymentItemCode = _Medicines.PaymentItemCode,
                                MedicineName = _Medicines.MedicineName,
                                UnitName = _Unit.Name,
                                ManufactureId = _Medicines.ManufactureId,
                                ManufactureName = _MedicineManufacture.Name,
                                UnitPrice = _Medicines.UnitPrice,
                                SellPrice = _Medicines.SellPrice,
                                OldUnitPrice = _Medicines.OldUnitPrice,
                                OldSellPrice = _Medicines.OldSellPrice,
                                Quantity = _Medicines.Quantity,
                                Description = _Medicines.Description,
                                ExpiryDate = _Medicines.ExpiryDate,
                                CreatedDate = _Medicines.CreatedDate,
                                ModifiedDate = _Medicines.ModifiedDate,
                                CreatedBy = _Medicines.CreatedBy,
                                ModifiedBy = _Medicines.ModifiedBy,
                                Hospital = _hospital.HospitalName

                            }).OrderByDescending(x => x.Id);
                }
                else
                {
                    return (from _Medicines in _context.Medicines
                            join _MedicineCategories in _context.MedicineCategories on _Medicines.MedicineCategoryId equals _MedicineCategories.Id
                            join _MedicineManufacture in _context.MedicineManufacture on _Medicines.ManufactureId equals _MedicineManufacture.Id
                            join _Unit in _context.Unit on _Medicines.UnitId equals _Unit.Id
                            where _Medicines.Cancelled == false && _Medicines.HospitalId == hospitalId
                            select new MedicinesCRUDViewModel
                            {
                                Id = _Medicines.Id,
                                Code = _Medicines.Code,
                                MedicineCategoryId = _Medicines.MedicineCategoryId,
                                MedicineCategoryName = _MedicineCategories.Name,
                                PaymentItemCode = _Medicines.PaymentItemCode,
                                MedicineName = _Medicines.MedicineName,
                                UnitName = _Unit.Name,
                                ManufactureId = _Medicines.ManufactureId,
                                ManufactureName = _MedicineManufacture.Name,
                                UnitPrice = _Medicines.UnitPrice,
                                SellPrice = _Medicines.SellPrice,
                                OldUnitPrice = _Medicines.OldUnitPrice,
                                OldSellPrice = _Medicines.OldSellPrice,
                                Quantity = _Medicines.Quantity,
                                Description = _Medicines.Description,
                                ExpiryDate = _Medicines.ExpiryDate,
                                CreatedDate = _Medicines.CreatedDate,
                                ModifiedDate = _Medicines.ModifiedDate,
                                CreatedBy = _Medicines.CreatedBy,
                                ModifiedBy = _Medicines.ModifiedBy,
                                Hospital = string.Empty

                            }).OrderByDescending(x => x.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting Medicine List.");
                throw;
            }
        }

        public IQueryable<MedicineHistoryCRUDViewModel> GetAllMedicineHistory()
        {
            try
            {
                return (from _MedicineHistory in _context.MedicineHistory
                        join _MedicineManufacture in _context.MedicineManufacture on _MedicineHistory.ManufactureId equals _MedicineManufacture.Id
                        join _Unit in _context.Unit on _MedicineHistory.UnitId equals _Unit.Id
                        select new MedicineHistoryCRUDViewModel
                        {
                            Id = _MedicineHistory.Id,
                            MedicineId = _MedicineHistory.MedicineId,
                            Code = _MedicineHistory.Code,
                            MedicineName = _MedicineHistory.MedicineName,
                            ManufactureId = _MedicineHistory.ManufactureId,
                            ManufactureName = _MedicineManufacture.Name,
                            UnitId = _MedicineHistory.UnitId,
                            UnitName = _Unit.Name,
                            UnitPrice = _MedicineHistory.UnitPrice,
                            SellPrice = _MedicineHistory.SellPrice,
                            OldUnitPrice = _MedicineHistory.OldUnitPrice,
                            OldSellPrice = _MedicineHistory.OldSellPrice,
                            OldQuantity = _MedicineHistory.OldQuantity,
                            NewQuantity = _MedicineHistory.NewQuantity,
                            TranQuantity = _MedicineHistory.TranQuantity,
                            UpdateQntType = _MedicineHistory.UpdateQntType,
                            UpdateQntNote = _MedicineHistory.UpdateQntNote,
                            Note = _MedicineHistory.Note,
                            Action = _MedicineHistory.Action,
                            CreatedDate = _MedicineHistory.CreatedDate,
                            ModifiedDate = _MedicineHistory.ModifiedDate,
                            CreatedBy = _MedicineHistory.CreatedBy,
                            ModifiedBy = _MedicineHistory.ModifiedBy,
                            Cancelled = _MedicineHistory.Cancelled,
                        }).OrderByDescending(x => x.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting All medicine history.");
                throw;
            }
        }
        public async Task<PatientTestDetail> CreatePatientTestDetail(PatientTestDetailCRUDViewModel vm)
        {
            PatientTestDetail _PatientTestDetail = vm;
            _PatientTestDetail.CreatedDate = DateTime.Now;
            _PatientTestDetail.ModifiedDate = DateTime.Now;
            _context.Add(_PatientTestDetail);
            await _context.SaveChangesAsync();
            return _PatientTestDetail;
        }
        public IQueryable<PaymentsDetailsCRUDViewModel> GetServicePaymentList()
        {
            try
            {
                var _GetAllItem = GetAllItem();
                var _ServicePaymentViewModel = (from _PaymentsDetails in _context.PaymentsDetails
                                                join _AllItem in _GetAllItem on _PaymentsDetails.PaymentItemCode equals _AllItem.PaymentItemCode
                                                where _PaymentsDetails.Cancelled == false
                                                select new PaymentsDetailsCRUDViewModel
                                                {
                                                    Id = _PaymentsDetails.Id,
                                                    PaymentsId = _PaymentsDetails.PaymentsId,
                                                    ItemDetailId = _PaymentsDetails.ItemDetailId,
                                                    PaymentItemCode = _PaymentsDetails.PaymentItemCode,
                                                    PaymentItemName = _AllItem.ItemName,
                                                    Quantity = _PaymentsDetails.Quantity,
                                                    UnitPrize = _PaymentsDetails.UnitPrize,
                                                    TotalAmount = _PaymentsDetails.TotalAmount,
                                                    CreatedDate = _PaymentsDetails.CreatedDate
                                                }).OrderByDescending(x => x.Id);

                return _ServicePaymentViewModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting Service Payment List.");
                throw;
            }
        }
        public IQueryable<AllItemViewModel> GetAllItem()
        {
            try
            {
                var listLabTests = (from _LabTests in _context.LabTests
                                    select new AllItemViewModel
                                    {
                                        PaymentItemCode = _LabTests.PaymentItemCode,
                                        ItemName = _LabTests.LabTestName,
                                    });

                var listPaymentCategories = (from _PaymentCategories in _context.PaymentCategories
                                             select new AllItemViewModel
                                             {
                                                 PaymentItemCode = _PaymentCategories.PaymentItemCode,
                                                 ItemName = _PaymentCategories.Name,
                                             });
                var listMedicines = (from _Medicines in _context.Medicines
                                     select new AllItemViewModel
                                     {
                                         PaymentItemCode = _Medicines.PaymentItemCode,
                                         ItemName = _Medicines.MedicineName,
                                     });

                var result = listPaymentCategories.Concat(listLabTests);
                var AllItem = result.Concat(listMedicines);

                return AllItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error into Getting All Item.");
                throw;
            }
        }
        public async Task<List<ManageUserRolesDetails>> GetManageRoleDetailsList(Int64 id)
        {
            var result = await (from tblObj in _context.ManageUserRolesDetails.Where(x => x.ManageRoleId == id)
                                select new ManageUserRolesDetails
                                {
                                    Id = tblObj.Id,
                                    RoleId = tblObj.RoleId,
                                    RoleName = tblObj.RoleName,
                                    IsAllowed = tblObj.IsAllowed,
                                }).OrderBy(x => x.RoleName).ToListAsync();
            return result;
        }
        public IQueryable<ItemDropdownListViewModel> GetCommonddlData(string strTableName)
        {
            var sql = "select Id, Name from " + strTableName + " where Cancelled = 0";
            var result = _context.ItemDropdownListViewModel.FromSqlRaw(sql);
            return result;
        }
        public IQueryable<UserProfileCRUDViewModel> GetUserProfileDetails()
        {
            var result = (from vm in _context.UserProfile
                          join _Department in _context.Department on vm.Department equals _Department.Id
                          into _Department
                          from objDepartment in _Department.DefaultIfEmpty()
                          join _SubDepartment in _context.SubDepartment on vm.SubDepartment equals _SubDepartment.Id
                          into _SubDepartment
                          from objSubDepartment in _SubDepartment.DefaultIfEmpty()
                          join _Designation in _context.Designation on vm.Designation equals _Designation.Id
                          into _Designation
                          from objDesignation in _Designation.DefaultIfEmpty()
                          join _Hospital in _context.Hospital on vm.HospitalId equals _Hospital.Id
                          into _Hospital
                          from Hospital in _Hospital.DefaultIfEmpty()
                          join _ManageRole in _context.ManageUserRoles on vm.RoleId equals _ManageRole.Id
                          into _ManageRole
                          from objManageRole in _ManageRole.DefaultIfEmpty()
                          where vm.Cancelled == false
                          select new UserProfileCRUDViewModel
                          {
                              UserProfileId = vm.UserProfileId,
                              ApplicationUserId = vm.ApplicationUserId,
                              EmployeeId = vm.EmployeeId,
                              FirstName = vm.FirstName,
                              LastName = vm.LastName,
                              DateOfBirth = vm.DateOfBirth,
                              Designation = vm.Designation,
                              DesignationDisplay = objDesignation.Name,
                              Department = vm.Department,
                              DepartmentDisplay = objDepartment.Name,
                              SubDepartment = vm.SubDepartment,
                              SubDepartmentDisplay = objSubDepartment.Name,
                              JoiningDate = vm.JoiningDate,
                              LeavingDate = vm.LeavingDate,
                              PhoneNumber = vm.PhoneNumber,
                              Email = vm.Email,
                              Address = vm.Address,
                              Country = vm.Country,
                              ProfilePicture = vm.ProfilePicture,
                              RoleId = vm.RoleId,
                              RoleIdDisplay = objManageRole.Name,
                              //UserType = vm.UserType,
                              CreatedDate = vm.CreatedDate,
                              ModifiedDate = vm.ModifiedDate,
                              CreatedBy = vm.CreatedBy,
                              ModifiedBy = vm.ModifiedBy,
                              Cancelled = vm.Cancelled,
                              HospitalId = (int)vm.HospitalId
                          }).OrderByDescending(x => x.UserProfileId);
            return result;
        }
        public IEnumerable<T> GetTableData<T>(ApplicationDbContext dbContext) where T : class
        {
            return dbContext.Set<T>();
        }
    }
}