using System;
using System.Collections.Generic;
using System.Linq;
using Mardis.Engine.DataAccess;
using Mardis.Engine.DataAccess.MardisCore;
using Mardis.Engine.Framework.Resources;
using Microsoft.EntityFrameworkCore;
using Mardis.Engine.Web.ViewModel.Utility;
using System.Diagnostics;

namespace Mardis.Engine.DataObject.MardisCore
{
    public class ServiceDao : ADao
    {
        public ServiceDao(MardisContext mardisContext)
               : base(mardisContext)
        {

        }

        public List<Service> GetService(Guid idTypeService, Guid idCustomer, Guid idAccount)
        {
            var itemsReturn = Context.Services
                                     .Join(Context.TypeServices,
                                        tb => tb.IdTypeService,
                                        ts => ts.Id,
                                        (tb, ts) => new { tb, ts })
                                     .Where(tb => tb.tb.IdCustomer == idCustomer &&
                                                  tb.tb.IdTypeService == idTypeService &&
                                                  tb.tb.IdAccount == idAccount &&
                                                  tb.ts.StatusRegister == CStatusRegister.Active &&
                                                  tb.tb.StatusRegister == CStatusRegister.Active)
                                     .Select(tb => tb.tb)
                                     .ToList();

            return itemsReturn;
        }

        public Service GetOne(Guid id, Guid idAccount)
        {
            var itemReturn = Context.Services
                .Include(s => s.ServiceDetails)
                .AsNoTracking()
                            .FirstOrDefault(tb => tb.Id == id &&
                                        tb.IdAccount == idAccount &&
                                        tb.StatusRegister == CStatusRegister.Active);

            return itemReturn;
        }

        public Service GetOneTraking(Guid id, Guid idAccount)
        {
            var itemReturn = Context.Services
                            .Include(s => s.ServiceDetails)
                            .FirstOrDefault(tb => tb.Id == id &&
                                        tb.IdAccount == idAccount &&
                                        tb.StatusRegister == CStatusRegister.Active);

            return itemReturn;
        }

        public List<Service> GetServicesByCustomerId(Guid idAccount, Guid idCustomer)
        {
            return Context.Services
                .Where(srv => srv.IdCustomer == idCustomer
                        && srv.IdAccount == idAccount
                        && srv.StatusRegister == CStatusRegister.Active)
                .ToList();
        }
        #region EditarPreguntasXCampaña
        public List<Campaign> GetCampaignAccount(Guid idAccount)
        {
            return Context.Campaigns
                .Where(srv => srv.IdAccount == idAccount
                        && srv.StatusRegister == CStatusRegister.Active)
                .ToList();
        }
        public List<Service> GetServicesByCampaignId(Guid idAccount, Guid Idcamping)
        {

            try
            {
                var _data = from s in Context.Services
                            join cs in Context.CampaignsServices on s.Id equals cs.IdService
                            where cs.IdCampaign.Equals(Idcamping) && s.StatusRegister == CStatusRegister.Active &&s.IdAccount.Equals(idAccount)
                            select s;

                return _data.ToList();
            }
            catch (Exception e)
            {
                var _serviceModel = new List<Service> ();
                return _serviceModel;
            }
            
        }



        public Question GetQUestioneOne(Guid idquestion)
        {

            try
            {
                var _data = Context.Questions.Where(x => x.Id.Equals(idquestion));

                return _data.First();
            }
            catch (Exception e)
            {
                var _serviceModel = new Question();
                return _serviceModel;
            }

        }
        #endregion
        public List<CampaignServices> Getcampaign(Guid Idservice)
        {
            return Context.CampaignsServices.Where(x => x.IdService == Idservice).ToList();
        }

        public List<Service> GetServicesByChannelId(Guid idAccount, Guid idChannel)
        {
            return Context.Services
                .Where(srv => srv.IdChannel == idChannel
                        && srv.IdAccount == idAccount
                        && srv.StatusRegister == CStatusRegister.Active)
                .ToList();
        }
        public List<Service> GetServicesByAccount(Guid idAccount)
        {
            return Context.Services
                .Where(srv => srv.IdAccount == idAccount
                        && srv.StatusRegister == CStatusRegister.Active)
                .ToList();
        }

        /// <summary>
        /// Obtiene el Servicio completo sin Tracking
        /// </summary>
        /// <param name="idService">Id del Servicio a ser consultado</param>
        /// <param name="idAccount">Id de la Cuenta general de la sesión actual</param>
        /// <returns>Objeto completamente mapeado sin Tracking</returns>
        public Service GetService(Guid idService, Guid idAccount)
        {
            var service = Context.Services
                .Include(s => s.ServiceDetails)
                   .ThenInclude(sd => sd.Questions)
                        .ThenInclude(q => q.QuestionDetails)
                .Include(s => s.ServiceDetails)
                    .ThenInclude(sd => sd.Sections)
                        .ThenInclude(sc => sc.Questions)
                            .ThenInclude(q => q.QuestionDetails)
                .AsNoTracking()
                .FirstOrDefault(s => s.Id == idService && s.IdAccount == idAccount);

            return service;
        }

