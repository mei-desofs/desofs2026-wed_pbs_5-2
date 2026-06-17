namespace LawyerApp.Application.DI
{
    public static class ApplicationDependecies
    {
        public static void RegisterApplicationDependecies(this IServiceCollection services)
        {

            services.AddScoped<Interfaces.User.IClient,Services.UserAggregate.ClientService>();
            services.AddScoped<Interfaces.User.IUserService, Services.UserAggregate.UserService>();
            services.AddScoped<Interfaces.Login.ILogin,Services.Login.LoginService>();
            services.AddScoped<Interfaces.LegalProcess.ILegalProcessService, Services.LegalProcess.LegalProcessService>();
            services.AddScoped<Interfaces.Document.IDocumentService, Services.Document.DocumentService>();
            services.AddScoped<Interfaces.Audit.IAuditService, Services.Audit.AuditService>();

        }
    }
}
