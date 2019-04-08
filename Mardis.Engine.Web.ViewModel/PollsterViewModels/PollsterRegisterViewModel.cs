using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mardis.Engine.Web.ViewModel.PollsterViewModels
{
 public   class PollsterRegisterViewModel
    {
        public int Id { get; set; }
        [Required]
        [Display(Name = "Nombre de Encuestador")]
        public string Name { get; set; }
        [Required]
        public string IMEI { get; set; }

        [Display(Name = "Telefono")]
        public string Phone { get; set; }
        public string Qsupport { get; set; }
        [Required]
        [Range(typeof(DateTime), "1/1/2016", "1/1/2040")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Inicio")]
        public DateTime Fecha_Inicio { get; set; }
        [Required]
        [Range(typeof(DateTime), "1/1/2016", "1/1/2040")]
        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Fin")]
        public DateTime Fecha_Fin { get; set; }

        [Display(Name = "Estado")]
        public string Status { get; set; }
        [Required]

        public string Oficina { get; set; }

        [Display(Name = "Usuario Movil")]
        public string UserCel { get; set; }
        [Display(Name = "Password Movil")]
        public string PassCel { get; set; }
        public Guid? idaccount { get; set; }

    }

    
}
