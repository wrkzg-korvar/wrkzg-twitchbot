using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>Manages background import jobs.</summary>
public interface IImportJobService
{
    /// <summary>Starts a new import job. Returns immediately with the job ID.</summary>
    Task<ImportJob> StartAsync(Stream fileStream, ImportConfiguration config, CancellationToken ct = default);

    /// <summary>Gets all jobs (active + recent).</summary>
    IReadOnlyList<ImportJob> GetAll();

    /// <summary>Gets a specific job by ID.</summary>
    ImportJob? GetById(string jobId);

    /// <summary>Cancels a running job.</summary>
    bool Cancel(string jobId);

    /// <summary>Gets the currently locked module paths.</summary>
    string[] GetLockedModules();
}