        public int SaveFormAggregate(IList<StructXmlModel> Aggregate , Guid Idaccount, String Name,IList<_FormTable> _TableIMGRPT) {
            DateTime localDate = DateTime.Now;
            string LogFile = localDate.ToString("MMyy");
            var servicedata = Context.Accounts.Where(x => x.Id == Idaccount);
            var serv = Context.Services.Where(z => z.IdAccount == Idaccount);
            string name = "Formulario_"+ serv.Count()+ " " + servicedata.First().Name+"_"+LogFile;
            Name = name;
          
                using (var transaction = Context.Database.BeginTransaction())
                {
                try
                {
                    Service service = null;
                    ServiceDetail serviceDetail = null;
                    Question question = null;
                    QuestionDetail questionDetail = null;
                    ServiceDetail serviceDetailSub = null;
                    Question questionsub = null;
                    QuestionDetail questionDetailsub = null;
                    int orderService = 0;

                    var typeService = Context.TypeServices.Where(x => x.StatusRegister == "A");
                    var custumer = Context.Customers.Where(x => x.IdAccount == Idaccount);
                    var channel = Context.Channels.Where(x => x.IdAccount == Idaccount);
                    var stateRegister = EntityState.Added;
                    service = new Service();
                    service.Code = custumer.First().Abbreviation;
                    service.Name = Name;
                    service.IdTypeService = typeService.First().Id;
                    service.PollTitle = Name;
                    service.IdAccount = Idaccount;
                    service.IdCustomer = custumer.First().Id;
                    service.CreationDate = DateTime.Now;
                    service.StatusRegister = "A";
                    service.IdChannel = channel.First().Id;
                    service.Icon = "glyphicon glyphicon-king";
                    service.IconColor = "bg-red";
                    service.Template = "Matrix";
                    Context.Services.Add(service);
                    Context.Entry(service).State = stateRegister;
                    Context.SaveChanges();
                    if (_TableIMGRPT.Count() > 0)
                    {

                        _TableIMGRPT.ToList().ForEach(x => x.idservice = service.Id);

                        Context._FormTables.AddRange(_TableIMGRPT);
                        Context.SaveChanges();
                    }
                    int orderServiceSub = 0;
                    foreach (var ServiceAggregate in Aggregate)
                    {
                        serviceDetail = new ServiceDetail();
                        Guid idService = service.Id;
                        serviceDetail.IdService = idService;
                        serviceDetail.Order = orderService;
                        serviceDetail.StatusRegister = "A";
                        serviceDetail.SectionTitle = ServiceAggregate.QuestionText;
                        serviceDetail.Weight = 0;
                        serviceDetail.HasPhoto = false;
                        serviceDetail.GroupName = null;
                        serviceDetail.IdSection = null;
                        serviceDetail.NumberOfCopies = 1;
                        if (ServiceAggregate.IsDynamic)
                        {
                            serviceDetail.IsDynamic = ServiceAggregate.IsDynamic;
                        }

                        Context.ServiceDetails.Add(serviceDetail);
                        Context.Entry(serviceDetail).State = stateRegister;
                        Context.SaveChanges();
                        orderService++;
                        int orderQuestion = 0;
                        foreach (var QuestionAggregate in ServiceAggregate.Question)
                        {
                            if (QuestionAggregate.id != null)
                            {
                                question = new Question();
                                question.IdServiceDetail = serviceDetail.Id;
                                question.Title = QuestionAggregate.QuestionText;
                                question.StatusRegister = "A";
                                question.Order = orderQuestion;
                                question.Weight = 0;
                                question.IdTypePoll = typePollAuto(QuestionAggregate.QuestionTipo);
                                question.HasPhoto = "N";
                                question.CountPhoto = 0;
                                question.IdProduct = null;
                                question.IdProductCategory = null;
                                question.AnswerRequired = false;
                                question.sequence = 1;
                                question.Aggregatefield = QuestionAggregate.id + "_" + QuestionAggregate.valueText;
                                Context.Questions.Add(question);
                                Context.Entry(question).State = stateRegister;
                                Context.SaveChanges();
                                orderQuestion++;
                                int orderQuestionDetail = 0;

                                if (QuestionAggregate.Detail != null)
                                {
                                    foreach (var QuestionDetailAggregate in QuestionAggregate.Detail)
                                    {
                                        questionDetail = new QuestionDetail();
                                        questionDetail.IdQuestion = question.Id;
                                        questionDetail.Answer = QuestionDetailAggregate.QuestionText;
                                        questionDetail.StatusRegister = "A";
                                        questionDetail.Order = orderQuestionDetail;
                                        questionDetail.Weight = 0;
                                        questionDetail.IsNext = "N";
                                        questionDetail.QuestionLink = null;
                                        questionDetail.IdQuestionRequired = null;
                                        questionDetail.Aggregatefield = QuestionDetailAggregate.valueText;
                                        Context.QuestionDetails.Add(questionDetail);
                                        Context.Entry(questionDetail).State = stateRegister;
                                        Context.SaveChanges();

                                        orderQuestionDetail++;

                                    }
                                }
                            }

                        }
                        if (ServiceAggregate.Sections != null) {

                        
                        foreach (var _sections in ServiceAggregate.Sections)
                            {
                                 serviceDetailSub = new ServiceDetail();
                                Guid idSeccion = serviceDetail.Id;
                                serviceDetailSub.IdSection = idSeccion;
                                serviceDetailSub.Order = orderServiceSub;
                                serviceDetailSub.StatusRegister = "A";
                                serviceDetailSub.SectionTitle = _sections.QuestionText;
                                serviceDetailSub.Weight = 0;
                                serviceDetailSub.HasPhoto = false;
                                serviceDetailSub.GroupName = null;
                                serviceDetailSub.IdService = null;
                                serviceDetailSub.NumberOfCopies = 1;
                                if (_sections.IsDynamic)
                                {
                                    serviceDetailSub.IsDynamic = _sections.IsDynamic;
                                }

                                Context.ServiceDetails.Add(serviceDetailSub);
                                Context.Entry(serviceDetailSub).State = stateRegister;
                                Context.SaveChanges();
                                orderServiceSub++;
                                int orderQuestionseccion = 0;
                                foreach (var QuestionAggregateSub in _sections.Question)
                                {
                                    if (QuestionAggregateSub.id != null)
                                    {
                                         questionsub = new Question();
                                        questionsub.IdServiceDetail = serviceDetailSub.Id;
                                        questionsub.Title = QuestionAggregateSub.QuestionText;
                                        questionsub.StatusRegister = "A";
                                        questionsub.Order = orderQuestionseccion;
                                        questionsub.Weight = 0;
                                        questionsub.IdTypePoll = typePollAuto(QuestionAggregateSub.QuestionTipo);
                                        questionsub.HasPhoto = "N";
                                        questionsub.CountPhoto = 0;
                                        questionsub.IdProduct = null;
                                        questionsub.IdProductCategory = null;
                                        questionsub.AnswerRequired = false;
                                        questionsub.sequence = 1;
                                        questionsub.Aggregatefield = QuestionAggregateSub.id + "_" + QuestionAggregateSub.valueText;
                                        Context.Questions.Add(questionsub);
                                        Context.Entry(questionsub).State = stateRegister;
                                        Context.SaveChanges();
                                        orderQuestionseccion++;
                                        int orderQuestionDetail = 0;

                                        if (QuestionAggregateSub.Detail != null)
                                        {
                                            foreach (var QuestionDetailAggregatesub in QuestionAggregateSub.Detail)
                                            {
                                                 questionDetailsub = new QuestionDetail();
                                                questionDetailsub.IdQuestion = questionsub.Id;
                                                questionDetailsub.Answer = QuestionDetailAggregatesub.QuestionText;
                                                questionDetailsub.StatusRegister = "A";
                                                questionDetailsub.Order = orderQuestionDetail;
                                                questionDetailsub.Weight = 0;
                                                questionDetailsub.IsNext = "N";
                                                questionDetailsub.QuestionLink = null;
                                                questionDetailsub.IdQuestionRequired = null;
                                                questionDetailsub.Aggregatefield = QuestionDetailAggregatesub.valueText;
                                                Context.QuestionDetails.Add(questionDetailsub);
                                                Context.Entry(questionDetailsub).State = stateRegister;
                                                Context.SaveChanges();

                                                orderQuestionDetail++;

                                            }
                                        }
                                    }
                                }


                            }


                        }



                    }
                    transaction.Commit();
                    return 1;
                }
                catch (Exception e)
                {
                    var st = new StackTrace(e, true);
                    // Get the top stack frame
                    var frame = st.GetFrame(0);
                    // Get the line number from the stack frame
                    var line = frame.GetFileLineNumber();

                    transaction.Rollback();
                    return 0;
                }
            }
           
          



           

        }
        Guid typePollAuto(string type) {


            if (type.Contains("select_one")) {

                return  Context.TypePolls.Where(x => x.Code == CTypePoll.One).First().Id;

            }
            else {
                if (type.Contains("select_multiple"))
                {

                    return Context.TypePolls.Where(x => x.Code == CTypePoll.Many).First().Id;

                }
                else {
                    return Context.TypePolls.Where(x => x.Code == CTypePoll.Open).First().Id;
                }
            }
            return Guid.Empty;
        }

    }
}
