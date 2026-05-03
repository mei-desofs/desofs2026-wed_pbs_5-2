namespace LawyerApp.Application.DTOS.Login
{
    public class LoginOutputDTO
    {
        public string Token { get; } = string.Empty;
        public string User_Role { get; } = string.Empty;
        public LoginOutputDTO() { }
        public LoginOutputDTO(string token, string user_Role)
        {
            Token = token;
            User_Role = user_Role;
        }
    }
}
