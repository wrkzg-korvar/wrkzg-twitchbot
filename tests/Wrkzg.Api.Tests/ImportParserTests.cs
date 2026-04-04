using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Wrkzg.Infrastructure.Import;
using Xunit;

namespace Wrkzg.Api.Tests;

/// <summary>
/// Unit tests for Deepbot CSV, Deepbot JSON, and Generic CSV parsers.
/// </summary>
public class ImportParserTests
{
    // ─── Deepbot CSV ─────────────────────────────────────────

    /// <summary>Verifies that the Deepbot CSV parser correctly reads username, points, and watched minutes columns.</summary>
    [Fact]
    public async Task DeepbotCsv_ParsesThreeColumns()
    {
        string csv = "nightowl42,720.0,13125.0\nloyalsub99,1500,52000\n";
        using MemoryStream stream = ToStream(csv);

        List<ImportUserRecord> records = await DeepbotCsvParser.ParseAsync(stream);

        records.Should().HaveCount(2);
        records[0].Username.Should().Be("nightowl42");
        records[0].Points.Should().Be(720);
        records[0].WatchedMinutes.Should().Be(13125);
        records[1].Username.Should().Be("loyalsub99");
        records[1].Points.Should().Be(1500);
        records[1].WatchedMinutes.Should().Be(52000);
    }

    /// <summary>Verifies that floating-point point values are rounded to the nearest integer.</summary>
    [Fact]
    public async Task DeepbotCsv_HandlesFloatPoints()
    {
        string csv = "user1,720.7,13125.3\n";
        using MemoryStream stream = ToStream(csv);

        List<ImportUserRecord> records = await DeepbotCsvParser.ParseAsync(stream);

        records[0].Points.Should().Be(721);
        records[0].WatchedMinutes.Should().Be(13125);
    }

    /// <summary>Verifies that empty lines in the CSV input are skipped without errors.</summary>
    [Fact]
    public async Task DeepbotCsv_SkipsEmptyLines()
    {
        string csv = "user1,100,200\n\n\nuser2,300,400\n";
        using MemoryStream stream = ToStream(csv);

        List<ImportUserRecord> records = await DeepbotCsvParser.ParseAsync(stream);

        records.Should().HaveCount(2);
    }

    /// <summary>Verifies that rows with an incorrect number of columns are skipped.</summary>
    [Fact]
    public async Task DeepbotCsv_SkipsInvalidRows()
    {
        string csv = "user1,100,200\nonly_two_cols,100\nuser2,300,400\n";
        using MemoryStream stream = ToStream(csv);

        List<ImportUserRecord> records = await DeepbotCsvParser.ParseAsync(stream);

        records.Should().HaveCount(2);
        records[0].Username.Should().Be("user1");
        records[1].Username.Should().Be("user2");
    }

    /// <summary>Verifies that negative point and watch-time values are clamped to zero.</summary>
    [Fact]
    public async Task DeepbotCsv_ClampsNegativeValues()
    {
        string csv = "user1,-50,-100\n";
        using MemoryStream stream = ToStream(csv);

        List<ImportUserRecord> records = await DeepbotCsvParser.ParseAsync(stream);

        records[0].Points.Should().Be(0);
        records[0].WatchedMinutes.Should().Be(0);
    }

    // ─── Deepbot JSON ────────────────────────────────────────

    /// <summary>Verifies that the Deepbot JSON parser extracts user data including VIP, mod, and date fields.</summary>
    [Fact]
    public async Task DeepbotJson_ParsesApiResponse()
    {
        string json = """
        [
            {
                "user": "nightowl42",
                "points": 720.0,
                "watch_time": 13125.0,
                "vip": 2,
                "mod": 1,
                "join_date": "2014-07-05T18:09:10Z",
                "last_seen": "2015-03-01T04:17:09Z"
            }
        ]
        """;
        using MemoryStream stream = ToStream(json);

        List<ImportUserRecord> records = await DeepbotJsonParser.ParseAsync(stream);

        records.Should().HaveCount(1);
        records[0].Username.Should().Be("nightowl42");
        records[0].Points.Should().Be(720);
        records[0].WatchedMinutes.Should().Be(13125);
        records[0].VipLevel.Should().Be(2);
        records[0].ModLevel.Should().Be(1);
        records[0].JoinDate.Should().NotBeNull();
        records[0].LastSeen.Should().NotBeNull();
    }

