using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for polls and votes.
/// </summary>
public interface IPollRepository
{
    Task<Poll?> GetActiveAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Poll>> GetAllAsync(CancellationToken ct = default);
    Task<Poll> CreateAsync(Poll poll, CancellationToken ct = default);
    Task UpdateAsync(Poll poll, CancellationToken ct = default);
    Task AddVoteAsync(PollVote vote, CancellationToken ct = default);
}
