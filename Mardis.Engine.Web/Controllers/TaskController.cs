using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mardis.Engine.Business.MardisCore;
using Mardis.Engine.Business.MardisSecurity;
using Mardis.Engine.DataAccess;
using Mardis.Engine.DataAccess.MardisCore;
using Mardis.Engine.Framework;
using Mardis.Engine.Framework.Resources.PagesConstants;
using Mardis.Engine.Web.Libraries.Security;
using Mardis.Engine.Web.Libraries.Services;
using Mardis.Engine.Web.Libraries.Util;
using Mardis.Engine.Web.Model;
using Mardis.Engine.Web.ViewModel.TaskViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using Mardis.Engine.Web.App_code;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System.Timers;
using OfficeOpenXml;
using System.Drawing;
using System.Net.Http.Headers;

namespace Mardis.Engine.Web.Controllers
{
    [Authorize]
    public class TaskController : AController<TaskController>
    {
        #region Variables y Constructores
        private readonly CampaignBusiness _campaignBusiness;
        private readonly TaskCampaignBusiness _taskCampaignBusiness;
        private readonly StatusTaskBusiness _statusTaskBusiness;
        private readonly TaskNotImplementedReasonBusiness _taskNotImplementedReasonBusiness;
        private readonly BranchImageBusiness _branchImageBusiness;
        private readonly ILogger<TaskController> _logger;
        private readonly Guid _idAccount;
        private readonly IDataProtector _protector;
        private readonly UserBusiness _userBusiness;
        private readonly Guid _userId;
        private readonly QuestionBusiness _questionBusiness;
        private readonly QuestionDetailBusiness _questionDetailBusiness;
        private readonly IMemoryCache _cache;
        private readonly ServiceBusiness _serviceBusiness;
        private IHostingEnvironment _Env;
        public Guid _typeuser;
        public static IDataProtector Protector;
        public readonly ProfileBusiness _profileBusiness;
        public Guid _Profile;
        public TaskController(UserManager<ApplicationUser> userManager,
                                IHttpContextAccessor httpContextAccessor,
                                MardisContext mardisContext,
                                ILogger<TaskController> logger,
                                ILogger<ServicesFilterController> loggeFilter,
                                    IDataProtectionProvider protectorProvider,
                                    IMemoryCache memoryCache,
                                    RedisCache distributedCache, IHostingEnvironment envrnmt)
            : base(userManager, httpContextAccessor, mardisContext, logger)
        {
            _protector = protectorProvider.CreateProtector(GetType().FullName);
            _logger = logger;
            ControllerName = CTask.Controller;
            TableName = CTask.TableName;
            _taskCampaignBusiness = new TaskCampaignBusiness(mardisContext, distributedCache);
            _statusTaskBusiness = new StatusTaskBusiness(mardisContext, distributedCache);
            _taskNotImplementedReasonBusiness = new TaskNotImplementedReasonBusiness(mardisContext);
            _branchImageBusiness = new BranchImageBusiness(mardisContext);
            _userBusiness = new UserBusiness(mardisContext);
            _questionBusiness = new QuestionBusiness(mardisContext);
            _questionDetailBusiness = new QuestionDetailBusiness(mardisContext);
            _cache = memoryCache;
            _campaignBusiness = new CampaignBusiness(mardisContext);
            _serviceBusiness = new ServiceBusiness(mardisContext, distributedCache);
            _profileBusiness = new ProfileBusiness(mardisContext);
            _Env = envrnmt;
            if (ApplicationUserCurrent.UserId != null)
            {
                _userId = new Guid(ApplicationUserCurrent.UserId);
                Global.UserID = _userId;
                Global.AccountId = ApplicationUserCurrent.AccountId;
                Global.ProfileId = ApplicationUserCurrent.ProfileId;
                Global.PersonId = ApplicationUserCurrent.PersonId;

                _typeuser = _profileBusiness.GetById(Global.ProfileId).IdTypeUser;
            }

            _idAccount = ApplicationUserCurrent.AccountId;
        }

        #endregion

        #region Acciones

