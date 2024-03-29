﻿using DataAccess.DataContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogic.Interfaces;
using DataAccess.DataModels;
using DataAccess.CustomModel;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using DataAccess.Enum;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Collections;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Nodes;
using System.Net.Mail;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using static Org.BouncyCastle.Math.EC.ECCurve;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace BusinessLogic.Repository
{
    public class AdminService : IAdminService
    {

        private readonly ApplicationDbContext _db;

        public AdminService(ApplicationDbContext db)
        {
            _db = db;
        }

        public Aspnetuser GetAspnetuser(string email)
        {
            var aspNetUser = _db.Aspnetusers.Include(x => x.Aspnetuserroles).FirstOrDefault(x => x.Email == email);
            return aspNetUser;
        }

        //public bool AdminLogin(AdminLogin adminLogin)
        //{
        //    return _db.Aspnetusers.Any(x=>x.Email == adminLogin.Email && x.Passwordhash==adminLogin.Password);
        //}

        public DashboardModel GetRequestsByStatus(int tabNo, int CurrentPage)
        {
            var query = from r in _db.Requests
                        join rc in _db.Requestclients on r.Requestid equals rc.Requestid
                        select new AdminDashTableModel
                        {
                            firstName = rc.Firstname,
                            lastName = rc.Lastname,
                            intDate = rc.Intdate,
                            intYear = rc.Intyear,
                            strMonth = rc.Strmonth,
                            requestorFname = r.Firstname,
                            requestorLname = r.Lastname,
                            createdDate = r.Createddate,
                            mobileNo = rc.Phonenumber,
                            city = rc.City,
                            state = rc.State,
                            street = rc.Street,
                            zipCode = rc.Zipcode,
                            requestTypeId = r.Requesttypeid,
                            status = r.Status,
                            Requestclientid = rc.Requestclientid,
                            reqId = r.Requestid,
                            regionId = rc.Regionid
                        };


            if (tabNo == 1)
            {

                query = query.Where(x => x.status == (int)StatusEnum.Unassigned);
            }

            else if (tabNo == 2)
            {

                query = query.Where(x => x.status == (int)StatusEnum.Accepted);
            }
            else if (tabNo == 3)
            {

                query = query.Where(x => x.status == (int)StatusEnum.MDEnRoute || x.status == (int)StatusEnum.MDOnSite);
            }
            else if (tabNo == 4)
            {

                query = query.Where(x => x.status == (int)StatusEnum.Conclude);
            }
            else if (tabNo == 5)
            {

                query = query.Where(x => (x.status == (int)StatusEnum.Cancelled || x.status == (int)StatusEnum.CancelledByPatient) || x.status == (int)StatusEnum.Closed);
            }
            else if (tabNo == 6)
            {

                query = query.Where(x => x.status == (int)StatusEnum.Unpaid);
            }

            //var region = query.ToList();

            //DashboardModel dashboard = new DashboardModel();
            //dashboard.adminDashboardList = region;
            //dashboard.regionList = _db.Regions.ToList();

            //var result = query.ToList();
            //int count = result.Count();
            //int TotalPage = (int)Math.Ceiling(count / (double)5);
            //result = result.Skip((CurrentPage - 1) * 5).Take(5).ToList();

            //DashboardModel dashboardModel = new DashboardModel();
            //dashboardModel.adminDashTableList = result;
            //dashboardModel.regionList = _db.Regions.ToList();
            //dashboardModel.TotalPage = TotalPage;
            //dashboardModel.CurrentPage = CurrentPage;
            //return dashboardModel;

            var result = query.ToList();
            int count = result.Count();
            int TotalPage = (int)Math.Ceiling(count / (double)5);
            result = result.Skip((CurrentPage - 1) * 5).Take(5).ToList();

            DashboardModel dashboard = new DashboardModel();
            dashboard.adminDashboardList = result;
            dashboard.regionList = _db.Regions.ToList();
            dashboard.TotalPage = TotalPage;
            dashboard.CurrentPage = CurrentPage;
            return dashboard;
        }

        public DashboardModel GetRequestByRegion(int regionId, int tabNo)
        {
            DashboardModel model = new DashboardModel();
            model = GetRequestsByStatus(tabNo, 1);
            model.adminDashboardList = model.adminDashboardList.Where(x => x.regionId == regionId).ToList();
            return model;
        }

        public bool UpdateAdminNotes(string additionalNotes, int reqId)
        {
            var reqNotes = _db.Requestnotes.FirstOrDefault(x => x.Requestid == reqId);
            try
            {

                if (reqNotes == null)
                {
                    Requestnote rn = new Requestnote();
                    rn.Requestid = reqId;
                    rn.Adminnotes = additionalNotes;
                    rn.Createdby = "admin";
                    //here instead of admin , add id of the admin through which admin is loggedIn 
                    rn.Createddate = DateTime.Now;
                    _db.Requestnotes.Add(rn);
                    _db.SaveChanges();
                }
                else
                {
                    reqNotes.Adminnotes = additionalNotes;
                    reqNotes.Modifieddate = DateTime.Now;
                    reqNotes.Modifiedby = "admin";
                    //here instead of admin , add id of the admin through which admin is loggedIn 
                    _db.Requestnotes.Update(reqNotes);
                    _db.SaveChanges();
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public StatusCountModel GetStatusCount()
        {
            var requestsWithClients = _db.Requests
     .Join(_db.Requestclients,
         r => r.Requestid,
         rc => rc.Requestid,
         (r, rc) => new { Request = r, RequestClient = rc })
     .ToList();

            StatusCountModel statusCount = new StatusCountModel
            {
                NewCount = requestsWithClients.Count(x => x.Request.Status == (int)StatusEnum.Unassigned),
                PendingCount = requestsWithClients.Count(x => x.Request.Status == (int)StatusEnum.Accepted),
                ActiveCount = requestsWithClients.Count(x => x.Request.Status == (int)StatusEnum.MDEnRoute || x.Request.Status == (int)StatusEnum.MDOnSite),
                ConcludeCount = requestsWithClients.Count(x => x.Request.Status == (int)StatusEnum.Conclude),
                ToCloseCount = requestsWithClients.Count(x => (x.Request.Status == (int)StatusEnum.Cancelled || x.Request.Status == (int)StatusEnum.CancelledByPatient) || x.Request.Status == (int)StatusEnum.Closed),
                UnpaidCount = requestsWithClients.Count(x => x.Request.Status == (int)StatusEnum.Unpaid)
            };

            return statusCount;
        }

        public ViewCaseViewModel ViewCaseViewModel(int Requestclientid, int RequestTypeId)
        {
            Requestclient obj = _db.Requestclients.FirstOrDefault(x => x.Requestclientid == Requestclientid);
            Request req = _db.Requests.FirstOrDefault(x => x.Requestid == obj.Requestid);
            ViewCaseViewModel viewCaseViewModel = new()
            {
                Requestclientid = obj.Requestclientid,
                Firstname = obj.Firstname,
                Lastname = obj.Lastname,
                Email = obj.Email,
                Phonenumber = obj.Phonenumber,
                City = obj.City,
                Street = obj.Street,
                State = obj.State,
                Zipcode = obj.Zipcode,
                Room = obj.Address,
                Notes = obj.Notes,
                RequestTypeId = RequestTypeId,
                ConfirmationNumber = req.Confirmationnumber,
            };
            return viewCaseViewModel;
        }

        public ViewNotesViewModel ViewNotes(int ReqId)
        {
            var requestNotes = _db.Requestnotes.Where(x => x.Requestid == ReqId).FirstOrDefault();
            var statuslogs = _db.Requeststatuslogs.Where(x => x.Requestid == ReqId).ToList();
            ViewNotesViewModel model = new ViewNotesViewModel();
            if (model == null)
            {
                model.TransferNotesList = null;
                model.PhysicianNotes = null;
                model.AdminNotes = null;
            }


            if (requestNotes != null)
            {
                model.PhysicianNotes = requestNotes.Physiciannotes;
                model.AdminNotes = requestNotes.Adminnotes;
            }
            if (statuslogs != null)
            {
                model.TransferNotesList = statuslogs;
            }

            return model;
        }

        public CancelCaseModel CancelCase(int reqId)
        {
            var casetags = _db.Casetags.ToList();
            var request = _db.Requests.Where(x => x.Requestid == reqId).FirstOrDefault();
            CancelCaseModel model = new()
            {
                PatientFName = request.Firstname,
                PatientLName = request.Lastname,
                casetaglist = casetags

            };
            return model;
        }

        public bool SubmitCancelCase(CancelCaseModel cancelCaseModel)
        {
            try
            {
                var req = _db.Requests.Where(x => x.Requestid == cancelCaseModel.reqId).FirstOrDefault();
                req.Status = (int)StatusEnum.Cancelled;
                req.Casetag = cancelCaseModel.casetag.ToString();
                var reqStatusLog = _db.Requeststatuslogs.Where(x => x.Requestid == cancelCaseModel.reqId).FirstOrDefault();
                if (reqStatusLog == null)
                {
                    Requeststatuslog rsl = new Requeststatuslog();
                    rsl.Requestid = (int)cancelCaseModel.reqId;
                    rsl.Status = (int)StatusEnum.Cancelled;
                    rsl.Notes = cancelCaseModel.notes;
                    rsl.Createddate = DateTime.Now;
                    _db.Requeststatuslogs.Add(rsl);
                    _db.SaveChanges();
                    return true;
                }
                else
                {
                    reqStatusLog.Status = (int)StatusEnum.Cancelled;
                    reqStatusLog.Notes = cancelCaseModel.notes;
                    _db.Requeststatuslogs.Update(reqStatusLog);
                    _db.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public List<Region> GetRegion()
        {
            var region = _db.Regions.ToList();
            return region;
        }
        public List<Physician> GetPhysician(int regionId)
        {
            var physician = _db.Physicians.Where(i => i.Regionid == regionId).ToList();
            return physician;
        }

        public void AssignCasePostData(AssignCaseModel assignCaseModel, int requestId)
        {
            var reqData = _db.Requests.Where(i => i.Requestid == requestId).FirstOrDefault();

            var reqstatusData = new Requeststatuslog()
            {
                Requestid = requestId,
                Notes = assignCaseModel.additionalNotes,
                Createddate = DateTime.Now,
                Status = 2

            };
            reqData.Status = 2;
            reqData.Physicianid = assignCaseModel.physicanNo;

            _db.Add(reqstatusData);
            _db.SaveChanges();

        }

        public BlockCaseModel BlockCase(int reqId)
        {
            var reqClient = _db.Requestclients.Where(x => x.Requestid == reqId).FirstOrDefault();
            BlockCaseModel model = new()
            {
                ReqId = reqId,
                firstName = reqClient.Firstname,
                lastName = reqClient.Lastname,
                reason = null
            };

            return model;
        }

        public bool SubmitBlockCase(BlockCaseModel blockCaseModel)
        {
            try
            {
                var request = _db.Requests.FirstOrDefault(r => r.Requestid == blockCaseModel.ReqId);
                if (request != null)
                {
                    if (request.Isdeleted == null)
                    {
                        request.Isdeleted = new BitArray(1);
                        request.Isdeleted[0] = true;
                        request.Status = (int)StatusEnum.Clear;
                        request.Modifieddate = DateTime.Now;

                        _db.Requests.Update(request);

                    }
                    Blockrequest blockrequest = new Blockrequest();

                    blockrequest.Phonenumber = request.Phonenumber == null ? "+91" : request.Phonenumber;
                    blockrequest.Email = request.Email;
                    blockrequest.Reason = blockCaseModel.reason;
                    blockrequest.Requestid = (int)blockCaseModel.ReqId;
                    blockrequest.Createddate = DateTime.Now;

                    _db.Blockrequests.Add(blockrequest);
                    _db.SaveChanges();
                    return true;

                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public ViewUploadModel GetAllDocById(int requestId)
        {

            var list = _db.Requestwisefiles.Where(x => x.Requestid == requestId).ToList();
            var reqClient = _db.Requestclients.Where(x => x.Requestid == requestId).FirstOrDefault();

            ViewUploadModel result = new()
            {
                files = list,
                firstName = reqClient.Firstname,
                lastName = reqClient.Lastname,

            };

            return result;

        }

        public bool UploadFiles(List<IFormFile> files, int reqId)
        {

            try
            {
                if (files != null)
                {
                    foreach (IFormFile file in files)
                    {
                        if (file != null && file.Length > 0)
                        {
                            //get file name
                            var fileName = Path.GetFileName(file.FileName);

                            //define path
                            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UploadedFiles", fileName);

                            // Copy the file to the desired location
                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                file.CopyTo(stream)
                       ;
                            }

                            Requestwisefile requestwisefile = new()
                            {
                                Filename = fileName,
                                Requestid = reqId,
                                Createddate = DateTime.Now
                            };

                            _db.Requestwisefiles.Add(requestwisefile);

                        }
                    }
                    _db.SaveChanges();
                    return true;
                }
                else { return false; }

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool DeleteFileById(int reqFileId)
        {
            try
            {
                var reqWiseFile = _db.Requestwisefiles.Where(x => x.Requestwisefileid == reqFileId).FirstOrDefault();
                if (reqWiseFile != null)
                {
                    _db.Requestwisefiles.Remove(reqWiseFile);
                    _db.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool DeleteAllFiles(List<string> filenames, int reqId)
        {
            try
            {
                var list = _db.Requestwisefiles.Where(x => x.Requestid == reqId).ToList();

                foreach (var filename in filenames)
                {
                    var existFile = list.Where(x => x.Filename == filename && x.Requestid == reqId).FirstOrDefault();
                    if (existFile != null)
                    {
                        list.Remove(existFile);
                        _db.Requestwisefiles.Remove(existFile);
                    }
                }
                _db.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        //public List<Healthprofessionaltype> GetProfession()
        //{
        //    var profession = _db.Healthprofessionaltypes.ToList();
        //    return profession;
        //}

        //public List<Healthprofessional> GetBusiness()
        //{
        //    var business = _db.Healthprofessionals.ToList();
        //    return business;
        //}

        public Order FetchOrder(int reqId)
        {
            var reqclientid = _db.Requests.Where(x => x.Requestid == reqId).FirstOrDefault();
            var Healthprofessional = _db.Healthprofessionals.ToList();
            var Healthprofessionaltype = _db.Healthprofessionaltypes.ToList();

            Order order = new()
            {
                Profession = Healthprofessionaltype,
                Business = Healthprofessional,
            };
            return order;
        }
        public JsonArray FetchVendors(int selectedValue)
        {
            var result = new JsonArray();
            IEnumerable<Healthprofessional> businesses = _db.Healthprofessionals.Where(prof => prof.Profession == selectedValue);

            foreach (Healthprofessional business in businesses)
            {
                result.Add(new { businessId = business.Vendorid, businessName = business.Vendorname });
            }
            return result;
        }

        public Healthprofessional VendorDetails(int selectedValue)
        {
            Healthprofessional business = _db.Healthprofessionals.First(prof => prof.Vendorid == selectedValue);

            return business;
        }

        public async Task SendOrderDetails(Order order)
        {
            Orderdetail orderDetail = new Orderdetail()
            {
                Vendorid = order.vendorid,
                Requestid = order.ReqId,
                Faxnumber = order.faxnumber,
                Email = order.email,
                Businesscontact = order.BusineesContact,
                Prescription = order.orderdetail,
                Noofrefill = order.refill,
                Createddate = DateTime.Now,
                Createdby = "admin",
            };
            await _db.Orderdetails.AddAsync(orderDetail);
            await _db.SaveChangesAsync();
        }


        public void TransferReqPostData(AssignCaseModel assignCaseModel, int requestId)
        {
            var reqData = _db.Requests.Where(i => i.Requestid == requestId).FirstOrDefault();

            reqData.Status = (int)StatusEnum.Accepted;
            reqData.Physicianid = assignCaseModel.physicanNo;

            Requeststatuslog requeststatuslog = new Requeststatuslog()
            {
                Requestid = requestId,
                Notes = assignCaseModel.additionalNotes,
                Createddate = DateTime.Now,
                Status = (int)StatusEnum.Accepted,
            };
            _db.Requests.Update(reqData);
            _db.Requeststatuslogs.Add(requeststatuslog);
            _db.SaveChanges();

        }

        public bool Clearcase(int requestId)
        {
            try
            {
                var req = _db.Requests.Where(x => x.Requestid == requestId).FirstOrDefault();

                if (req != null)
                {

                    req.Status = (int)StatusEnum.Clear;
                    _db.Requests.Update(req);
                    _db.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public SendAgreement Agreement(int reqClientId)
        {
            var obj = _db.Requestclients.FirstOrDefault(x => x.Requestclientid == reqClientId);

            SendAgreement sendAgreement = new()
            {
                phonenumber = obj.Phonenumber,
                ReqClientId = reqClientId,
                email = obj.Email,
            };
            return sendAgreement;
        }

        public void SendAgreementEmail(SendAgreement model, string link)
        {
            Requestclient reqCli = _db.Requestclients.FirstOrDefault(requestCli => requestCli.Requestclientid == model.ReqClientId);

            string mail = "tatva.dotnet.yashvariya@outlook.com";
            string password = "Itzvariya@23";

            SmtpClient client = new("smtp.office365.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(mail, password),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            MailMessage mailMessage = new()
            {
                From = new MailAddress(mail, "HalloDoc"),
                Subject = "Hallodoc review agreement",
                IsBodyHtml = true,
                Body = "<h3>Admin has sent you the agreement papers to review. Click on the link below to read the agreement.</h3><a href=\"" + link + "\">Review Agreement link</a>",
            };

            mailMessage.To.Add(model.email);

            client.Send(mailMessage);
        }

        public bool ReviewAgree(ReviewAgreement Agreement)
        {
            try
            {
                var reqClient = _db.Requestclients.Where(x => x.Requestclientid == Agreement.ReqClientId).FirstOrDefault();
                var req = _db.Requests.FirstOrDefault(x => x.Requestid == reqClient.Requestid);
                if (req != null)
                {
                    req.Status = (int)StatusEnum.MDEnRoute;

                    Requeststatuslog requeststatuslog = new Requeststatuslog();
                    requeststatuslog.Requestid = req.Requestid;
                    requeststatuslog.Status = req.Status;
                    requeststatuslog.Createddate = DateTime.Now;

                    _db.Requests.Update(req);
                    _db.Requeststatuslogs.Add(requeststatuslog);
                    _db.SaveChanges();

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }
        public CancelAngreement CancelAgreement(int requestClientId)
        {
            Requestclient reqClient = _db.Requestclients.Where(x => x.Requestclientid == requestClientId).FirstOrDefault();
            Request request = _db.Requests.FirstOrDefault(x => x.Requestid == reqClient.Requestid);
            CancelAngreement obj = new()
            {
                ReqClientId = requestClientId,
                Firstname = request.Firstname + " " + request.Lastname,
            };
            return obj;
        }

        public bool CancelAgreement(CancelAngreement cancel)
        {
            try
            {
                Requestclient reqClient = _db.Requestclients.Where(x => x.Requestclientid == cancel.ReqClientId).FirstOrDefault();

                if (reqClient != null)
                {
                    Request request = _db.Requests.FirstOrDefault(x => x.Requestid == reqClient.Requestid);
                    request.Status = (int)StatusEnum.CancelledByPatient;

                    Requeststatuslog requeststatuslog = new Requeststatuslog();
                    requeststatuslog.Requestid = request.Requestid;
                    requeststatuslog.Status = request.Status;
                    requeststatuslog.Notes = cancel.Reason;
                    requeststatuslog.Createddate = DateTime.Now;

                    _db.Requests.Update(request);
                    _db.Requeststatuslogs.Add(requeststatuslog);
                    _db.SaveChanges();

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public CloseCase closeCase(int reqId)
        {

            var requestclient = _db.Requestclients.FirstOrDefault(x => x.Requestid == reqId);
            var requestwisefile = _db.Requestwisefiles.Where(x => x.Requestid == reqId).ToList();

            CloseCase obj = new()
            {
                ReqId = reqId,
                Firstname = requestclient.Firstname,
                Lastname = requestclient.Lastname,
                email = requestclient.Email,
                phoneno = requestclient.Phonenumber,
                file = requestwisefile,

            };
            return obj;
        }

        public bool EditCloseCase(CloseCase closeCase)
        {
            try
            {
                var requestclient = _db.Requestclients.FirstOrDefault(x => x.Requestid == closeCase.ReqId);
                requestclient.Phonenumber = closeCase.phoneno;
                requestclient.Email = closeCase.email;
                _db.Requestclients.Update(requestclient);
                _db.SaveChanges();

                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ChangeCloseCase(CloseCase closeCase)
        {
            try
            {
                Request req = _db.Requests.FirstOrDefault(x => x.Requestid == closeCase.ReqId);


                req.Status = (int)StatusEnum.Unpaid;
                _db.Requests.Update(req);
                _db.SaveChanges();
                return true;
            }
            catch { return false; }
        }

        public EncounterFormModel EncounterForm(int reqId)
        {
            var reqClient = _db.Requestclients.FirstOrDefault(x => x.Requestid == reqId);
            var encForm = _db.Encounterforms.FirstOrDefault(x => x.Requestid == reqId);
            EncounterFormModel ef = new EncounterFormModel();
            ef.reqid = reqId;
            ef.FirstName = reqClient.Firstname;
            ef.LastName = reqClient.Lastname;
            ef.Location = reqClient.Street + reqClient.City + reqClient.State + reqClient.Zipcode;
            //ef.BirthDate = new DateTime((int)(reqClient.Intyear), Convert.ToInt16(reqClient.Strmonth), (int)(reqClient.Intdate)).ToString("yyyy-MM-dd");
            ef.PhoneNumber = reqClient.Phonenumber;
            ef.Email = reqClient.Email;
            if (encForm != null)
            {
                ef.HistoryIllness = encForm.Illnesshistory;
                ef.MedicalHistory = encForm.Medicalhistory;
                //ef.Date = new DateTime((int)(encForm.Intyear), Convert.ToInt16(encForm.Strmonth), (int)(encForm.Intdate)).ToString("yyyy-MM-dd");
                ef.Medications = encForm.Medications;
                ef.Allergies = encForm.Allergies;
                ef.Temp = encForm.Temperature;
                ef.Hr = encForm.Heartrate;
                ef.Rr = encForm.Respirationrate;
                ef.BpS = encForm.Bloodpressuresystolic;
                ef.BpD = encForm.Bloodpressurediastolic;
                ef.O2 = encForm.Oxygenlevel;
                ef.Pain = encForm.Pain;
                ef.Heent = encForm.Heent;
                ef.Cv = encForm.Cardiovascular;
                ef.Chest = encForm.Chest;
                ef.Abd = encForm.Abdomen;
                ef.Extr = encForm.Extremities;
                ef.Skin = encForm.Skin;
                ef.Neuro = encForm.Neuro;
                ef.Other = encForm.Other;
                ef.Diagnosis = encForm.Diagnosis;
                ef.TreatmentPlan = encForm.Treatmentplan;
                ef.MedicationDispensed = encForm.Medicationsdispensed;
                ef.Procedures = encForm.Procedures;
                ef.FollowUp = encForm.Followup;
            }
            return ef;
        }

        public bool SubmitEncounterForm(EncounterFormModel model)
        {
            try
            {
                //concludeEncounter _obj = new concludeEncounter();

                var ef = _db.Encounterforms.FirstOrDefault(r => r.Requestid == model.reqid);

                if (ef == null)
                {
                    Encounterform _encounter = new Encounterform()
                    {
                        Requestid = model.reqid,
                        Firstname = model.FirstName,
                        Lastname = model.LastName,
                        Location = model.Location,
                        Phonenumber = model.PhoneNumber,
                        Email = model.Email,
                        Illnesshistory = model.HistoryIllness,
                        Medicalhistory = model.MedicalHistory,
                        //date = model.Date,
                        Medications = model.Medications,
                        Allergies = model.Allergies,
                        Temperature = model.Temp,
                        Heartrate = model.Hr,
                        Respirationrate = model.Rr,
                        Bloodpressuresystolic = model.BpS,
                        Bloodpressurediastolic = model.BpD,
                        Oxygenlevel = model.O2,
                        Pain = model.Pain,
                        Heent = model.Heent,
                        Cardiovascular = model.Cv,
                        Chest = model.Chest,
                        Abdomen = model.Abd,
                        Extremities = model.Extr,
                        Skin = model.Skin,
                        Neuro = model.Neuro,
                        Other = model.Other,
                        Diagnosis = model.Diagnosis,
                        Treatmentplan = model.TreatmentPlan,
                        Medicationsdispensed = model.MedicationDispensed,
                        Procedures = model.Procedures,
                        Followup = model.FollowUp,
                        Isfinalized = false
                    };

                    _db.Encounterforms.Add(_encounter);

                    //_obj.indicate = true;
                }
                else
                {
                    var efdetail = _db.Encounterforms.FirstOrDefault(x => x.Requestid == model.reqid);

                    efdetail.Requestid = model.reqid;
                    efdetail.Illnesshistory = model.HistoryIllness;
                    efdetail.Medicalhistory = model.MedicalHistory;
                    //efdetail.Date = model.Date;
                    efdetail.Medications = model.Medications;
                    efdetail.Allergies = model.Allergies;
                    efdetail.Temperature = model.Temp;
                    efdetail.Heartrate = model.Hr;
                    efdetail.Respirationrate = model.Rr;
                    efdetail.Bloodpressuresystolic = model.BpS;
                    efdetail.Bloodpressurediastolic = model.BpD;
                    efdetail.Oxygenlevel = model.O2;
                    efdetail.Pain = model.Pain;
                    efdetail.Heent = model.Heent;
                    efdetail.Cardiovascular = model.Cv;
                    efdetail.Chest = model.Chest;
                    efdetail.Abdomen = model.Abd;
                    efdetail.Extremities = model.Extr;
                    efdetail.Skin = model.Skin;
                    efdetail.Neuro = model.Neuro;
                    efdetail.Other = model.Other;
                    efdetail.Diagnosis = model.Diagnosis;
                    efdetail.Treatmentplan = model.TreatmentPlan;
                    efdetail.Medicationsdispensed = model.MedicationDispensed;
                    efdetail.Procedures = model.Procedures;
                    efdetail.Followup = model.FollowUp;
                    efdetail.Modifieddate = DateTime.Now;
                    ef.Isfinalized = false;
                    _db.Encounterforms.Update(efdetail);
                    // _obj.indicate = true;
                };


                _db.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public bool CreateRequest(CreateRequestModel model, string sessionEmail)
        {
            try
            {
                CreateRequestModel _create = new CreateRequestModel();

                var stateMain = _db.Regions.Where(r => r.Name.ToLower() == model.state.ToLower().Trim()).FirstOrDefault();

                if (stateMain == null)
                {
                    return false;
                }
                else
                {
                    Request _req = new Request();
                    Requestclient _reqClient = new Requestclient();
                    User _user = new User();
                    Aspnetuser _asp = new Aspnetuser();
                    Requestnote _note = new Requestnote();

                    var admin = _db.Admins.Where(r => r.Email == sessionEmail).Select(r => r).First();

                    var existUser = _db.Aspnetusers.FirstOrDefault(r => r.Email == model.email);

                    if (existUser == null)
                    {
                        _asp.Username = model.firstname + "_" + model.lastname;
                        _asp.Email = model.email;
                        _asp.Phonenumber = model.phone;
                        _asp.Createddate = DateTime.Now;
                        _db.Aspnetusers.Add(_asp);
                        _db.SaveChanges();

                        _user.Aspnetuserid = _asp.Id;
                        _user.Firstname = model.firstname;
                        _user.Lastname = model.lastname;
                        _user.Email = model.email;
                        _user.Mobile = model.phone;
                        _user.City = model.city;
                        _user.State = model.state;
                        _user.Street = model.street;
                        _user.Zipcode = model.zipcode;
                        _user.Strmonth = model.dateofbirth.Substring(5, 2);
                        _user.Intdate = Convert.ToInt16(model.dateofbirth.Substring(0, 4));
                        _user.Intyear = Convert.ToInt16(model.dateofbirth.Substring(8, 2));
                        _user.Createdby = _asp.Id;
                        _user.Createddate = DateTime.Now;
                        _user.Regionid = _db.Regions.Where(r => r.Name.ToLower() == model.state.ToLower()).Select(r => r.Regionid).FirstOrDefault();
                        _db.Users.Add(_user);
                        _db.SaveChanges();

                        string registrationLink = "http://localhost:5145/Home/CreateAccount?aspuserId=" + _asp.Id;

                        try
                        {
                            //SendRegistrationEmailCreateRequest(data.email, registrationLink);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }

                    _req.Requesttypeid = 1;
                    _req.Userid = Convert.ToInt32(admin.Aspnetuserid);
                    _req.Firstname = admin.Firstname;
                    _req.Lastname = admin.Lastname;
                    _req.Phonenumber = admin.Mobile;
                    _req.Email = admin.Email;
                    _req.Status = 1;
                    _req.Confirmationnumber = admin.Firstname.Substring(0, 1) + DateTime.Now.ToString().Substring(0, 19);
                    _req.Createddate = DateTime.Now;

                    _db.Requests.Add(_req);
                    _db.SaveChanges();



                    _reqClient.Requestid = _req.Requestid;
                    _reqClient.Firstname = model.firstname;
                    _reqClient.Lastname = model.lastname;
                    _reqClient.Phonenumber = model.phone;
                    _reqClient.Strmonth = model.dateofbirth.Substring(5, 2);
                    _reqClient.Intdate = Convert.ToInt16(model.dateofbirth.Substring(8, 2));
                    _reqClient.Intyear = Convert.ToInt16(model.dateofbirth.Substring(0, 4));
                    _reqClient.Street = model.street;
                    _reqClient.City = model.city;
                    _reqClient.State = model.state;
                    _reqClient.Zipcode = model.zipcode;
                    _reqClient.Regionid = _db.Regions.Where(r => r.Name.ToLower() == model.state.ToLower()).Select(r => r.Regionid).FirstOrDefault();
                    _reqClient.Email = model.email;

                    _db.Requestclients.Add(_reqClient);
                    _db.SaveChanges();

                    _note.Requestid = _req.Requestid;
                    _note.Adminnotes = model.admin_notes;
                    _note.Createdby = _db.Aspnetusers.Where(r => r.Email == model.email).Select(r => r.Id).FirstOrDefault();
                    _note.Createddate = DateTime.Now;
                    _db.Requestnotes.Add(_note);
                    _db.SaveChanges();


                }
                return true;
            }
            catch
            {
                return false;
            }



        }

        //public AdminProfile ProfileInfo(int adminId)
        //{
        //    Admin? obj = _db.Admins.FirstOrDefault(x => x.Adminid == adminId);

        //    var region = _db.Regions.FirstOrDefault(x => x.Regionid == obj.Regionid).Name;
        //    var regionList = _db.Regions.ToList();

        //    AdminProfile profile = new()
        //    {
        //        UserName = obj.Firstname + obj.Lastname,
        //        AdminId = adminId.ToString(),
        //        //AdminPassword=obj.,
        //        Status = obj.Status,
        //        Role = obj.Roleid.ToString() ?? "",
        //        FirstName = obj.Firstname,
        //        LastName = obj.Lastname,
        //        AdminPhone = obj.Mobile,
        //        Email = obj.Email,
        //        ConfirmEmail = obj.Email,
        //        Address1 = obj.Address1,
        //        Address2 = obj.Address2,
        //        City = region,
        //        State = region,
        //        Zip = obj.Zip,
        //        BillingPhone = obj.Altphone,
        //        RegionList = regionList,
        //    };

        //    return profile;
        //}

        //public MyProfileModel MyProfile(string sessionEmail)
        //{
        //    var myProfileMain = _db.Admins.Where(x => x.Email == sessionEmail).Select(x => new MyProfileModel()
        //    {
        //        fname = x.Firstname,
        //        lname = x.Lastname,
        //        email = x.Email,
        //        confirm_email = x.Email,
        //        mobile_no = x.Mobile,
        //        addr1 = x.Address1,
        //        addr2 = x.Address2,
        //        city = x.City,
        //        zip = x.Zip,
        //        state = _db.Regions.Where(r => r.Regionid == x.Regionid).Select(r => r.Name).First(),
        //        roles = _db.Aspnetroles.ToList(),
        //    }).ToList().FirstOrDefault();

        //    var aspnetuser = _db.Aspnetusers.Where(r => r.Email == sessionEmail).First();



        //    myProfileMain.username = aspnetuser.Username;
        //    myProfileMain.password = aspnetuser.Passwordhash;

        //    return myProfileMain;
        //}

        public bool VerifyState(string state)
        {

            var stateMain = _db.Regions.Where(r => r.Name.ToLower() == state.ToLower().Trim()).FirstOrDefault();

            if (stateMain == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public List<ProviderModel> GetProvider()
        {
            var provider = from phy in _db.Physicians
                           join role in _db.Roles on phy.Roleid equals role.Roleid
                           join phynoti in _db.Physiciannotifications on phy.Physicianid equals phynoti.Pysicianid
                           orderby phy.Physicianid
                           select new ProviderModel
                           {
                               phyId = phy.Physicianid,
                               firstName = phy.Firstname,
                               lastName = phy.Lastname,
                               status = phy.Status.ToString(),
                               role = role.Name,
                               onCallStatus = "un available",
                               notification = phynoti.Isnotificationstopped[0],
                           };
            var result = provider.ToList();

            return result;
        }

        public bool StopNotification(int PhysicianId)
        {

            var phyNotification = _db.Physiciannotifications.Where(r => r.Pysicianid == PhysicianId).Select(r => r).First();

            var notification = new BitArray(1);
            notification[0] = false;

            if (phyNotification.Isnotificationstopped[0] == notification[0])
            {
                phyNotification.Isnotificationstopped = new BitArray(1);
                phyNotification.Isnotificationstopped[0] = true;
                _db.Physiciannotifications.Update(phyNotification);
                _db.SaveChanges();

                return true;
            }
            else
            {
                phyNotification.Isnotificationstopped = new BitArray(1);
                phyNotification.Isnotificationstopped[0] = false;
                _db.Physiciannotifications.Update(phyNotification);
                _db.SaveChanges();

                return false;
            }
        }


        public ProviderModel providerContact(int PhysicianId)
        {
            ProviderModel provider = new ProviderModel()
            {
                phyId = PhysicianId,
            };

            return provider;
        }
        public void providerContactEmail(int phyIdMain, string msg)
        {
            ProviderModel _provider = new ProviderModel();

            _provider.phyId = phyIdMain;

            var provider = _db.Physicians.FirstOrDefault(x => x.Physicianid == phyIdMain);

            try
            {
                SendRegistrationproviderContactEmail(provider.Email, msg, phyIdMain);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        public void SendRegistrationproviderContactEmail(string provider, string msg, int phyIdMain)
        {
            string senderEmail = "tatva.dotnet.yashvariya@outlook.com";
            string senderPassword = "Itzvariya@23";
            SmtpClient client = new SmtpClient("smtp.office365.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false
            };

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "HalloDoc"),
                Subject = "Mail For provider",
                IsBodyHtml = true,
                Body = $"{msg}",
            };


            mailMessage.To.Add(provider);

            client.Send(mailMessage);

            Emaillog emailLog = new Emaillog()
            {
                Subjectname = mailMessage.Subject,
                Emailtemplate = "Sender : " + senderEmail + "Reciver :" + provider + "Subject : " + mailMessage.Subject + "Message : " + msg,
                Emailid = provider,
                Roleid = 1,
                Adminid = _db.Admins.Where(r => r.Email == provider).Select(r => r.Adminid).First(),
                Physicianid = phyIdMain,
                Createdate = DateTime.Now,
                Sentdate = DateTime.Now,
                Isemailsent = new BitArray(1, true),

            };

            _db.Emaillogs.Add(emailLog);
            _db.SaveChanges();
        }

        

        public EditPhysicianAccount EditPhysician(int PhysicianId)
        {
            var physician = _db.Physicians.FirstOrDefault(x => x.Physicianid == PhysicianId);
            var aspnetuser = _db.Aspnetusers.FirstOrDefault(x => x.Id == physician.Aspnetuserid);
            var region = _db.Regions.FirstOrDefault(x => x.Regionid == physician.Regionid).Name;
            EditPhysicianAccount obj = new()
            {
                PhysicianId = PhysicianId,
                UserName = aspnetuser.Username,
                Password = aspnetuser.Passwordhash,
                FirstName = physician.Firstname,
                LastName = physician.Lastname,
                Email = physician.Email,
                PhoneNumber = physician.Mobile,
                MedicalLicenseNumber = physician.Medicallicense,
                NPINumber = physician.Npinumber,
                SyncEmail = physician.Syncemailaddress,
                Address1 = physician.Address1,
                Address2 = physician.Address2,
                City = physician.City,
                State = region,
                Zip = physician.Zip,
                Phone = physician.Altphone,
                BusinessName = physician.Businessname,
                BusinessWebsite = physician.Businesswebsite,
                RegionList = _db.Regions.ToList(),
                
            };

            return obj;
        }

        public bool EditSavePhysician(EditPhysicianAccount editPhysicianAccount)
        {
            try
            {
                
                var physician = _db.Physicians.FirstOrDefault(x => x.Physicianid == editPhysicianAccount.PhysicianId);
                //var aspnetuser = _db.Aspnetusers.FirstOrDefault(x => x.Id == physician.Aspnetuserid);
                //var region = _db.Regions.Where(x => x.Regionid == physician.Regionid).Select(x=>x.Name).First();
                switch (editPhysicianAccount.FormId)
                {
                    case 1:
                        break;
                    case 2:
                        physician.Firstname = editPhysicianAccount.FirstName;
                        physician.Lastname = editPhysicianAccount.LastName;
                        physician.Email = editPhysicianAccount.Email;
                        physician.Mobile = editPhysicianAccount.Phone;
                        physician.Medicallicense = editPhysicianAccount.MedicalLicenseNumber;
                        physician.Npinumber = editPhysicianAccount.NPINumber;
                        physician.Syncemailaddress = editPhysicianAccount.SyncEmail;
                        _db.Physicians.Update(physician);
                        _db.SaveChanges();
                        break;
                    case 3:
                        physician.Address1 = editPhysicianAccount.Address1;
                        physician.Address2 = editPhysicianAccount.Address2;
                        physician.City = editPhysicianAccount.City;
                        physician.Zip = editPhysicianAccount.Zip;
                        physician.Altphone = editPhysicianAccount.Phone;
                        _db.Physicians.Update(physician);
                        _db.SaveChanges();
                        break;
                    case 4:
                        physician.Businessname = editPhysicianAccount.BusinessName;
                        physician.Businesswebsite = editPhysicianAccount.BusinessWebsite;
                        _db.Physicians.Update(physician);
                        _db.SaveChanges();
                        break;
                    case 5:
                        break;
                }

                return true;
            }
            catch
            {
                return false;
            }
            

        }

        public List<Physicianlocation> GetPhysicianlocations()
        {
            var phyLocation = _db.Physicianlocations.ToList();
            return phyLocation;
        }

        public MyProfileModel MyProfile(string sessionEmail)
        {
            var myProfileMain = _db.Admins.Where(x => x.Email == sessionEmail).Select(x => new MyProfileModel()
            {
                fname = x.Firstname,
                lname = x.Lastname,
                email = x.Email,
                confirm_email = x.Email,
                mobile_no = x.Mobile,
                addr1 = x.Address1,
                addr2 = x.Address2,
                city = x.City,
                zip = x.Zip,
                state = _db.Regions.Where(r => r.Regionid == x.Regionid).Select(r => r.Name).First(),
                roles = _db.Aspnetroles.ToList(),
            }).ToList().FirstOrDefault();

            var aspnetuser = _db.Aspnetusers.Where(r => r.Email == sessionEmail).First();



            myProfileMain.username = aspnetuser.Username;
            //myProfileMain.password = aspnetuser.Passwordhash;

            return myProfileMain;
        }

        public bool ResetPassword(string tokenEmail, string resetPassword)
        {
            try
            {
                var aspUser = _db.Aspnetusers.Where(r => r.Email == tokenEmail).Select(r => r).First();

                if (aspUser.Passwordhash != resetPassword)
                {
                    aspUser.Passwordhash = resetPassword;
                    _db.Aspnetusers.Update(aspUser);

                    _db.SaveChanges();

                    return true;
                }
                return false;



            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public bool SubmitAdminInfo(MyProfileModel model, string tokenEmail)
        {
            try
            {

                var aspUser = _db.Aspnetusers.Where(r => r.Email == tokenEmail).Select(r => r).First();

                var adminInfo = _db.Admins.Where(r => r.Email == tokenEmail).Select(r => r).First();

                if (adminInfo.Firstname != model.fname || adminInfo.Lastname != model.lname || adminInfo.Email != model.email || adminInfo.Mobile != model.mobile_no)
                {
                    if (adminInfo.Firstname != model.fname)
                    {

                        adminInfo.Firstname = model.fname;

                    }

                    if (adminInfo.Lastname != model.lname)
                    {
                        adminInfo.Lastname = model.lname;
                    }

                    if (adminInfo.Email != model.email)
                    {
                        adminInfo.Email = model.email;
                        aspUser.Email = model.email;

                        int index = model.email.IndexOf('@');
                        var username = model.email.Substring(0, index);
                        aspUser.Username = username;
                    }

                    if (adminInfo.Mobile != model.mobile_no)
                    {
                        adminInfo.Mobile = model.mobile_no;
                        aspUser.Phonenumber = model.mobile_no;
                    }


                    aspUser.Modifieddate = DateTime.Now;

                    _db.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool SubmitBillingInfo(MyProfileModel model, string tokenEmail)
        {
            try
            {
                var adminInfo = _db.Admins.Where(r => r.Email == tokenEmail).Select(r => r).First();

                var regionid = _db.Regions.Where(x => x.Name.ToLower() == model.state.ToLower()).Select(x => x.Regionid).First();

                if (adminInfo.Address1 != model.addr1 || adminInfo.Address2 != model.addr2 || adminInfo.City != model.city || adminInfo.Zip != model.zip || adminInfo.Regionid != regionid)
                {

                    if (adminInfo.Address1 != model.addr1)
                    {
                        adminInfo.Address1 = model.addr1;
                    }

                    if (adminInfo.Address2 != model.addr2)
                    {
                        adminInfo.Address2 = model.addr2;
                    }

                    if (adminInfo.City != model.city)
                    {
                        adminInfo.City = model.city;
                    }

                    if (adminInfo.Zip != model.zip)
                    {
                        adminInfo.Zip = model.zip;
                    }

                    if (adminInfo.Regionid != regionid)
                    {
                        adminInfo.Regionid = regionid;
                    }
                    if (adminInfo.Altphone != model.altphone)
                    {
                        adminInfo.Altphone = model.altphone;
                    }
                    _db.SaveChanges();

                    return true;
                }

                return false;

            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public List<AccountAccess> AccountAccess()
        {
            var obj = (from role in _db.Roles
                       where role.Isdeleted != new BitArray(1, true)
                       select new AccountAccess
                       {
                           Name = role.Name,
                           RoleId = role.Roleid,
                           AccountType = role.Accounttype,
                       }).ToList();
            return obj;
        }

        public void DeleteRole(int roleId)
        {
            var role = _db.Roles.FirstOrDefault(x => x.Roleid == roleId);
            role.Isdeleted = new BitArray(1, true);
            _db.Roles.Update(role);
            _db.SaveChanges();
        }

        public CreateAccess FetchRole(short selectedValue)
        {
            if (selectedValue == 0)
            {
                CreateAccess obj = new()
                {
                    Menu = _db.Menus.ToList(),
                };
                return obj;
            }
            else if (selectedValue == 1 || selectedValue == 2)
            {

                CreateAccess obj = new()
                {
                    Menu = _db.Menus.Where(x => x.Accounttype == selectedValue).ToList(),
                };
                return obj;
            }
            else
            {
                CreateAccess obj = new();
                return obj;
            }
        }

        public void CreateRole(List<int> menuIds, string roleName, short accountType)
        {
            Role role = new()
            {
                Name = roleName,
                Accounttype = accountType,
                Createdby = "Admin",
                Createddate = DateTime.Now,
                Isdeleted = new BitArray(1, true),
            };
            _db.Roles.Add(role);
            _db.SaveChanges();

            foreach (int menuId in menuIds)
            {
                Rolemenu rolemenu = new()
                {
                    Roleid = role.Roleid,
                    Menuid = menuId,
                };
                _db.Rolemenus.Add(rolemenu);
                _db.SaveChanges();
            };


        }


    }
}
