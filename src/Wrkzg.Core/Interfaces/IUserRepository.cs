using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wrkzg.Core.Models;

namespace Wrkzg.Core.Interfaces;

/// <summary>
/// Repository for tracked Twitch viewers.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetByTwitchIdAsync(string twitchId, CancellationToken ct = default);
    Task<User> GetOrCreateAsync(string twitchId, string username, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetTopByPointsAsync(int count, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetTopByWatchTimeAsync(int count, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
    Task<User> CreateAsync(User user, CancellationToken ct = default);
}
