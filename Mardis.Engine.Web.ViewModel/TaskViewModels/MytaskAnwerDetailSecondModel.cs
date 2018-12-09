using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mardis.Engine.Web.ViewModel.TaskViewModels
{
   public class MytaskAnwerDetailSecondModel
    {

     
        public int Id { get; set; }
        public string Marca { get; set; }
        public decimal? PrecioSaco { get; set; }
        public string Descuento { get; set; }
        public decimal? ValorDescuento { get; set; }
        public string RequisitosDescuento { get; set; }
        public int? Peso { get; set; }
        public string Factura { get; set; }
        public string ImagenFactura { get; set; }
        public List<string> QuestionComplete = new List<string>();
        public string ImagenSacos { get; set; }
        public Guid AnswerDetailId { get; set; }

 
    }
}
