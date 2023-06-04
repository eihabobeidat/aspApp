using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class RegisterDTO
    {
        [Required]
        public string username { get; set; }
        [Required]
        [MinLength(8)]
        [MaxLength(20)]
        /*[StringLength(20, MinimumLength = 8)] another way to implment attributed above*/
        public string password { get; set; }

        [Required]public string knownAs { get; set; }
        [Required] public string gender { get; set; }
        [Required] public DateOnly? birthOfDate { get; set; } //to make required work, this should be optional so that the server do not fill it with the default value which is the current date.
        [Required] public string city { get; set; }
        [Required] public string country { get; set; }

    }
}
