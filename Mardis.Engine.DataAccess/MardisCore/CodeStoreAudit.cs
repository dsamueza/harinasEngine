using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mardis.Engine.DataAccess.MardisCore
{
    [Table("CodeStoreAudit", Schema = "MardisCore")]
    public class CodeStoreAudit : IEntityId
    {
        [Key]
        public int Id { get; set; }
        public int lastCode { get; set; }
        public string Code { get; set; }
        public Guid? idbrach { get; set; } = Guid.Empty;
        public Guid? Users { get; set; } = Guid.Empty;
        public DateTime creationDate { get; set; }
        public DateTime? assignDate { get; set; } = DateTime.Now;
        public string cityCode { get; set; }

    }
}
