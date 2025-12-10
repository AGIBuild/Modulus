using System.Threading;
using System.Threading.Tasks;

namespace Modulus.Core.Runtime;

/// <summary>
/// Interface for seeding host-specific data (modules and menus).
/// Each Host application implements this to seed its own data.
/// </summary>
public interface IHostDataSeeder
{
    /// <summary>
    /// Gets the host type identifier (e.g., "Modulus.Host.Avalonia").
    /// </summary>
    string HostType { get; }

    /// <summary>
    /// Seeds the host module and bundled modules with their menus.
    /// Called once at application startup after database migration.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SeedAsync(CancellationToken cancellationToken = default);
}

