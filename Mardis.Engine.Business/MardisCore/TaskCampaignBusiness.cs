using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Mardis.Engine.Converter;
using Mardis.Engine.DataAccess;
using Mardis.Engine.DataAccess.MardisCore;
using Mardis.Engine.DataObject.MardisCommon;
using Mardis.Engine.DataObject.MardisCore;
using Mardis.Engine.DataObject.MardisSecurity;
using Mardis.Engine.Framework;
using Mardis.Engine.Framework.Resources;
using Mardis.Engine.Framework.Resources.PagesConstants;
using Mardis.Engine.Web.ViewModel.Filter;
using Mardis.Engine.Web.ViewModel.TaskViewModels;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Schema;
using AutoMapper;
using Mardis.Engine.Business.MardisSecurity;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using StackExchange.Redis;
using Microsoft.Extensions.Caching.Distributed;
using iTextSharp;
using iTextSharp.text;
using System.IO;
using iTextSharp.text.pdf;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Mardis.Engine.Web.ViewModel.BranchViewModels;
using System.Net;
using DocumentFormat.OpenXml;
using System.Text.RegularExpressions;
using Mardis.Engine.Web.ViewModel.UserViewModels;
using System.Text;
using System.Linq.Dynamic.Core;
using Mardis.Engine.Web.ViewModel.Utility;

namespace Mardis.Engine.Business.MardisCore
{
    public class TaskCampaignBusiness : ABusiness
    {
        #region VARIABLES Y CONSTRUCTORES
        static  AzureStorageUtil azureStorageUtil;
        private readonly TaskCampaignDao _taskCampaignDao;
        private readonly QuestionDao _questionDao;
        private readonly QuestionDetailDao _questionDetailDao;
        private readonly StatusTaskBusiness _statusTaskBusiness;
        private readonly SequenceBusiness _sequenceBusiness;
        private readonly CampaignServicesDao _campaignServicesDao;
        private readonly BranchDao _branchDao;
        private readonly AnswerDetailDao _answerDetailDao;
        private readonly AnswerDao _answerDao;
        private readonly BranchImageBusiness _branchImageBusiness;
        private readonly UserDao _userDao;
        private readonly CampaignDao _campaignDao;
        private readonly ServiceDetailTaskBusiness _serviceDetailTaskBusiness;
        private readonly PersonDao _personDao;
        private readonly ProfileDao _profileDao;
        private readonly TypeUserBusiness _typeUserBusiness;
        private readonly ServiceDetailDao _serviceDetailDao;
        private readonly RedisCache _redisCache;
        private readonly ServiceDetailBusiness _serviceDetailBusiness;
        private readonly BranchMigrateDao _branchMigrateDao;
        private readonly IList<TaskMigrateResultViewModel> lstTaskResult = new List<TaskMigrateResultViewModel>();
        private readonly IList<Branch> lsBranch = new List<Branch>();

