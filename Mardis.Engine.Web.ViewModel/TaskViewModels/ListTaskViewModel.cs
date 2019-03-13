using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mardis.Engine.Web.ViewModel.TaskViewModels
{
 public   class ListTaskViewModel
    {
        public Guid Id { get; set; }

        public DateTime StartDate { get; set; }

        public string CampaignName { get; set; }

        public string BranchName { get; set; }


        public string Street { get; set; }


        public string status { get; set; }

        public string comment { get; set; }

        public Guid BranchId { get; set; }

        public string BranchExternalCode { get; set; }

        public string BranchMardisCode { get; set; }

        public string Route { get; set; }
        public string NamePollster { get; set; }

        public string TypeBussiness { get; set; }
        public string Code { get; set; }
        public string Longitude { get; set; }
        public string city { get; set; }
        public string Latitude { get; set; }
        public string StatusMigrate { get; set; }
        public string Icon { get; set; }


    }
}
