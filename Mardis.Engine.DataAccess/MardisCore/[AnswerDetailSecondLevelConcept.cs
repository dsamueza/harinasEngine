using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mardis.Engine.DataAccess.MardisCore
{
    [Table("AnswerDetailSecondLevelConcept", Schema = "MardisCore")]
    public class _AnswerDetailSecondLevelConcept : IEntityId
    {

        public int Id { get; set; }
        public int AnswerDetailSecondLevelid { get; set; }
        public Guid IdquestionDetail { get; set; }
        public string   CodeConcept { get; set; } 
        public string comment  { get; set; }

        [ForeignKey("AnswerDetailSecondLevelid")]
        public AnswerDetailSecondLevel AnswerdetailModelSeconds { get; set; }
    }
}
