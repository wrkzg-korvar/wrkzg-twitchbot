using System;
using System.Collections.Generic;

namespace Wrkzg.Core.Models;

/// <summary>
/// Generic paginated result wrapper for list endpoints.
/// </summary>
public class PaginatedResult<T>
{
    /// <summary>Items on the current page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Total number of items matching the query (before pagination).</summary>
    public int TotalCount { get; init; }

    /// <summary>Current page number (1-based).</summary>
    public int Page { get; init; }

    /// <summary>Items per page.</summary>
    public int PageSize { get; init; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
}
