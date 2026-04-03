using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wrkzg.Core.Models;

namespace Wrkzg.Infrastructure.Data.Configurations;

public class TriviaQuestionConfiguration : IEntityTypeConfiguration<TriviaQuestion>
{
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
            .HasMaxLength(1000);
    }
}
