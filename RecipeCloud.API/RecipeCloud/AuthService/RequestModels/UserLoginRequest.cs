using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AuthService.Data.Models.RequestModels
{
	public class UserLoginRequest
	{
        //[Required(ErrorMessage = ErrorMessages.EmailRequired)]
        //[EmailAddress(ErrorMessage = ErrorMessages.EmailValidError)]
        [DefaultValue("string")]
        public string Email { get; set; }
        //[Required(ErrorMessage = ErrorMessages.PasswordRequired)]
        //[MinLength(8, ErrorMessage = ErrorMessages.MinPasswordLength)]
        [DefaultValue("String123!")]
        public string Password { get; set; }
	}
}
