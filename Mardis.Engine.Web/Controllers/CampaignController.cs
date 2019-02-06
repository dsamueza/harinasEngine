#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using Mardis.Engine.Business;
using Mardis.Engine.Business.MardisCore;
using Mardis.Engine.Business.MardisSecurity;
using Mardis.Engine.DataAccess;
using Mardis.Engine.DataAccess.MardisCore;
using Mardis.Engine.Framework;
using Mardis.Engine.Framework.Resources.PagesConstants;
using Mardis.Engine.Web.Libraries.Security;
using Mardis.Engine.Web.Libraries.Services;
using Mardis.Engine.Web.Model;
using Mardis.Engine.Web.ViewModel;
using Mardis.Engine.Web.ViewModel.CampaignViewModels;
using Mardis.Engine.Web.ViewModel.Filter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using OfficeOpenXml;
using System.Net.Http.Headers;
using System.Drawing;
using Mardis.Engine.Web.ViewModel.PollsterViewModels;


#endregion

namespace Mardis.Engine.Web.Controllers
{
    [Authorize]
    public class CampaignController : AController<CampaignController>
    {
        #region Variables & Constructores
        private readonly CampaignBusiness _campaignBusiness;
        private readonly TaskCampaignBusiness _taskCampaignBusiness;
        private readonly CommonBusiness _commonBusiness;
        private readonly CustomerBusiness _customerBusiness;
        private readonly StatusCampaignBusiness _statusCampaignBusiness;
        private readonly UserBusiness _userBusiness;
        private readonly ChannelBusiness _channelBusiness;
        private readonly ServiceBusiness _serviceBusiness;
        public static IDataProtector Protector;
        private readonly StatusTaskBusiness _statusTaskBusiness;
        private readonly TaskNotImplementedReasonBusiness _taskNotImplementedReasonBusiness;
        public Guid _userId;
        public Guid _Profile;
        public Guid _typeuser;
        public readonly ProfileBusiness _profileBusiness;
        private readonly IHostingEnvironment _hostingEnv;

        public CampaignController(
                                    UserManager<ApplicationUser> userManager,
                                    IHttpContextAccessor httpContextAccessor,
                                    MardisContext mardisContext,
                                    ILogger<CampaignController> logger,
                                    ILogger<ServicesFilterController> loggeFilter,
                                    IDataProtectionProvider protectorProvider,
                                    IMemoryCache memoryCache,
                                    IHostingEnvironment hostingEnvironment,
                                    RedisCache distributedCache) :
            base(userManager, httpContextAccessor, mardisContext, logger)
        {
            Protector = protectorProvider.CreateProtector(GetType().FullName);
            _campaignBusiness = new CampaignBusiness(mardisContext);
            TableName = CCampaign.TableName;
            ControllerName = CCampaign.Controller;
            _taskCampaignBusiness = new TaskCampaignBusiness(mardisContext, distributedCache);
            _commonBusiness = new CommonBusiness(mardisContext);
            _customerBusiness = new CustomerBusiness(mardisContext);
            _statusCampaignBusiness = new StatusCampaignBusiness(mardisContext, memoryCache);
            _userBusiness = new UserBusiness(mardisContext);
            _channelBusiness = new ChannelBusiness(mardisContext);
            _serviceBusiness = new ServiceBusiness(mardisContext, distributedCache);
            _statusTaskBusiness = new StatusTaskBusiness(mardisContext, distributedCache);
            _taskNotImplementedReasonBusiness = new TaskNotImplementedReasonBusiness(mardisContext);
            _hostingEnv = hostingEnvironment;
            _profileBusiness = new ProfileBusiness(mardisContext);
            if (ApplicationUserCurrent.UserId != null)
            {

                _userId = new Guid(ApplicationUserCurrent.UserId);
                _Profile = ApplicationUserCurrent.ProfileId;
                _typeuser = _profileBusiness.GetById(_Profile).IdTypeUser;
            }
        }
        #endregion

