using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mardis.Engine.DataAccess.MardisSecurity;
namespace Mardis.Engine.DataAccess.MardisCore
{

    [Table("historialTareas", Schema = "MardisCore")]
    public class historialTareas : IEntityId
    {
        [Key]
        public int Id { get; set; }


        public Guid idtask { get; set; }
        public Guid IdStatusTask { get; set; }


        public DateTime DateUpdate { get; set; } = DateTime.Now;

        public DateTime DateModification { get; set; } = DateTime.Now;

        public Guid? UserValidator { get; set; } 
        public string CommentTaskNoImplemented { get; set; }

        [ForeignKey("IdStatusTask")]
        public StatusTask StatusTask { get; set; }


        [ForeignKey("UserValidator")]
        public User Users { get; set; }

        [ForeignKey("idtask")]
        public TaskCampaign Tasks { get; set; }


    }
}
