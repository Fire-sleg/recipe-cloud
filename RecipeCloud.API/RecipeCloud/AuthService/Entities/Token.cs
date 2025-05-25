namespace AuthService.Entities
{
    public class Token
    {
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpireDate { get; set; }
    }
}
