using System;
using System.Collections.Generic;

namespace Mardis.Engine.Web.ViewModel.BranchViewModels
{
    public class BranchTaskCampaignsProfileViewModel
    {

        public Guid Id { get; set; }
        
        public string TaskCode { get; set; }

        public string CampaignName { get; set; }

        public string MerchantName { get; set; }

        public string StatusName { get; set; }

        public DateTime StartDate { get; set; }
        public Guid Idcampaign { get; set; }

        public string Icon { get; set; }

        public string IconColor { get; set; }
        public string Comment { get; set; }
        public string pollster { get; set; }
        public string Route { get; set; }

        public DateTime DateModi { get; set; }
        public List<BranchCampaignServicesProfileViewModel> CampaignServices { get; set; }

    }
}