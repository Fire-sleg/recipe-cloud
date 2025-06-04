using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace AuthService.Data.Models.RequestModels
{
    public class UserRegisterResponse
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        //public string FirstName { get; set; }
        //public string LastName { get; set; }
        //public string MiddleName { get; set; }
    }
}
