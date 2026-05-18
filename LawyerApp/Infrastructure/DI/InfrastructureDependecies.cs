using LawyerApp.Application.Interfaces.Security;
using LawyerApp.Domain.Aggregates.UserAggregate.Interfaces;
using LawyerApp.Infrastructure.Persistence;
using LawyerApp.Infrastructure.Persistence.Repositories;
using LawyerApp.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace LawyerApp.Infrastructure.DI
{
    public static class InfrastructureDependecies
    {
        public static void RegisterInfrastructureDependecies(this IServiceCollection services, WebApplicationBuilder builder)
        {

            // Database Connection String Retrieval FRom HASHICORP VAULT
            /*
            var vaultSettings = builder.Configuration.GetSection("VaultSettings").Get<VaultSettings>();
            IAuthMethodInfo authMethod = new TokenAuthMethodInfo(vaultSettings.Token);
            var vaultClientSettings = new VaultClientSettings(vaultSettings.ServerUri, authMethod)
            {
                 Namespace = vaultSettings.Namespace
            };
            var vaultClient = new VaultClient(vaultClientSettings);
            var dbSecret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                path: vaultSettings.SecretPath,
                mountPoint: vaultSettings.MountPoint
            );
            var connectionString = dbSecret.Data.Data["uri"].ToString();
            */

            var connectionString = builder.Configuration.GetSection("ConnectionString:" + "PostgreSQL").Get<String>();
            builder.Services.AddDbContext<LawyerAppDbContext>(options =>
                options.UseNpgsql(connectionString));


            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ILegalProcessRepository, LegalProcessRepository>();
            builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
            builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        }
    }
}
