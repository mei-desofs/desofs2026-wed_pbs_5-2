namespace LawyerApp.Shared.DI
{
    public static class SharedDependecies
    {
        public static void RegisterSharedDependecies(this IServiceCollection services)
        {
            services.AddScoped<Abstractions.IJwtProvider, Infrastructure.Authentication.JwtProvider>();
        }
    }
}