        [HttpGet]
        public IActionResult Register(Guid idTask, Guid? idCampaign = null, string returnUrl = null)
        {

            try
            {
                ViewData["ReturnUrl"] = returnUrl;
                var model = _taskCampaignBusiness.GetTask(idCampaign, idTask, ApplicationUserCurrent.AccountId);
                model.ReturnUrl = returnUrl;

                GetMerchants();

                return View(model);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }


        [HttpGet]
        public IActionResult GroupTask(string filterValues, bool deleteFilter, int pageSize = 12, int pageIndex = 1)
        {
            try
            {
                var filters = GetFilters(filterValues, deleteFilter);
                var campaigns = _campaignBusiness.GetPaginatedCampaignsDinamic(filters, pageSize, pageIndex, ApplicationUserCurrent.AccountId, _protector, _userId, _typeuser);
                return View(campaigns);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }
        /// <summary>
        /// funcion para obtener la pregunta enlazada
        /// </summary>
        [HttpGet]
        public QuestionDetail Obtnerpregunta(Guid id)
        {
            return _questionDetailBusiness.GetOne(id);
        }

        [HttpPost]
        public IActionResult Register(TaskRegisterViewModel model, string returnUrl = null)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    GetMerchants();
                    return View(model);
                }

                _taskCampaignBusiness.Save(model, ApplicationUserCurrent.AccountId);

                if (!string.IsNullOrEmpty(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        [HttpGet]
        public IActionResult TasksCampaign(string idCampaign, string filterValues, bool deleteFilter, string view, int pageIndex = 1, int pageSize = 6)
        {
            try
            {
                if (!string.IsNullOrEmpty(idCampaign))
                {    // SetSessionVariable("idCampaign", idCampaign);
                    _taskCampaignBusiness.UserCampaignRedis(Guid.Parse(ApplicationUserCurrent.UserId), idCampaign);
                }
                else
                {
                    //idCampaign = GetSessionVariable("idCampaign");
                    idCampaign = _taskCampaignBusiness.GetUserCampaignRedis(Guid.Parse(ApplicationUserCurrent.UserId), idCampaign);
                }

                if (!string.IsNullOrEmpty(view))
                {
                    SetSessionVariable("view", view);
                }
                else
                {
                    view = GetSessionVariable("view");
                }

                var id = Guid.Empty;
                if (!string.IsNullOrEmpty(idCampaign))
                {
                    try
                    {
                        id = Guid.Parse(_protector.Unprotect(idCampaign));
                    }
                    catch (Exception)
                    {

                        return RedirectToAction("GroupTask", "Task");
                    }
                   
                }
                var filters = GetFilters(filterValues, deleteFilter);
                if (filters.Where(x => x.NameFilter == "IdCampaign").Count() > 0) {
                    var varcampid = filters.Where(x => x.NameFilter == "IdCampaign").First().Value;
                    id = Guid.Parse(varcampid);
                    idCampaign = _protector.Protect(varcampid);
                    SetSessionVariable("idCampaign", idCampaign);
                }
               
                var tasks = _campaignBusiness.GetPaginatedTaskPerCampaignViewModelDinamic(id, pageIndex, pageSize, filters, ApplicationUserCurrent.AccountId);
                ViewBag.CountTasks = _taskCampaignBusiness._CountAllTasCamping(id, filters).ToString();
                ViewBag.idcampaign = idCampaign;
                var _MyTask = _taskCampaignBusiness._ModelTasks(tasks);
                _taskCampaignBusiness.taskunBlockAll(Guid.Parse(ApplicationUserCurrent.UserId));
                if (view == "list")
                {
                    return View("~/Views/Task/TaskList.cshtml", tasks);
                }

                return View(_MyTask);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }


        private void GetMerchants()
        {
            ViewBag.Merchants =
                _userBusiness.GetUserListByType(CTypePerson.PersonMerchant, ApplicationUserCurrent.AccountId)
                    .Select(c => new SelectListItem() { Text = c.Person.Name + " " + c.Person.SurName, Value = c.Id.ToString() })
                    .ToList();

            ViewBag.StatusList = _statusTaskBusiness.GetAllStatusTasks(ApplicationUserCurrent.AccountId, Guid.Parse(ApplicationUserCurrent.UserId))
          .Select(s => new SelectListItem() { Text = s.Name, Value = s.Id.ToString() })
              .ToList();
        }

        [HttpGet]
        public IActionResult Index(string pidCampaign, string filterValues, bool deleteFilter, int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                if (!string.IsNullOrEmpty(pidCampaign))
                {
                    SetSessionVariable("pidCampaign", pidCampaign);
                }
                else
                {
                    pidCampaign = GetSessionVariable("pidCampaign");
                }

                var filters = GetFilters(filterValues, deleteFilter);
                var idCampaign = Guid.Parse(CampaignController.Protector.Unprotect(pidCampaign));
                var idAccount = ApplicationUserCurrent.AccountId;
                var model = _taskCampaignBusiness.GetPaginatedTasksList(idCampaign, idAccount, filters, pageIndex, pageSize);

                return View(model);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        public IActionResult MyTasks(string filterValues, bool deleteFilter, string view, int pageIndex = 1, int pageSize = 15)
        {
            try
            {
                var filters = GetFilters(filterValues, deleteFilter);

                if (!string.IsNullOrEmpty(view))
                {
                    SetSessionVariable("view", view);
                }
                else
                {
                    view = GetSessionVariable("view");
                }

                if (ApplicationUserCurrent.UserId == null)
                {
                    ApplicationUserCurrent.UserId = Convert.ToString(Global.UserID);
                    ApplicationUserCurrent.AccountId = Global.AccountId;
                    ApplicationUserCurrent.ProfileId = Global.ProfileId;
                    ApplicationUserCurrent.PersonId = Global.PersonId;
                }
                var model = _taskCampaignBusiness.GetTasksPerCampaign(Guid.Parse(ApplicationUserCurrent.UserId), pageIndex, pageSize, filters, ApplicationUserCurrent.AccountId);

                if (view == "list")
                {
                    return View("~/Views/Task/TaskList.cshtml", model);
                }

                return View(model);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = e.Message });
            }
        }

        [HttpGet]
        public IActionResult Profile(Guid idTask, string campaign = null)
        {
            try
            {
                ViewData[CTask.IdRegister] = idTask.ToString();
                ViewData[CTask.IsUse] = _taskCampaignBusiness._IstaskBlock(Guid.Parse(ApplicationUserCurrent.UserId), idTask, ApplicationUserCurrent.UserName);
                ViewData["taskcampaign"] = campaign;
                LoadSelectItems();

                return View();
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }

        }

        [HttpGet]
        public JsonResult Get(Guid idTask)
        {
            try
            {
                var model = _taskCampaignBusiness.GetSectionsPoll(idTask, _idAccount,Global.ProfileId);
                    JSonConvertUtil.Convert(model);
                return Json(model);

            }

          catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }

        [HttpPost]
        public JsonResult Save(string task)
        {
            try
            {
                var model = JSonConvertUtil.Deserialize<MyTaskViewModel>(task);

                if (model == null)
                {
                    return null;
                }
                if (ApplicationUserCurrent.UserId == null)
                {
                    ApplicationUserCurrent.UserId = Convert.ToString(Global.UserID);
                    ApplicationUserCurrent.AccountId = Global.AccountId;
                    ApplicationUserCurrent.ProfileId = Global.ProfileId;
                    ApplicationUserCurrent.PersonId = Global.PersonId;
                }
                _taskCampaignBusiness.SaveAnsweredPoll(model, ApplicationUserCurrent.AccountId, ApplicationUserCurrent.ProfileId, Guid.Parse(ApplicationUserCurrent.UserId), model.IdStatusTask,"");

                return Json("OK");
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }
        [HttpPost]
        public JsonResult SaveAnswerQuestion(String AnswerQuestion, String fintransaccion, String Idtask, String idstatus, String CodigoGemini, String task, String comment)
        {
            try
            {
                var model = JSonConvertUtil.Deserialize<List<MyTaskViewAnswer>>(AnswerQuestion);
          
                //var varmodel=model_2.ServiceCollection.First().ServiceDetailCollection.Where(x=>x.QuestionCollection.)
                _SaveAnswerQuestionComplete(task, Idtask);

                if (model == null) return null;
                _taskCampaignBusiness.CrearAnswerQuestion(model, ApplicationUserCurrent.AccountId, Guid.Parse(ApplicationUserCurrent.UserId), ApplicationUserCurrent.ProfileId, fintransaccion, Idtask , Guid.Parse(idstatus), CodigoGemini, comment);
               
                return Json(model);
            }
            catch (Exception ex)
            {
                    _logger.LogError(new EventId(0, "Error Index"), ex.Message);
                return null;
            }
        }
        [HttpPost]
        public JsonResult SaveQuestionDinamic(String Idtask, String tasks, String dinamic)
        {
            try
            {
                var modelAnswerSec = JSonConvertUtil.Deserialize<List<MytaskAnwerDetailSecondModel>>(dinamic);
                if (modelAnswerSec == null) return Json("2"); 
               var _post= _taskCampaignBusiness.saveDinamic(modelAnswerSec);

                return Json(_post);
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId(0, "Error Index"), ex.Message);
                return Json("-1");
            }
        }

        [HttpPost]
        public JsonResult SaveCambioHarinas(String Idtask, String tasks, String dinamic, List<String> idMarca)
        {
            List<String> result = new List<String>();
            try
            {
                if (idMarca.Count() > 0)
                {
                    var modelAnswer = JSonConvertUtil.Deserialize<List<MyTaskQuestionDetailsViewModel>>(dinamic);
                    if (modelAnswer == null) return Json("2");
                    //modelAnswer.First().AnwerDetailSecondModel.First().QuestionComplete.Add(idMarca.First());
                    modelAnswer.First().SelectedAnswer = idMarca.First();
                    var _post = _taskCampaignBusiness.saveDinamicCambioHarinas(modelAnswer);

                    return Json(_post);
                }
                else
                {
                    _logger.LogError(new EventId(0, "Error Index"), "EL Campo de la Harina Actual es Obligatorio");
                    result.Add("-1");
                    result.Add((new List<MytaskAnwerDetailSecondModel>()).ToString());
                    return Json(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId(0, "Error Index"), ex.Message);
                result.Add("-1");
                result.Add((new List<MytaskAnwerDetailSecondModel>()).ToString());
                return Json(result);
            }
        }

        public async void _SaveAnswerQuestionComplete(String _model,string Idtask) {
            int resp = 0;
            var _task = JSonConvertUtil.Deserialize<MyTaskViewModel>(_model);
            var _questionModel = new List<MyTaskQuestionsViewModel>();
            var _questionModelSub = new List<MyTaskQuestionsViewModel>();

            foreach (var _question in _task.ServiceCollection.First().ServiceDetailCollection.Where(z=>z.hasConcept==true))
            {


                _questionModel.AddRange(_question.QuestionCollection.Where(x => x.CodeTypePoll == "COMPLETE"));


            }
            foreach (var _question in _task.ServiceCollection.First().ServiceDetailCollection)
            {
                foreach (var _seccion in _question.Sections.Where(z => z.hasConcept == true))
                {

                    _questionModelSub.AddRange(_seccion.QuestionCollection.Where(x => x.CodeTypePoll == "COMPLETE"));
                }

            }

            var _union = ((from a in _questionModel select a)
                             .Union
                          (from a in _questionModelSub select a)).ToList();
            if (_union.Count() > 0) {
                _taskCampaignBusiness.CreateAnswerComplete(_union, Guid.Parse(Idtask), ApplicationUserCurrent.AccountId, Guid.Parse(ApplicationUserCurrent.UserId));
            }

      






           
        }
        public JsonResult SaveAnswerQuestionMultiple( String id, String value , String idanswer, String Idtask, String idstatus, String CodigoGemini, String comment)
        {
            try
            {
                List<MyTaskViewAnswer> model = new List<MyTaskViewAnswer>();
                model.Add(new MyTaskViewAnswer {Idquestion = Guid.Parse(value),
                                                AnswerQuestion = id,
                                                estado = "P",
                                                idTask = Guid.Parse(Idtask),
                                                 idAnswer= idanswer
                });
                //JSonConvertUtil.Deserialize<List<MyTaskViewAnswer>>(AnswerQuestion);
                if (model == null) return null;
                _taskCampaignBusiness.CrearAnswerQuestion(model, ApplicationUserCurrent.AccountId, Guid.Parse(ApplicationUserCurrent.UserId), ApplicationUserCurrent.ProfileId, "ok", Idtask , Guid.Parse(idstatus), CodigoGemini, comment);
                return Json(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId(0, "Error Index"), ex.Message);
                return null;
            }
        }
        #endregion

        public void LoadSelectItems()
        {
            ViewBag.StatusList = _statusTaskBusiness.GetAllStatusTasks(ApplicationUserCurrent.AccountId, Guid.Parse(ApplicationUserCurrent.UserId))
                .Select(s => new SelectListItem() { Text = s.Name, Value = s.Id.ToString() })
                    .ToList();

            ViewBag.ReasonsList =
                _taskNotImplementedReasonBusiness.GetAllTaskNotImplementedReason()
                    .Select(t => new SelectListItem() { Value = t.Id.ToString(), Text = t.Name })
                    .ToList();
        }

        [HttpGet]
        public List<BranchImages> GetBranchImagesList(Guid branchId)
        {
            try
            {
                var resultList = _branchImageBusiness.GetBranchesImagesList(branchId, ApplicationUserCurrent.AccountId);
                return resultList;
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }

        [HttpGet]
        public List<StatusTask> GetAllStatusTask()
        {
            return _statusTaskBusiness.GetAllStatusTasks(ApplicationUserCurrent.AccountId, Guid.Parse(ApplicationUserCurrent.UserId));
        }

        [HttpGet]
        public List<TaskNoImplementedReason> GetAllTaskNotImplementedReason()
        {
            return _taskNotImplementedReasonBusiness.GetAllTaskNotImplementedReason();
        }

        [HttpPost]
        public string SaveTaskRegister(string inputTask)
        {

            var taskCampaign = _taskCampaignBusiness.SaveTaskRegister(inputTask, ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(taskCampaign);
        }

        [HttpGet]
        public string GetTaskListByServiceAndBranch(Guid idBranch, Guid idService)
        {
            var listResult = _taskCampaignBusiness.GetTaskListByServiceAndBranch(idBranch, idService, ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(listResult);
        }

        [HttpPost]
        public JsonResult AddSection(string task, string idSection)
        {
            var model = JSonConvertUtil.Deserialize<MyTaskViewModel>(task);
            //  _taskCampaignBusiness.AddSection(model, ApplicationUserCurrent.AccountId, ApplicationUserCurrent.ProfileId, Guid.Parse(ApplicationUserCurrent.UserId), Guid.Parse(idSection));
            //model = _taskCampaignBusiness.GetSectionsPoll(model.IdTask, _idAccount,idSection);
            //return RedirectToAction("Profile", new { idTask = model.IdTask });
            return Json(model);
        }
        [HttpPost]
        public IActionResult DuplicateSection(MyTaskViewModel model)
        {

            //if (!ModelState.IsValid)
            //{
            //    return RedirectToAction("Profile", new { idTask = model.IdTask });
            //}
            // _serviceBusiness.DuplicateSection(model, ApplicationUserCurrent.AccountId);
            return RedirectToAction("Register", new { idTask = model.IdTask });
        }

        #region Imágenes

        [HttpPost]
        public async Task<bool> UploadBranchImage(string fileName, string type)
        {
            var idBranch = Guid.Parse(fileName.Split('_')[1]);

            fileName += @"@" + type + @"@" + DateTime.Now.ToString("yyyyMMdd") + @"@" + DateTime.Now.ToString("HHmmss") +
                        ".jpg";

            try
            {
                if (idBranch == Guid.Empty)
                {
                    throw new ExceptionMardis();
                }

                var files = Request.Form.Files;

                await UploadFilesToAzure(idBranch.ToString(), files, fileName);

            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        [HttpGet]
        public string DeleteImageBranch(Guid idImageBranch)
        {
            var imageBranch = _branchImageBusiness.GetBranchImageById(idImageBranch, ApplicationUserCurrent.AccountId);
            AzureStorageUtil.DeleteBlob(CBranch.ImagesContainer, imageBranch.NameFile);
            _branchImageBusiness.DeleteBranchImage(idImageBranch, ApplicationUserCurrent.AccountId);

            var itemResult = _branchImageBusiness.GetBranchesImagesList(imageBranch.IdBranch, ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(itemResult);
        }

        private async Task UploadFilesToAzure(string branch, IFormFileCollection files, string fileName)
        {
            foreach (var file in files)
            {

                byte[] bytes;
                using (var stream = file.OpenReadStream())
                {
                    bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);
                }

                var fileStream = (new MemoryStream(bytes));

                await AzureStorageUtil.UploadFromStream(fileStream, CBranch.ImagesContainer, fileName);
                var uri = AzureStorageUtil.GetUriFromBlob(CBranch.ImagesContainer, fileName);
                _branchImageBusiness.SaveBranchImages(new BranchImages()
                {
                    IdBranch = new Guid(branch),
                    NameContainer = CBranch.ImagesContainer,
                    NameFile = fileName,
                    UrlImage = uri
                });
            }
        }

        #endregion

        [HttpPost]
        public bool ChangeStatusNotImplemented(Guid idTask, Guid idReason, string comment)
        {
            try
            {
                var myTask = _taskCampaignBusiness.GetTaskByIdForRegisterPage(idTask, ApplicationUserCurrent.AccountId);
                var status = _statusTaskBusiness.GeStatusTaskByName(CTask.StatusNotImplemented);
                myTask.IdTaskNoImplementedReason = idReason;
                myTask.IdStatusTask = status.Id;
                myTask.CommentTaskNoImplemented = comment;
                myTask.DateModification = DateTime.Now;
                _taskCampaignBusiness.ModifyTask(myTask);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception.Message);
                return false;
            }
            return true;
        }
        #region Exportar
        [HttpGet]

        public IActionResult Export(string id)
        {
            var idCa = Guid.Empty;
            if (!string.IsNullOrEmpty(id))
            {
                idCa = Guid.Parse(_protector.Unprotect(id));
            }
            string sWebRootFolder = _Env.WebRootPath;

            var listado = _taskCampaignBusiness.ListTask(idCa).Select(x => new
            {
                x.StartDate,
                x.Branch.Code,
                x.Branch.ExternalCode,
                x.Branch.TypeBusiness,
                x.Branch.Name,
                x.Branch.Cluster,
                Propietario = x.Branch.PersonOwner.Name,
                x.Branch.PersonOwner.Mobile,
                x.Branch.PersonOwner.Phone,
                Canton = x.Branch.District.Name,
                Estado_Tarea = x.StatusTask.Name,
                Encuestador = x.Pollster == null ? "Sin Indentificar" : x.Pollster.Name
                ,Estado_c=x.PollsTaskss.First().pollsstatus
                ,Commentario=x.PollsTaskss.First().Comment
                ,x.IdAccount,
                factura=x.PollsTaskss.First().novelty!=null?"SI":"NO"
                ,
                id = x.Id
            }).ToList();
            var log = DateTime.Now;
            string LogFile = log.ToString("yyyyMMddHHmmss");
            string sFileName = @"Listado.xlsx";
            string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
            FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
            if (file.Exists)
            {
                file.Delete();
                file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
            }
            using (ExcelPackage package = new ExcelPackage(file))
            {
                // add a new worksheet to the empty workbook
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Datos");

                ExcelWorksheet worksheet2 = package.Workbook.Worksheets.Add("Observaciones");

                //First add the headers
                Color colFromHex = System.Drawing.ColorTranslator.FromHtml("#B7DEE8");


                worksheet2.Column(1).Width = 20;
                worksheet2.Column(2).Width = 20;
                worksheet2.Column(3).Width = 20;
                worksheet2.Column(4).Width = 50;

                worksheet2.Cells[1, 1].Value = "Cod. Encuesta";
                worksheet2.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
                worksheet2.Cells[1, 1].Style.Font.Bold = true;
                worksheet2.Cells[1, 1].Style.Font.Size = 12;
                worksheet2.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet2.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(colFromHex);

                worksheet2.Cells[1, 2].Value = "Estado";
                worksheet2.Cells[1, 2].Style.Font.Size = 12;
                worksheet2.Cells[1, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet2.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet2.Cells[1, 2].Style.Font.Color.SetColor(Color.White);
                worksheet2.Cells[1, 2].Style.Font.Bold = true;

                worksheet2.Cells[1, 3].Value = "Fecha";
                worksheet2.Cells[1, 3].Style.Font.Size = 12;
                worksheet2.Cells[1, 3].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet2.Cells[1, 3].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet2.Cells[1, 3].Style.Font.Color.SetColor(Color.White);
                worksheet2.Cells[1, 3].Style.Font.Bold = true;

                worksheet2.Cells[1, 4].Value = "Observaciones";
                worksheet2.Cells[1, 4].Style.Font.Size = 12;
                worksheet2.Cells[1, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet2.Cells[1, 4].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet2.Cells[1, 4].Style.Font.Color.SetColor(Color.White);
                worksheet2.Cells[1, 4].Style.Font.Bold = true;

                worksheet.Column(1).Width = 20;
                worksheet.Column(2).Width = 20;
                worksheet.Column(3).Width = 20;
                worksheet.Column(4).Width = 20;
                worksheet.Column(5).Width = 32;
     
                worksheet.Column(7).Width = 20;
                worksheet.Column(8).Width = 20;
                worksheet.Column(9).Width = 20;
                worksheet.Column(10).Width = 20;
                worksheet.Column(11).Width = 20;
                worksheet.Column(12).Width = 40;

                if (!listado.First().IdAccount.Equals(Guid.Parse("85024910-FC12-4DD8-AE58-761BF972DEB7")))
                {
                    worksheet.Column(6).Width = 20;
                }
                if (listado.First().IdAccount.Equals(Guid.Parse("85024910-FC12-4DD8-AE58-761BF972DEB7"))){
                    worksheet.Column(6).Width = 20;
                }
                worksheet.Cells[1, 1].Value = "Ciudad";

                worksheet.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.Font.Size = 12;

                worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(colFromHex);

                worksheet.Cells[1, 2].Value = "Cluster";
                worksheet.Cells[1, 2].Style.Font.Size = 12;
                worksheet.Cells[1, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 2].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 2].Style.Font.Bold = true;

                worksheet.Cells[1, 3].Value = "Cod. Encuesta";
                worksheet.Cells[1, 3].Style.Font.Size = 12;
                worksheet.Cells[1, 3].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 3].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 3].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 3].Style.Font.Bold = true;


                worksheet.Cells[1, 4].Value = "PT_INDICE";
                worksheet.Cells[1, 4].Style.Font.Size = 12;
                worksheet.Cells[1, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 4].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 4].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 4].Style.Font.Bold = true;

                worksheet.Cells[1, 5].Value = "Nombre local";
                worksheet.Cells[1, 5].Style.Font.Size = 12;
                worksheet.Cells[1, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 5].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 5].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 5].Style.Font.Bold = true;

                worksheet.Cells[1, 6].Value = "Estado Campo";
                worksheet.Cells[1, 6].Style.Font.Size = 12;
                worksheet.Cells[1, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 6].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 6].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 6].Style.Font.Bold = true;

                worksheet.Cells[1, 7].Value = "Telefono";
                worksheet.Cells[1, 7].Style.Font.Size = 12;
                worksheet.Cells[1, 7].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 7].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 7].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 7].Style.Font.Bold = true;
                worksheet.Cells[1, 8].Value = "Encuestador";
                worksheet.Cells[1, 8].Style.Font.Size = 12;
                worksheet.Cells[1, 8].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 8].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 8].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 8].Style.Font.Bold = true;
                worksheet.Cells[1, 9].Value = "Fecha";
                worksheet.Cells[1, 9].Style.Font.Size = 12;
                worksheet.Cells[1, 9].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 9].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 9].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 9].Style.Font.Bold = true;
                worksheet.Cells[1, 10].Value = "Estado";
                worksheet.Cells[1, 10].Style.Font.Size = 12;
                worksheet.Cells[1, 10].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 10].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 10].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 10].Style.Font.Bold = true;

                worksheet.Cells[1, 12].Value = "Observaciones Encuesta";
                worksheet.Cells[1, 12].Style.Font.Size = 12;
                worksheet.Cells[1, 12].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 12].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 12].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 12].Style.Font.Bold = true;
                if (!listado.First().IdAccount.Equals(Guid.Parse("85024910-FC12-4DD8-AE58-761BF972DEB7")))
                {
                    worksheet.Cells[1, 11].Value = "Canal";
                    worksheet.Cells[1, 11].Style.Font.Size = 12;
                    worksheet.Cells[1, 11].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, 11].Style.Fill.BackgroundColor.SetColor(colFromHex);
                    worksheet.Cells[1, 11].Style.Font.Color.SetColor(Color.White);
                    worksheet.Cells[1, 11].Style.Font.Bold = true;
                }
                    if (listado.First().IdAccount.Equals(Guid.Parse("85024910-FC12-4DD8-AE58-761BF972DEB7")))
                {
                    worksheet.Cells[1, 11].Value = "Factura";
                    worksheet.Cells[1, 11].Style.Font.Size = 12;
                    worksheet.Cells[1, 11].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    worksheet.Cells[1, 11].Style.Fill.BackgroundColor.SetColor(colFromHex);
                    worksheet.Cells[1, 11].Style.Font.Color.SetColor(Color.White);
                    worksheet.Cells[1, 11].Style.Font.Bold = true;
                }
                int rows = 2;
                int rowsobs = 2;
                foreach (var t in listado)
                {
                    worksheet.Cells[rows, 1].Value = t.Canton;
                    worksheet.Cells[rows, 2].Value = t.Cluster;
                    worksheet.Cells[rows, 3].Value = t.Code;
                    worksheet.Cells[rows, 4].Value = t.ExternalCode;
                    worksheet.Cells[rows, 5].Value = t.Name.ToUpper();
                    worksheet.Cells[rows, 6].Value = t.Estado_c;
                    worksheet.Cells[rows, 7].Value = t.Phone;
                    worksheet.Cells[rows, 8].Value = t.Encuestador;
                    worksheet.Cells[rows, 9].Value = t.StartDate;
                    worksheet.Cells[rows, 9].Style.Numberformat.Format = "yyyy-mm-dd";
                    worksheet.Cells[rows, 10].Value = t.Estado_Tarea;

                    worksheet.Cells[rows, 12].Value = t.Commentario;
                    if (!listado.First().IdAccount.Equals(Guid.Parse("85024910-FC12-4DD8-AE58-761BF972DEB7")))
                    {
                        worksheet.Cells[rows, 11].Value = t.TypeBusiness.ToUpper().TrimEnd().TrimStart();
                    }
                    if (listado.First().IdAccount.Equals(Guid.Parse("85024910-FC12-4DD8-AE58-761BF972DEB7")))
                    {
                        worksheet.Cells[rows, 11].Value = t.factura;
                    }
                    rows++;
                  
                }

                var _model = from x in _taskCampaignBusiness.GetHistoryCampign(idCa)
                             select new { date = x.DateModification, status = x.StatusTask.Name, user = x.Users.Email, comment = x.CommentTaskNoImplemented,code=x.Tasks.Code,x.idtask };
          
                if (_model != null)
                {
                    _model = _model.OrderByDescending(x => x.idtask).ToList();

                }
                foreach (var h in _model)
                {

                    worksheet2.Cells[rowsobs, 1].Value = h.code;
                    worksheet2.Cells[rowsobs, 2].Value = h.status;
                    worksheet2.Cells[rowsobs, 3].Value = h.date;
                    worksheet2.Cells[rowsobs, 3].Style.Numberformat.Format = "yyyy-mm-dd HH:mm";
                    worksheet2.Cells[rowsobs, 4].Value = h.comment;

                    rowsobs++;
                }

                // add a new worksheet to the empty workbook


                package.Save();
            }
            var result = PhysicalFile(Path.Combine(sWebRootFolder, sFileName), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            Response.Headers["Content-Disposition"] = new ContentDispositionHeaderValue("attachment")
            {
                FileName = file.Name
            }.ToString();

            return result;
        }
        #endregion
        #region Impresion
        [HttpPost]
        public JsonResult UploadFile(String Idtask ,String task)
        {
            try
            {
                var Filepath = _Env.WebRootPath ;
                var imgg = "";
                var outs = "";
                if (Idtask !=null)
                    outs = _taskCampaignBusiness.PrintFile(Guid.Parse(Idtask), Filepath, ApplicationUserCurrent.AccountId, imgg, task);


                return Json(outs);
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId(0, "Error Index"), ex.Message);

                return Json("");
            }
        }
        #endregion
        #region cargaMasiva
        [HttpGet]
        public IActionResult Massive(Guid idTask, Guid? idCampaign = null, string returnUrl = null)
        {

            try
            {
                ViewData["ReturnUrl"] = returnUrl;

                TempData["Idcampaing"] = JsonConvert.SerializeObject(idCampaign);
                return View();
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }
        [HttpPost]
        public IActionResult Massive(IFormFile fileBranch)
        {
            DateTime localDate = DateTime.Now;
            if (fileBranch == null)
            {

                TempData["ErrorCarga"] = "Verfique si el archivo fue cargado";
                return RedirectToAction("Massive");
            }
            Guid idcampaing = JsonConvert.DeserializeObject<Guid>(TempData["Idcampaing"].ToString());
            string LogFile = localDate.ToString("yyyyMMddHHmmss");
            var Filepath = _Env.WebRootPath + "\\Form\\ " + LogFile + "_" + fileBranch.FileName.ToString();
           
            using (var fileStream = new FileStream(Filepath, FileMode.Create))
            {
                fileBranch.CopyTo(fileStream);
            }


            return RedirectToAction("LoadTask", "Task", new { @idCampaign = idcampaing, @path = Filepath, @nameFile = fileBranch.FileName.ToString() });
        }
        [HttpGet]
        public IActionResult LoadTask(Guid idCampaign, string path = null, string nameFile = null)
        {

            try
            {
                ViewBag.file = nameFile;
                ViewBag.path = path;
                ViewBag.idcampingn = idCampaign;
                //Guid idAccount = ApplicationUserCurrent.AccountId;
                //var msg =_taskCampaignBusiness.taskMigrate(path, idAccount, idCampaign);

                return View();
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }
        [HttpPost]
        public JsonResult LoadTask(string idcampaign, string idpath,string idstatus)
        {

            try
            {
                Timer timer = new Timer(3000);
                Guid idCampaignGuid = Guid.Parse(idcampaign);
                Guid idAccount = ApplicationUserCurrent.AccountId;
                var data = _taskCampaignBusiness.taskMigrate(idpath, idAccount, idCampaignGuid, idstatus);
                timer.Start();
                var rows = from x in data
                           select new
                           {
                               description = x.description,
                               data = x.Element,
                               Code = x.Code

                           };
          
                var jsondata =  rows.ToArray();
                return Json(jsondata);

            }
            catch (Exception e)
            {
                 
                         _logger.LogError(new EventId(0, "Error Index"), e.Message);
         IList<TaskMigrateResultViewModel> data = new List<TaskMigrateResultViewModel>();
                data.Add(new TaskMigrateResultViewModel { description = e.Message, Element = "0", Code = "0" });
              var rows = from x in data
                           select new
                           {
                               description = x.description,
                               data = x.Element,
                               Code = x.Code

                           };
          
                var jsondata =  rows.ToArray();
                return Json(jsondata);
              
            }
        }
        #endregion

        #region Update BranchImage
        [HttpPost]
        public JsonResult ChangeImage(string idIdimg, string imgdata)
        {

            try
            {



                return Json(_taskCampaignBusiness.UpdateBranch(idIdimg, imgdata).ToString());

            }
            catch (Exception e)
            {


                return Json("0");

            }
        }
        [HttpPost]
        public JsonResult SaveImage(string Idtask, string imgdata,string idbranch ,string idcampaign)
        {

            try
            {



                return Json(_taskCampaignBusiness.AddBranch(Idtask, imgdata, Guid.Parse(idbranch), Guid.Parse(idcampaign)));

            }
            catch (Exception e)
            {

              
                return Json("0");

            }
        }

        [HttpPost]
        public JsonResult DeleteImage( string imgdata)
        {

            try
            {



                return Json(_taskCampaignBusiness.DeleteBranch(imgdata));

            }
            catch (Exception e)
            {


                return Json("0");

            }
        }
        #endregion

        #region historia Estados 

        [HttpGet]
        public JsonResult HistoryTB(string id)
        {

            try
            {

                if (!id.Equals("0"))
                {

                    var _model = from x in _taskCampaignBusiness.GetHistory(Guid.Parse(id))
                                 select new { date = x.DateModification, status = x.StatusTask.Name, user = x.Users.Email ,comment=x.CommentTaskNoImplemented};
                    if (_model!=null)
                    {
                        var _data = _model.OrderByDescending(x=>x.date).Distinct().ToList();
                        return Json(_data);
                    }
                    else
                    {
                        return Json(null);
                    }
                }
                return Json(null);
            }
               

            catch (Exception e)
            {


                return Json("0");

            }
        }


        #endregion

        #region CreaciondeCodigo
        [HttpPost]
        public JsonResult ChangeCode(string branch, string code,string taskid)
        {

            try
            {

                var _status = _taskCampaignBusiness._GeneretorCode(Guid.Parse(branch), code, Guid.Parse(ApplicationUserCurrent.UserId), Guid.Parse(taskid));

                return Json(_status);

            }
            catch (Exception e)
            {


                return Json('0');

            }
        }
        #endregion
        #region Cerrar Ventana
        [HttpPost]
        public   JsonResult CloseWindows(string Id)
        {

            try
            {

                _taskCampaignBusiness.taskunBlock(Guid.Parse(ApplicationUserCurrent.UserId),Guid.Parse(Id));

                return Json("");

            }
            catch (Exception e)
            {


                return Json("0");

            }
        }
        #endregion

        #region validarPreguntasXProfile
        [HttpPost]
        public JsonResult ProfileQuestion(string task)
        {

            try
            {
                var _task = JSonConvertUtil.Deserialize<MyTaskViewModel>(task);

                 var _model  = _taskCampaignBusiness.ControlQuestion(Guid.Parse(ApplicationUserCurrent.UserId), _task);
                return Json(_model);

            }
            catch (Exception e)
            {


                return Json("0");

            }
        }
        #endregion
      
    }
}