        [HttpGet]
        public string GetCampaignById(string idCampaign)
        {
            try
            {
                var itemReturn = new Campaign();

                if (!string.IsNullOrEmpty(idCampaign))
                {
                    itemReturn = _campaignBusiness.GetCampaignById(new Guid(idCampaign), ApplicationUserCurrent.AccountId);
                }

                return JSonConvertUtil.Convert(itemReturn);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }

        [HttpGet]
        public string GetSimpleCampaignById(string idCampaign)
        {
            try
            {
                var itemReturn = new Campaign();

                if (!string.IsNullOrEmpty(idCampaign))
                {
                    itemReturn = _campaignBusiness.GetSimpleCampaignById(new Guid(idCampaign), ApplicationUserCurrent.AccountId);
                }

                return JSonConvertUtil.Convert(itemReturn);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }
       
        [HttpGet]
        public string GetCampaignByName(string nameCampaign)
        {
            try
            {
                Campaign itemReturn = null;

                if (!string.IsNullOrEmpty(nameCampaign))
                {
                    itemReturn = _campaignBusiness.GetCampaignByName(nameCampaign, ApplicationUserCurrent.AccountId);
                }

                return JSonConvertUtil.Convert(itemReturn);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }
       private string EstadoLocal(string statusregister) {
            var STATUS = "";

            switch (statusregister)
            {
                case "A":
                    STATUS = "ACTIVO";
                    break;
                case "I":
                    STATUS = "INACTIVO";
                    break;
                case "N":
                    STATUS = "NUEVO";
                    break;
            }

            return STATUS; 
        }
        [HttpGet]
        public IActionResult ExportInforme(string id)
        {
            string sWebRootFolder = _hostingEnv.WebRootPath;

            var locales = _taskCampaignBusiness.ListBranch(id).Select(b => new
            {
                b.Id,
                b.Code,
                b.ExternalCode,
                NameBranch = b.Name,
                NameOwner = b.PersonOwner.Name,
                b.MainStreet,
                b.Reference,
                b.PersonOwner.Phone,
                b.TypeBusiness,
                b.LatitudeBranch,
                b.LenghtBranch,
                NameCity = b.District.Name,
                b.Cluster,
               estado= EstadoLocal(b.StatusRegister)
            }).ToList();

            var fotos = _taskCampaignBusiness.ListBranchImages(id).Select(bi => new
            {
                bi.IdBranch,
                bi.Branch.Code,
                bi.Branch.ExternalCode,
                bi.UrlImage
            }).ToList();

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
                ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Locales");
                ExcelWorksheet worksheet2 = package.Workbook.Worksheets.Add("Fotos");

                //First add the headers
                Color colFromHex = System.Drawing.ColorTranslator.FromHtml("#B7DEE8");

                worksheet.Column(1).Width = 20;
                worksheet.Column(2).Width = 10;
                worksheet.Column(3).Width = 12;
                worksheet.Column(4).Width = 30;
                worksheet.Column(5).Width = 30;
                worksheet.Column(6).Width = 40;
                worksheet.Column(7).Width = 30;
                worksheet.Column(8).Width = 15;
                worksheet.Column(9).Width = 15;
                worksheet.Column(10).Width = 16;
                worksheet.Column(11).Width = 16;
                worksheet.Column(12).Width = 20;
                worksheet.Column(13).Width = 10;

                worksheet2.Column(1).Width = 30;
                worksheet2.Column(2).Width = 10;
                worksheet2.Column(3).Width = 100;

                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 1].Style.Font.Size = 12;
                worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 1].Style.Font.Bold = true;

                worksheet.Cells[1, 2].Value = "Cod. Encuesta";
                worksheet.Cells[1, 2].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 2].Style.Font.Bold = true;
                worksheet.Cells[1, 2].Style.Font.Size = 12;
                worksheet.Cells[1, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(colFromHex);

                worksheet.Cells[1, 3].Value = "PT_INDICE";
                worksheet.Cells[1, 3].Style.Font.Size = 12;
                worksheet.Cells[1, 3].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 3].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 3].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 3].Style.Font.Bold = true;

