using AuthService.Entities;

namespace AuthService.Data.Models.RequestModels
{
    public class UserLoginResponse
    {
        public string Id { get; set; }
        public string Username { get; set; }
        public string Role { get; set; } 
        public Token TokenModel { get; set; }

        public UserLoginResponse(string id, string username, string role, Token token)
        {
            Id = id;
            Username = username;
            Role = role;
            TokenModel = token;
        }
    }

}