        public TaskCampaignBusiness(MardisContext mardisContext, RedisCache distributedCache)
            : base(mardisContext)
        {
            _taskCampaignDao = new TaskCampaignDao(mardisContext);
            _questionDetailDao = new QuestionDetailDao(mardisContext);
            _statusTaskBusiness = new StatusTaskBusiness(mardisContext, distributedCache);
            _sequenceBusiness = new SequenceBusiness(mardisContext);
            _campaignServicesDao = new CampaignServicesDao(mardisContext);
            _branchDao = new BranchDao(mardisContext);
            _answerDao = new AnswerDao(mardisContext);
            _answerDetailDao = new AnswerDetailDao(mardisContext);
            _branchImageBusiness = new BranchImageBusiness(mardisContext);
            _userDao = new UserDao(mardisContext);
            _campaignDao = new CampaignDao(mardisContext);
            _serviceDetailTaskBusiness = new ServiceDetailTaskBusiness(mardisContext);
            _personDao = new PersonDao(mardisContext);
            _profileDao = new ProfileDao(mardisContext);
            _typeUserBusiness = new TypeUserBusiness(mardisContext, distributedCache);
            _serviceDetailDao = new ServiceDetailDao(mardisContext);
            _questionDao = new QuestionDao(mardisContext);
            _redisCache = distributedCache;
            _serviceDetailBusiness = new ServiceDetailBusiness(mardisContext);
            azureStorageUtil = new AzureStorageUtil();
            _branchMigrateDao = new BranchMigrateDao(mardisContext);
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<Service, MyTaskServicesViewModel>()
                    .ForMember(dest => dest.ServiceDetailCollection, opt => opt.MapFrom(src => src.ServiceDetails.OrderBy(sd => sd.Order)));
                cfg.CreateMap<ServiceDetail, MyTaskServicesDetailViewModel>()
                    .ForMember(dest => dest.QuestionCollection, opt => opt.MapFrom(src => src.Questions.OrderBy(q => q.Order)))
                    .ForMember(dest => dest.Sections, opt => opt.MapFrom(src => src.Sections.OrderBy(s => s.Order)));
                cfg.CreateMap<Question, MyTaskQuestionsViewModel>()
                   // .ForMember(dest => dest.HasPhoto, opt => opt.MapFrom(src => src.HasPhoto.IndexOf("S", StringComparison.Ordinal) >= 0))
                    .ForMember(dest => dest.QuestionDetailCollection, opt => opt.MapFrom(src => src.QuestionDetails.OrderBy(qd => qd.Order)))
                    .ForMember(dest => dest.CodeTypePoll, opt => opt.MapFrom(src => src.TypePoll.Code));
                cfg.CreateMap<QuestionDetail, MyTaskQuestionDetailsViewModel>();
            });
        }

        #endregion

        public TaskListViewModel GetPaginatedTasksList(Guid idCampaign, Guid idAccount, List<FilterValue> filters, int pageIndex, int pageSize)
        {
            var watch = new Stopwatch();

            var itemResult = new TaskListViewModel { IdCampaign = idCampaign };

            filters = filters ?? new List<FilterValue>();

            filters = AddHiddenFilter("IdCampaign", idCampaign.ToString(), filters, itemResult.FilterName);

            var tasks = _taskCampaignDao.GetPaginatedTasksList(idAccount, filters, pageIndex, pageSize);
            var countTasks = _taskCampaignDao.GetPaginatedTasksCount(idAccount, filters);

            foreach (var task in tasks)
            {
                var branch = _branchDao.GetOnebyId(task.IdBranch, idAccount);
                var merchant = _userDao.GetUserById(task.IdMerchant);
                watch.Start();
                var status = _statusTaskBusiness.GetStatusTask(task.IdStatusTask);
                watch.Stop();
                var campaign = _campaignDao.GetOne(task.IdCampaign, idAccount);
                var tvm = new TaskListItemViewModel()
                {
                    BranchName = branch.Name,
                    Code = task.Code,
                    Id = task.Id,
                    MerchantName = task.Pollster.Name,
                    Route = task.Route,
                    StartDate = task.StartDate,
                    StatusName = status.Name,
                    CampaignId = task.IdCampaign,
                    CampaignName = campaign.Name,
                    BranchCode = branch.ExternalCode
                    ,BranchId=branch.Id
                };

                itemResult.TasksList.Add(tvm);
            }

            Debugger.Log(0, "TaskCapaugnBusiness", $" ms: {watch.ElapsedMilliseconds}");

            return ConfigurePagination(itemResult, pageIndex, pageSize, filters, countTasks);
        }

        public TaskPerCampaignViewModel GetTasksPerCampaign(Guid userId, int pageIndex, int pageSize, List<FilterValue> filters, Guid idAccount)
        {
            filters = filters ?? new List<FilterValue>();
                      var itemResult = new TaskPerCampaignViewModel("MyTasks", "Task");
            var user = _userDao.GetUserById(userId);

            if (user.Profile.TypeUser.Name == CTypePerson.PersonMerchant)
            {
                filters = AddHiddenFilter("IdMerchant", userId.ToString(), filters, itemResult.FilterName);
            }

            int max;

            itemResult = GetTasksPropertiesDinamic(pageIndex, pageSize, filters, idAccount, itemResult, out max);

            return ConfigurePagination(itemResult, pageIndex, pageSize, filters, max);
        }
        #region taskDinamic

        private TaskPerCampaignViewModel GetTasksPropertiesDinamic(int pageIndex, int pageSize, List<FilterValue> filters, Guid idAccount,
            TaskPerCampaignViewModel itemResult, out int max)
        {
            itemResult.tasks = new List<MyStatusTaskViewModel>();
            var data = _taskCampaignDao.statusAllow(idAccount, pageIndex, pageSize);
            int aux=0;
            max = 0;
            foreach (var allow in data) {
                aux = _taskCampaignDao.GetTaskCountByCampaignAndStatus(allow.Name, filters, idAccount);
                var taskslist= GetTaskList(pageIndex, pageSize, filters, idAccount, allow.Name);
                max = (aux > max) ? aux: max;
                itemResult.tasks.Add(new MyStatusTaskViewModel { TasksList = taskslist, CountTasks = aux, type = allow.Name , color =allow.color});
            }

            return itemResult;
        }
        #endregion
        private TaskPerCampaignViewModel GetTasksProperties(int pageIndex, int pageSize, List<FilterValue> filters, Guid idAccount,
              TaskPerCampaignViewModel itemResult, out int max)
        {
        
            max = _taskCampaignDao.GetTaskCountByCampaignAndStatus(CTask.StatusImplemented, filters, idAccount);

            var countImplementedTasks = max;
            var countNotImplementedTasks = _taskCampaignDao.GetTaskCountByCampaignAndStatus(CTask.StatusNotImplemented, filters,
                idAccount);

            max = (max > countNotImplementedTasks) ? max : countNotImplementedTasks;

            var countPendingTasks = _taskCampaignDao.GetTaskCountByCampaignAndStatus(CTask.StatusPending, filters, idAccount);

            max = (max > countPendingTasks) ? max : countPendingTasks;

            var countStartedTasks = _taskCampaignDao.GetTaskCountByCampaignAndStatus(CTask.StatusStarted, filters, idAccount);

            max = (max > countStartedTasks) ? max : countStartedTasks;

            itemResult.ImplementedTasksList = GetTaskList(pageIndex, pageSize, filters, idAccount,
                CTask.StatusImplemented);

            itemResult.NotImplementedTasksList = GetTaskList(pageIndex, pageSize, filters, idAccount,
                CTask.StatusNotImplemented);

            itemResult.PendingTasksList = GetTaskList(pageIndex, pageSize, filters, idAccount,
                CTask.StatusPending);

            itemResult.StartedTasksList = GetTaskList(pageIndex, pageSize, filters, idAccount,
                CTask.StatusStarted);

        
            itemResult.CountImplementedTasks = countImplementedTasks;
            itemResult.CountNotImplementedTasks = countNotImplementedTasks;
            itemResult.CountPendingTasks = countPendingTasks;
            itemResult.CountStartedTasks = countStartedTasks;

            return itemResult;
        }

        private List<MyTaskItemViewModel> GetTaskList(int pageIndex, int pageSize, List<FilterValue> filters, Guid idAccount, string statusTask)
        {
            var tasks = _taskCampaignDao.GetPaginatedTasksByCampaignAndStatus(statusTask, pageIndex, pageSize, filters,
                idAccount);

            return ConvertTask.ConvertTaskToMyTaskViewItemModel(tasks);
        }

        public TaskCampaign GetTaskByIdForRegisterPage(Guid idTask, Guid idAccount)
        {
            return _taskCampaignDao.GetTaskByIdForRegisterPage(idTask, idAccount);
        }

        public List<TaskCampaign> GetAlltasksByCampaignId(Guid idCampaign, Guid idAccount)
        {
            return _taskCampaignDao.GetTaskListByCampaign(idCampaign, idAccount);
        }

        public bool ModifyTask(TaskCampaign task)
        {
            using (var transaccion = Context.Database.BeginTransaction())
            {
                try
                {
                    Context.TaskCampaigns.Add(task);

                    Context.Entry(task).State = EntityState.Modified;

                    Context.SaveChanges();
                    transaccion.Commit();
                }
                catch (Exception)
                {
                    transaccion.Rollback();
                    return false;
                }

            }
            return true;
        }

        public TaskCampaign SaveTaskRegister(string inputTask, Guid idAccount)
        {
            TaskCampaign result;
            var taskModelView = JSonConvertUtil.Deserialize<TaskRegisterModelView>(inputTask);
            var task = new TaskCampaign()
            {
                IdAccount = idAccount,
                IdBranch = taskModelView.IdBranch,
                IdCampaign = taskModelView.IdCampaign,
                IdMerchant = taskModelView.IdMerchant,
                IdStatusTask = taskModelView.IdStatusTask,
                Description = taskModelView.Description,
                StartDate = taskModelView.StartDate,
                Campaign = null,
                Branch = null
            };
            if (taskModelView.IdStatusTask == Guid.Empty)
            {
                var status = _statusTaskBusiness.GeStatusTaskByName(CTask.StatusPending);
                task.IdStatusTask = status.Id;
            }

            using (var transaccion = Context.Database.BeginTransaction())
            {
                try
                {
                    if (string.IsNullOrEmpty(task.Code))
                    {
                        var nextSequence = _sequenceBusiness.NextSequence(CTask.SequenceCode,
                            idAccount);

                        task.Code = nextSequence.ToString();
                    }

                    var stateRegister = EntityState.Added;

                    if (Guid.Empty != task.Id)
                    {
                        stateRegister = EntityState.Modified;
                    }

                    Context.TaskCampaigns.Add(task);

                    Context.Entry(task).State = stateRegister;

                    //graba cambios en COntexto
                    Context.SaveChanges();
                    transaccion.Commit();
                    result = GetTaskByIdForRegisterPage(task.Id, idAccount);
                }
                catch
                {
                    transaccion.Rollback();
                    result = null;
                }
            }

            return result;
        }

        public List<TaskCampaign> GetTaskListByServiceAndBranch(Guid idBranch, Guid idService, Guid idAccount)
        {
            var campaignServicesList = _campaignServicesDao.GetCampaignServicesByService(idService, idAccount);

            Guid[] campaignIds = campaignServicesList.Select(cs => cs.IdCampaign).ToArray();

            return _taskCampaignDao.GetTaskListByCampaignIdsAndBranch(idBranch, campaignIds, idAccount);
        }

        public List<TaskCampaign> GetTasksBranchesByCampaign(Guid idCampaign, Guid idAccount)
        {
            var taskList = _taskCampaignDao.GetAlltasksByCampaignId(idCampaign, idAccount);

            foreach (var task in taskList)
            {
                task.Branch = _branchDao.GetOne(task.IdBranch, idAccount);
                task.StatusTask = _statusTaskBusiness.GetStatusTask(task.IdStatusTask);
            }

            return taskList;
        }


        //Metodo devuelve las tareas y campañas
        public List<MyTaskViewModel> GetSectionsPollLista(Guid idAccount)
        {

            var taskWithPoll = _taskCampaignDao.GetForProfileLista(idAccount);
            if (taskWithPoll == null)
            {
                return null;
            }
            var myWatch = new Stopwatch();
            myWatch.Start();
            myWatch.Stop();
            return taskWithPoll;

        }





        /// <summary>
        /// Este método Obtiene la estructura de la encuesta sin Preguntas ni subsecciones
        /// </summary>
        /// <param name="idTask">Identificador de Tarea</param>
        /// <param name="idAccount">Identificador de cuenta</param>
        /// <returns></returns>
        public MyTaskViewModel GetSectionsPoll(Guid idTask, Guid idAccount)
        {

            var taskWithPoll = _taskCampaignDao.GetForProfile(idTask, idAccount);
            taskWithPoll.novelty = _taskCampaignDao.GetNoveltyTask(idTask);
            if (taskWithPoll == null)
            {
                return null;
            }

#if DEBUG
            var myWatch = new Stopwatch();
            myWatch.Start();

#endif
            taskWithPoll.ServiceCollection = GetServiceListFromCampaign(taskWithPoll.IdCampaign, idAccount, idTask);

#if DEBUG
            myWatch.Stop();
#endif
            taskWithPoll = AnswerTheQuestionsFromTaskPoll(idTask, taskWithPoll);
            taskWithPoll.BranchImages = GetImagesTask(idAccount, taskWithPoll);
            taskWithPoll.IdTaskNotImplemented = taskWithPoll.IdStatusTask;
#if DEBUG
            myWatch.Stop();
#endif

            return taskWithPoll;
        }

        public List<MyTaskServicesViewModel> GetSectionsPollService(Guid IdCampaign, Guid idAccount)
        {

            List<MyTaskServicesViewModel> taskWithPoll = new List<MyTaskServicesViewModel>();
            if (taskWithPoll == null)
            {
                return null;
            }

#if DEBUG
            var myWatch = new Stopwatch();
            myWatch.Start();
#endif
            taskWithPoll = GetServiceListFromCampaignGeo(IdCampaign, idAccount);

#if DEBUG
            myWatch.Stop();
#endif

#if DEBUG
            myWatch.Stop();
#endif

            return taskWithPoll;
        }




        public object GetBranchApi(Guid IdAccount)
        {
            var result = _branchDao.GetAllBranches().Where(x => x.IdAccount == IdAccount).ToList();
            return result;

        }

        public List<TaskCampaign> ListTask(Guid? idCampaign)
        {
            return _taskCampaignDao.GetAlltasksByCampaignNewId(idCampaign);
        }


        private List<BranchImages> GetImagesTask(Guid idAccount, MyTaskViewModel taskWithPoll)
        {
            var images =
                _branchImageBusiness.GetBranchesImagesList(taskWithPoll.IdBranch, idAccount, taskWithPoll.IdCampaign);

            foreach (var service in taskWithPoll.ServiceCollection)
            {
                foreach (var section in service.ServiceDetailCollection)
                {
                    images.AddRange(
                        section.QuestionCollection.Where(
                                q => q.CodeTypePoll == CTypePoll.Image && !string.IsNullOrEmpty(q.Answer))
                            .Select(q => new BranchImages()
                            {
                                NameFile = q.Title,
                                UrlImage = q.Answer
                            })
                    );
                }
            }
            var orderImg = images.OrderBy(x => x.Order).ToList();
            return orderImg;
        }

        private MyTaskViewModel AnswerTheQuestionsFromTaskPoll(Guid idTask, MyTaskViewModel taskWithPoll)
        {
            var answers = _answerDao.GetAllAnswers(idTask);



            taskWithPoll.ServiceCollection.AsParallel()
                .ForAll(s =>
                {
                    s.ServiceDetailCollection =
                        _serviceDetailBusiness.GetAnsweredSections(s.ServiceDetailCollection, answers);
                });
            return taskWithPoll;
        }

        public TaskRegisterViewModel GetTask(Guid? idCampaign, Guid idTask, Guid idAccount)
        {
            var itemResult = new TaskRegisterViewModel();

            //Creo Tarea
            if (idTask == Guid.Empty && idCampaign != null)
            {
                var campaign = _campaignDao.GetOne(idCampaign.Value, idAccount);
                itemResult.CampaignName = campaign.Name;
                itemResult.IdCampaign = idCampaign.Value;
            }
            //Recupero Tarea
            else if (idTask != Guid.Empty)
            {
                var task = _taskCampaignDao.GetTaskByIdForRegisterPage(idTask, idAccount);
                var campaign = _campaignDao.GetOne(task.IdCampaign, idAccount);
                itemResult = ConvertTask.ToTaskRegisterViewModel(task);
                itemResult.CampaignName = campaign.Name;
                itemResult.IdCampaign = task.IdCampaign;
            }
            //Mala Invocación de método
            else
            {
                throw new ExceptionMardis("Se ha invocado de manera incorrecta a la tarea");
            }

            return itemResult;
        }

        public bool Save(TaskRegisterViewModel model, Guid idAccount)
        {
            var task = ConvertTask.FromTaskRegisterViewModel(model);
            //if (model.Id == Guid.Empty)
            //{
            //    var status = _statusTaskBusiness.GeStatusTaskByName(CTask.StatusPending);
            //    task.IdStatusTask = status.Id;
            //}

            task.IdAccount = idAccount;
            task.Branch = null;
            task.Campaign = null;
            task.Merchant = null;
            task.Answers = null;

            if (string.IsNullOrEmpty(task.Code))
            {
                var code = _sequenceBusiness.NextSequence(CTask.SequenceCode, idAccount).ToString();
                task.Code = code;
            }

            _taskCampaignDao.InsertOrUpdate(task);
            //SaveTask(task, idAccount);
            return true;
        }

        public void SaveAnsweredPoll(MyTaskViewModel model, Guid idAccount, Guid idProfile, Guid idUser, Guid status, string comment)
        {
            try
            {
                SaveBranchData(model, idAccount);
                CleanAnswers(model);

                foreach (var service in model.ServiceCollection)
                {
                    foreach (var serviceDetail in service.ServiceDetailCollection)
                    {
                        CreateAnswer(model, idAccount, serviceDetail);

                        if (serviceDetail.Sections != null)
                        {
                            foreach (var section in serviceDetail.Sections)
                            {
                                CreateAnswer(model, idAccount, section);
                            }
                        }

                    }
                }

                FinalizeTask(model, idAccount, idProfile, idUser,status,  comment);
            }
            catch (Exception ex)
            {
                string resultado = ex.Message;
                throw;
            }

        }

        private void FinalizeTask(MyTaskViewModel model, Guid idAccount, Guid idProfile, Guid idUser, Guid status, string comment)
        {
            var profile = _profileDao.GetById(idProfile);

            switch (_typeUserBusiness.Get(profile.IdTypeUser).Name)
            {
                case CTypePerson.PersonMerchant:
                case CTypePerson.PersonSupervisor:
                case CTypePerson.PersonSystem:
                    _taskCampaignDao.ImplementTask(model.IdTask, _statusTaskBusiness.GeStatusTaskByName(CTask.StatusImplemented).Id,
                        idAccount,status, idUser,  comment);
                    break;
                case CTypePerson.PersonValidator:
                    _taskCampaignDao.ValidateTask(model.IdTask, _statusTaskBusiness.GeStatusTaskByName(CTask.StatusImplemented).Id,
                        idAccount, idUser);
                    break;
            }
        }

        private void FinalizeTaskAnswerQuestion(Guid idtask, Guid idAccount, Guid idProfile, Guid idUser ,Guid status, string comment)
        {
            var profile = _profileDao.GetById(idProfile);

            switch (_typeUserBusiness.Get(profile.IdTypeUser).Name)
            {
                case CTypePerson.PersonMerchant:
                case CTypePerson.PersonSupervisor:
                case CTypePerson.PersonValidator:
                case CTypePerson.PersonSystem:
                    _taskCampaignDao.ImplementTask(idtask, _statusTaskBusiness.GeStatusTaskByName(CTask.StatusImplemented).Id,
                        idAccount,status, idUser,comment);
                    break;
                //case CTypePerson.PersonValidator:
                //    _taskCampaignDao.ValidateTask(idtask, _statusTaskBusiness.GeStatusTaskByName(CTask.StatusImplemented).Id,
                //        idAccount, idUser);
                //    break;
            }
        }

        private void FinalizeTaskAnswerQuestionGemini(Guid idtask, Guid idAccount, Guid idProfile, Guid idUser, Guid status,string CodeGemini)
        {
            var profile = _profileDao.GetById(idProfile);

            switch (_typeUserBusiness.Get(profile.IdTypeUser).Name)
            {
                case CTypePerson.PersonMerchant:
                case CTypePerson.PersonSupervisor:
                case CTypePerson.PersonSystem:
                    _taskCampaignDao.ImplementTaskGemini(idtask, _statusTaskBusiness.GeStatusTaskByName(CTask.StatusImplemented).Id,
                        idAccount, status,CodeGemini);
                    break;
                case CTypePerson.PersonValidator:
                    _taskCampaignDao.ValidateTask(idtask, _statusTaskBusiness.GeStatusTaskByName(CTask.StatusImplemented).Id,
                        idAccount, idUser);
                    break;
            }
        }
        private void CreateAnswer(MyTaskViewModel model, Guid idAccount, MyTaskServicesDetailViewModel serviceDetail)
        {
            foreach (var question in serviceDetail.QuestionCollection)
            {
                if (question.IdQuestionDetail == Guid.Empty && string.IsNullOrEmpty(question.Answer))
                {
                    continue;
                }
                if (question.IdQuestionDetail != Guid.Empty ||
                    (question.CodeTypePoll == CTypePoll.Open && !string.IsNullOrEmpty(question.Answer)))
                {
                    var answer = //_answerDao.GetAnswerValueByQuestion(question.Id, model.IdTask, idAccount) ??
                                 CreateAnswer(model, idAccount, question, serviceDetail);

                    CreateAnswerDetail(answer, question);
                }
            }
        }
        #region AnswerQuestionComplete
        public async void CreateAnswerComplete(List<MyTaskQuestionsViewModel> _model, Guid idtask,Guid idaccount,Guid iduser)
        {
            
            var answerDetail = new List<AnswerDetail>();
            foreach (var question in _model)
            {
                Guid Idanswer = (Guid)question.IdAnswer;
                Idanswer = Idanswer == null ? Guid.Empty : Idanswer;
                if (Idanswer == Guid.Empty && question.QuestionComplete.Count()>0)
                {
                    var answer = new Answer()
                    {
                        IdAccount = idaccount,
                        IdMerchant = iduser,
                        IdQuestion = question.Id,
                        IdServiceDetail = question.IdServiceDetail,
                        IdTask = idtask,
                        DateCreation = DateTime.Now,
                        StatusRegister = CStatusRegister.Active,
                        sequenceSection = 0
                    };
                    answer =  _answerDao.InsertOrUpdate(answer);
                    Idanswer = answer.Id;
                }

                foreach (var iddetail in question.QuestionComplete)
               {
                    answerDetail.Add(new AnswerDetail()
                    {
                        DateCreation = DateTime.Now,
                        IdAnswer = Idanswer,
                        IdQuestionDetail = Guid.Parse(iddetail),
                        CopyNumber = 0,
                        StatusRegister = CStatusRegister.Active
                    });

                }
                if (Idanswer != Guid.Empty)
                     CreateAnswerDetailQuestionComplete(Idanswer, answerDetail);
                answerDetail = new List<AnswerDetail>();
            }
        
            
        }
        private async void CreateAnswerDetailQuestionComplete(Guid idanswer , List<AnswerDetail> Detail )
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                try
                {
                    Context.AnswerDetails.RemoveRange(Context.AnswerDetails.Where(a => a.Answer.Id == idanswer));
                 Context.SaveChanges();
                    if (Detail.Count() > 0) {
                        Context.AnswerDetails.AddRange(Detail);
                    }
                    else
                    {

                        Context.AnswerDetails.Add(new AnswerDetail()
                        {
                            DateCreation = DateTime.Now,
                            IdAnswer = idanswer,
                            CopyNumber = 0,
                            StatusRegister = CStatusRegister.Active
                        });

                    }
                    
                   Context.SaveChanges();
                    transaction.Commit();

                }
                catch (Exception)
                {
                    transaction.Rollback();
                   
                }
            }
          
        }

        #endregion
        #region Preguntas dinamicas Harinas
        public  int saveDinamic(List<MytaskAnwerDetailSecondModel> _data)
        {
            using (var transaction = Context.Database.BeginTransaction())
            {
                try
                {
                    int _res = 0;
                    Mapper.Initialize(cfg =>
                    {
                        cfg.CreateMap<MytaskAnwerDetailSecondModel,  AnswerDetailSecondLevel> ();
                    });
                    var itemResult = new List<AnswerDetailSecondLevel>();
                    var Model = _data;
                    itemResult = Mapper.Map<List<AnswerDetailSecondLevel>>(Model);


                    Context.AnswerDetailSecondLevels.UpdateRange(itemResult);
                    Context.SaveChanges();
                    var _isfac = Context.AnswerDetails.Include(tb=>tb.Answer).Where(x => x.Id.Equals(Model.First().AnswerDetailId));
                    if (_isfac.Count() > 0) {

                        var idtask = _isfac.First().Answer.IdTask;
                        var idquestion = _isfac.First().Answer.IdQuestion;

                        var distintc = (from a in Context.Answers
                                        join b in Context.AnswerDetails on a.Id equals b.IdAnswer
                                        join c in Context.AnswerDetailSecondLevels on b.Id equals c.AnswerDetailId
                                        where a.IdTask.Equals(idtask) && a.Question.Equals(idquestion)
                                        select new {
                                            c.Factura
                                        })
                           .Distinct()   ;

                        var tasksmodel = Context.PollTasks.Where(x => x.idtask.Equals(idtask)).First();
                        if (distintc.Count() > 1)
                        {

                            
                            tasksmodel.novelty = null;
                            Context.PollTasks.Update(tasksmodel);
                            Context.SaveChanges();
                            _res = 2;
                        }
                        else {
                            if (distintc.First().Factura.Equals("no"))
                            {
                                tasksmodel.novelty = null;
                                Context.PollTasks.Update(tasksmodel);
                                Context.SaveChanges();
                                _res = 2;
                            }
                            else {
                                tasksmodel.novelty = "CON FACTURA";
                                Context.PollTasks.Update(tasksmodel);
                                Context.SaveChanges();
                                _res = 1;
                            }


                        }
                                     
                    
                    }
                  
                       

                    foreach (var _item in _data)
                    {


                        Context._AnswerDetailSecondLevelConcepts.RemoveRange(Context._AnswerDetailSecondLevelConcepts.Where(a => a.AnswerDetailSecondLevelid == _item.Id));
                        Context.SaveChanges();
                        foreach (var concept in _item.QuestionComplete) {

                            _AnswerDetailSecondLevelConcept _modelInsert = new _AnswerDetailSecondLevelConcept();
                            var _questionDetail = Context.QuestionDetails.Where(x => x.Id.Equals(Guid.Parse(concept)));
                            if (_questionDetail.Count() > 0) {
                                _modelInsert.IdquestionDetail = Guid.Parse(concept);
                                _modelInsert.AnswerDetailSecondLevelid = _item.Id;
                                _modelInsert.comment = _questionDetail.First().Answer;
                                _modelInsert.CodeConcept = _questionDetail.First().Idconcept;
                                Context._AnswerDetailSecondLevelConcepts.Add(_modelInsert);
                                Context.SaveChanges();


                            }

                        }

                    }

                    transaction.Commit();
                    return _res;
                }
                catch (Exception)
                {

                    transaction.Rollback();
                    return -1;
                }
            }

        }
        #endregion
        /*Crear Respuestas para gurdar informacion por seccion*/
        #region AnswerQuestion
        public void CrearAnswerQuestion(List<MyTaskViewAnswer> model, Guid idAccount, Guid IdMerchant, Guid idProfile, String fintransaccion, String Idtask, Guid status,string CodigoGemini, string comment)
        {
            Guid idtask = new Guid();
            foreach (var answerquestion in model)
            {

                if (answerquestion.estado == "P")
                {
                    try
                    {
                        var question = _questionDao.GetOne(answerquestion.Idquestion);

                        idtask = Guid.Parse(Idtask);
                        Idtask = idtask.ToString();
                        var answer = new Answer()
                        {
                            IdAccount = idAccount,
                            IdMerchant = IdMerchant,
                            IdQuestion = question.Id,
                            IdServiceDetail = question.IdServiceDetail,
                            IdTask = Guid.Parse(Idtask),
                            DateCreation = DateTime.Now,
                            StatusRegister = CStatusRegister.Active,
                            sequenceSection = 0
                        };
                        if (answerquestion.idAnswer != "")
                        {
                            if (Guid.Parse(answerquestion.idAnswer) != Guid.Empty)

                            {

                                answer.Id = Guid.Parse(answerquestion.idAnswer);
                            }
                            else
                            {
                                var answ = _answerDao.GetAnswerValueByQuestion(answerquestion.Idquestion, answerquestion.idTask, idAccount);
                                if (answ != null)
                                    answer.Id = answ.Id;


                                if (answ == null)
                                    answer = _answerDao.InsertOrUpdate(answer);
                            }

                        }



                        if (question.TypePoll.Code == CTypePoll.One)
                            CreateAnswerDetailQuestion(answer, question, Guid.Parse(answerquestion.AnswerQuestion), "");
                        if (question.TypePoll.Code == CTypePoll.Open)
                            CreateAnswerDetailQuestion(answer, question, Guid.Parse("00000000-0000-0000-0000-000000000000"), answerquestion.AnswerQuestion);
                        if (question.TypePoll.Code == CTypePoll.Many)
                            CreateAnswerDetailQuestionMany(answer, question, Guid.Parse(answerquestion.AnswerQuestion), answerquestion.AnswerQuestion);
                        answerquestion.idAnswer = answer.Id.ToString();
                        answerquestion.estado = "I";

                    }



                    catch (Exception ex)
                    {
                        answerquestion.idAnswer = "";
                        answerquestion.estado = "E";
                    }
                }
            }


            if (fintransaccion == "ok")
            {
                //         
                if (status == Guid.Parse("c30b7742-9775-4bc1-809b-74509d7dfe8a")) {

                    FinalizeTaskAnswerQuestionGemini(Guid.Parse(Idtask), idAccount, idProfile, IdMerchant, status, CodigoGemini);

                }
                else { 
                FinalizeTaskAnswerQuestion(Guid.Parse(Idtask), idAccount, idProfile, IdMerchant, status,  comment);
                   _taskCampaignDao.AccessTheWebAsync(Guid.Parse(Idtask), idAccount);

                }
            }

        }
        private void CreateAnswerDetailQuestion(Answer answer, Question question, Guid Idquestiondetail, String Answervalue)
        {
         
                Context.AnswerDetails.RemoveRange(Context.AnswerDetails.Where(a => a.Answer.Id == answer.Id));
            Context.SaveChanges();
            var answerDetail =
                new AnswerDetail()
                {
                    DateCreation = DateTime.Now,
                    IdAnswer = answer.Id,
                    CopyNumber = 0,
                    StatusRegister = CStatusRegister.Active
                };


            if (Idquestiondetail != Guid.Empty)
            {
                answerDetail.IdQuestionDetail = Idquestiondetail;
            }

            if (question.TypePoll.Code == CTypePoll.Open)
            {
                answerDetail.AnswerValue = Answervalue;
            }

            _answerDetailDao.InsertOrUpdate(answerDetail);
        }
        private void CreateAnswerDetailQuestionMany(Answer answer, Question question, Guid Idquestiondetail, String Answervalue)
        {

            using (var transaction = Context.Database.BeginTransaction())
            {
               
                Context.AnswerDetails.RemoveRange(Context.AnswerDetails.Where(a => a.Answer.Id == answer.Id && a.IdQuestionDetail == Idquestiondetail));
                var status = Context.SaveChanges();
                //if (Context.AnswerDetails.Where(a => a.Answer.Id == answer.Id ).Count() > 0) {
                //    Context.Answers.RemoveRange(Context.Answers.Where(a => a.Id == answer.Id));
                //    var status2 = Context.SaveChanges();
                //}
                if (status == 0)
                {
                    var answerDetail =
                        new AnswerDetail()
                        {
                            DateCreation = DateTime.Now,
                            IdAnswer = answer.Id,
                            CopyNumber = 0,
                            StatusRegister = CStatusRegister.Active
                        };


                    if (Idquestiondetail != Guid.Empty)
                    {
                        answerDetail.IdQuestionDetail = Idquestiondetail;
                    }

                    _answerDetailDao.InsertOrUpdate(answerDetail);
                }

            }
         
        }
        #endregion

        private void CreateAnswerDetail(Answer answer, MyTaskQuestionsViewModel question)
        {
            var answerDetail =
                new AnswerDetail()
                {
                    DateCreation = DateTime.Now,
                    IdAnswer = answer.Id,
                    CopyNumber = question.CopyNumber,
                    StatusRegister = CStatusRegister.Active
                };

            if (question.IdQuestionDetail != Guid.Empty)
            {
                answerDetail.IdQuestionDetail = question.IdQuestionDetail;
            }

            if (question.CodeTypePoll == CTypePoll.Open)
            {
                answerDetail.AnswerValue = question.Answer;
            }

            _answerDetailDao.InsertOrUpdate(answerDetail);
        }

        private Answer CreateAnswer(MyTaskViewModel model, Guid idAccount, MyTaskQuestionsViewModel question,
            MyTaskServicesDetailViewModel serviceDetail)
        {
            var answer = new Answer()
            {
                IdAccount = idAccount,
                IdMerchant = model.IdMerchant,
                IdQuestion = question.Id,
                IdServiceDetail = serviceDetail.Id,
                IdTask = model.IdTask,
                DateCreation = DateTime.Now,
                StatusRegister = CStatusRegister.Active
            };

            answer = _answerDao.InsertOrUpdate(answer);
            return answer;
        }

        private void CleanAnswers(MyTaskViewModel model)
        {
            Context.AnswerDetails.RemoveRange(Context.AnswerDetails.Where(a => a.Answer.IdTask == model.IdTask));
            Context.Answers.RemoveRange(Context.Answers.Where(a => a.IdTask == model.IdTask));
            Context.SaveChanges();
        }


        private void SaveBranchData(MyTaskViewModel model, Guid idAccount)
        {
            var branch = _branchDao.GetOne(model.IdBranch, idAccount);
            var person = _personDao.GetOne(branch.IdPersonOwner);

            branch = ConvertBranch.FromMyTaskViewModel(model, branch);
            person = ConvertPerson.FromMyTaskViewModel(model, person);

            _branchDao.InsertOrUpdate(branch);
            _personDao.InsertOrUpdate(person);
        }

        public bool AddSection(MyTaskViewModel model, Guid idAccount, Guid idProfile, Guid idUser, Guid idseccion,Guid status, string comment)
        {

            SaveAnsweredPoll(model, idAccount, idProfile, idUser,status,  comment);

            var sections = _serviceDetailTaskBusiness.GetSections(model.IdTask, idseccion, idAccount);

            if (!sections.Any())
            {
                _serviceDetailTaskBusiness.AddSection(idseccion, model.IdTask);
                //model.ServiceCollection.Add(_serviceDetailTaskBusiness.AddSection(idseccion, model.IdTask).ServiceDetail);
            }

            _serviceDetailTaskBusiness.AddSection(idseccion, model.IdTask);
            return true;
        }

        public List<MyTaskServicesViewModel> GetServiceListFromCampaign(Guid idCampaign, Guid idAccount, Guid idTask)
        {
            int numero_seccion = 0;
            var servicesParalel = new ConcurrentBag<MyTaskServicesViewModel>();
            var campaignServices = _redisCache.Get<List<MyTaskServicesViewModel>>("CampaignServices:" + idCampaign);
            string idseccion = "";



            if (campaignServices == null || idseccion != "")
            {
                var services = _campaignServicesDao.GetCampaignServicesByCampaign(idCampaign, idAccount);

                services.AsParallel()
                    .ForAll(s =>
                    {
                        servicesParalel.Add(new MyTaskServicesViewModel()
                        {
                            Code = s.Service.Code,
                            Id = s.Service.Id,
                            Name = s.Service.Name,
                            Template = s.Service.Template,
                            ServiceDetailCollection = GetSectionsFromService(s.IdService, idAccount, idTask)
                        });
                    });
                campaignServices = servicesParalel.ToList();

                _redisCache.Set("CampaignServices:" + idCampaign, campaignServices);
            }
            //agregar seccion si es dinamica
            if (campaignServices != null)
            {
                int camp = 0;
                foreach (var q in campaignServices)
                {
                    var s = q.ServiceDetailCollection.Where(x => x.IsDynamic == true).ToList();
                    if (s != null)
                    {
                        foreach (var a in s)
                        {
                            var preguntas = _questionDao.GetQuestion(a.Id).ToList();
                            numero_seccion = 0;

                            foreach (var qtarea in preguntas)
                            {
                                var respuestas = _answerDao.GetAnswerListByQuestionAccount(qtarea.Id, idAccount, idTask).ToList().Count;
                                if (respuestas > 1)
                                {
                                    numero_seccion = respuestas;
                                }

                            }
                            if (numero_seccion > 1)
                            {

                                int numero = 0;
                                foreach (var n in q.ServiceDetailCollection.Where(x => x.Id == a.Id).ToList())
                                {
                                    if (numero > 0)
                                    {
                                        q.ServiceDetailCollection.Remove(n);
                                    }
                                    else
                                    {
                                        foreach (var ques in n.QuestionCollection)
                                        {
                                            ques.sequence = 0;
                                        }
                                    }
                                    numero++;
                                }
                                numero = 0;
                                for (int numRep = 0; numRep < numero_seccion - 1; numRep++)
                                {

                                    MyTaskServicesDetailViewModel SeccionInsertar = new MyTaskServicesDetailViewModel();
                                    SeccionInsertar = GetSectionsFromServiceID(q.Id, idAccount, a.Id, numero + 1);
                                    q.ServiceDetailCollection.Insert(SeccionInsertar.Order, SeccionInsertar);
                                    numero++;
                                }
                            }
                            else
                            {
                                var num = q.ServiceDetailCollection.Where(x => x.Id == a.Id).ToList();
                                int numero = 0;
                                foreach (var n in q.ServiceDetailCollection.Where(x => x.Id == a.Id).ToList())
                                {
                                    if (numero > 0)
                                    {
                                        q.ServiceDetailCollection.Remove(n);
                                    }
                                    numero++;
                                }
                            }
                        }

                    }
                    campaignServices[camp].ServiceDetailCollection.OrderBy(x => x.Order).ToList();
                    camp++;
                }

            }
            return campaignServices;
        }
        public List<MyTaskServicesViewModel> GetServiceListFromCampaignGeo(Guid idCampaign, Guid idAccount)
        {
            var servicesParalel = new ConcurrentBag<MyTaskServicesViewModel>();
            var campaignServices = _redisCache.Get<List<MyTaskServicesViewModel>>("CampaignServices:" + idCampaign);

            if (campaignServices == null)
            {
                var services = _campaignServicesDao.GetCampaignServicesByCampaign(idCampaign, idAccount);

                services.AsParallel()
                    .ForAll(s =>
                    {
                        servicesParalel.Add(new MyTaskServicesViewModel()
                        {
                            Code = s.Service.Code,
                            Id = s.Service.Id,
                            Name = s.Service.Name,
                            Template = s.Service.Template,
                            ServiceDetailCollection = GetSectionsFromServiceGeo(s.IdService, idAccount)
                        });
                    });
                campaignServices = servicesParalel.ToList();

                _redisCache.Set("CampaignServices:" + idCampaign, campaignServices);
            }

            return campaignServices;
        }

        public List<MyTaskServicesDetailViewModel> GetSectionsFromService(Guid idService, Guid idAccount, Guid idTask)
        {
            var sections =
                Mapper.Map<List<MyTaskServicesDetailViewModel>>(_serviceDetailDao.GetServiceDetailsFromService(idService, idAccount, idTask));


            sections.AsParallel().ForAll(s =>
            {
                s.QuestionCollection = GetQuestionsFromSection(s.Id);
                s.Sections.AsParallel().ForAll(sc => sc.QuestionCollection = GetQuestionsFromSection(sc.Id));
            });

            return sections;
        }
        public MyTaskServicesDetailViewModel GetSectionsFromServiceID(Guid idService, Guid idAccount, Guid idservicedetail, int Orden)
        {
            var sections = Mapper.Map<MyTaskServicesDetailViewModel>(_serviceDetailDao.GetServiceDetailsFromServiceID(idService, idAccount, idservicedetail));


            if (sections != null)
            {
                sections.QuestionCollection = GetQuestionsFromSectionID(sections.Id, Orden);
                sections.Sections.AsParallel().ForAll(sc => sc.QuestionCollection = GetQuestionsFromSectionID(sc.Id, Orden));
            }
            return sections;
        }
        public List<MyTaskServicesDetailViewModel> GetSectionsFromServiceGeo(Guid idService, Guid idAccount)
        {
            var sections =
                Mapper.Map<List<MyTaskServicesDetailViewModel>>(_serviceDetailDao.GetServiceDetailsFromServiceGeo(idService, idAccount));


            sections.AsParallel().ForAll(s =>
            {
                s.QuestionCollection = GetQuestionsFromSection(s.Id);
                s.Sections.AsParallel().ForAll(sc => sc.QuestionCollection = GetQuestionsFromSection(sc.Id));
            });

            return sections;
        }

        /// <summary>
        /// Obtener Preguntas con sus respectivas opciones de respuestas mediante el código de la sección
        /// </summary>
        /// <param name="idSection">Identificador de la sección</param>
        /// <returns>Listado de preguntas con opciones de respuestas por Sección</returns>
        public List<MyTaskQuestionsViewModel> GetQuestionsFromSection(Guid idSection)
        {
            var questions = Mapper.Map<List<MyTaskQuestionsViewModel>>(_questionDao.GetCompleteQuestion(idSection).Result);

            /* foreach (var q in questions)
             {
                 q.sequence = 1;
                 resultado.Add(q);
             }*/

            return questions.OrderBy(q => q.Order).ToList();
        }
        public List<MyTaskQuestionsViewModel> GetQuestionsFromSectionID(Guid idSection, int orden)
        {
            var questions = Mapper.Map<List<MyTaskQuestionsViewModel>>(_questionDao.GetCompleteQuestion(idSection).Result);

            foreach (var q in questions)
            {
                q.sequence = orden;

            }

            return questions.OrderBy(q => q.Order).ToList();
        }
        #region Impresion

        public string PrintFile(Guid idtask, string path, Guid idaccount, string Img, String taskModel)
        {
            try
            {

                var task = _taskCampaignDao.Get(idtask, idaccount);
                var branchImge = _branchImageBusiness.GetBranchesImagesList(task.IdBranch, idaccount, task.IdCampaign);
                var branch = _branchDao.GetOne(task.IdBranch, idaccount);
                var person = _branchDao.GetOnePerson(Guid.Parse(branch.IdPersonOwner.ToString()));
                var model = JSonConvertUtil.Deserialize<MyTaskViewModel>(taskModel);
                #region variable de estilo

                var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
                var boldFont1 = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 11);
                var boldFont2 = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
                var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9);
                #endregion
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                string pathFull = path + "\\form\\" + idtask.ToString() + ".pdf";
                System.IO.FileStream fs = new FileStream(pathFull, FileMode.Create);

                Document document = new Document(PageSize.A4, 10, 10, 10, 10);
      
                PdfWriter writer = PdfWriter.GetInstance(document, fs);
                document.AddAuthor("Mardis Research");
                document.AddCreator("Mardis Research");
                document.AddKeywords("Mardis");
                document.AddSubject("Documentacion Pruebas");
                document.AddTitle("Documentacion Pruebas");
                document.AddHeader("Header", "Header Text");

                #region CuerpoPDF
                document.Open();

                // Cabecera
                var logo = iTextSharp.text.Image.GetInstance((path + "\\M_MARDIS.png"));
                logo.Alignment = Element.ALIGN_LEFT;
                logo.ScaleAbsoluteHeight(35);
                logo.ScaleAbsoluteWidth(35);

                PdfPTable tblCuerpo = new PdfPTable(1);
                PdfPCell clLogo = new PdfPCell(logo);
                clLogo.Border = 0;
                clLogo.HorizontalAlignment = Element.ALIGN_LEFT;
                PdfPCell clPieLogo = new PdfPCell(new Phrase("Mardis Research", FontFactory.GetFont("Arial", 10, 1)));
                clPieLogo.Border = 0;
                clPieLogo.HorizontalAlignment = Element.ALIGN_LEFT;
                tblCuerpo.AddCell(clLogo);
                tblCuerpo.AddCell(clPieLogo);

                // Escribimos el encabezamiento en el documento
                PdfPCell cltitulo = new PdfPCell(new Paragraph("Encuesta", boldFont2));
                cltitulo.Colspan = 2;
                cltitulo.Border = 0;
                cltitulo.PaddingTop = 10;
                cltitulo.PaddingBottom = 10;
                cltitulo.HorizontalAlignment = 1;
                tblCuerpo.AddCell(cltitulo);
                tblCuerpo.WidthPercentage = 100;
                iTextSharp.text.Font _standardFont = new iTextSharp.text.Font(iTextSharp.text.Font.FontFamily.HELVETICA, 9, iTextSharp.text.Font.NORMAL, BaseColor.BLACK);
                int i = 1;
                foreach (var section in model.ServiceCollection.First().ServiceDetailCollection)
                {
                    if (section.GroupName != "modal")
                    {
                   
                     
                        PdfPTable tblTituloSeccion = new PdfPTable(1);
                        tblTituloSeccion.WidthPercentage = 100;
                        PdfPCell clTituloSeccion = null;
                        if (section.IsDynamic)
                        {
                         
                            clTituloSeccion = new PdfPCell(new Paragraph(section.SectionTitle + " #" + i,boldFont1));
                            i++;
                        }
                        else
                        {
                            clTituloSeccion = new PdfPCell(new Paragraph(section.SectionTitle, boldFont1));
                        }
                        clTituloSeccion.Colspan = 2;
                        clTituloSeccion.Border = 0;
                        clTituloSeccion.HorizontalAlignment = Element.ALIGN_CENTER;
                        tblTituloSeccion.AddCell(clTituloSeccion);
                        tblCuerpo.AddCell(tblTituloSeccion);
                        var seccionnumber = section.QuestionCollection.Count();
                        var tamtable = seccionnumber <= 3 ? seccionnumber : Math.Abs(seccionnumber / 3);
                        PdfPTable tblPreguntas = new PdfPTable(tamtable);
                        
                        var clmarca = new PdfPCell(new Phrase());
                        var clTablaMarca = new PdfPCell(new Phrase());
                        var tblMarca = new PdfPTable(1);
                        var tblrespuestaMarca = new PdfPTable(1);
                        int preguntaCont = 0;
                        int j = 0;
                        var aux = 0;
                        int preguntaslista = section.QuestionCollection.Count();
                        
                        foreach (var question in section.QuestionCollection)
                        {
                            j++;
                            aux++;
                            bool banderaMarcas = false;
                            PdfPCell clpregunta = null;
                            var phrase = new Phrase();
                            // Configuramos el título de las columnas de la tabla
                            if ((question.CodeTypePoll == "ONE" || question.CodeTypePoll == "OPEN") && question.Weight != -1)
                            {
                                if (question.CodeTypePoll == "OPEN")
                                {
                                    phrase.Add(new Chunk(question.Title + ": ", boldFont));
                                    phrase.Add(new Chunk(question.Answer, _standardFont));
                                    clpregunta = new PdfPCell(phrase);
                                    clpregunta.BorderWidth = 0;
                                    clpregunta.PaddingTop = 10;
                                    clpregunta.PaddingBottom = 10;
              
                                }
                                else
                                {
                                    var questiondetail = question.IdQuestionDetail.Equals(Guid.Empty) ? " " : question.QuestionDetailCollection.Where(x => x.Id.Equals(question.IdQuestionDetail)).ToList().First().Answer;


                                    phrase.Add(new Chunk(question.Title + ": ", boldFont));
                                    phrase.Add(new Chunk(questiondetail, _standardFont));
                                    clpregunta = new PdfPCell(phrase);
                                    clpregunta.BorderWidth = 0;
                                    clpregunta.PaddingTop = 10;
                                    clpregunta.PaddingBottom = 10;
                          
                                }
                                tblPreguntas.AddCell(clpregunta);
                                preguntaCont++;
                            }
                            else if ((question.CodeTypePoll == "MANY") && (question.HasPhoto == "N" || question.HasPhoto == "L"))
                            {
                                var pregunta = question.Title;
                                var respuestas = "";
                                int cont = 0;
                                foreach (var answer in question.QuestionDetailCollection)
                                {
                                    if (cont == 0)
                                    {
                                        respuestas = answer.Answer;
                                    }
                                    respuestas = respuestas + " " + answer.Answer;
                                }
                                phrase.Add(new Chunk(question.Title + ": ", boldFont));
                                phrase.Add(new Chunk(respuestas, _standardFont));
                                clpregunta = new PdfPCell(phrase);
                                clpregunta.PaddingTop = 10;
                                clpregunta.PaddingBottom = 10;
                                clpregunta.BorderWidth = 0;

                                tblPreguntas.AddCell(clpregunta);
                                preguntaCont++;
                            }
                            else if (question.CodeTypePoll == "MANY" && question.HasPhoto == "D")
                            {
                                clpregunta = new PdfPCell(new Phrase("Marcas"));
                                clpregunta.HorizontalAlignment = Element.ALIGN_CENTER;

                                tblMarca = new PdfPTable(7);
                                tblMarca.WidthPercentage = 100;

                                PdfPCell clHarina = new PdfPCell(new Phrase("Harina", boldFont));
                                clHarina.BorderWidth = 0;
                                clHarina.BorderWidthBottom = 0.75f;
                                PdfPCell clPrecioFact = new PdfPCell(new Phrase("Fact?", boldFont));
                                clPrecioFact.BorderWidth = 0;
                                clPrecioFact.BorderWidthBottom = 0.75f;
                                PdfPCell clPeso = new PdfPCell(new Phrase("Peso(kg)", boldFont));
                                clPeso.BorderWidth = 0;
                                clPeso.BorderWidthBottom = 0.75f;
                                PdfPCell clPrecio = new PdfPCell(new Phrase("Precio($)", boldFont));
                                clPrecio.BorderWidth = 0;
                                clPrecio.BorderWidthBottom = 0.75f;
                                PdfPCell clDescuento = new PdfPCell(new Phrase("Desc?", boldFont));
                                clDescuento.BorderWidth = 0;
                                clDescuento.BorderWidthBottom = 0.75f;
                                PdfPCell clValorDescuento = new PdfPCell(new Phrase("Desc(%)", boldFont));
                                clValorDescuento.BorderWidth = 0;
                                clValorDescuento.BorderWidthBottom = 0.75f;
                                PdfPCell clReqDescuento = new PdfPCell(new Phrase("Req. Desc", boldFont));
                                clReqDescuento.BorderWidth = 0;
                                clReqDescuento.BorderWidthBottom = 0.75f;

                                tblMarca.AddCell(clHarina);
                                tblMarca.AddCell(clPrecioFact);
                                tblMarca.AddCell(clPeso);
                                tblMarca.AddCell(clPrecio);
                                tblMarca.AddCell(clDescuento);
                                tblMarca.AddCell(clValorDescuento);
                                tblMarca.AddCell(clReqDescuento);

                                foreach (var answer in question.QuestionDetailCollection)
                                {
                                    if (answer.Checked)
                                    {
                                        var marcas = answer.AnwerDetailSecondModel;
                                        foreach (var marca in marcas)
                                        {
                                            clHarina = new PdfPCell(new Phrase(marca.Marca, _standardFont));
                                            clHarina.BorderWidth = 0;
                                            clPrecioFact = new PdfPCell(new Phrase(marca.Factura, _standardFont));
                                            clPrecioFact.BorderWidth = 0;
                                            clPeso = new PdfPCell(new Phrase(marca.Peso == null ? "0" : marca.Peso.ToString(), _standardFont));
                                            clPeso.BorderWidth = 0;
                                            clPrecio = new PdfPCell(new Phrase(marca.PrecioSaco == null ? "0" : marca.PrecioSaco.ToString(), _standardFont));
                                            clPrecio.BorderWidth = 0;
                                            clDescuento = new PdfPCell(new Phrase(marca.Descuento, _standardFont));
                                            clDescuento.BorderWidth = 0;
                                            clValorDescuento = new PdfPCell(new Phrase(marca.ValorDescuento == null ? "0" : marca.ValorDescuento.ToString(), _standardFont));
                                            clValorDescuento.BorderWidth = 0;
                                            clReqDescuento = new PdfPCell(new Phrase(marca.RequisitosDescuento, _standardFont));
                                            clReqDescuento.BorderWidth = 0;

                                            tblMarca.AddCell(clHarina);
                                            tblMarca.AddCell(clPrecioFact);
                                            tblMarca.AddCell(clPeso);
                                            tblMarca.AddCell(clPrecio);
                                            tblMarca.AddCell(clDescuento);
                                            tblMarca.AddCell(clValorDescuento);
                                            tblMarca.AddCell(clReqDescuento);

                                        }
                                    }
                                }
                                clTablaMarca = new PdfPCell(tblMarca);
                                banderaMarcas = true;
                            }
                            if (aux == seccionnumber) {

                                for (int xi = 0; xi < (tamtable - preguntaCont); xi++) {
                                    clpregunta = new PdfPCell(new Phrase("", _standardFont));
                                    clpregunta.BorderWidth = 0;
                                    tblPreguntas.AddCell(clpregunta);
                                }
                                banderaMarcas = true;
                            }
                            if (preguntaCont == tamtable || banderaMarcas) //|| j == section.QuestionCollection.Count())
                            {
                                if (banderaMarcas)
                                {
                                    tblCuerpo.AddCell(tblPreguntas);
                                    tblCuerpo.AddCell(clpregunta);
                                    tblCuerpo.AddCell(clTablaMarca);
                                    tblPreguntas = new PdfPTable(tamtable);
                                    preguntaCont = 0;
                                }
                                else //if(preguntaCont < 3 && j == section.QuestionCollection.Count())
                                {
                                    tblCuerpo.AddCell(tblPreguntas);
                                    tblPreguntas = new PdfPTable(tamtable);
                                    preguntaCont = 0;
                                }
                            }
                        }
                        if (j > 0)
                        {
                            tblCuerpo.AddCell(tblPreguntas);
                        }
                    }
                }
                document.Add(tblCuerpo);
                document.Add(Chunk.NEWLINE);
                PdfPTable tbUB2 = new PdfPTable(1);
                PdfPCell cell22 = new PdfPCell(new Phrase("FOTOS", boldFont2));
                cell22.Colspan = 2;
                cell22.Border = 0;
                cell22.PaddingBottom = 20;
                cell22.PaddingTop = 50;
                cell22.HorizontalAlignment = Element.ALIGN_LEFT;
                tbUB2.AddCell(cell22);
                document.Add(tbUB2);
                PdfPTable tbImge = new PdfPTable(1);
                if (branchImge.ToList().Count() > 0)
                {
                    foreach (var item in branchImge.ToList().OrderBy(x => x.Order))
                    {
                        var img = iTextSharp.text.Image.GetInstance((item.UrlImage));
                        PdfPCell imageCell = new PdfPCell(img);


                        img.Alignment = 1;
                        img.ScaleAbsoluteHeight(400);
                        img.ScaleAbsoluteWidth(500);
                        imageCell.HorizontalAlignment = 1;
                        imageCell.VerticalAlignment = 1;
                        imageCell.PaddingBottom = 10f;
                        imageCell.Border = 0;
                        tbImge.AddCell(imageCell);






                    }
                }
                document.Add(tbImge);
                PdfPTable tbUB = new PdfPTable(1);
                PdfPCell cell2 = new PdfPCell(new Phrase("UBICACIÓN", boldFont2));
                cell2.Colspan = 2;
                cell2.Border = 0;
                cell2.PaddingBottom = 15f;
                cell2.HorizontalAlignment = Element.ALIGN_LEFT;
                tbUB.AddCell(cell2);
                document.Add(tbUB);
                var logos = iTextSharp.text.Image.GetInstance(("https://maps.googleapis.com/maps/api/staticmap?zoom=16&size=800x700&maptype=roadmap&markers=color:red%7Clabel:C%7C" + branch.LatitudeBranch + "," + branch.LenghtBranch + "&key=AIzaSyDC0qg4xC1qSUey6eFuhzuA1fJ2ZPFkO84"));
                logos.Alignment = 1;
                logos.ScaleAbsoluteHeight(300);
                logos.ScaleAbsoluteWidth(300);
                document.Add(logos);
                PdfPTable footerTbl = new PdfPTable(1);
                footerTbl.TotalWidth = document.PageSize.Width;



                //numero de la page

                //Chunk myFooter = new Chunk("Página " + (document.PageNumber), FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8, grey));
                //PdfPCell footer = new PdfPCell(new Phrase(myFooter));
                //footer.Border = iTextSharp.text.Rectangle.NO_BORDER;
                //footer.HorizontalAlignment = Element.ALIGN_CENTER;
                //footerTbl.AddCell(footer);


                footerTbl.WriteSelectedRows(0, -1, 0, (document.BottomMargin + 80), writer.DirectContent);

                document.Close();


                writer.Close();
                fs.Close();
                //System.IO.FileStream fs2 = fs;

                //MemoryStream memStream = new MemoryStream();
                //using (FileStream fss = File.Open(pathFull, FileMode.Open))
                //{

                //    fss.Position = 0;
                //    fss.CopyTo(memStream);

                //}
                byte[] by2tes = System.IO.File.ReadAllBytes(pathFull);
                File.WriteAllBytes("myfile.pdf", by2tes);

                MemoryStream stream = new MemoryStream(by2tes);
                AzureStorageUtil.UploadFromStream(stream, "evidencias", branch.Code + "_" + branch.Name + ".pdf").Wait();
                var uri = AzureStorageUtil.GetUriFromBlob("evidencias", branch.Code + "_" + branch.Name + ".pdf");
                // loading bytes from a file is very easy in C#. The built in System.IO.File.ReadAll* methods take care of making sure every byte is read properly.
                if (File.Exists(pathFull))
                {
                    File.Delete(pathFull);
                }
                return uri;
            }
            catch (Exception ex)
            {

                return "";
            }

            #endregion


        }
        public byte[] ReadPDF(string filePath)
        {
            byte[] buffer;
            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            try
            {
                int length = (int)fileStream.Length;  // get file length
                buffer = new byte[length];            // create buffer
                int count;                            // actual number of bytes read
                int sum = 0;                          // total number of bytes read

                // read until Read method returns 0 (end of the stream has been reached)
                while ((count = fileStream.Read(buffer, sum, length - sum)) > 0)
                    sum += count;  // sum is a buffer offset for next reading
            }
            finally
            {
                fileStream.Close();
            }
            return buffer;
        }
  
        //public string PrintFile(Guid idtask, string path, Guid idaccount, string Img) {
        //    try
        //    {

        //        var task = _taskCampaignDao.Get(idtask, idaccount);
        //        var branchImge = _branchImageBusiness.GetBranchesImagesList(task.IdBranch, idaccount,task.IdCampaign);
        //        var branch = _branchDao.GetOne(task.IdBranch, idaccount);
        //        var person = _branchDao.GetOnePerson(Guid.Parse(branch.IdPersonOwner.ToString()));
        //        #region variable de estilo

        //        var normalFont = FontFactory.GetFont(FontFactory.HELVETICA, 12);
        //        var boldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);

        //        #endregion
        //        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        //        string pathFull = path + "\\form\\" + idtask.ToString() + ".pdf";
        //        System.IO.FileStream fs = new FileStream(pathFull, FileMode.Create);
        //        Document document = new Document(PageSize.A4, 10, 10, 10, 10);

        //        PdfWriter writer = PdfWriter.GetInstance(document, fs);
        //        document.AddAuthor("Mardis Research");
        //        document.AddCreator("Mardis Research");
        //        document.AddKeywords("Mardis");
        //        document.AddSubject("Documentacion Pruebas");
        //        document.AddTitle("Documentacion Pruebas");
        //        document.AddHeader("Header", "Header Text");
        //        document.Open();
        //        #region CuerpoPDF

        //        var logo = iTextSharp.text.Image.GetInstance((path + "\\M_MARDIS.png"));
        //        logo.Alignment = 1;
        //        logo.ScaleAbsoluteHeight(55);
        //        logo.ScaleAbsoluteWidth(55);
        //        document.Add(logo);

        //        // Cabecera
        //        PdfPTable table = new PdfPTable(2);
        //        //actual width of table in points
        //        table.TotalWidth = 216f;
        //        table.LockedWidth = true;

        //        float[] widths = new float[] { 1f, 2f };
        //        table.SetWidths(widths);
        //        table.HorizontalAlignment = 1;
        //        //leave a gap before and after the table
        //        table.SpacingBefore = 20f;
        //        table.SpacingAfter = 30f;
        //        PdfPCell cell = new PdfPCell(new Phrase("Mardis Research", FontFactory.GetFont("Arial", 10, 1)));
        //        PdfPCell cell1 = new PdfPCell(new Phrase("Documentación Engine"));
        //        cell.Colspan = 2;
        //        cell.Border = 0;
        //        cell.HorizontalAlignment = 1;
        //        cell1.Colspan = 2;
        //        cell1.Border = 0;
        //        cell1.HorizontalAlignment = 1;
        //        table.AddCell(cell);
        //        table.AddCell(cell1);
        //        table.HorizontalAlignment = 1;
        //        document.Add(table);
        //        float[] columnWidths = { 3, 5 , 3, 5 , 3, 5 };
        //        //PdfPTable tbDatos = new PdfPTable(columnWidths);
        //        //tbDatos.AddCell(new PdfPCell(new Phrase("Codigo :", FontFactory.GetFont("Arial", 10, 1)))
        //        //{
        //        //    Border = 0,
        //        //    HorizontalAlignment = Element.ALIGN_LEFT,
        //        //    PaddingBottom = 10f
        //        //});
        //        //tbDatos.AddCell(new PdfPCell(new Phrase( branch.ExternalCode, FontFactory.GetFont("Arial", 9, 0)))
        //        //{
        //        //    Border = 0,
        //        //    HorizontalAlignment = Element.ALIGN_LEFT,
        //        //    PaddingBottom = 10f
        //        //});
        //        ////   tbDatos.AddCell(new PdfPCell(new Phrase()) { Border = 0, HorizontalAlignment = Element.ALIGN_LEFT, PaddingBottom = 40f });
        //        //tbDatos.AddCell(new PdfPCell(new Phrase("Local :", FontFactory.GetFont("Arial", 10, 1)))
        //        //{
        //        //    Border = 0,
        //        //    HorizontalAlignment = Element.ALIGN_LEFT,
        //        //    PaddingBottom = 10f
        //        //});
        //        //tbDatos.AddCell(new PdfPCell(new Phrase( branch.Name, FontFactory.GetFont("Arial", 9, 0)))
        //        //{
        //        //    Border = 0,
        //        //    HorizontalAlignment = Element.ALIGN_LEFT,
        //        //    PaddingBottom = 10f
        //        //});
        //        //tbDatos.AddCell(new PdfPCell(new Phrase("Dirección :", FontFactory.GetFont("Arial", 10, 1)))
        //        //{
        //        //    Border = 0,
        //        //    HorizontalAlignment = Element.ALIGN_LEFT,
        //        //    PaddingBottom = 10f
        //        //});
        //        //tbDatos.AddCell(new PdfPCell(new Phrase(branch.MainStreet, FontFactory.GetFont("Arial", 9, 0)))
        //        //{
        //        //    Border = 0,
        //        //    HorizontalAlignment = Element.ALIGN_LEFT,
        //        //    PaddingBottom = 10f
        //        //});
        //        //tbDatos.AddCell(new PdfPCell(new Phrase("Cliente :", FontFactory.GetFont("Arial", 10, 1)))
        //        //{
        //        //    Border = 0,
        //        //    HorizontalAlignment = Element.ALIGN_LEFT,
        //        //    PaddingBottom = 30f
        //        //});
        //        //tbDatos.AddCell(new PdfPCell(new Phrase(person.Name+" "+ person.SurName, FontFactory.GetFont("Arial", 9, 0)))
        //        //{
        //        //    Border = 0,
        //        //    HorizontalAlignment = Element.ALIGN_LEFT,
        //        //    PaddingBottom = 30f
        //        //});
        //        //tbDatos.AddCell(new PdfPCell(new Phrase("Ruc :", FontFactory.GetFont("Arial", 10, 1)))
        //        //{
        //        //    Border = 0,
        //        //    HorizontalAlignment = Element.ALIGN_LEFT,
        //        //    PaddingBottom = 30f
        //        //});
        //        //tbDatos.AddCell(new PdfPCell(new Phrase(person.Document, FontFactory.GetFont("Arial", 9, 0)))
        //        //{
        //        //    Border = 0,
        //        //    HorizontalAlignment = Element.ALIGN_LEFT,
        //        //    PaddingBottom = 30f
        //        //});
        //        //tbDatos.AddCell(new PdfPCell(new Phrase("", FontFactory.GetFont("Arial", 10, 0)))
        //        //{
        //        //    Border = 0,
        //        //    HorizontalAlignment = Element.ALIGN_LEFT,
        //        //    PaddingBottom = 30f
        //        //});
        //        //tbDatos.AddCell(new PdfPCell(new Phrase("" , FontFactory.GetFont("Arial", 10, 0)))
        //        //{
        //        //    Border = 0,
        //        //    HorizontalAlignment = Element.ALIGN_LEFT,
        //        //    PaddingBottom = 30f
        //        //});
        //        ////   tbDatos.AddCell(new PdfPCell(new Phrase()) { Border = 0, HorizontalAlignment = Element.ALIGN_LEFT, PaddingBottom = 40f });

        //        //document.Add(tbDatos);

        //        PdfPTable tbUB2 = new PdfPTable(1);
        //        PdfPCell cell22 = new PdfPCell(new Phrase("Fotos", boldFont));
        //        cell22.Colspan = 2;
        //        cell22.Border = 0;
        //        cell22.PaddingBottom = 15f;
        //        cell22.HorizontalAlignment = Element.ALIGN_LEFT;
        //        tbUB2.AddCell(cell22);
        //        document.Add(tbUB2);
        //        var base64Data = Regex.Match(Img, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;

        //        byte[] imageBytes = Convert.FromBase64String(base64Data);

        //        var encuesta = iTextSharp.text.Image.GetInstance((imageBytes));
        //        encuesta.Alignment = 1;
        //        encuesta.ScaleAbsoluteHeight(850);
        //        encuesta.ScaleAbsoluteWidth(600);
        //        document.Add(encuesta);

        //        PdfPTable tbImge = new PdfPTable(1);
        //        PdfPTable tbImge1 = new PdfPTable(2);
        //        if (branchImge.ToList().Count() > 0) { 
        //        foreach (var item in branchImge.ToList().OrderBy(x => x.Order))
        //        {
        //        var    img = iTextSharp.text.Image.GetInstance((item.UrlImage));
        //            PdfPCell imageCell = new PdfPCell(img);


        //                    img.Alignment = 1;
        //                    img.ScaleAbsoluteHeight(180);
        //                    img.ScaleAbsoluteWidth(220);
        //                    imageCell.HorizontalAlignment = 1;
        //                    imageCell.VerticalAlignment = 1;
        //                    imageCell.PaddingBottom = 10f;
        //                    imageCell.Border = 0;
        //                      tbImge.AddCell(imageCell);






        //        }
        //        }
        //        document.Add(tbImge);
        //        //if (branchImge.Count() % 2 != 0)
        //        //{
        //        //    PdfPCell imageCell = new PdfPCell();
        //        //    imageCell.HorizontalAlignment = 1;
        //        //    imageCell.VerticalAlignment = 1;
        //        //    imageCell.PaddingBottom = 10f;
        //        //    imageCell.Border = 0;
        //        //    tbImge.AddCell(imageCell);
        //        //}

        //        PdfPTable tbUB = new PdfPTable(1);
        //        PdfPCell cell2 = new PdfPCell(new Phrase("UBICACIÓN", boldFont));
        //        cell2.Colspan = 2;
        //        cell2.Border = 0;
        //        cell2.PaddingBottom = 15f;
        //        cell2.HorizontalAlignment = Element.ALIGN_LEFT;
        //        tbUB.AddCell(cell2);
        //        document.Add(tbUB);
        //        var logos = iTextSharp.text.Image.GetInstance(("https://maps.googleapis.com/maps/api/staticmap?zoom=16&size=800x700&maptype=roadmap&markers=color:red%7Clabel:C%7C" + branch.LatitudeBranch + "," + branch.LenghtBranch + "&key=AIzaSyDC0qg4xC1qSUey6eFuhzuA1fJ2ZPFkO84"));
        //        logos.Alignment = 1;
        //        logos.ScaleAbsoluteHeight(300);
        //        logos.ScaleAbsoluteWidth(300);
        //        document.Add(logos);
        //        BaseColor grey = new BaseColor(128, 128, 128);
        //        iTextSharp.text.Font font = FontFactory.GetFont("Arial", 9, iTextSharp.text.Font.NORMAL, grey);
        //        //tbl footer
        //        PdfPTable footerTbl = new PdfPTable(1);
        //        footerTbl.TotalWidth = document.PageSize.Width;



        //        //numero de la page

        //        //Chunk myFooter = new Chunk("Página " + (document.PageNumber), FontFactory.GetFont(FontFactory.HELVETICA_OBLIQUE, 8, grey));
        //        //PdfPCell footer = new PdfPCell(new Phrase(myFooter));
        //        //footer.Border = iTextSharp.text.Rectangle.NO_BORDER;
        //        //footer.HorizontalAlignment = Element.ALIGN_CENTER;
        //        //footerTbl.AddCell(footer);


        //        footerTbl.WriteSelectedRows(0, -1, 0, (document.BottomMargin + 80), writer.DirectContent);

        //        document.Close();


        //        writer.Close();
        //        fs.Close();
        //        //System.IO.FileStream fs2 = fs;

        //        //MemoryStream memStream = new MemoryStream();
        //        //using (FileStream fss = File.Open(pathFull, FileMode.Open))
        //        //{

        //        //    fss.Position = 0;
        //        //    fss.CopyTo(memStream);

        //        //}
        //        byte[] by2tes = System.IO.File.ReadAllBytes(pathFull);
        //        File.WriteAllBytes("myfile.pdf", by2tes);

        //        MemoryStream stream = new MemoryStream(by2tes);
        //        AzureStorageUtil.UploadFromStream(stream, "evidencias", branch.Code+"_"+branch.Name + ".pdf").Wait();
        //        var uri = AzureStorageUtil.GetUriFromBlob("evidencias", branch.Code +"_"+ branch.Name+".pdf");
        //        // loading bytes from a file is very easy in C#. The built in System.IO.File.ReadAll* methods take care of making sure every byte is read properly.
        //        if (File.Exists(pathFull))
        //        {
        //            File.Delete(pathFull);
        //        }
        //        return uri;
        //    }
        //    catch (Exception ex)
        //    {

        //        return "";
        //    }

        //    #endregion


        //}
     
        #endregion
        #region Rutas
        public IList<TaskMigrateResultViewModel> taskMigrate(string fileBrachMassive, Guid idAccount, Guid idcampaing , string status)
        {
            int j = 0;
            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(fileBrachMassive, false))
            {

                //Read the first Sheets 
                Sheet sheet = doc.WorkbookPart.Workbook.Sheets.GetFirstChild<Sheet>();
                Worksheet worksheet = (doc.WorkbookPart.GetPartById(sheet.Id.Value) as WorksheetPart).Worksheet;
                IEnumerable<Row> rows = worksheet.GetFirstChild<SheetData>().Descendants<Row>();
                Branch BranchModel=new Branch();
                foreach (Row row in rows)
                {
                    j++;
                    //Read the first row as header
                    if (row.RowIndex.Value != 1)
                    {

                       
                        int i = 0;
                        if (row.Descendants<Cell>().Count() >= 17)
                        {
                            foreach (Cell cell in row.Descendants<Cell>())
                            {

                                try
                                {
                                    i++;
                                    switch (i)
                                    {
                                        case 1:
                                            BranchModel = _branchMigrateDao.GetLocal(GetCellValue(doc, cell), idAccount);
                                            BranchModel.routeDate = DateTime.Now;
                                            if(BranchModel.Id== Guid.Parse("00000000-0000-0000-0000-000000000000"))
                                            {
                                                BranchModel.Code = GetCellValue(doc, cell);
                                             //   BranchModel.ExternalCode = BranchModel.Code;
                                                BranchModel.PersonOwner.Code = GetCellValue(doc, cell);
                                                BranchModel.IdAccount = idAccount;
                                                BranchModel.PersonOwner.IdAccount = idAccount;
                                                BranchModel.IdCountry = Guid.Parse("BE7CF5FF-296B-464D-82FA-EF0B4F48721B");// Pais ecuador
                                                BranchModel.IsAdministratorOwner = "SI";
                                                BranchModel.Zone = "-";
                                                BranchModel.Neighborhood = "-";
                                          
                                                BranchModel.NumberBranch = "-";
                                                BranchModel.SecundaryStreet = "-";
                                                BranchModel.StatusRegister = "A";
                                                BranchModel.PersonOwner.SurName = "-";
                                                BranchModel.PersonOwner.TypeDocument = "CI";
                                                BranchModel.PersonOwner.StatusRegister = "A";
                                                Isval(BranchModel.Code, 1, j);
                                            }
                                            BranchModel.ESTADOAGGREGATE = "S";
                                            break;
                                        case 2:
                                            BranchModel.ExternalCode = GetCellValue(doc, cell);
                                            break;
                                        case 3:
                                            BranchModel.TypeBusiness = GetCellValue(doc, cell);
                                            break;
                                        case 4:
                                            BranchModel.Name = GetCellValue(doc, cell);
                                            break;
                                        case 5:
                                            BranchModel.MainStreet = GetCellValue(doc, cell);
                                            break;
                                        case 6:
                                            BranchModel.Reference = GetCellValue(doc, cell);
                                            break;
                                        case 7:
                                            BranchModel.PersonOwner.Name = GetCellValue(doc, cell);
                                            BranchModel.Label = BranchModel.PersonOwner.Name;
                                            break;
                                        case 8:
                                            BranchModel.PersonOwner.Document = GetCellValue(doc, cell);
                                            break;
                                        case 9:
                                            BranchModel.PersonOwner.Phone = GetCellValue(doc, cell);
                                            break;
                                        case 10:
                                            BranchModel.PersonOwner.Mobile = GetCellValue(doc, cell);
                                            break;
                                        case 11:
                                            var latitud = GetCellValue(doc, cell);
                                            var lat = latitud.Contains("E") ? ExponecialToString(latitud) : latitud;



                                            BranchModel.LatitudeBranch = lat.Length <= 10 ? lat : lat.Substring(0, 11);
                                            break;
                                        case 12:
                                            var longitud = GetCellValue(doc, cell);
                                            string len = longitud.Contains("E") ? ExponecialToString(longitud) : longitud;
                                            BranchModel.LenghtBranch = len.Length <= 10 ? len : len.Substring(0, 11);

                                            break;
                                        case 13:
                                            BranchModel.IdProvince = _branchMigrateDao.GetProviceByName(GetCellValue(doc, cell));
                                            Isval(BranchModel.IdProvince.ToString(), 4, j);
                                            break;
                                        case 14:
                                            BranchModel.IdDistrict = _branchMigrateDao.GetDistrictByName(GetCellValue(doc, cell), BranchModel.IdProvince);
                                            ValidoCodigo(BranchModel.Code, idAccount, BranchModel.IdDistrict, j);
                                            Isval(BranchModel.IdDistrict.ToString(), 5, j);
                                            break;
                                        case 15:
                                            BranchModel.IdParish = _branchMigrateDao.GetParishByName(GetCellValue(doc, cell), BranchModel.IdDistrict);
                                            Isval(BranchModel.IdParish.ToString(), 6, j);
                                            break;
                                        case 16:
                                            BranchModel.Cluster= GetCellValue(doc, cell);
                                            BranchModel.IdSector = _branchMigrateDao.GetSectorByName("CENTRO", BranchModel.IdDistrict);
                                            Isval(BranchModel.IdSector.ToString(), 7, j);
                                            break;
                                        case 17:
                                            BranchModel.RUTAAGGREGATE = GetCellValue(doc, cell);
                                            break;
                                        case 18:
                                            BranchModel.IMEI_ID = GetCellValue(doc, cell);

                                            break;

                                    }
                                }
                                catch (Exception e)
                                {

                                    var ex = e.Message.ToString();

                                    int ne = -1;

                                    switch (ex)
                                    {
                                        case "Error al consultar Cuidad":
                                            ne = 2;
                                            break;
                                        case "Error al consultar Parroquias":
                                            ne = 3;
                                            break;
                                        case "Error al consultar Sectores":
                                            ne = 4;
                                            break;

                                    }

                                }

                            }
                            if (row.RowIndex.Value != 1)
                            {
                                lsBranch.Add(BranchModel);
                            }
                        }
                        else {

                            lstTaskResult.Add(new TaskMigrateResultViewModel { description = "Existen Columnas vacias(Todas las columnas debe tener INFORMACION )", line = j, type = "E" });
                        }
                    }

                }


            }
            // 
            IList<TaskMigrateResultViewModel> result = new List<TaskMigrateResultViewModel>();
            int numberError = lstTaskResult.Where(x => x.type == "E").Count();
            if (numberError < 1)
            {
                if (status.Equals("2"))
                {

                    var regCarga = lsBranch.Select(z => z.Code).Distinct().Count();
                    if (regCarga == (j - 1))
                    {
                        if (_branchMigrateDao.SaveBranchMigrate(lsBranch, idAccount, idcampaing))
                        {
                            result.Add(new TaskMigrateResultViewModel { description = "Locales Cargados", Element = (j - 1).ToString() });
                            result.Add(new TaskMigrateResultViewModel { description = "Errores", Element = "0" });
                        }
                        else
                        {
                            result.Add(new TaskMigrateResultViewModel { description = "Errores", Element = "NA" });
                            result.Add(new TaskMigrateResultViewModel { description = "No se actualizo la información volver", Element = (j - 1).ToString() });

                        }
                    }
                    else
                    {
                        result.Add(new TaskMigrateResultViewModel { description = "Registros verificados", Element = (j - 1).ToString() });
                        result.Add(new TaskMigrateResultViewModel { description = "Existen codigos duplicados en el Documento. Favor verificar y volver a cargar", Element = ((j - 1) - regCarga).ToString() });
                    }
                    if (File.Exists(fileBrachMassive))
                    {
                        File.Delete(fileBrachMassive);
                    }

                }
                else
                {
                    var regCarga = lsBranch.Select(z => z.Code).Distinct().Count();
                    if (regCarga == (j - 1))
                    {

                        result.Add(new TaskMigrateResultViewModel { description = "Registro verificados", Element = (j - 1).ToString() });
                        result.Add(new TaskMigrateResultViewModel { description = "Errores", Element = "0" });
                    }
                    else
                    {
                        result.Add(new TaskMigrateResultViewModel { description = "Registros verificados", Element = (j - 1).ToString() });
                        result.Add(new TaskMigrateResultViewModel { description = "Existen codigos duplicados en el Documento. Favor verificar y volver a cargar", Element = ((j - 1) - regCarga).ToString() });
                        result.Add(new TaskMigrateResultViewModel { description = "Errores", Element = "0" });
                    }
                }
            }
            else
            {
             string url=   urlError(lstTaskResult, fileBrachMassive);
                result.Add(new TaskMigrateResultViewModel { description = "Errores", Element = numberError.ToString(), Code= url });
                result.Add(new TaskMigrateResultViewModel { description = "Registro verificados", Element = (j - 1).ToString() });
           
            }
          
            return result;
        }
        string ExponecialToString(String lat)
        {

            var tam = int.Parse(lat.Substring(lat.Length - 1));
            var cero = "0.";
            for (int i = 1; i < tam; i++)
            {
                cero = cero + "0";

            }


            char[] MyChar = { '.', ',', ' ', '-' };
            lat = lat.Substring(0, lat.Length - 3);
            lat = lat.Replace(".", "");

            string cadenaTexto = lat;
            string negativo = "";
            int resultado;
            resultado = cadenaTexto.IndexOf("-");
            negativo = resultado == 0 ? "-" : "";
            lat = lat.Replace("-", "");
            string NewString = lat.TrimEnd(MyChar);
            lat = negativo + cero + NewString;
            return lat;


        }
        private string GetCellValue(SpreadsheetDocument doc, Cell cell)
        {
            string value = "";
            if (cell.CellValue != null)
            {

                value = cell.CellValue.InnerText;
                if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
                {
                    return doc.WorkbookPart.SharedStringTablePart.SharedStringTable.ChildElements.GetItem(int.Parse(value)).InnerText;
                }
            }
            else
            {
                value = "NA";

            }
            return value;

        }
        public string ValidoCodigo(string code, Guid Idaccount, Guid Iddistrict, int fil)
        {

            if (!_branchMigrateDao.CodigoUnico(code, Idaccount, Iddistrict)) lstTaskResult.Add(new TaskMigrateResultViewModel
            { description = "Este registro ya se encuentra creado en otra ciudad. Por favor asigne un codigo Unico", line = fil, type = "E" });

            return "";
        }
        public string Isval(string data, int col, int fil)
        {

            switch (col)
            {
                case 1:
                    if (data == null || data == " " || data == "NA")
                        lstTaskResult.Add(new TaskMigrateResultViewModel { description = "El codigó se encuentra en vacio", line = fil, type = "E" });
                    break;
                case 2:
                    if (data == null || data == " " || data == "NA")
                        lstTaskResult.Add(new TaskMigrateResultViewModel { description = "La latitud se encuentra en vacia", line = fil, type = "E" });
                    break;
                case 3:
                    if (data == null || data == " " || data == "NA")
                        lstTaskResult.Add(new TaskMigrateResultViewModel { description = "La longitud se encuentra vacia ", line = fil, type = "E" });
                    break;
                case 4:
                    if (data == null || data == "00000000-0000-0000-0000-000000000000")
                        lstTaskResult.Add(new TaskMigrateResultViewModel { description = "La Provicia se encuentra vacia o no existe en la base de datos", line = fil, type = "E" });
                    break;
                case 5:
                    if (data == null || data == "00000000-0000-0000-0000-000000000000") lstTaskResult.Add(new TaskMigrateResultViewModel
                    { description = "La Cuidad se encuentra vacia o no existe en la base de datos", line = fil, type = "E" });
                    break;
                case 6:
                    if (data == null || data == "00000000-0000-0000-0000-000000000000") lstTaskResult.Add(new TaskMigrateResultViewModel
                    { description = "La Parroquia se encuentra vacia o no existe en la base de datos", line = fil, type = "E" });
                    break;
                case 7:
                    if (data == null || data == "00000000-0000-0000-0000-000000000000") lstTaskResult.Add(new TaskMigrateResultViewModel
                    { description = "El sector se encuentra vacio o no existe en la base de datos", line = fil, type = "E" });
                    break;
                case 8:
                    if (_personDao.GetPersonByCode(data) == null) lstTaskResult.Add(new TaskMigrateResultViewModel
                    { description = "El IMEI no se encuentra asignado a ningún encuestador", line = fil, type = "E" });
                    break;
            }
            return "";
        }

        public string urlError(IList<TaskMigrateResultViewModel> item, string fileBrachMassive)
        {
            // Create a spreadsheet document by supplying the filepath.
            // By default, AutoSave = true, Editable = true, and Type = xlsx.
            string cadena = fileBrachMassive.Replace(".xlsx","");

            string[] separadas;

            separadas = cadena.Split();
            string filepath = cadena+"_error.xlsx";
            SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.
                Create(filepath, SpreadsheetDocumentType.Workbook);

            // Add a WorkbookPart to the document.
            WorkbookPart workbookpart = spreadsheetDocument.AddWorkbookPart();
            workbookpart.Workbook = new Workbook();

            // Add a WorksheetPart to the WorkbookPart.
            WorksheetPart worksheetPart = workbookpart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(new SheetData());

            // Add Sheets to the Workbook.
            Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.
                AppendChild<Sheets>(new Sheets());

            // Append a new worksheet and associate it with the workbook.
            Sheet sheetData = new Sheet()
            {
                Id = spreadsheetDocument.WorkbookPart.
                GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Errores"
            };
            sheets.Append(sheetData);
            worksheetPart.Worksheet.Save();

            workbookpart.Workbook.Save();

            // Close the document.
            spreadsheetDocument.Close();

            UpdateCell(filepath, "FILA", 1, "A", item) ;

            byte[] by2tes = System.IO.File.ReadAllBytes(filepath);
            DateTime localDate = DateTime.Now;
            string LogFile = localDate.ToString("yyyyMMddHHmmss");
            MemoryStream stream = new MemoryStream(by2tes);
            AzureStorageUtil.UploadFromStream(stream, "evidencias", LogFile + ".xlsx").Wait();
            var uri = AzureStorageUtil.GetUriFromBlob("evidencias", LogFile + ".xlsx");
            // loading bytes from a file is very easy in C#. The built in System.IO.File.ReadAll* methods take care of making sure every byte is read properly.
            if (File.Exists(filepath))
            {
                File.Delete(filepath);
            }

            return uri;
        }
        public static void UpdateCell(string docName, string text, uint rowIndex, string columnName, IList<TaskMigrateResultViewModel> item)
        {
            using (SpreadsheetDocument spreadSheet = SpreadsheetDocument.Open(docName, true))
            {
                Sheet sheet = spreadSheet.WorkbookPart.Workbook.Sheets.GetFirstChild<Sheet>();
                WorksheetPart worksheetPart = (spreadSheet.WorkbookPart.GetPartById(sheet.Id.Value) as WorksheetPart);  
           
             
                if (worksheetPart != null)
                {
                    // Create new Worksheet
                    Worksheet worksheet = new Worksheet();
                    worksheetPart.Worksheet = worksheet;

                    // Create new SheetData
                    SheetData sheetData = new SheetData();

                    // Create new row
                    Row row = new Row() { RowIndex = rowIndex };

                    // Create new cell
                    Cell cell = null;
                    cell = new Cell() { CellReference = "A" + rowIndex, DataType = CellValues.Number, CellValue = new CellValue("FILA") };
                    row.Append(cell);


                    cell = new Cell() { CellReference = "B" + rowIndex, DataType = CellValues.Number, CellValue = new CellValue("OBSERVACION") };
                    // Append cell to row
                    row.Append(cell);

                    sheetData.Append(row);
                    uint i = 2;
                    foreach (var data in item)
                    {

                        Row rows = new Row() { RowIndex = i };

                        cell = new Cell() { CellReference = "A" + i, DataType = CellValues.Number, CellValue = new CellValue(data.line.ToString()) };
                        rows.Append(cell);

         
                        cell = new Cell() { CellReference = "B" + i, DataType = CellValues.Number, CellValue = new CellValue(data.description.ToString()) };
                        // Append cell to row
                        rows.Append(cell);
                  
                        sheetData.Append(rows);
                        i++;
                    }


                    // Append row to sheetData
               

                    // Append sheetData to worksheet
                    worksheet.Append(sheetData);

                    worksheetPart.Worksheet.Save();
                }
                spreadSheet.WorkbookPart.Workbook.Save();
                spreadSheet.Close();

             
            }

        }
        #endregion

        #region ActualizarBranchImage
        public int UpdateBranch(string id, string imagen)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            string[] separadas;
            var _modelImage=    _branchDao.GetDataImage(Guid.Parse(id));


            separadas = imagen.Split(',');
            byte[] bytes = Convert.FromBase64String(separadas[1]);
            MemoryStream imageStream = new MemoryStream(bytes);
            AzureStorageUtil.UploadFromStream(imageStream, _modelImage.NameContainer, _modelImage.Id.ToString() + ".jpg").Wait();
          
            var uri = AzureStorageUtil.GetUriFromBlob(_modelImage.NameContainer, _modelImage.Id.ToString() + ".jpg");


            return _branchDao.UpdateDataImage(Guid.Parse(id),uri);
            

        }
        #endregion

        #region Historia Estados


        public IList<historialTareas> GetHistory(Guid IdTask)
        {

           var _model= _taskCampaignDao.GetDataHystory(IdTask);

            if (_model.Count() > 0)
            {


                return _model.OrderByDescending(x => x.DateModification).ToList();

            }


            return _model;


        }


        #endregion
        #region  Mapear Tablas

        public TaskCampaigViewModel _ModelTasks(TaskPerCampaignViewModel _model)
        {
            Mapper.Initialize(cfg =>
            {
                cfg.CreateMap<TaskPerCampaignViewModel, TaskCampaigViewModel>();
            });
            var _marpermodel = new TaskCampaigViewModel();

            _marpermodel = Mapper.Map<TaskCampaigViewModel>(_model);

            _model.Properties.ControllerName = "Task";
            _model.Properties.ActionName = "TasksCampaign";
            return _marpermodel;
        }

        public int _CountAllTasCamping(Guid Idcampaign, List<FilterValue> filters)
        {
            try
            {

                filters = filters ?? new List<FilterValue>();

                var itemResult = new TaskPerCampaignViewModel();

                filters = AddHiddenFilter("IdCampaign", Idcampaign.ToString(), filters, itemResult.FilterName);
                var strPredicate = $"StatusRegister ==  \"{CStatusRegister.Active}\" ";
                strPredicate += GetFilterPredicate(filters);

                var _data =  Context.TaskCampaigns.Where(strPredicate).Count();
                return _data;
            }
            catch (Exception)
            {

                return 0;
            }
       
       
        }

        protected string GetFilterPredicate(List<FilterValue> filterValues, string operatorFilter = "AND")
        {
            var op = operatorFilter == "OR" ? "|| " : "&& ";
            var builderPredicate = new StringBuilder();

            if (filterValues != null)
            {
                foreach (var filterValue in filterValues)
                {
                    var filter = CoreFilterDetailDao.GetCoreFilterDetail(filterValue.IdFilter);

                    var filterTable = string.IsNullOrEmpty(filter.Table) ? "" : filter.Table + ".";

                    switch (filterValue.Criteria)
                    {
                        case "Contains":
                            builderPredicate.Append(op + filterTable + filter.Property + ".");
                            builderPredicate.Append(filterValue.Criteria + "(\"");
                            builderPredicate.Append(filterValue.Value + "\")");
                            break;
                        case "NotContains":
                            builderPredicate.Append(op + "!" + filterTable + filter.Property + ".");
                            builderPredicate.Append("Contains(\"");
                            builderPredicate.Append(filterValue.Value + "\")");
                            break;
                        default:
                            if (filterValue.NameFilter.IndexOf("Id", StringComparison.Ordinal) >= 0)
                            {
                                var fs = $" == \"{filterValue.Value}\" ";
                                builderPredicate.Append(op + filterTable + filter.Property + fs);
                            }
                            else
                            {
                                builderPredicate.Append(op + filterTable + filter.Property + " ");
                                builderPredicate.Append(filterValue.Criteria + " \"");
                                builderPredicate.Append(filterValue.Value + "\"");
                            }
                            break;
                    }
                }
            }
            return builderPredicate.ToString();
        }

        #endregion


        #region  Generador de codigo Nuevos

        public bool _GeneretorCode(Guid idbranch , string Code,Guid users, Guid idtask)
        {


            if (_branchDao.IsUseCode(Code))
            {

            return    _branchDao.SaveNewCodeBranch(Code, idbranch, users, idtask);


            }

            return false;
        }
        #endregion

        #region Seleccionar Tareas x Usuarios
        public string _IstaskBlock(Guid user, Guid idtask,string email)
        {

            
            var _securityModel = _redisCache.Get<List<ViewSecurityTasksModel>>("UsersbyTasks_pruebas");
            if (_securityModel == null)
            {

                IList<ViewSecurityTasksModel> _model = new List<ViewSecurityTasksModel>();

                _model.Add(new ViewSecurityTasksModel { Idtask = idtask, Iduser = user, Mail=email });

                _redisCache.Set("UsersbyTasks_pruebas", _model);
                return "";
            }
            else {
                var _model = _securityModel.Where(x => x.Iduser.Equals(user)&& x.Idtask.Equals(idtask));
                if (_model.Count() > 0)
                {

                    return "";
                }
                else {

                    var _modelTask = _securityModel.Where(x => x.Idtask.Equals(idtask));

                    if (_modelTask.Count() > 0)
                    {

                        return _modelTask.First().Mail;

                    }
                    else {

                        IList<ViewSecurityTasksModel> _modelAdd = new List<ViewSecurityTasksModel>();

                        _securityModel.Add(new ViewSecurityTasksModel { Idtask = idtask, Iduser = user, Mail = email });

                        _redisCache.Set("UsersbyTasks_pruebas", _securityModel);

                        return "";
                    }



                }

            }
           
        }

        public bool taskunBlock(Guid user,Guid idtask)
        {


            var _securityModel = _redisCache.Get<List<ViewSecurityTasksModel>>("UsersbyTasks_pruebas");
            if (_securityModel != null)
            {

                _securityModel.RemoveAll(x => x.Iduser.Equals(user) && x.Idtask== idtask);

                _redisCache.Set("UsersbyTasks_pruebas", _securityModel);
                
            }

            return true;
        }

        public bool taskunBlockAll(Guid user)
        {


            var _securityModel = _redisCache.Get<List<ViewSecurityTasksModel>>("UsersbyTasks_pruebas");
            if (_securityModel != null)
            {

                _securityModel.RemoveAll(x => x.Iduser.Equals(user));

                _redisCache.Set("UsersbyTasks_pruebas", _securityModel);

            }

            return true;
        }
        #endregion
        #region validarPreguntasXProfile
        public List<QuestionRequeredModel> ControlQuestion(Guid user, MyTaskViewModel _model)
        {
           var _user= _userDao.GetUserById(user);
            IList<QuestionRequeredModel> _questionRequeredModel = new List<QuestionRequeredModel>();
            var questionrequerid = _userDao.GetQuestionProfile(_user.IdProfile,_model.IdStatusTask);
            if (questionrequerid.Count() > 0) { 
            foreach (var _service in _model.ServiceCollection) {

                foreach (var seccion in _service.ServiceDetailCollection) {
                    foreach (var control in questionrequerid) {
                        var _questionrequerid = seccion.QuestionCollection.Where(x => x.Id.Equals(control.IdQuestion)&& x.Answer == null && x.QuestionComplete.Count() < 1 && x.QuestionDetailCollection.Where(z => z.Checked.Equals(1)).Count() < 1 && x.IdQuestionDetail.Equals(Guid.Empty));

                            //foreach (var aswers in _questionrequerid)
                            //{
                            //    if (aswers.Answer == null && aswers.QuestionComplete.Count()<1 && aswers.QuestionDetailCollection.Where(z=>z.Checked.Equals(1)).Count()<1&&aswers.IdQuestionDetail.Equals(Guid.Empty)) {


                            //            _questionRequeredModel.Add(new QuestionRequeredModel { name = aswers.Title, status = "N", type = "R" });
                            //    }

                            //}
                            if (_questionrequerid.Count() > 0)
                            {
                                foreach (var aswers in _questionrequerid)
                                {
                                    _questionRequeredModel.Add(new QuestionRequeredModel { name = aswers.Title, status = "N", type = "R" });
                                }
                            }
                        }
                    foreach (var subseccion in seccion.Sections) {

                        foreach (var control in questionrequerid)
                        {
                                var _questionrequerid = subseccion.QuestionCollection.Where(x => x.Id.Equals(control.IdQuestion) && x.Answer == null && x.QuestionComplete.Count() < 1 && x.QuestionDetailCollection.Where(z => z.Checked.Equals(1)).Count() < 1 && x.IdQuestionDetail.Equals(Guid.Empty));

                                //foreach (var aswers in _questionrequerid)
                                //{
                                //    if (aswers.Answer == null && aswers.QuestionComplete.Count()<1 && aswers.QuestionDetailCollection.Where(z=>z.Checked.Equals(1)).Count()<1&&aswers.IdQuestionDetail.Equals(Guid.Empty)) {


                                //            _questionRequeredModel.Add(new QuestionRequeredModel { name = aswers.Title, status = "N", type = "R" });
                                //    }

                                //}
                                if (_questionrequerid.Count() > 0)
                                {
                                    foreach (var aswers in _questionrequerid)
                                    {
                                        _questionRequeredModel.Add(new QuestionRequeredModel { name = aswers.Title, status = "N", type = "R" });
                                    }
                                }
                            }

                    }
                    
                    


                }


            }
            }
            return _questionRequeredModel.ToList();
        }
        #endregion
    }
}
