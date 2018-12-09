using Mardis.Engine.Web.Libraries.Model;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Mardis.Engine.Web.Model;
using Mardis.Engine.DataAccess.MardisCommon;
using Mardis.Engine.Web.ViewModel.PollsterViewModels;

namespace Mardis.Engine.Web.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Mardis.Engine.DataAccess.MardisCommon.Pollster> Pollster { get; set; }
        public DbSet<Mardis.Engine.Web.ViewModel.PollsterViewModels.PollsterRegisterViewModel> PollsterRegisterViewModel { get; set; }
    }
}
