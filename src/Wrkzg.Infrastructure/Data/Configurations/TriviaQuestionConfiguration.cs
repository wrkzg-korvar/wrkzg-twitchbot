using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core entity type configuration for the <see cref="TriviaQuestion"/> model.
/// Configures JSON value conversion for the AcceptedAnswers list stored as TEXT in SQLite.
/// </summary>
public class TriviaQuestionConfiguration : IEntityTypeConfiguration<TriviaQuestion>
{
    /// <summary>Configures the schema for the TriviaQuestions table.</summary>
    public void Configure(EntityTypeBuilder<TriviaQuestion> builder)
    {
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Question).IsRequired().HasMaxLength(500);
        builder.Property(q => q.Answer).IsRequired().HasMaxLength(200);
        builder.Property(q => q.Category).HasMaxLength(50);

        builder.Property(q => q.AcceptedAnswers)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasMaxLength(1000)
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                v => v.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                v => v.ToList()));
    }
}
