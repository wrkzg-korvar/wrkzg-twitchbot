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
    /// <summary>
    /// Retrieves a user by their database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the user.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching user, or null if not found.</returns>
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by their Twitch user identifier.
    /// </summary>
    /// <param name="twitchId">The Twitch-assigned user identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching user, or null if not found.</returns>
    Task<User?> GetByTwitchIdAsync(string twitchId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves an existing user by Twitch ID, or creates a new one if not found.
    /// Updates the username if the user already exists with a different name.
    /// </summary>
    /// <param name="twitchId">The Twitch-assigned user identifier.</param>
    /// <param name="username">The current Twitch display name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The existing or newly created user.</returns>
    Task<User> GetOrCreateAsync(string twitchId, string username, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the users with the most loyalty points, ordered descending.
    /// </summary>
    /// <param name="count">The maximum number of users to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of top users by points.</returns>
    Task<IReadOnlyList<User>> GetTopByPointsAsync(int count, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the users with the most watch time, ordered descending.
    /// </summary>
    /// <param name="count">The maximum number of users to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of top users by watch time.</returns>
    Task<IReadOnlyList<User>> GetTopByWatchTimeAsync(int count, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing user (points, watch time, last seen, etc.).
    /// </summary>
    /// <param name="user">The user with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all tracked users.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of all users.</returns>
    Task<IReadOnlyList<User>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user by their Twitch username (case-insensitive).
    /// </summary>
    /// <param name="username">The Twitch username to search for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching user, or null if not found.</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>
    /// Creates a new user record.
    /// </summary>
    /// <param name="user">The user to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created user with its assigned database identifier.</returns>
    Task<User> CreateAsync(User user, CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated list of users with server-side sorting and search.
    /// </summary>
    /// <param name="search">Optional search term matching username or display name.</param>
    /// <param name="sortBy">Column to sort by (points, username, watchtime, messages, lastseen, firstseen).</param>
    /// <param name="sortDirection">Sort direction (asc or desc).</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A paginated result containing the matching users.</returns>
    Task<PaginatedResult<User>> GetPaginatedAsync(
        string? search = null,
        string sortBy = "points",
        string sortDirection = "desc",
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    /// <summary>Returns the total number of tracked users.</summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The total user count.</returns>
    Task<int> CountAsync(CancellationToken ct = default);
}
