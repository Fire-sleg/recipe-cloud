using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace AuthService.Data.Models.RequestModels
{
    public class UserRegisterRequest
    {
        //[Required(ErrorMessage =ErrorMessages.UserNameRequired)]
        public string UserName { get; set; }

        //[Required(ErrorMessage =ErrorMessages.EmailRequired)]
        //[EmailAddress(ErrorMessage = ErrorMessages.EmailValidError)]
        public string Email { get; set; }

        //[Required(ErrorMessage =ErrorMessages.PasswordRequired)]
        //[MinLength(8,ErrorMessage =ErrorMessages.MinPasswordLength)]
        public string Password { get; set; }

        //[Required(ErrorMessage = ErrorMessages.FirstNasmeRequired)]
        //[MaxLength(25, ErrorMessage = ErrorMessages.MaxNamesLength)]
        public string FirstName { get; set; }

        //[Required(ErrorMessage = ErrorMessages.LastNameRequired)]
        //[MaxLength(25, ErrorMessage = ErrorMessages.MaxNamesLength)]
        public string LastName { get; set; }

        //[Required(ErrorMessage = ErrorMessages.MidleNameRequired)]
        //[MaxLength(25, ErrorMessage = ErrorMessages.MaxNamesLength)]
        public string MiddleName { get; set; }
    }
}
