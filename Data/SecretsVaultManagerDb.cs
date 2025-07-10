using Microsoft.EntityFrameworkCore;
using SecretVaultManager.Models;

namespace SecretVaultManager.Data
{
    public class SecretsVaultManagerDb : DbContext
    {
        public SecretsVaultManagerDb(DbContextOptions<SecretsVaultManagerDb> options) : base(options) { }

        public DbSet<Secret> Secrets { get; set; }
        public DbSet<User> Users { get; set; }
    }
}
