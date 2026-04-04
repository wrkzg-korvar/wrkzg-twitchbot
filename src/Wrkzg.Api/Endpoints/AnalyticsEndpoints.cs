using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wrkzg.Core.Interfaces;
using Wrkzg.Core.Models;

namespace Wrkzg.Api.Endpoints;

/// <summary>
/// REST endpoints for stream analytics data.
/// </summary>
public static class AnalyticsEndpoints
{
    /// <summary>Registers stream analytics API endpoints.</summary>
    public static void MapAnalyticsEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapGroup("/api/analytics").WithTags("Analytics");

        group.MapGet("/sessions", async (IStreamAnalyticsRepository repo,
            int? limit, int? offset, CancellationToken ct) =>
        {
            IReadOnlyList<StreamSession> sessions = await repo.GetSessionsAsync(
                limit ?? 50, offset ?? 0, ct);

            return Results.Ok(sessions.Select(s => new
            {
                id = s.Id,
                twitchStreamId = s.TwitchStreamId,
                startedAt = s.StartedAt,
                endedAt = s.EndedAt,
                durationMinutes = s.DurationMinutes,
                peakViewers = s.PeakViewers,
                averageViewers = s.AverageViewers,
                title = s.Title,
                categories = s.CategorySegments.Select(c => new
                {
                    categoryName = c.CategoryName,
                    durationMinutes = c.DurationMinutes,
                    startedAt = c.StartedAt,
                    endedAt = c.EndedAt
                })
            }));
        });

        group.MapGet("/sessions/latest", async (IStreamAnalyticsRepository repo, CancellationToken ct) =>
        {
            StreamSession? session = await repo.GetLatestSessionAsync(ct);
            if (session is null)
            {
                return Results.NotFound(new { error = "No stream sessions recorded yet." });
            }

            return Results.Ok(MapSessionDetail(session));
        });

        group.MapGet("/sessions/{id:int}", async (int id, IStreamAnalyticsRepository repo, CancellationToken ct) =>
        {
            StreamSession? session = await repo.GetSessionByIdAsync(id, ct);
            if (session is null)
            {
                return Results.NotFound();
            }

            return Results.Ok(MapSessionDetail(session));
        });

        group.MapGet("/summary", async (IStreamAnalyticsRepository repo,
            int? days, CancellationToken ct) =>
        {
            int periodDays = days ?? 30;
            DateTimeOffset since = DateTimeOffset.UtcNow.AddDays(-periodDays);
            IReadOnlyList<StreamSession> sessions = await repo.GetSessionsSinceAsync(since, ct);

            if (sessions.Count == 0)
            {
                return Results.Ok(new
                {
                    period = new { from = since, to = DateTimeOffset.UtcNow },
                    totalStreams = 0,
                    totalHoursStreamed = 0.0,
                    averageStreamDurationMinutes = 0,
                    averageViewers = 0.0,
                    peakViewers = 0,
                    topCategories = Array.Empty<object>()
                });
            }

            double totalMinutes = sessions.Sum(s => s.DurationMinutes ?? 0);
            double avgDuration = totalMinutes / sessions.Count;
            double avgViewers = sessions.Where(s => s.AverageViewers.HasValue)
                .Select(s => s.AverageViewers!.Value)
                .DefaultIfEmpty(0)
                .Average();
            int peakViewers = sessions.Max(s => s.PeakViewers);

            // Category aggregation
            List<CategorySegment> allSegments = sessions.SelectMany(s => s.CategorySegments).ToList();
            var topCategories = allSegments
                .GroupBy(c => c.CategoryName)
                .Select(g => new
                {
                    name = g.Key,
                    hours = Math.Round(g.Sum(c => c.DurationMinutes ?? 0) / 60.0, 1),
                    avgViewers = Math.Round(g.Where(c => c.AverageViewers.HasValue)
                        .Select(c => c.AverageViewers!.Value)
                        .DefaultIfEmpty(0)
                        .Average(), 1),
                    sessions = g.Select(c => c.StreamSessionId).Distinct().Count()
                })
                .OrderByDescending(c => c.hours)
                .Take(10)
                .ToList();

            return Results.Ok(new
            {
                period = new { from = since, to = DateTimeOffset.UtcNow },
                totalStreams = sessions.Count,
                totalHoursStreamed = Math.Round(totalMinutes / 60.0, 1),
                averageStreamDurationMinutes = (int)avgDuration,
                averageViewers = Math.Round(avgViewers, 1),
                peakViewers,
                topCategories
            });
        });

        group.MapGet("/categories", async (IStreamAnalyticsRepository repo,
            int? days, CancellationToken ct) =>
        {
            int periodDays = days ?? 30;
            DateTimeOffset since = DateTimeOffset.UtcNow.AddDays(-periodDays);
            IReadOnlyList<StreamSession> sessions = await repo.GetSessionsSinceAsync(since, ct);

            List<CategorySegment> allSegments = sessions.SelectMany(s => s.CategorySegments).ToList();
            var categories = allSegments
                .GroupBy(c => c.CategoryName)
                .Select(g => new
                {
                    name = g.Key,
                    totalMinutes = g.Sum(c => c.DurationMinutes ?? 0),
                    hours = Math.Round(g.Sum(c => c.DurationMinutes ?? 0) / 60.0, 1),
                    avgViewers = Math.Round(g.Where(c => c.AverageViewers.HasValue)
                        .Select(c => c.AverageViewers!.Value)
                        .DefaultIfEmpty(0)
                        .Average(), 1),
                    peakViewers = g.Max(c => c.PeakViewers ?? 0),
                    sessions = g.Select(c => c.StreamSessionId).Distinct().Count()
                })
                .OrderByDescending(c => c.hours)
                .ToList();

            return Results.Ok(categories);
        });
    }

    private static object MapSessionDetail(StreamSession session)
    {
        return new
        {
            id = session.Id,
            twitchStreamId = session.TwitchStreamId,
            startedAt = session.StartedAt,
            endedAt = session.EndedAt,
            durationMinutes = session.DurationMinutes,
            peakViewers = session.PeakViewers,
            averageViewers = session.AverageViewers,
            title = session.Title,
            categories = session.CategorySegments.Select(c => new
            {
                categoryName = c.CategoryName,
                twitchCategoryId = c.TwitchCategoryId,
                durationMinutes = c.DurationMinutes,
                peakViewers = c.PeakViewers,
                averageViewers = c.AverageViewers,
                startedAt = c.StartedAt,
                endedAt = c.EndedAt
            }),
            snapshots = session.ViewerSnapshots.Select(v => new
            {
                viewerCount = v.ViewerCount,
                timestamp = v.Timestamp
            })
        };
    }
}
