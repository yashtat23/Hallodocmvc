﻿using System;
using BusinessLogic.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccess.CustomModel;
using DataAccess.DataModels;
using DataAccess.DataContext;
using DataAccess.CustomModels;
using System.Collections;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Globalization;
using BusinessLogic.Services;
using DataAccess.Enums;
using DataAccess.Enum;
using System.Net.Mail;
using System.Net;
using System.Security.Cryptography;

namespace BusinessLogic.Repository
{
    public class PatientService : IPatientService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _http;
        private readonly IJwtService _jwtService;

        public PatientService(ApplicationDbContext db,IHttpContextAccessor http,IJwtService jwtService)
        {
            _db = db;
            _http = http;
            _jwtService = jwtService;
        }

        public static string GenerateSHA256(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            using (var hashEngine = SHA256.Create())
            {
                var hashedBytes = hashEngine.ComputeHash(bytes, 0, bytes.Length);
                var sb = new StringBuilder();
                foreach (var b in hashedBytes)
                {
                    var hex = b.ToString("x2");
                    sb.Append(hex);
                }
                return sb.ToString();
            }
        }

        public bool CreateAccount(CreateAccountModel model)
        {
            try
            {
                Aspnetuser asp = new();
                User user = new();
                var existUser = _db.Aspnetusers.FirstOrDefault(r => r.Email == model.email);

                if (existUser == null)
                {
                    asp.Id = Guid.NewGuid().ToString();
                    asp.Email = model.email;
                    asp.Username = model.email.Split('@')[0];
                    asp.Passwordhash = GenerateSHA256(model.password);
                    asp.Createddate = DateTime.Now;
                    _db.Aspnetusers.Add(asp);

                    Aspnetuserrole aspnetuserrole = new Aspnetuserrole();
                    aspnetuserrole.Userid = asp.Id;
                    aspnetuserrole.Roleid = 2;
                    _db.Aspnetuserroles.Add(aspnetuserrole);

                    user.Aspnetuserid = asp.Id;
                    user.Email = model.email;
                    user.Firstname = asp.Username;
                    user.Lastname = asp.Username;
                    user.Createdby = asp.Id;
                    user.Createddate = DateTime.Now;
                    _db.Users.Add(user);
                    _db.SaveChanges();
                }
                else
                {
                    existUser.Passwordhash = GenerateSHA256(model.password);
                    _db.Aspnetusers.Update(existUser);
                    _db.SaveChanges();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }


        public void SendRegistrationEmailCreateRequest(string email, string registrationLink)
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
                Subject = "Create Account",
                IsBodyHtml = true,
                Body = $"Click the following link to Create Account: <a href='{registrationLink}'>{registrationLink}</a>"
            };



            mailMessage.To.Add(email);

            client.Send(mailMessage);
        }

        public Aspnetuser GetAspnetuser(string email)
        {
            var aspNetUser = _db.Aspnetusers.Include(x => x.Aspnetuserroles).FirstOrDefault(x => x.Email == email);
            return aspNetUser;
        }

        public LoginResponseViewModel PatientLogin(LoginVm model)
        {
            var user = _db.Aspnetusers.Include(u=>u.Aspnetuserroles).Where(u => u.Email == model.Email).FirstOrDefault();

            if (user == null)
                return new LoginResponseViewModel() { Status = ResponseStatus.Failed, Message = "User Not Found" };
            if (user.Passwordhash == null)
                return new LoginResponseViewModel() { Status = ResponseStatus.Failed, Message = "There is no Password with this Account" };
            if (user.Passwordhash == model.Password)
            {
                var jwtToken = _jwtService.GetJwtToken(user);

                return new LoginResponseViewModel() { Status = ResponseStatus.Success, Token = jwtToken };
            }

            return new LoginResponseViewModel() { Status = ResponseStatus.Failed, Message = "Password does not match" };
        }

