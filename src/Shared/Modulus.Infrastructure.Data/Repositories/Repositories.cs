using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Modulus.Infrastructure.Data.Models;

namespace Modulus.Infrastructure.Data.Repositories;

public class ModuleRepository : IModuleRepository
{
    private readonly ModulusDbContext _context;

    public ModuleRepository(ModulusDbContext context)
    {
        _context = context;
    }

    public Task<ModuleEntity?> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        return _context.Modules
            .Include(m => m.Menus)
            .FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<ModuleEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Modules
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ModuleEntity>> GetEnabledModulesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Modules
            .AsNoTracking()
            .Where(m => m.IsEnabled)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertAsync(ModuleEntity module, CancellationToken cancellationToken = default)
    {
        var existing = await _context.Modules.FindAsync(new object[] { module.Id }, cancellationToken);
        if (existing == null)
        {
            await _context.Modules.AddAsync(module, cancellationToken);
        }
        else
        {
            // Update properties
            existing.Name = module.Name;
            existing.Version = module.Version;
            existing.Path = module.Path;
            existing.IsSystem = module.IsSystem;
            existing.Author = module.Author;
            existing.Website = module.Website;
            existing.MenuLocation = module.MenuLocation;
            existing.EntryComponent = module.EntryComponent;
            existing.State = module.State;
            // Note: IsEnabled is preserved unless explicitly reset logic is needed
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var module = await _context.Modules.FindAsync(new object[] { id }, cancellationToken);
        if (module != null)
        {
            _context.Modules.Remove(module);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateStateAsync(string id, ModuleState state, CancellationToken cancellationToken = default)
    {
        var module = await _context.Modules.FindAsync(new object[] { id }, cancellationToken);
        if (module != null)
        {
            module.State = state;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class MenuRepository : IMenuRepository
{
    private readonly ModulusDbContext _context;

    public MenuRepository(ModulusDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<MenuEntity>> GetByModuleIdAsync(string moduleId, CancellationToken cancellationToken = default)
    {
        return await _context.Menus
            .AsNoTracking()
            .Where(m => m.ModuleId == moduleId)
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MenuEntity>> GetAllEnabledAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Menus
            .AsNoTracking()
            .Include(m => m.Module)
            .Where(m => m.Module.IsEnabled && m.Module.State == ModuleState.Ready)
            .OrderBy(m => m.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceModuleMenusAsync(string moduleId, IEnumerable<MenuEntity> menus, CancellationToken cancellationToken = default)
    {
        // Use a transaction
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var existingMenus = await _context.Menus
                    .Where(m => m.ModuleId == moduleId)
                    .ToListAsync(cancellationToken);

                _context.Menus.RemoveRange(existingMenus);
                await _context.Menus.AddRangeAsync(menus, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }
}

