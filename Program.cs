using Microsoft.EntityFrameworkCore;
using SecretVaultManager.Crypto.Services;
using SecretVaultManager.Data;
using SecretVaultManager.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Add fake KMS service
builder.Services.AddSingleton<IKeyManagementService, KeyManagementService>();

// Add Encryption Service
builder.Services.AddSingleton<ISecretEncryptionService, SecretEncryptionService>();

// API - Services
builder.Services.AddScoped<ISecretService, SecretService>();

// Add database
builder.Services.AddDbContext<SecretsVaultManagerDb>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("SecretVaultManagerDb")));


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