    /// <summary>Verifies that Deepbot VIP level 10 is mapped to null (regular user).</summary>
    [Fact]
    public async Task DeepbotJson_MapsVip10AsRegular()
    {
        string json = """[{"user": "regular_user", "points": 100, "watch_time": 200, "vip": 10, "mod": 0}]""";
        using MemoryStream stream = ToStream(json);

        List<ImportUserRecord> records = await DeepbotJsonParser.ParseAsync(stream);

        records[0].VipLevel.Should().BeNull();
    }

    /// <summary>Verifies that Deepbot VIP levels 1 through 3 are mapped to their corresponding values.</summary>
    [Fact]
    public async Task DeepbotJson_MapsVipLevelsCorrectly()
    {
        string json = """
        [
            {"user": "bronze", "points": 0, "watch_time": 0, "vip": 1, "mod": 0},
            {"user": "silver", "points": 0, "watch_time": 0, "vip": 2, "mod": 0},
            {"user": "gold",   "points": 0, "watch_time": 0, "vip": 3, "mod": 0}
        ]
        """;
        using MemoryStream stream = ToStream(json);

        List<ImportUserRecord> records = await DeepbotJsonParser.ParseAsync(stream);

        records[0].VipLevel.Should().Be(1);
        records[1].VipLevel.Should().Be(2);
        records[2].VipLevel.Should().Be(3);
    }

    /// <summary>Verifies that the parser handles the Deepbot JSON envelope with a "msg" wrapper array.</summary>
    [Fact]
    public async Task DeepbotJson_HandlesMsgWrapper()
    {
        string json = """{"msg": [{"user": "wrapped_user", "points": 500, "watch_time": 1000, "vip": 0, "mod": 0}]}""";
        using MemoryStream stream = ToStream(json);

        List<ImportUserRecord> records = await DeepbotJsonParser.ParseAsync(stream);

        records.Should().HaveCount(1);
        records[0].Username.Should().Be("wrapped_user");
    }

    // ─── Generic CSV ─────────────────────────────────────────

    /// <summary>Verifies that the generic CSV parser detects column headers and returns a correct preview.</summary>
    [Fact]
    public async Task GenericCsv_DetectsHeaders()
    {
        string csv = "name,currency,watch_hours\nuser1,500,10\nuser2,600,20\n";
        using MemoryStream stream = ToStream(csv);

        CsvPreview preview = await GenericCsvParser.PreviewColumnsAsync(
            stream, hasHeader: true, delimiter: ',', previewRows: 5);

        preview.Headers.Should().BeEquivalentTo(new[] { "name", "currency", "watch_hours" });
        preview.SampleRows.Should().HaveCount(2);
        preview.ColumnCount.Should().Be(3);
    }

    /// <summary>Verifies that named column mappings are applied correctly during generic CSV parsing.</summary>
    [Fact]
    public async Task GenericCsv_AppliesColumnMapping()
    {
        string csv = "name,currency,watch_mins\nuser1,500,10\nuser2,600,20\n";
        using MemoryStream stream = ToStream(csv);

        Dictionary<string, string> mapping = new()
        {
            { "username", "name" },
            { "points", "currency" },
            { "watchedMinutes", "watch_mins" }
        };

        List<ImportUserRecord> records = await GenericCsvParser.ParseAsync(
            stream, mapping, hasHeader: true, delimiter: ',');

        records.Should().HaveCount(2);
        records[0].Username.Should().Be("user1");
        records[0].Points.Should().Be(500);
        records[0].WatchedMinutes.Should().Be(10);
    }

    /// <summary>Verifies that numeric column index mappings work for headerless CSV files.</summary>
    [Fact]
    public async Task GenericCsv_AppliesNumericColumnIndex()
    {
        string csv = "user1,500,10\nuser2,600,20\n";
        using MemoryStream stream = ToStream(csv);

        Dictionary<string, string> mapping = new()
        {
            { "username", "0" },
            { "points", "1" },
            { "watchedMinutes", "2" }
        };

        List<ImportUserRecord> records = await GenericCsvParser.ParseAsync(
            stream, mapping, hasHeader: false, delimiter: ',');

        records.Should().HaveCount(2);
        records[0].Username.Should().Be("user1");
        records[0].Points.Should().Be(500);
    }

    // ─── Helpers ─────────────────────────────────────────────

    private static MemoryStream ToStream(string content)
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(content));
    }
}
