using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mardis.Engine.DataAccess.MardisCore
{
    [Table("AnswerDetailSecondLevel", Schema = "MardisCore")]
    public class AnswerDetailSecondLevel : IEntityId
    {

        public AnswerDetailSecondLevel()
        {
            _AnswerDetailSecondLevelConceptData = new List<_AnswerDetailSecondLevelConcept>();
        }
        [Key]
        public int Id { get; set; }
        public string Marca { get; set; }
        public decimal? PrecioSaco { get; set; }
        public string Descuento { get; set; }
        public decimal? ValorDescuento { get; set; }
        public string RequisitosDescuento { get; set; }
        public int? Peso { get; set; }
        public string Factura { get; set; }
        public string ImagenFactura { get; set; }

        public string ImagenSacos { get; set; }
        public Guid AnswerDetailId { get; set; }

        [ForeignKey("AnswerDetailId")]
        public AnswerDetail AnswerdetailModel { get; set; }
        public List<_AnswerDetailSecondLevelConcept> _AnswerDetailSecondLevelConceptData { get; set; }
    }
}
