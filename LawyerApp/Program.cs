using LawyerApp.Domain.Interfaces.Security;
using LawyerApp.Infrastructure.HashiCorp;
using LawyerApp.Infrastructure.Persistence;
using LawyerApp.Infrastructure.Persistence.Repositories;
using LawyerApp.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

var builder = WebApplication.CreateBuilder(args);

// Define env variables
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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


// DataBase Connection
var connectionString = builder.Configuration.GetSection("ConnectionString:" +"PostgreSQL").Get<String>();
builder.Services.AddDbContext<LawyerAppDbContext>(options =>
    options.UseNpgsql(connectionString));


builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ILegalProcessRepository, LegalProcessRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddScoped<LawyerApp.Application.Interfaces.User.IClient, LawyerApp.Application.Services.UserAggregate.ClientService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
