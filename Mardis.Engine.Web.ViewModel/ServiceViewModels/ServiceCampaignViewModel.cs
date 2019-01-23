using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mardis.Engine.Web.ViewModel.ServiceViewModels
{
   public class ServiceCampaignViewModel
    {
        public List<AccountCampaignViewModel> Campaings { get; set; } = new List<AccountCampaignViewModel>();

        public List<ServiceCampaingItemViewModel> Services { get; set; } = new List<ServiceCampaingItemViewModel>();


    }
}