                worksheet.Cells[1, 4].Value = "Nombre Local";
                worksheet.Cells[1, 4].Style.Font.Size = 12;
                worksheet.Cells[1, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 4].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 4].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 4].Style.Font.Bold = true;


                worksheet.Cells[1, 5].Value = "Nombre Propietario";
                worksheet.Cells[1, 5].Style.Font.Size = 12;
                worksheet.Cells[1, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 5].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 5].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 5].Style.Font.Bold = true;

                worksheet.Cells[1, 6].Value = "Dirección";
                worksheet.Cells[1, 6].Style.Font.Size = 12;
                worksheet.Cells[1, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 6].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 6].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 6].Style.Font.Bold = true;

                worksheet.Cells[1, 7].Value = "Referencia";
                worksheet.Cells[1, 7].Style.Font.Size = 12;
                worksheet.Cells[1, 7].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 7].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 7].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 7].Style.Font.Bold = true;

                worksheet.Cells[1, 8].Value = "Teléfono";
                worksheet.Cells[1, 8].Style.Font.Size = 12;
                worksheet.Cells[1, 8].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 8].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 8].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 8].Style.Font.Bold = true;

                worksheet.Cells[1, 9].Value = "Tipo Negocio";
                worksheet.Cells[1, 9].Style.Font.Size = 12;
                worksheet.Cells[1, 9].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 9].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 9].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 9].Style.Font.Bold = true;

                worksheet.Cells[1, 10].Value = "Latitud";
                worksheet.Cells[1, 10].Style.Font.Size = 12;
                worksheet.Cells[1, 10].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 10].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 10].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 10].Style.Font.Bold = true;

                worksheet.Cells[1, 11].Value = "Longitud";
                worksheet.Cells[1, 11].Style.Font.Size = 12;
                worksheet.Cells[1, 11].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 11].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 11].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 11].Style.Font.Bold = true;

                worksheet.Cells[1, 12].Value = "Ciudad";
                worksheet.Cells[1, 12].Style.Font.Size = 12;
                worksheet.Cells[1, 12].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 12].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 12].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 12].Style.Font.Bold = true;

                worksheet.Cells[1, 13].Value = "Clúster";
                worksheet.Cells[1, 13].Style.Font.Size = 12;
                worksheet.Cells[1, 13].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 13].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 13].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 13].Style.Font.Bold = true;

                worksheet.Cells[1, 14].Value = "Estado Local";
                worksheet.Cells[1, 14].Style.Font.Size = 12;
                worksheet.Cells[1, 14].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet.Cells[1, 14].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet.Cells[1, 14].Style.Font.Color.SetColor(Color.White);
                worksheet.Cells[1, 14].Style.Font.Bold = true;




                worksheet2.Cells[1, 1].Value = "ID";
                worksheet2.Cells[1, 1].Style.Font.Size = 12;
                worksheet2.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet2.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet2.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
                worksheet2.Cells[1, 1].Style.Font.Bold = true;

                worksheet2.Cells[1, 2].Value = "PT_INDICE";
                worksheet2.Cells[1, 2].Style.Font.Size = 12;
                worksheet2.Cells[1, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet2.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet2.Cells[1, 2].Style.Font.Color.SetColor(Color.White);
                worksheet2.Cells[1, 2].Style.Font.Bold = true;

                worksheet2.Cells[1, 3].Value = "Link Foto";
                worksheet2.Cells[1, 3].Style.Font.Size = 12;
                worksheet2.Cells[1, 3].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                worksheet2.Cells[1, 3].Style.Fill.BackgroundColor.SetColor(colFromHex);
                worksheet2.Cells[1, 3].Style.Font.Color.SetColor(Color.White);
                worksheet2.Cells[1, 3].Style.Font.Bold = true;


                int rows = 2;
                foreach (var t in locales)
                {
                    worksheet.Cells[rows, 1].Value = t.Id;
                    worksheet.Cells[rows, 2].Value = t.Code;
                    worksheet.Cells[rows, 3].Value = t.ExternalCode;
                    worksheet.Cells[rows, 4].Value = t.NameBranch.ToUpper();
                    worksheet.Cells[rows, 5].Value = t.NameOwner;
                    worksheet.Cells[rows, 6].Value = t.MainStreet;
                    worksheet.Cells[rows, 7].Value = t.Reference;
                    worksheet.Cells[rows, 8].Value = t.Phone;
                    worksheet.Cells[rows, 9].Value = t.TypeBusiness.ToUpper().TrimEnd().TrimStart();
                    worksheet.Cells[rows, 10].Value = t.LatitudeBranch;
                    worksheet.Cells[rows, 11].Value = t.LenghtBranch;
                    worksheet.Cells[rows, 12].Value = t.NameCity;
                    worksheet.Cells[rows, 13].Value = t.Cluster;
                    worksheet.Cells[rows, 14].Value = t.estado;
                    rows++;
                }

                int rowsfotos = 2;
                foreach (var t in fotos)
                {
                    worksheet2.Cells[rowsfotos, 1].Value = t.IdBranch;
                    worksheet2.Cells[rowsfotos, 2].Value = t.ExternalCode;
                    worksheet2.Cells[rowsfotos, 3].Value = t.UrlImage;
                    rowsfotos++;
                }

                var _model = from x in _taskCampaignBusiness.GetHistoryCampign(Guid.Parse(id))
                             select new { date = x.DateModification, status = x.StatusTask.Name, user = x.Users.Email, comment = x.CommentTaskNoImplemented, code = x.Tasks.Code, x.idtask };
                if (_model != null)
                {
                    _model = _model.OrderByDescending(x => x.idtask).ToList();

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

        #region Exportar
        [HttpGet]

        public IActionResult Export(string id)
        {
            var idCa = Guid.Empty;
            if (!string.IsNullOrEmpty(id))
            {
                idCa = Guid.Parse(Protector.Unprotect(id));
            }
            string sWebRootFolder = _hostingEnv.WebRootPath;

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
                ,
                Estado_c = x.PollsTaskss.First().pollsstatus
                ,
                Commentario = x.PollsTaskss.First().Comment
                ,
                x.IdAccount,
                factura = x.PollsTaskss.First().novelty != null ? "SI" : "NO"
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
                if (listado.First().IdAccount.Equals(Guid.Parse("85024910-FC12-4DD8-AE58-761BF972DEB7")))
                {
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
                    worksheet.Cells[rows, 5].Value = t.Name;
                    worksheet.Cells[rows, 6].Value = t.Estado_c;
                    worksheet.Cells[rows, 7].Value = t.Phone;
                    worksheet.Cells[rows, 8].Value = t.Encuestador;
                    worksheet.Cells[rows, 9].Value = t.StartDate;
                    worksheet.Cells[rows, 9].Style.Numberformat.Format = "yyyy-mm-dd";
                    worksheet.Cells[rows, 10].Value = t.Estado_Tarea;

                    worksheet.Cells[rows, 12].Value = t.Commentario;
                    if (!listado.First().IdAccount.Equals(Guid.Parse("85024910-FC12-4DD8-AE58-761BF972DEB7")))
                    {
                        worksheet.Cells[rows, 11].Value = t.TypeBusiness;
                    }
                    if (listado.First().IdAccount.Equals(Guid.Parse("85024910-FC12-4DD8-AE58-761BF972DEB7")))
                    {
                        worksheet.Cells[rows, 11].Value = t.factura;
                    }
                    rows++;

                }

                var _model = from x in _taskCampaignBusiness.GetHistoryCampign(idCa)
                             select new { date = x.DateModification, status = x.StatusTask.Name, user = x.Users.Email, comment = x.CommentTaskNoImplemented, code = x.Tasks.Code, x.idtask };

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

        //[HttpGet]

        //public IActionResult Export(string id)
        //{
        //    var idCa = Guid.Empty;
        //    if (!string.IsNullOrEmpty(id))
        //    {
        //        idCa = Guid.Parse(Protector.Unprotect(id));
        //    }
        //    string sWebRootFolder = _hostingEnv.WebRootPath;

        //    var listado = _taskCampaignBusiness.ListTask(idCa).Select(x => new
        //    {
        //        x.StartDate,
        //        x.Branch.Code,
        //        x.Branch.ExternalCode,
        //        x.Branch.TypeBusiness,
        //        x.Branch.Name,
        //        x.Branch.Cluster,
        //        Propietario = x.Branch.PersonOwner.Name,
        //        x.Branch.PersonOwner.Mobile,
        //        x.Branch.PersonOwner.Phone,
        //        Canton = x.Branch.District.Name,
        //        Estado_Tarea = x.StatusTask.Name,
        //        Encuestador = x.Pollster == null ? "Sin Indentificar" : x.Pollster.Name
        //    }).ToList();
        //    var log = DateTime.Now;
        //    string LogFile = log.ToString("yyyyMMddHHmmss");
        //    string sFileName = @"Listado.xlsx";
        //    string URL = string.Format("{0}://{1}/{2}", Request.Scheme, Request.Host, sFileName);
        //    FileInfo file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
        //    if (file.Exists)
        //    {
        //        file.Delete();
        //        file = new FileInfo(Path.Combine(sWebRootFolder, sFileName));
        //    }
        //    using (ExcelPackage package = new ExcelPackage(file))
        //    {
        //        // add a new worksheet to the empty workbook
        //        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Datos");
        //        //First add the headers
        //        Color colFromHex = System.Drawing.ColorTranslator.FromHtml("#B7DEE8");


        //        worksheet.Column(1).Width = 20;
        //        worksheet.Column(2).Width = 20;
        //        worksheet.Column(3).Width = 20;
        //        worksheet.Column(4).Width = 20;
        //        worksheet.Column(5).Width = 32;
        //        worksheet.Column(6).Width = 20;
        //        worksheet.Column(7).Width = 20;
        //        worksheet.Column(8).Width = 20;
        //        worksheet.Column(9).Width = 20;
        //        worksheet.Column(10).Width = 20;

        //        worksheet.Cells[1, 1].Value = "Ciudad";

        //        worksheet.Cells[1, 1].Style.Font.Color.SetColor(Color.White);
        //        worksheet.Cells[1, 1].Style.Font.Bold=true;
        //        worksheet.Cells[1, 1].Style.Font.Size = 12;

        //        worksheet.Cells[1, 1].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //        worksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(colFromHex);

        //        worksheet.Cells[1, 2].Value = "Cluster";
        //        worksheet.Cells[1, 2].Style.Font.Size = 12;
        //        worksheet.Cells[1, 2].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //        worksheet.Cells[1, 2].Style.Fill.BackgroundColor.SetColor(colFromHex);
        //        worksheet.Cells[1, 2].Style.Font.Color.SetColor(Color.White);
        //        worksheet.Cells[1, 2].Style.Font.Bold = true;

        //        worksheet.Cells[1, 3].Value = "Cod. Encuesta";
        //        worksheet.Cells[1, 3].Style.Font.Size = 12;
        //        worksheet.Cells[1, 3].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //        worksheet.Cells[1, 3].Style.Fill.BackgroundColor.SetColor(colFromHex);
        //        worksheet.Cells[1, 3].Style.Font.Color.SetColor(Color.White);
        //        worksheet.Cells[1, 3].Style.Font.Bold = true;


        //        worksheet.Cells[1, 4].Value = "PT_INDICE";
        //        worksheet.Cells[1, 4].Style.Font.Size = 12;
        //        worksheet.Cells[1, 4].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //        worksheet.Cells[1, 4].Style.Fill.BackgroundColor.SetColor(colFromHex);
        //        worksheet.Cells[1, 4].Style.Font.Color.SetColor(Color.White);
        //        worksheet.Cells[1, 4].Style.Font.Bold = true;

        //        worksheet.Cells[1, 5].Value = "Nombre local";
        //        worksheet.Cells[1, 5].Style.Font.Size = 12;
        //        worksheet.Cells[1, 5].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //        worksheet.Cells[1, 5].Style.Fill.BackgroundColor.SetColor(colFromHex);
        //        worksheet.Cells[1, 5].Style.Font.Color.SetColor(Color.White);
        //        worksheet.Cells[1, 5].Style.Font.Bold = true;
        //        worksheet.Cells[1, 6].Value = "Tipo de Negocio";
        //        worksheet.Cells[1, 6].Style.Font.Size = 12;
        //        worksheet.Cells[1, 6].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //        worksheet.Cells[1, 6].Style.Fill.BackgroundColor.SetColor(colFromHex);
        //        worksheet.Cells[1, 6].Style.Font.Color.SetColor(Color.White);
        //        worksheet.Cells[1, 6].Style.Font.Bold = true;
        //        worksheet.Cells[1, 7].Value = "Telefono";
        //        worksheet.Cells[1, 7].Style.Font.Size = 12;
        //        worksheet.Cells[1, 7].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //        worksheet.Cells[1, 7].Style.Fill.BackgroundColor.SetColor(colFromHex);
        //        worksheet.Cells[1, 7].Style.Font.Color.SetColor(Color.White);
        //        worksheet.Cells[1, 7].Style.Font.Bold = true;
        //        worksheet.Cells[1, 8].Value = "Encuestador";
        //        worksheet.Cells[1, 8].Style.Font.Size = 12;
        //        worksheet.Cells[1, 8].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //        worksheet.Cells[1, 8].Style.Fill.BackgroundColor.SetColor(colFromHex);
        //        worksheet.Cells[1, 8].Style.Font.Color.SetColor(Color.White);
        //        worksheet.Cells[1, 8].Style.Font.Bold = true;
        //        worksheet.Cells[1, 9].Value = "Fecha";
        //        worksheet.Cells[1, 9].Style.Font.Size = 12;
        //        worksheet.Cells[1, 9].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //        worksheet.Cells[1, 9].Style.Fill.BackgroundColor.SetColor(colFromHex);
        //        worksheet.Cells[1, 9].Style.Font.Color.SetColor(Color.White);
        //        worksheet.Cells[1, 9].Style.Font.Bold = true;
        //        worksheet.Cells[1, 10].Value = "Estado";
        //        worksheet.Cells[1, 10].Style.Font.Size = 12;
        //        worksheet.Cells[1, 10].Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
        //        worksheet.Cells[1, 10].Style.Fill.BackgroundColor.SetColor(colFromHex);
        //        worksheet.Cells[1,10].Style.Font.Color.SetColor(Color.White);
        //        worksheet.Cells[1, 10].Style.Font.Bold = true;

        //        int rows = 2;
        //        foreach (var t in listado)
        //        {
        //            worksheet.Cells[rows, 1].Value = t.Canton;
        //            worksheet.Cells[rows, 2].Value = t.Cluster;
        //            worksheet.Cells[rows, 3].Value = t.Code;
        //            worksheet.Cells[rows, 4].Value = t.ExternalCode;
        //            worksheet.Cells[rows, 5].Value = t.Name;
        //            worksheet.Cells[rows, 6].Value = t.TypeBusiness;
        //            worksheet.Cells[rows, 7].Value = t.Phone;
        //            worksheet.Cells[rows, 8].Value = t.Encuestador;
        //            worksheet.Cells[rows, 9].Value = t.StartDate;
        //            worksheet.Cells[rows, 9].Style.Numberformat.Format = "yyyy-mm-dd";
        //            worksheet.Cells[rows, 10].Value = t.Estado_Tarea;

        //            rows++;
        //        }
        //        //Add values


        //        package.Save(); //Save the workbook.
        //    }
        //    var result = PhysicalFile(Path.Combine(sWebRootFolder, sFileName), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        //    Response.Headers["Content-Disposition"] = new ContentDispositionHeaderValue("attachment")
        //    {
        //        FileName = file.Name
        //    }.ToString();

        //    return result;
        //}

        #endregion

        [HttpGet]
        public IActionResult Register(string idCampaign)
        {
            try
            {
                var id = Guid.Empty;
                if (!string.IsNullOrEmpty(idCampaign))
                {
                    id = Guid.Parse(Protector.Unprotect(idCampaign));
                }
                var model = _campaignBusiness.GetCampaign(id, ApplicationUserCurrent.AccountId);

                GetDropDownListData(model);

                return View(model);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }
             public IActionResult TasksCampaign(string idCampaign)
        {
            try
            {
                var id = Guid.Empty;
                if (!string.IsNullOrEmpty(idCampaign))
                {
                    id = Guid.Parse(Protector.Unprotect(idCampaign));
                }
                var model = _campaignBusiness.GetCampaign(id, ApplicationUserCurrent.AccountId);

                GetDropDownListData(model);

                return View(model);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

       [HttpPost]
        public IActionResult Register(CampaignRegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    GetDropDownListData(model);
                    return View(model);
                }
                _campaignBusiness.Save(model, ApplicationUserCurrent.AccountId);
                return RedirectToAction("Index");
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        private void GetDropDownListData(CampaignRegisterViewModel model)
        {
            var myWatch = new Stopwatch();
            ViewBag.CustomerList =
                _customerBusiness.GetCustomersByAccount(ApplicationUserCurrent.AccountId)
                    .Select(s => new SelectListItem() { Text = s.Name, Value = s.Id.ToString() })
                    .ToList();

            myWatch.Start();
            ViewBag.StatusList =
                _statusCampaignBusiness.GetStatusCampaigns()
                    .Select(s => new SelectListItem() { Text = s.Name, Value = s.Id.ToString() })
                    .ToList();
            myWatch.Stop();

            Debugger.Log(0, "Drop", $"ms: {myWatch.ElapsedMilliseconds}");

            ViewBag.SupervisorList =
                _userBusiness.GetUserListByType(CTypePerson.PersonSupervisor, ApplicationUserCurrent.AccountId)
                    .Select(s => new SelectListItem() { Text = s.Profile.Name, Value = s.Id.ToString() })
                    .ToList();

            ViewBag.ChannelList =
                _channelBusiness.GetChanelListByCustomer(model.IdCustomer, ApplicationUserCurrent.AccountId);

            ViewBag.Services =
                _serviceBusiness.GetServicesByChannelId(ApplicationUserCurrent.AccountId, model.IdChannel)
                    .Select(c => new SelectListItem() { Text = c.Name, Value = c.Id.ToString() })
                    .ToList();
        }

        #region Guardar

        [HttpPost]
        public string SaveCampaign(Campaign campaign, string inputServices)
        {
            try
            {
                var idAccount = ApplicationUserCurrent.AccountId;
                var itemServices = JsonConvert.DeserializeObject<List<ListCampaignServicesViewModel>>(inputServices);

                campaign = _campaignBusiness.SaveCampaign(campaign, itemServices, idAccount);

                return JSonConvertUtil.Convert(campaign);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return null;
            }
        }

        #endregion

        [HttpPost]
        public override bool Delete(string input)
        {
            var itemIds = JsonConvert.DeserializeObject<string[]>(input);

            var campaignIds = itemIds.Select(i => Protector.Unprotect(i)).ToList();

            var idTasks = "";

            foreach (var tasks in itemIds.Select(item => _taskCampaignBusiness.GetAlltasksByCampaignId(new Guid(Protector.Unprotect(item)), ApplicationUserCurrent.AccountId)))
            {
                foreach (var tsk in tasks)
                {
                    if ((idTasks.IndexOf(',') < 0) && (!(string.IsNullOrEmpty(idTasks))) ||
                        (idTasks.IndexOf(',') >= 0) && (!(string.IsNullOrEmpty(idTasks))))
                    {
                        idTasks += ",";
                    }
                    idTasks += "\"" + tsk.Id + "\"";
                }
                idTasks = "[" + idTasks + "]";

                var idsTask = JsonConvert.DeserializeObject<string[]>(idTasks);

                if (idsTask.Any())
                {
                    _commonBusiness.DeleteId(CTask.TableName, idsTask);
                }
            }

            return base.Delete(JSonConvertUtil.Convert(campaignIds));
        }

        [HttpGet]
        public string GetCampaignTaskDetails(Guid idCampaign)
        {
            var result = _campaignBusiness.GetCampaignTaskDetails(idCampaign, ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(result);
        }

        [HttpGet]
        public string GetCampaignTaskDetailsByMerchant(Guid idCampaign, Guid idMerchant)
        {
            var result = _campaignBusiness.GeCampaignTaskDetailsByMerchant(idCampaign, idMerchant, ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(result);
        }

        [HttpGet]
        public string GetCampaignTasks(Guid idCampaign)
        {
            var listResult = _taskCampaignBusiness.GetAlltasksByCampaignId(idCampaign, ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(listResult);
        }


        [HttpGet]
        public IActionResult TasksPerCampaign(string idCampaign, string filterValues, bool deleteFilter, string view, int pageIndex = 1, int pageSize = 10)
        {
            try
            {
                if (!string.IsNullOrEmpty(idCampaign))
                {
                    SetSessionVariable("idCampaign", idCampaign);
                }
                else
                {
                    idCampaign = GetSessionVariable("idCampaign");
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
                    id = Guid.Parse(Protector.Unprotect(idCampaign));
                }
                var filters = GetFilters(filterValues, deleteFilter);
                if (filters.Where(x => x.NameFilter == "IdCampaign").Count() > 0)
                {
                    var varcampid = filters.Where(x => x.NameFilter == "IdCampaign").First().Value;
                    id = Guid.Parse(varcampid);
                    idCampaign = Protector.Protect(varcampid);
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
                _MyTask.Properties.ActionName = "TasksPerCampaign";
                _MyTask.Properties.ControllerName = "Campaign";
                return View(_MyTask);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        [HttpGet]
        public IActionResult Geoposition(string campaign, string filterValues, bool deleteFilter)
        {
            try
            {
                if (!string.IsNullOrEmpty(campaign))
                {
                    SetSessionVariable("idCampaign", campaign);
                }
                else
                {
                    campaign = GetSessionVariable("idCampaign");
                }
                var filters = GetFilters(filterValues, deleteFilter);
                var ubicationList = _campaignBusiness.GetCampaignGeoposition(filters, campaign, ApplicationUserCurrent.AccountId, Protector);
                ubicationList.Properties.ControllerName = "Campaign";
                ubicationList.Properties.ActionName = "Geoposition";
                return View(ubicationList);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }
       

        [HttpGet]
        public string GetActiveCampaignsList()
        {
            var listResult = _campaignBusiness.GetActiveCampaignsList(ApplicationUserCurrent.AccountId);

            return JSonConvertUtil.Convert(listResult);
        }

        [HttpGet]
        public IActionResult TaskPoll(Guid idTask)
        {
            try
            {
                ViewData[CTask.IdRegister] = idTask.ToString();

                ViewBag.StatusList = _statusTaskBusiness.GetAllStatusTasks(ApplicationUserCurrent.AccountId, Guid.Parse(ApplicationUserCurrent.UserId))
                    .Select(s => new SelectListItem() { Text = s.Name, Value = s.Id.ToString() })
                    .ToList();

                ViewBag.ReasonsList =
                    _taskNotImplementedReasonBusiness.GetAllTaskNotImplementedReason()
                        .Select(t => new SelectListItem() { Value = t.Id.ToString(), Text = t.Name })
                        .ToList();

                return View();
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        public IActionResult Index(string filterValues, bool deleteFilter, int pageSize = 12, int pageIndex = 1)
        {
            try
            {
                var filters = GetFilters(filterValues, deleteFilter);
                var campaigns = _campaignBusiness.GetPaginatedCampaignsDinamic(filters, pageSize, pageIndex, ApplicationUserCurrent.AccountId, Protector, _userId, _typeuser);
                return View(campaigns);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        public IActionResult ImportBranches(string campaign, string filterValues, bool deleteFilter)
        {
            try
            {
                if (!string.IsNullOrEmpty(campaign))
                {
                    SetSessionVariable("idCampaign", campaign);
                }
                else
                {
                    campaign = GetSessionVariable("idCampaign");
                }
                var filters = GetFilters(filterValues, deleteFilter);
                var result = _campaignBusiness.ImportBranch(ApplicationUserCurrent.AccountId, filters);

                return View(result);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

      

            public IActionResult SelectBranches()
        {
            try
            {
                var filters = JSonConvertUtil.Deserialize<List<FilterValue>>(GetSessionVariable("filter"));
                var model = _campaignBusiness.GetBranchesSelected(ApplicationUserCurrent.AccountId, filters);
                return RedirectToAction("ImportBranches", model);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }
        #region Administracion de Rutas
        public IActionResult AdminRoute() {

            return View();
        }
        public IActionResult Route()
        {

            return View();
        }

        public IActionResult Pollsters(string returnUrl = null)
        {

            ViewData["ReturnUrl"] = returnUrl;
            
            var _model = _campaignBusiness.GetPollster();
            return View(_model);
        }

        public IActionResult PollsterRegister(int idPolls, string returnUrl = null)
        {

            try
            {

                ViewData["ReturnUrl"] = returnUrl;
                var model = idPolls != 0 ? _campaignBusiness.GetPollster(idPolls) : null;
                if (model == null)
                {
                    model = new PollsterRegisterViewModel();
                 
                }
                return View(model);
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }
        [HttpPost]
        public IActionResult PollsterRegister(PollsterRegisterViewModel _model, string returnUrl = null)
        {

            try
            {
                if (!ModelState.IsValid)
                {
                 
                    return View(_model);
                }
                _campaignBusiness.SavePollsters(_model);
               
                return RedirectToAction("Pollsters");

            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }
        }

        public IActionResult DeletePollster(int idPolls, string returnUrl = null)
        {
          var result=  _campaignBusiness.DeletePollster(idPolls);
            if (result != 3)
            {
                return RedirectToAction("Pollsters");
            }
            else {
                return RedirectToAction("Pollsters", new { returnUrl = "Tasks" });
            }
            

        }

        public IActionResult UpdatePollster(string Imei, string returnUrl = null)
        {
            var result = _campaignBusiness.StatusPollster(Imei);
          
                return RedirectToAction("Pollsters", new { returnUrl = "Branch" });
           


        }
        public JsonResult ActiveRoute()
        {
            var model = _campaignBusiness.GetActiveRoute(ApplicationUserCurrent.AccountId);
            return Json(model);
        }

        [HttpPost]
        public async Task<JsonResult> ChangeStatus(string id)
        {
            int model = await _campaignBusiness.ChangeStatusRoute(ApplicationUserCurrent.AccountId, id);
            return Json(model);
        }


        public JsonResult GetEncuestador(string route)
        {


            var model = _campaignBusiness.GetRoute(ApplicationUserCurrent.AccountId, route);

            return Json(model);
        }


        public JsonResult deleteEcuestador(string route, string imeid)
        {


            var model = _campaignBusiness.deleteRoute(ApplicationUserCurrent.AccountId, route, imeid);

            return Json(model);
        }
        public JsonResult deleteAccount(string active)
        {


            var model = _campaignBusiness.deleteCuenta(ApplicationUserCurrent.AccountId, active);

            return Json(model);
        }

        public JsonResult GetActiveEncuestador(string id)
        {


            var model = _campaignBusiness.GetEncuestadoresbyIMEI(ApplicationUserCurrent.AccountId);
            var _autocompleteModel = from x in model
                                     select new { name=x.Name+"("+x.IMEI+")" , abbr=x.Name, id=x.IMEI , phone=x.Phone };           
            return Json(_autocompleteModel);
        }
        public JsonResult SaveEncuestador(string route, string id)
        {


            var model = _campaignBusiness.SaveImei(ApplicationUserCurrent.AccountId,id,route);

            return Json(model);
        }


        #endregion
        #region modoSupervisor
        [HttpGet]
        public IActionResult ProfileCampaign(Guid idTask , String idcampaing)
        {
            try
            {
                ViewData[CTask.IdRegister] = idTask.ToString();
     
                ViewData[CTask.IdCampaing] =idcampaing;
                LoadSelectItems();

                return View();
            }
            catch (Exception e)
            {
                _logger.LogError(new EventId(0, "Error Index"), e.Message);
                return RedirectToAction("Index", "StatusCode", new { statusCode = 1 });
            }

        }
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

        #endregion
    }
}

