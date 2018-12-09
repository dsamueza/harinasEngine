using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mardis.Engine.Web.ViewModel.UserViewModels
{
  public  class ViewSecurityTasksModel
    {
        public Guid Idtask { get; set; }
        public Guid Iduser { get; set; }
        public string Mail { get; set; }
    }
}
