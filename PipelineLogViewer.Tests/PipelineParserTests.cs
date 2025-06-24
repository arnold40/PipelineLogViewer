using System.Text;
using FluentAssertions;

namespace PipelineLogViewer.Tests;

public class PipelineParserTests
{
    private readonly PipelineParser _parser = new();

    [Fact]
    public void ParseLogs_ShouldReconstructPipelineInReverseOrder()
    {
        // Arrange: 1 -> 2 -> -1; reversed: 2, 1, 0
        var input = """
            1 0 0 [Start] 1
            1 2 0 [End] -1
            1 1 0 [Middle] 2
            """;

        // Act
        var result = _parser.ParseLogs(input);

        // Assert
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().ContainInOrder(
            "Pipeline 1",
            "  2| End",
            "  1| Middle",
            "  0| Start"
        );
    }

    [Fact]
    public void ParseLogs_ShouldDecodeHexAndReverseOrder()
    {
        // Arrange
        var hexBody = Convert.ToHexString(Encoding.ASCII.GetBytes("OK"));
        var input = $"""
            2 3 1 [{hexBody}] -1
            2 2 1 [{hexBody}] 3
            """;

        // Act
        var result = _parser.ParseLogs(input);

        // Assert
        result.Should().Contain("Pipeline 2");
        result.Should().Contain("  3| OK");
        result.Should().Contain("  2| OK");
    }

    [Fact]
    public void ParseLogs_WithMultiplePipelines_ShouldHandleEachSeparately()
    {
        var input = """
            1 0 0 [one] -1
            2 0 0 [two] -1
            """;

        var result = _parser.ParseLogs(input);

        result.Should().Contain("Pipeline 1");
        result.Should().Contain("Pipeline 2");
        result.Should().Contain("  0| one");
        result.Should().Contain("  0| two");
    }

    [Fact]
    public void ParseLogs_WithMalformedLines_ShouldSkipThem()
    {
        var input = """
            1 0 0 [Good line] -1
            not a valid log line
            2 0 0 [Also good] -1
            """;

        var result = _parser.ParseLogs(input);

        result.Should().Contain("Pipeline 1");
        result.Should().Contain("Pipeline 2");
        result.Should().Contain("  0| Good line");
        result.Should().Contain("  0| Also good");
    }

    [Fact]
    public void ParseLogs_WithMissingTerminalNode_ShouldNotIgnoreIncompleteChains()
    {
        var input = """
            1 0 0 [Start] 1
            1 1 0 [Middle] 2
            """;

        var result = _parser.ParseLogs(input);
        result.Should().Contain("Pipeline 1");
    }

    [Fact]
    public void ParseLogs_ShouldHandleUUIDIdentifiers()
    {
        var input = """
            1 a 0 [First] b
            1 b 0 [Second] c
            1 c 0 [Third] -1
            """;

        var result = _parser.ParseLogs(input);

        result.Should().Contain("  c| Third");
        result.Should().Contain("  b| Second");
        result.Should().Contain("  a| First");
    }

    [Fact]
    public void ParseLogs_WithInvalidEncoding_ShouldShowError()
    {
        var input = "1 x 9 [some body] -1";

        var result = _parser.ParseLogs(input);

        result.Should().Contain("Invalid encoding");
    }

    [Theory]
    [InlineData("4F4B", "OK")]
    [InlineData("54657374206D7367", "Test msg")]
    public void ParseLogs_WithHexBody_ShouldDecodeProperly(string hex, string expected)
    {
        var input = $"2 1 1 [{hex}] -1";

        var result = _parser.ParseLogs(input);

        result.Should().Contain($"  1| {expected}");
    }

    [Fact]
    public void ParseLogs_WithEmptyInput_ShouldReturnEmpty()
    {
        var input = "";

        var result = _parser.ParseLogs(input);

        result.Should().BeEmpty();
    }
}
