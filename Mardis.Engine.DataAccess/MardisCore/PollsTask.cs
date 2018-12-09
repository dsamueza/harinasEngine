using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mardis.Engine.DataAccess.MardisCore
{
    [Table("pollsTask", Schema = "MardisCore")]
    public class PollsTask : IEntityId
    {
        [Key]
        public int Id { get; set; }
        public string Comment { get; set; }
        public string pollsstatus { get; set; }
        public string core { get; set; }
        public Guid idtask { get; set; }
        public string  novelty { get; set; }
        [ForeignKey("idtask")]
        public TaskCampaign Tasks { get; set; }



    }
}