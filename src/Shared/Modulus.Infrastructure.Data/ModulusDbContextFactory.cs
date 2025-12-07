using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Modulus.Infrastructure.Data;

public class ModulusDbContextFactory : IDesignTimeDbContextFactory<ModulusDbContext>
{
    public ModulusDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ModulusDbContext>();
        optionsBuilder.UseSqlite("Data Source=modulus.db");

        return new ModulusDbContext(optionsBuilder.Options);
    }
}

