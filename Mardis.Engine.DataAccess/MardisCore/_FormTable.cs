using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mardis.Engine.DataAccess.MardisCore
{
    [Table("_FormTable", Schema = "MardisCore")]
    public class _FormTable
    {
        [Key]
        public int id { get; set; }
        public string _table { get; set; }
        public Guid idservice { get; set; }
        public string typeoftable { get; set; }
        public DateTime? createDate { get; set; }
    }
}
