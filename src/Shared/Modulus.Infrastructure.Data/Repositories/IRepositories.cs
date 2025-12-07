using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Modulus.Infrastructure.Data.Models;

namespace Modulus.Infrastructure.Data.Repositories;

public interface IModuleRepository
{
    Task<ModuleEntity?> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModuleEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModuleEntity>> GetEnabledModulesAsync(CancellationToken cancellationToken = default);
    Task UpsertAsync(ModuleEntity module, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
    Task UpdateStateAsync(string id, ModuleState state, CancellationToken cancellationToken = default);
}

public interface IMenuRepository
{
    Task<IReadOnlyList<MenuEntity>> GetByModuleIdAsync(string moduleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuEntity>> GetAllEnabledAsync(CancellationToken cancellationToken = default);
    Task ReplaceModuleMenusAsync(string moduleId, IEnumerable<MenuEntity> menus, CancellationToken cancellationToken = default);
}

