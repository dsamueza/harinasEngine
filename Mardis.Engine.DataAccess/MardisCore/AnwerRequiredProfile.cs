using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mardis.Engine.DataAccess.MardisCore
{
    [Table("AnwerRequiredProfile", Schema = "MardisCore")]
  public  class AnwerRequiredProfile
    {
        public int Id { get; set; }   
        public Guid idProfile { get; set; }
        public string permissionType { get; set; }
        public Guid IdQuestion { get; set; }
        public Guid Idstatustask { get; set; }
}
}
