namespace LawyerApp.Application.DI
{
    public static class ApplicationDependecies
    {
        public static void RegisterApplicationDependecies(this IServiceCollection services)
        {

            services.AddScoped<Interfaces.User.IClient,Services.UserAggregate.ClientService>();
            services.AddScoped<Interfaces.Login.ILogin,Services.Login.LoginService>();

        }
    }
}
