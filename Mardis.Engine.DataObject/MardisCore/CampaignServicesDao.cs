﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Mardis.Engine.DataAccess;
using Mardis.Engine.DataAccess.MardisCommon;
using Mardis.Engine.DataAccess.MardisCore;
using Mardis.Engine.Framework.Resources;
using Mardis.Engine.Web.ViewModel.BranchViewModels;
using Microsoft.EntityFrameworkCore;

namespace Mardis.Engine.DataObject.MardisCore
{
    public class CampaignServicesDao : ADao
    {
        public CampaignServicesDao(MardisContext mardisContext) : base(mardisContext)
        {
        }

        public List<CampaignServices> GetCampaignServicesByCampaign(Guid idCampaign, Guid idAccount)
        {
            return Context.CampaignsServices
                .Include(cs => cs.Service)
                .Where(cs => cs.IdCampaign == idCampaign &&
                              cs.StatusRegister == CStatusRegister.Active &&
                              cs.IdAccount == idAccount)
                .ToList();
        }

        public List<CampaignServices> GetCampaignServicesByService(Guid idService, Guid idAccount)
        {
            return Context.CampaignsServices
                .Where(cs => cs.IdService == idService &&
                             cs.StatusRegister == CStatusRegister.Active &&
                             cs.IdAccount == idAccount)
                .ToList();
        }

        public async Task<IQueryable<CampaignServices>> GetCompleteCampaignServices(Guid idCampaign, Guid idAccount)
        {
            return await Task.FromResult(Context.CampaignsServices
                .Include(cs => cs.Service.ServiceDetails)
                    .ThenInclude(sd => sd.Questions)
                        .ThenInclude(q => q.TypePoll)
                .Include(cs => cs.Service.ServiceDetails)
                    .ThenInclude(sd => sd.Questions)
                        .ThenInclude(q => q.QuestionDetails)
                .Include(cs => cs.Service.ServiceDetails)
                    .ThenInclude(sd => sd.Sections)
                        .ThenInclude(s => s.Questions)
                            .ThenInclude(q => q.QuestionDetails)
                .Include(cs => cs.Service.ServiceDetails)
                    .ThenInclude(sd => sd.Sections)
                        .ThenInclude(s => s.Questions)
                            .ThenInclude(q => q.TypePoll)
                .Where(cs => cs.IdCampaign == idCampaign &&
                             cs.StatusRegister == CStatusRegister.Active &&
                             cs.IdAccount == idAccount));
        }

        #region Administracion de Rutas

        public IList<RouteBranchViewModel> GetActRoute(Guid idAccount)
        {


            IList<RouteBranchViewModel> _model = new List<RouteBranchViewModel>();
            //var query = Context.Branches.Where(x => x.IdAccount.Equals(idAccount)).Select(new { });
            //var query = from data in Context.Branches
            //                  .GroupBy(g => new { g.RUTAAGGREGATE, g.IdAccount })
            //                  .Where(w => w.Key.IdAccount.Equals(idAccount))
            //                 .Select(s => new { ruta = s.Key.RUTAAGGREGATE, numero = s.Key.IdAccount } );

            //var query = from data in Context.Branches
            //            where data.IdAccount == idAccount
            //            group data by new { data.IdAccount, data.RUTAAGGREGATE } into grupo
            //            select new { route = grupo.Key.RUTAAGGREGATE , numbreBranches = grupo.Count()};
            var query = Context.Branches.Where(x => x.IdAccount == idAccount && x.RUTAAGGREGATE != "")
                        .Select(s => new { s.RUTAAGGREGATE, s.ESTADOAGGREGATE }).Distinct().OrderBy(x=>x.RUTAAGGREGATE);
            var result = query.ToList();
           foreach (var item in result)
            {
                if (item != null)
                {

                    var List_data = _model.Where(x => x.route == item.RUTAAGGREGATE);
                    if (List_data.Count() > 0)
                    {
                        if (item.ESTADOAGGREGATE == "S")
                        {
                            List_data.First().status = true;
                        }
                        else
                        {
                            List_data.First().status = false;
                        }
                    }
                    else
                    {                
                        if (item.ESTADOAGGREGATE == "S")
                        {
                            RouteBranchViewModel route = new RouteBranchViewModel();
                            _model.Add(new RouteBranchViewModel() { route = item.RUTAAGGREGATE ,status=true});
                        }
                        else
                        {
                            RouteBranchViewModel route = new RouteBranchViewModel();
                            _model.Add(new RouteBranchViewModel() { route = item.RUTAAGGREGATE,status=false });

                        }
                     }
                }
           }
              var resulta =_model.ToList();
              return resulta;
        }

        public async Task<int> UpdateStatusRoute( Guid idAccount ,string route)
        {

            try
            {
    

                using (var transaction = Context.Database.BeginTransaction())
                {
                    var updatebranches = Context.Branches.Where(x => x.RUTAAGGREGATE.Trim()==route.Trim() && x.IdAccount == idAccount).ToList();
                    var Isactive = Context.Branches.Where(x => x.IdAccount == idAccount && x.RUTAAGGREGATE.Trim() == route && x.ESTADOAGGREGATE == "S").Select(x => x.Id).FirstOrDefault();
                    var estados = Isactive != Guid.Parse("00000000-0000-0000-0000-000000000000") ? "" : "S";
                    updatebranches.ForEach(a => a.ESTADOAGGREGATE = estados) ;
                    Context.Branches.UpdateRange(updatebranches);
                    Context.SaveChanges();
                    transaction.Commit();
                }

            }
            catch (Exception e)
            {
               
                e.Message.ToString();
                return 0;
            }
          

            return 1;
        }


