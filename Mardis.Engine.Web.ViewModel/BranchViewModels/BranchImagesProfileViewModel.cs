using System;

namespace Mardis.Engine.Web.ViewModel.BranchViewModels
{
    public class BranchImagesProfileViewModel
    {

        public Guid Id { get; set; }

        public string FileName { get; set; }

        public string Base64Image { get; set; }

        public Guid? Idtask { get; set; }

        public Guid? Idcampaign { get; set; }
      

    }
}