using System.Collections.Generic;
using FluentAssertions;
using Wrkzg.Core.Models;
using Xunit;

namespace Wrkzg.Core.Tests.Models;

/// <summary>Tests for the ImportResult model success logic and summary formatting.</summary>
public class ImportResultTests
{
    /// <summary>Verifies that Success is true when there are no errors.</summary>
    [Fact]
    public void Success_NoErrors_ReturnsTrue()
    {
        ImportResult result = new();
        result.Success.Should().BeTrue();
    }

    /// <summary>Verifies that Success is true when errors contain only warnings.</summary>
    [Fact]
    public void Success_OnlyWarnings_ReturnsTrue()
    {
        ImportResult result = new()
        {
            Errors = new List<ImportRowError>
            {
                new() { Severity = ImportErrorSeverity.Warning, Message = "test" }
            }
        };
        result.Success.Should().BeTrue();
    }

    /// <summary>Verifies that Success is false when errors contain error-severity items.</summary>
    [Fact]
    public void Success_WithErrors_ReturnsFalse()
    {
        ImportResult result = new()
        {
            Errors = new List<ImportRowError>
            {
                new() { Severity = ImportErrorSeverity.Error, Message = "test" }
            }
        };
        result.Success.Should().BeFalse();
    }

    /// <summary>Verifies that Summary formats the import statistics into a readable string.</summary>
    [Fact]
    public void Summary_FormatsCorrectly()
    {
        ImportResult result = new()
        {
            TotalRows = 100,
            ImportedCount = 90,
            CreatedCount = 60,
            UpdatedCount = 30,
            SkippedCount = 10
        };

        result.Summary.Should().Be("Imported 90/100 users (60 new, 30 updated, 10 skipped)");
    }
}
