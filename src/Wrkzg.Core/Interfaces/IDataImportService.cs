using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Handles importing user data from external bot formats.
/// </summary>
public interface IDataImportService
{
    /// <summary>
    /// Validates an import file without writing to the database.
    /// Returns a preview of what would be imported.
    /// </summary>
    Task<ImportResult> PreviewAsync(
        Stream fileStream,
        ImportConfiguration config,
        CancellationToken ct = default);

    /// <summary>
    /// Executes the import, writing data to the database.
    /// </summary>
    Task<ImportResult> ExecuteAsync(
        Stream fileStream,
        ImportConfiguration config,
        CancellationToken ct = default);
}