        public bool AddPatientInfo(PatientInfoModel patientInfoModel)
        {

            var stateMain = _db.Regions.Where(r => r.Name.ToLower() == patientInfoModel.state.ToLower().Trim()).FirstOrDefault();
            if (stateMain == null)
            {
                return false;
            }

            var aspnetuser = _db.Aspnetusers.Where(m => m.Email == patientInfoModel.email).FirstOrDefault();
            User u = new User();
            if (aspnetuser == null)
            {
                Aspnetuser aspnetuser1 = new Aspnetuser();
                aspnetuser1.Id = Guid.NewGuid().ToString();
                aspnetuser1.Passwordhash = patientInfoModel.password;
                aspnetuser1.Email = patientInfoModel.email;
                string username = patientInfoModel.firstname + patientInfoModel.lastname;
                aspnetuser1.Username = username;
                aspnetuser1.Phonenumber = patientInfoModel.phonenumber;
                aspnetuser1.Createddate = DateTime.Now;
                aspnetuser1.Modifieddate = DateTime.Now;
                _db.Aspnetusers.Add(aspnetuser1);

                Aspnetuserrole role = new Aspnetuserrole();
                role.Userid = aspnetuser1.Id;
                role.Roleid = 2;
                _db.Aspnetuserroles.Add(role);

                u.Aspnetuserid = aspnetuser1.Id;
                u.Firstname = patientInfoModel.firstname;
                u.Lastname = patientInfoModel.lastname;
                u.Email = patientInfoModel.email;
                u.Mobile = patientInfoModel.phonenumber;
                u.Street = patientInfoModel.street;
                u.City = patientInfoModel.city;
                u.State = patientInfoModel.state;
                u.Zipcode = patientInfoModel.zipcode;
                u.Createdby = patientInfoModel.firstname + patientInfoModel.lastname;
                u.Intyear = patientInfoModel.Dateofbirth.Year;
                u.Intdate = patientInfoModel.Dateofbirth.Day;
                u.Strmonth = patientInfoModel.Dateofbirth.ToString("MMM");

                u.Createddate = DateTime.Now;
                u.Modifieddate = DateTime.Now;
                u.Status = (int)StatusEnum.Unassigned;
                u.Regionid = stateMain.Regionid;

                _db.Users.Add(u);
                _db.SaveChanges();
                Aspnetuserrole aspnetuserrole = new Aspnetuserrole();
                aspnetuserrole.Userid = aspnetuser1.Id;
                aspnetuserrole.Roleid = (int)AspNetRole.user;
                _db.Aspnetuserroles.Add(aspnetuserrole);
                _db.SaveChanges();

            }
            else
            {
                u = _db.Users.Where(m => m.Email == patientInfoModel.email).FirstOrDefault();
            }


            Request request = new Request();
            request.Requesttypeid = 2;
            request.Status = 1;
            request.Createddate = DateTime.Now;
            request.Isurgentemailsent = new BitArray(1);
            request.Firstname = patientInfoModel.firstname;
            request.Lastname = patientInfoModel.lastname;
            request.Phonenumber = patientInfoModel.phonenumber;
            request.Email = patientInfoModel.email;
            request.Userid = u.Userid;
            request.Calltype = 0;
            request.Physicianid = 30;

            _db.Requests.Add(request);
            _db.SaveChanges();

            Requestclient info = new Requestclient();
            info.Request = request;
            info.Requestid = request.Requestid;
            info.Notes = patientInfoModel.symptoms;
            info.Firstname = patientInfoModel.firstname;
            info.Lastname = patientInfoModel.lastname;
            info.Phonenumber = patientInfoModel.phonenumber;
            info.Email = patientInfoModel.email;
            info.Street = patientInfoModel.street;
            info.City = patientInfoModel.city;
            info.State = patientInfoModel.state;
            info.Zipcode = patientInfoModel.zipcode;
            info.Intyear = patientInfoModel.Dateofbirth.Year;
            info.Intdate = patientInfoModel.Dateofbirth.Day;
            info.Strmonth = patientInfoModel.Dateofbirth.ToString("MMM");
            info.Regionid = stateMain.Regionid;


            _db.Requestclients.Add(info)
;
            _db.SaveChanges();

            var regionData = _db.Regions.Where(x => x.Regionid == u.Regionid).FirstOrDefault();

            var count = (from req in _db.Requests where req.Userid == u.Userid && req.Createddate.Date == DateTime.Now.Date select req).Count();

            request.Confirmationnumber = $"{regionData.Abbreviation.Substring(0, 2)}{request.Createddate.Day.ToString().PadLeft(2, '0')}{request.Createddate.Month.ToString().PadLeft(2, '0')}{(request.Createddate.Year % 100).ToString().PadLeft(2, '0')}{u.Lastname.ToUpper().Substring(0, 2)}{u.Firstname.ToUpper().Substring(0, 2)}{count.ToString().PadLeft(4, '0')}";

            _db.Requests.Update(request);
            _db.SaveChanges();

            if (patientInfoModel.File != null)
            {
                foreach (IFormFile file in patientInfoModel.File)
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
                            Requestid = request.Requestid,
                            Createddate = DateTime.Now
                        };

                        _db.Requestwisefiles.Add(requestwisefile);
                        _db.SaveChanges();
                    };
                }
            }
            return true;
        }


        //public Task<bool> IsEmailExists(string email)
        //{
        //    bool isExist = _db.Aspnetusers.Any(x => x.Email == email);
        //    if (isExist)
        //    {
        //        return Task.FromResult(true);
        //    }
        //    return Task.FromResult(false);
        //}

        public bool AddFamilyReq(FamilyReqModel familyReqModel, string createaccountLink)
        {
            var statemain = _db.Regions.Where(x => x.Name.ToLower() == familyReqModel.state.ToLower().Trim()).FirstOrDefault();
            if (statemain == null)
            {
                return false;
            }
            User user = new User();
            Aspnetuser asp = new Aspnetuser();
            var existUser = _db.Aspnetusers.FirstOrDefault(r => r.Email == familyReqModel.patientEmail);

            if (existUser == null)
            {
                asp.Id = Guid.NewGuid().ToString();
                asp.Username = familyReqModel.patientFirstName + "_" + familyReqModel.patientLastName;
                asp.Email = familyReqModel.patientEmail;
                asp.Phonenumber = familyReqModel.patientPhoneNo;
                asp.Createddate = DateTime.Now;
                _db.Aspnetusers.Add(asp);
                _db.SaveChanges();

                user.Aspnetuserid = asp.Id;
                user.Firstname = familyReqModel.patientFirstName;
                user.Lastname = familyReqModel.patientFirstName;
                user.Email = familyReqModel.patientEmail;
                user.Mobile = familyReqModel.patientPhoneNo;
                user.City = familyReqModel.city;
                user.State = familyReqModel.state;
                user.Street = familyReqModel.street;
                user.Zipcode = familyReqModel.zipCode;
                user.Intyear = int.Parse(familyReqModel.patientDob.ToString("yyyy"));
                user.Intdate = int.Parse(familyReqModel.patientDob.ToString("dd"));
                user.Strmonth = familyReqModel.patientDob.ToString("MMM");
                user.Createdby = asp.Id;
                user.Createddate = DateTime.Now;
                user.Regionid = statemain.Regionid;
                _db.Users.Add(user);
                _db.SaveChanges();

                Aspnetuserrole aspnetuserrole = new Aspnetuserrole();
                aspnetuserrole.Userid = asp.Id;
                aspnetuserrole.Roleid = (int)AspNetRole.user;
                _db.Aspnetuserroles.Add(aspnetuserrole);
                _db.SaveChanges();

                try
                {
                    SendRegistrationEmailCreateRequest(familyReqModel.patientEmail, createaccountLink);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
            else
            {
                user = _db.Users.Where(m => m.Email == familyReqModel.patientEmail).FirstOrDefault();
            }
            Request request = new Request();
            request.Requesttypeid = (int)RequestTypeEnum.Family;
            request.Status = (int)StatusEnum.Unassigned;
            request.Createddate = DateTime.Now;
            request.Isurgentemailsent = new BitArray(1, false);
            request.Firstname = familyReqModel.firstName;
            request.Lastname = familyReqModel.lastName;
            request.Phonenumber = familyReqModel.phoneNo;
            request.Email = familyReqModel.email;
            request.Relationname = familyReqModel.relation;
            request.Userid = user.Userid;
            request.Calltype = 0;

            _db.Requests.Add(request);
            _db.SaveChanges();

            Requestclient info = new Requestclient();
            info.Requestid = request.Requestid;
            info.Notes = familyReqModel.symptoms;
            info.Firstname = familyReqModel.patientFirstName;
            info.Lastname = familyReqModel.patientLastName;
            info.Phonenumber = familyReqModel.patientPhoneNo;
            info.Email = familyReqModel.patientEmail;
            info.Street = familyReqModel.street;
            info.City = familyReqModel.city;
            info.State = familyReqModel.state;
            info.Zipcode = familyReqModel.zipCode;
            info.Regionid = statemain.Regionid;

            _db.Requestclients.Add(info);
            _db.SaveChanges();

            if (familyReqModel.File != null && familyReqModel.File.Length > 0)
            {
                //get file name
                var fileName = Path.GetFileName(familyReqModel.File.FileName);

                //define path
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UploadedFiles", fileName);

                // Copy the file to the desired location
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    familyReqModel.File.CopyTo(stream);
                }

                Requestwisefile requestwisefile = new()
                {
                    Filename = fileName,
                    Requestid = request.Requestid,
                    Createddate = DateTime.Now
                };

                _db.Requestwisefiles.Add(requestwisefile);
                _db.SaveChanges();
            };
            return true;
        }

        public bool AddConciergeReq(ConciergeReqModel conciergeReqModel, string createAccountLink)
        {
            var stateMain = _db.Regions.Where(x => x.Name.ToLower() == conciergeReqModel.state.ToLower().Trim()).FirstOrDefault();

            if (stateMain == null)
            {
                return false;
            }
            User user = new User();
            Aspnetuser asp = new Aspnetuser();
            var existUser = _db.Aspnetusers.FirstOrDefault(r => r.Email == conciergeReqModel.patientEmail);

            if (existUser == null)
            {
                asp.Id = Guid.NewGuid().ToString();
                asp.Username = conciergeReqModel.patientFirstName + "_" + conciergeReqModel.patientLastName;
                asp.Email = conciergeReqModel.patientEmail;
                asp.Phonenumber = conciergeReqModel.patientPhoneNo;
                asp.Createddate = DateTime.Now;
                _db.Aspnetusers.Add(asp);
                _db.SaveChanges();

                user.Aspnetuserid = asp.Id;
                user.Firstname = conciergeReqModel.patientFirstName;
                user.Lastname = conciergeReqModel.patientLastName;
                user.Email = conciergeReqModel.patientEmail;
                user.Mobile = conciergeReqModel.patientPhoneNo;
                //user.City = conciergeReqModel.city;
                //user.State = conciergeReqModel.state;
                //user.Street = conciergeReqModel.street;
                //user.Zipcode = conciergeReqModel.zipCode;
                user.Intyear = int.Parse(conciergeReqModel.patientDob.ToString("yyyy"));
                user.Intdate = int.Parse(conciergeReqModel.patientDob.ToString("dd"));
                user.Strmonth = conciergeReqModel.patientDob.ToString("MMM");
                user.Createdby = asp.Id;
                user.Createddate = DateTime.Now;
                user.Regionid = stateMain.Regionid;
                _db.Users.Add(user);
                _db.SaveChanges();

                Aspnetuserrole aspnetuserrole = new Aspnetuserrole();
                aspnetuserrole.Userid = asp.Id;
                aspnetuserrole.Roleid = (int)AspNetRole.user;
                _db.Aspnetuserroles.Add(aspnetuserrole);
                _db.SaveChanges();

                try
                {
                    SendRegistrationEmailCreateRequest(conciergeReqModel.patientEmail, createAccountLink);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                user = _db.Users.Where(m => m.Email == conciergeReqModel.patientEmail).FirstOrDefault();
            }

            Request request = new Request();
            request.Requesttypeid = (int)RequestTypeEnum.Concierge;
            request.Status = (int)StatusEnum.Unassigned;
            request.Createddate = DateTime.Now;
            request.Isurgentemailsent = new BitArray(1, false);
            request.Firstname = conciergeReqModel.firstName;
            request.Lastname = conciergeReqModel.lastName;
            request.Phonenumber = conciergeReqModel.phoneNo;
            request.Email = conciergeReqModel.email;
            request.Relationname = "Concierge";
            request.Userid = user.Userid;
            request.Calltype = 0;

            _db.Requests.Add(request);
            _db.SaveChanges();

            Requestclient info = new Requestclient();
            info.Requestid = request.Requestid;
            info.Notes = conciergeReqModel.symptoms;
            info.Firstname = conciergeReqModel.patientFirstName;
            info.Lastname = conciergeReqModel.patientLastName;
            info.Phonenumber = conciergeReqModel.patientPhoneNo;
            info.Email = conciergeReqModel.patientEmail;
            info.Regionid = stateMain.Regionid;


            _db.Requestclients.Add(info);
            _db.SaveChanges();

            Concierge concierge = new Concierge();
            concierge.Conciergename = conciergeReqModel.firstName + " " + conciergeReqModel.lastName;
            concierge.Createddate = DateTime.Now;
            concierge.Regionid = stateMain.Regionid;
            concierge.Street = conciergeReqModel.street;
            concierge.City = conciergeReqModel.city;
            concierge.State = conciergeReqModel.state;
            concierge.Zipcode = conciergeReqModel.zipCode;

            _db.Concierges.Add(concierge);
            _db.SaveChanges();

            Requestconcierge reqCon = new Requestconcierge();
            reqCon.Requestid = request.Requestid;
            reqCon.Conciergeid = concierge.Conciergeid;

            _db.Requestconcierges.Add(reqCon);
            _db.SaveChanges();

            return true;
        }

        public bool AddBusinessReq(BusinessReqModel businessReqModel, string createAccountLink)
        {
            var stateMain = _db.Regions.Where(x => x.Name.ToLower() == businessReqModel.state.ToLower().Trim()).FirstOrDefault();

            if (stateMain == null)
            {
                return false;
            }
            User user = new User();
            Aspnetuser asp = new Aspnetuser();
            var existUser = _db.Aspnetusers.FirstOrDefault(r => r.Email == businessReqModel.patientEmail);

            if (existUser == null)
            {
                asp.Id = Guid.NewGuid().ToString();
                asp.Username = businessReqModel.patientFirstName + "_" + businessReqModel.patientLastName;
                asp.Email = businessReqModel.patientEmail;
                asp.Phonenumber = businessReqModel.patientPhoneNo;
                asp.Createddate = DateTime.Now;
                _db.Aspnetusers.Add(asp);
                _db.SaveChanges();

                user.Aspnetuserid = asp.Id;
                user.Firstname = businessReqModel.patientFirstName;
                user.Lastname = businessReqModel.patientLastName;
                user.Email = businessReqModel.patientEmail;
                user.Mobile = businessReqModel.patientPhoneNo;
                user.City = businessReqModel.city;
                user.State = businessReqModel.state;
                user.Street = businessReqModel.street;
                user.Zipcode = businessReqModel.zipCode;
                user.Intyear = int.Parse(businessReqModel.patientDob.ToString("yyyy"));
                user.Intdate = int.Parse(businessReqModel.patientDob.ToString("dd"));
                user.Strmonth = businessReqModel.patientDob.ToString("MMM");
                user.Createdby = asp.Id;
                user.Createddate = DateTime.Now;
                user.Regionid = stateMain.Regionid;
                _db.Users.Add(user);
                _db.SaveChanges();

                Aspnetuserrole aspnetuserrole = new Aspnetuserrole();
                aspnetuserrole.Userid = asp.Id;
                aspnetuserrole.Roleid = (int)AspNetRole.user;
                _db.Aspnetuserroles.Add(aspnetuserrole);
                _db.SaveChanges();

                try
                {
                    SendRegistrationEmailCreateRequest(businessReqModel.patientEmail, createAccountLink);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                user = _db.Users.Where(m => m.Email == businessReqModel.patientEmail).FirstOrDefault();
            }
            Request request = new Request();
            request.Requesttypeid = (int)RequestTypeEnum.Business;
            request.Status = (int)StatusEnum.Unassigned;
            request.Createddate = DateTime.Now;
            request.Isurgentemailsent = new BitArray(1, false);
            request.Firstname = businessReqModel.firstName;
            request.Lastname = businessReqModel.lastName;
            request.Phonenumber = businessReqModel.phoneNo;
            request.Email = businessReqModel.email;
            request.Relationname = "Business";
            request.Userid = user.Userid;
            request.Calltype = 0;

            _db.Requests.Add(request);
            _db.SaveChanges();

            Requestclient info = new Requestclient();
            info.Requestid = request.Requestid;
            info.Notes = businessReqModel.symptoms;
            info.Firstname = businessReqModel.patientFirstName;
            info.Lastname = businessReqModel.patientLastName;
            info.Phonenumber = businessReqModel.patientPhoneNo;
            info.Email = businessReqModel.patientEmail;
            info.Regionid = stateMain.Regionid;
            info.Street = businessReqModel.street;
            info.City = businessReqModel.city;
            info.State = businessReqModel.state;
            info.Zipcode = businessReqModel.zipCode;
            info.Regionid = stateMain.Regionid;
            _db.Requestclients.Add(info);
            _db.SaveChanges();

            Business business = new Business();
            business.Createddate = DateTime.Now;
            business.Name = businessReqModel.businessName;
            business.Phonenumber = businessReqModel.phoneNo;
            business.City = businessReqModel.city;
            business.Zipcode = businessReqModel.zipCode;

            _db.Businesses.Add(business);
            _db.SaveChanges();

            Requestbusiness requestbusiness = new Requestbusiness();
            requestbusiness.Businessid = business.Businessid;
            requestbusiness.Requestid = request.Requestid;

            _db.Requestbusinesses.Add(requestbusiness);
            _db.SaveChanges();
            return true;
        }


        public Task<bool> IsEmailExists(string email)
        {
            bool isExist = _db.Aspnetusers.Any(x => x.Email == email);
            if (isExist)
            {
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }


        //public MedicalHistoryList GetMedicalHistory(int userid)
        //{
        //    var user = _db.Users.FirstOrDefault(x => x.Userid == userid);

        //    var medicalhistory = (from request in _db.Requests
        //                          join requestfile in _db.Requestwisefiles
        //                          on request.Requestid equals requestfile.Requestid
        //                          where request.Email == user.Email && request.Email != null
        //                          group requestfile by request.Requestid into groupedFiles
        //                          select new MedicalHistory
        //                          {

        //                              reqId = groupedFiles.Select(x => x.Request.Requestid).FirstOrDefault(),
        //                              createdDate = groupedFiles.Select(x => x.Request.Createddate).FirstOrDefault(),
        //                              currentStatus = groupedFiles.Select(x => x.Request.Status).FirstOrDefault(),
        //                              document = groupedFiles.Select(x => x.Filename.ToString()).ToList(),
        //                              ConfirmationNumber = groupedFiles.Select(x => x.Request.Confirmationnumber).FirstOrDefault(),
        //                          }).ToList();

        //    MedicalHistoryList medicalHistoryList = new()
        //    {
        //        medicalHistoriesList = medicalhistory,
        //        id = userid,
        //        firstName = user.Firstname,
        //        lastName = user.Lastname
        //    };

        //    return medicalHistoryList;
        //}

        public MedicalHistoryList GetMedicalHistory(string email)
        {
            var user = _db.Users.FirstOrDefault(x => x.Email == email);
            List<MedicalHistory> list = new();
            MedicalHistoryList model = new();
            model.id = user.Userid;
            model.firstName = user.Firstname;
            model.lastName = user.Lastname;
            var requests = _db.Requests.Where(m => m.Userid == user.Userid).ToList();
            foreach (var item in requests)
            {
                int count = _db.Requestwisefiles.Where(m => m.Requestid == item.Requestid).Count();
                list.Add(new MedicalHistory { createdDate = item.Createddate, currentStatus = item.Status, docCount = count, reqId = item.Requestid });
            }
            model.medicalHistoriesList = list;
            return model;
        }

        public DocumentModel GetAllDocById(int requestId)
        {
            var list = _db.Requestwisefiles.Where(x => x.Requestid == requestId).ToList();
            var reqClient = _db.Requestclients.Where(x => x.Requestid == requestId).FirstOrDefault();
            var req = _db.Requests.Where(x => x.Requestid == requestId).FirstOrDefault();

            DocumentModel result = new()
            {
                files = list,
                firstName = reqClient.Firstname,
                lastName = reqClient.Lastname,
                confirmationnumber = req.Confirmationnumber,
            };

            return result;
        }

        //public void AddFile(IFormFile file)
        //{
        //    var fileName = Path.GetFileName(file.FileName);

        //    //define path
        //    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UploadedFiles", fileName);

        //    // Copy the file to the desired location
        //    using (var stream = new FileStream(filePath, FileMode.Create))
        //    {
        //        file.CopyTo(stream);
        //    }
        //    var u = _http.HttpContext.Session.GetInt32("rid");
        //    Requestwisefile requestwisefile = new()
        //    {
        //        Filename = fileName,
        //        Requestid = (int)u,
        //        Createddate = DateTime.Now
        //    };
        //    _db.Requestwisefiles.Add(requestwisefile);
        //    _db.SaveChanges();
        //}

        public bool UploadDocuments(List<IFormFile> files, int reqId)
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

        public List<PatientInfoModel> subinformation(PatientInfoModel patientInfoModel)
        {
            var y = (from Request in _db.Requests
                     where patientInfoModel != null
                     select new PatientInfoModel
                     {
                         firstname = patientInfoModel.firstname,
                         lastname = patientInfoModel.lastname,
                         email = patientInfoModel.email,
                         phonenumber = patientInfoModel.phonenumber,
                     }).ToList();
        return y;
        }

        public void Editing(string email, User model)
        {
            var userdata = _db.Users.Where(x => x.Email == email).FirstOrDefault();

            if (userdata.Email == model.Email)
            {
                userdata.Firstname = model.Firstname;
                userdata.Lastname = model.Lastname;
                userdata.Mobile = model.Mobile;
                userdata.Email = model.Email;
                userdata.Street = model.Street;
                userdata.City = model.City;
                userdata.State = model.State;
                userdata.Zipcode = model.Zipcode;
                userdata.Modifieddate = DateTime.Now;

                _db.Users.Update(userdata);
                _db.SaveChanges();
            }

        }

        public Profile GetProfile(int userid)
        {
            var user = _db.Users.FirstOrDefault(x => x.Userid == userid);
            if (user.Intdate == null && user.Intyear == null && user.Strmonth == "")
            {
                Profile obj = new()
                {

                    FirstName = user.Firstname,
                    LastName = user.Lastname,
                    Email = user.Email,
                    PhoneNo = user.Mobile,
                    State = user.State,
                    City = user.City,
                    Street = user.Street,
                    ZipCode = user.Zipcode,
                    //DateOfBirth = new DateTime(Convert.ToInt32(user.Intyear), DateTime.ParseExact(user.Strmonth, "MMM", CultureInfo.InvariantCulture).Month, Convert.ToInt32(user.Intdate)),
                    //isMobileCheck = user.Ismobile[0] ? 1 : 0,

                };
                return obj;
            }
            else if(user.Intdate != null && user.Intyear != null && user.Strmonth != "")
            {
                Profile profile = new()
                {

                    FirstName = user.Firstname,
                    LastName = user.Lastname,
                    Email = user.Email,
                    PhoneNo = user.Mobile,
                    State = user.State,
                    City = user.City,
                    Street = user.Street,
                    ZipCode = user.Zipcode,
                    DateOfBirth = new DateTime(Convert.ToInt32(user.Intyear), DateTime.ParseExact(user.Strmonth, "MMM", CultureInfo.InvariantCulture).Month, Convert.ToInt32(user.Intdate)),
                    //isMobileCheck = user.Ismobile[0] ? 1 : 0,

                };
                return profile;
            }
            else
            {
                Profile profile = new()
                {

                    FirstName = user.Firstname,
                    LastName = user.Lastname,
                    Email = user.Email,
                    //PhoneNo = user.Mobile,
                    //State = user.State,
                    //City = user.City,
                    //Street = user.Street,
                    //ZipCode = user.Zipcode,
                    //DateOfBirth = new DateTime(Convert.ToInt32(user.Intyear), DateTime.ParseExact(user.Strmonth, "MMM", CultureInfo.InvariantCulture).Month, Convert.ToInt32(user.Intdate)),
                    //isMobileCheck = user.Ismobile[0] ? 1 : 0,

                };
                return profile;
            }
           
            
        }

        public bool EditProfile(Profile profile)
        {

            try
            {
                var existingUser = _db.Users.Where(x => x.Userid == profile.userId).FirstOrDefault();
                if (existingUser != null)
                {
                    existingUser.Firstname = profile.FirstName;
                    existingUser.Lastname = profile.LastName;
                    existingUser.Mobile = profile.PhoneNo;
                    existingUser.Street = profile.Street;
                    existingUser.City = profile.City;
                    existingUser.State = profile.State;
                    existingUser.Zipcode = profile.ZipCode;
                    existingUser.Intdate = profile.DateOfBirth.Day;
                    existingUser.Strmonth = profile.DateOfBirth.ToString("MMM");
                    existingUser.Intyear = profile.DateOfBirth.Year;

                    //existingUser.Ismobile[1] = profile.isMobileCheck == 1 ? true : false;
                    _db.Users.Update(existingUser);
                    _db.SaveChanges();

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex) { return false; }
        }

        //me & someoneelse page
        public PatientInfoModel FetchData(string email)
        {
            User? user = _db.Users.FirstOrDefault(i => i.Email == email);
            var BirthDay = Convert.ToInt32(user.Intdate);
            var BirthMonth = user.Strmonth;
            var BirthYear = Convert.ToInt32(user.Intyear);
            var userdata = new PatientInfoModel()
            {
                firstname = user.Firstname,
                lastname = user.Lastname,
                email = user.Email,
                phonenumber = user.Mobile,
            };
            return userdata;
        }
        public bool StoreData(PatientInfoModel patientRequestModel)
        {
            try
            {
            
            var stateMain = _db.Regions.Where(r => r.Name.ToLower() == patientRequestModel.state.ToLower().Trim()).FirstOrDefault();
            if (stateMain == null) { return false; }

            Request request = new Request();
            request.Requesttypeid = 2;
            request.Status = 1;
            request.Createddate = DateTime.Now;
            request.Isurgentemailsent = new BitArray(1);
            request.Firstname = patientRequestModel.firstname;
            request.Lastname = patientRequestModel.lastname;
            request.Phonenumber = patientRequestModel.phonenumber;
            request.Email = patientRequestModel.email;
            request.Userid = request.Userid;
                request.Calltype = 0;

            _db.Requests.Add(request);
            _db.SaveChanges();

            Requestclient info = new Requestclient();
            info.Request = request;
            info.Requestid = request.Requestid;
            info.Notes = patientRequestModel.symptoms;
            info.Firstname = patientRequestModel.firstname;
            info.Lastname = patientRequestModel.lastname;
            info.Phonenumber = patientRequestModel.phonenumber;
            info.Email = patientRequestModel.email;
            info.Street = patientRequestModel.street;
            info.City = patientRequestModel.city;
            info.State = patientRequestModel.state;
            info.Zipcode = patientRequestModel.zipcode;
            info.Intyear = patientRequestModel.Dateofbirth.Year;
            info.Intdate = patientRequestModel.Dateofbirth.Day;
            info.Strmonth = patientRequestModel.Dateofbirth.ToString("MMM");
            info.Regionid = stateMain.Regionid;


            _db.Requestclients.Add(info)
;
            _db.SaveChanges();


            if (patientRequestModel.File != null)
            {
                foreach (IFormFile file in patientRequestModel.File)
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
                            Requestid = request.Requestid,
                            Createddate = DateTime.Now
                        };

                        _db.Requestwisefiles.Add(requestwisefile);
                        _db.SaveChanges();
                    };
                }
            }


            var reqclientdata = new Requestclient()                 //request client
            {
                Requestid = request.Requestid,
                Firstname = patientRequestModel.firstname,
                Lastname = patientRequestModel.lastname,
                Email = patientRequestModel.email,
                Notiemail = patientRequestModel.email,
                Phonenumber = patientRequestModel.phonenumber,
                Address = patientRequestModel.street + " " + patientRequestModel.city + " " + patientRequestModel.state + " " + patientRequestModel.zipcode,
                Notimobile = patientRequestModel.phonenumber,
                State = patientRequestModel.state,
                Street = patientRequestModel.street,
                City = patientRequestModel.city,
            };
            _db.Requestclients.Add(reqclientdata);
            _db.SaveChanges();

                return true;

            }
            catch (Exception ex) { return false; }
        }

        public bool SomeElseReq(FamilyReqModel model, string createAccountLink, string loginid)
        {
            try
            {

            }
            catch { }
            var stateMain = _db.Regions.Where(x => x.Name.ToLower() == model.state.ToLower().Trim()).FirstOrDefault();

            if (stateMain == null)
            {
                return false;
            }
            User user = new User();
            Aspnetuser asp = new Aspnetuser();
            var existUser = _db.Aspnetusers.FirstOrDefault(r => r.Email == model.patientEmail);

            if (existUser == null)
            {
                asp.Id = Guid.NewGuid().ToString();
                asp.Username = model.patientFirstName + "_" + model.patientLastName;
                asp.Email = model.patientEmail;
                asp.Phonenumber = model.patientPhoneNo;
                asp.Createddate = DateTime.Now;
                _db.Aspnetusers.Add(asp);
                _db.SaveChanges();

                user.Aspnetuserid = asp.Id;
                user.Firstname = model.patientFirstName;
                user.Lastname = model.patientLastName;
                user.Email = model.patientEmail;
                user.Mobile = model.patientPhoneNo;
                user.City = model.city;
                user.State = model.state;
                user.Street = model.street;
                user.Zipcode = model.zipCode;
                user.Intyear = int.Parse(model.patientDob.ToString("yyyy"));
                user.Intdate = int.Parse(model.patientDob.ToString("dd"));
                user.Strmonth = model.patientDob.ToString("MMM");
                user.Createdby = loginid;
                user.Createddate = DateTime.Now;
                user.Regionid = stateMain.Regionid;
                _db.Users.Add(user);
                _db.SaveChanges();


                Aspnetuserrole aspnetuserrole = new Aspnetuserrole();
                aspnetuserrole.Userid = asp.Id;
                aspnetuserrole.Roleid = (int)AspNetRole.user;
                _db.Aspnetuserroles.Add(aspnetuserrole);
                _db.SaveChanges();
                try
                {
                    createAccountLink = createAccountLink + "/" + asp.Id;
                    SendRegistrationEmailCreateRequest(model.patientEmail, createAccountLink);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                user = _db.Users.Where(m => m.Email == model.patientEmail).FirstOrDefault();
            }

            var loginPatient = _db.Users.Where(x => x.Aspnetuserid == loginid).FirstOrDefault();

            Request request = new Request();
            request.Requesttypeid = (int)RequestTypeEnum.Family;
            request.Status = (int)StatusEnum.Unassigned;
            request.Createddate = DateTime.Now;
            request.Isurgentemailsent = new BitArray(1, false);
            request.Firstname = loginPatient.Firstname;
            request.Lastname = loginPatient.Lastname;
            request.Phonenumber = loginPatient.Mobile;
            request.Email = loginPatient.Email;
            request.Relationname = "Friend";
            request.Userid = user.Userid;
            request.Calltype = 0;

            _db.Requests.Add(request);
            _db.SaveChanges();

            Requestclient info = new Requestclient();
            info.Requestid = request.Requestid;
            info.Notes = model.symptoms;
            info.Firstname = model.patientFirstName;
            info.Lastname = model.patientLastName;
            info.Phonenumber = model.patientPhoneNo;
            info.Email = model.patientEmail;
            info.Street = model.street;
            info.City = model.city;
            info.State = model.state;
            info.Zipcode = model.zipCode;
            info.Regionid = stateMain.Regionid;
            info.Intyear = int.Parse(model.patientDob.ToString("yyyy"));
            info.Intdate = int.Parse(model.patientDob.ToString("dd"));
            info.Strmonth = model.patientDob.ToString("MMM");

            _db.Requestclients.Add(info);
            _db.SaveChanges();


            if (model.File != null)
            {
                if (model.File != null && model.File.Length > 0)
                {
                    //get file name
                    var fileName = Path.GetFileName(model.File.FileName);

                    //define path
                    var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UploadedFiles", fileName);

                    // Copy the file to the desired location
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        model.File.CopyTo(stream);
                    }

                    Requestwisefile requestwisefile = new()
                    {
                        Filename = fileName,
                        Requestid = request.Requestid,
                        Createddate = DateTime.Now
                    };

                    _db.Requestwisefiles.Add(requestwisefile);
                    _db.SaveChanges();
                };
            }
            return true;
        }
    }
}