        public IList<String> GetIMEIRoute(string routes, Guid idAccount)
        {

            var query = Context.Branches.Where(x => x.IdAccount == idAccount && x.RUTAAGGREGATE.Trim() == routes).Select(x=>x.IMEI_ID).Distinct().ToList();
            //     result.upda
            return query;
        }

        public IList<Pollster> GetPollsterdata(Guid IdAccount)
        {

            var query = Context.Pollsters.Where(x=>x.idaccount.Equals(IdAccount)).ToList();
            //     result.upda
            return query;
        }
        public IList<Pollster> GetIdPersonByDocumentAndTypeDocumentAndAccount(IList<string> document, string typeDocument,Guid idAccount)
        {
            return Context.Pollsters
                .Where(p => document.Contains(p.IMEI) &&
                     p.Status == CStatusRegister.Active).ToList();
                    
        }
        public int UpdateRouteImei(string document, string routes, Guid idAccount)
        {

            try
            {
                var route = Context.Branches.Where(x => x.IdAccount == idAccount && x.RUTAAGGREGATE.Trim() == routes && x.IMEI_ID.Contains(document)).Select(x => x.IMEI_ID).Distinct().First();

                var actuallyRoute = route.Replace("-" + document, "");
                actuallyRoute = actuallyRoute.Replace(document + "-", "");
                actuallyRoute = actuallyRoute.Replace(document, "");
                var updatebranches = Context.Branches.Where(x => x.IdAccount == idAccount && x.RUTAAGGREGATE.Trim() == routes && x.IMEI_ID.Contains(document)).ToList();
                updatebranches.ForEach(a => a.IMEI_ID = actuallyRoute);
                Context.Branches.UpdateRange(updatebranches);
                Context.SaveChanges();
                return 1;
            }
            catch (Exception)
            {

                return -1;
            }
       


       
        }
        public int UpdateRouteAccount(Guid idAccount, string type)
        {

            try
            {
                if (type == "S")
                {

                    var updatebranches = Context.Branches.Where(x => x.IdAccount == idAccount).ToList();
                    updatebranches.ForEach(a => a.ESTADOAGGREGATE = "S");
                    Context.Branches.UpdateRange(updatebranches);
                    Context.SaveChanges();

                }
                else
                {
                    var updatebranches = Context.Branches.Where(x => x.IdAccount == idAccount).ToList();
                    updatebranches.ForEach(a => a.ESTADOAGGREGATE = "");
                    Context.Branches.UpdateRange(updatebranches);
                    Context.SaveChanges();
                }


                return 1;
            }
            catch (Exception)
            {

                return -1;
            }




        }

        public int AddRouteImei(string document, string rout, Guid idAccount)
        {

            try
            {
                var routes = Context.Branches.Where(x => x.IdAccount == idAccount && x.RUTAAGGREGATE.Trim() == rout.Trim() && x.IMEI_ID!=null).Select(x => x.IMEI_ID).Distinct().ToList();

                if (routes.Count() > 0)
                {
                    var route = routes.First();
                    var actuallyRoute = (route.Length > 5 || route!=null) ? route + '-' + document : document;
                    var updatebranches = Context.Branches.Where(x => x.IdAccount == idAccount && x.RUTAAGGREGATE == rout).ToList();
                    updatebranches.ForEach(a => a.IMEI_ID = actuallyRoute);
                    Context.Branches.UpdateRange(updatebranches);
                    Context.SaveChanges();


                }
                else {
                    var actuallyRoute =  document;
                    var updatebranches = Context.Branches.Where(x => x.IdAccount == idAccount && x.RUTAAGGREGATE == rout).ToList();
                    updatebranches.ForEach(a => a.IMEI_ID = actuallyRoute);
                    Context.Branches.UpdateRange(updatebranches);
                    Context.SaveChanges();

                }

               
                return 1;
            }
            catch (Exception e)
            {

                return -1;
            }




        }

        public int SavePollster(Pollster entity)
        {

            try
            {
                Context.Pollsters.Add(entity);

                if (entity.Id < 1) Context.Entry(entity).State = EntityState.Added;
                else Context.Entry(entity).State = EntityState.Modified;


                Context.SaveChanges();
                return 1;


            }
            catch (Exception)
            {

                return 0;
            }
          
        }
        public Pollster GetPollster_Edit(int Id)
        {
            var resultList = Context.Pollsters
                .Where(x => x.Id.Equals(Id)).First();
            //.Where(usr => usr.Equipaments. == typeUser &&
            //              usr.StatusRegister == CStatusRegister.Active &&
            //              usr.IdAccount == idAccount)

            return resultList;
        }
        public int DeletePollsters(int Id)
        {
            try
            {
             
                Context.Pollsters.RemoveRange(Context.Pollsters.Where(x => x.Id == Id));
                var status = Context.SaveChanges();
                return 1;
            }
            catch (Exception e)
            {
                var tasks = Context.Pollsters.Include(x => x.Tasks).Where(i => i.Id == Id).Select(d=>d.Tasks);
                if (tasks.Count() > 0)
                { return 3; }
                else { return 0; }
          
                throw;
            }            
        }

        public int UpdateRouteImei(string Imei)
        {

            try
            {
                

                    var updatebranches = Context.Branches.Where(z=>z.IMEI_ID.Trim()==Imei.Trim()).ToList();
                    updatebranches.ForEach(a => a.ESTADOAGGREGATE = "");
                    Context.Branches.UpdateRange(updatebranches);
                    Context.SaveChanges();

                

                return 1;
            }
            catch (Exception)
            {

                return -1;
            }




        }
    }
    #endregion


}