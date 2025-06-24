using System;
using System.Text;
using FluentAssertions;
using Xunit;

namespace PipelineLogViewer.Tests;

public class PipelineParserTests
{
    // We'll extract the parsing logic to make it more testable
    private readonly PipelineParser _parser = new();

    [Fact]
    public void ParseLogs_WithValidInput_ShouldReturnFormattedOutput()
    {
        // Arrange
        var input = """
            pipeline1 msg1 0 [Hello World] msg2
            pipeline1 msg2 0 [How are you?] END
            """;

        // Act
        var result = _parser.ParseLogs(input);

        // Assert
        result.Should().Contain("Pipeline pipeline1");
        result.Should().Contain("msg1| Hello World");
        result.Should().Contain("msg2| How are you?");
    }

    [Fact]
    public void ParseLogs_WithHexEncoding_ShouldDecodeCorrectly()
    {
        // Arrange
        var hexMessage = Convert.ToHexString(Encoding.ASCII.GetBytes("Hello"));
        var input = $"pipeline1 msg1 1 [{hexMessage}] END";

        // Act
        var result = _parser.ParseLogs(input);

        // Assert
        result.Should().Contain("msg1| Hello");
    }

    [Fact]
    public void ParseLogs_WithMultiplePipelines_ShouldSeparateThem()
    {
        // Arrange
        var input = """
            pipeline1 msg1 0 [First pipeline] END
            pipeline2 msg1 0 [Second pipeline] END
            """;

        // Act
        var result = _parser.ParseLogs(input);

        // Assert
        result.Should().Contain("Pipeline pipeline1");
        result.Should().Contain("Pipeline pipeline2");
        result.Should().Contain("First pipeline");
        result.Should().Contain("Second pipeline");
    }

    [Fact]
    public void ParseLogs_WithComplexChain_ShouldPreserveOrder()
    {
        // Arrange
        var input = """
            pipeline1 msg3 0 [Third] END
            pipeline1 msg1 0 [First] msg2
            pipeline1 msg2 0 [Second] msg3
            """;

        // Act
        var result = _parser.ParseLogs(input);

        // Assert
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var messageLines = lines.Where(l => l.Contains('|')).ToArray();
        
        messageLines[0].Should().Contain("msg1| First");
        messageLines[1].Should().Contain("msg2| Second");
        messageLines[2].Should().Contain("msg3| Third");
    }

    [Fact]
    public void ParseLogs_WithInvalidEncoding_ShouldHandleGracefully()
    {
        // Arrange
        var input = "pipeline1 msg1 99 [Invalid] END";

        // Act
        var result = _parser.ParseLogs(input);

        // Assert
        result.Should().Contain("Invalid encoding");
    }

    [Fact]
    public void ParseLogs_WithEmptyInput_ShouldReturnEmptyResult()
    {
        // Arrange
        var input = "";

        // Act
        var result = _parser.ParseLogs(input);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void ParseLogs_WithMalformedLines_ShouldIgnoreThem()
    {
        // Arrange
        var input = """
            pipeline1 msg1 0 [Valid] END
            This is not a valid log line
            Another invalid line
            pipeline2 msg1 0 [Also valid] END
            """;

        // Act
        var result = _parser.ParseLogs(input);

        // Assert
        result.Should().Contain("Pipeline pipeline1");
        result.Should().Contain("Pipeline pipeline2");
        result.Should().Contain("Valid");
        result.Should().Contain("Also valid");
    }

    [Fact]
    public void ParseLogs_WithOrphanedMessages_ShouldHandleGracefully()
    {
        // Arrange - msg2 references msg3 which doesn't exist
        var input = """
            pipeline1 msg1 0 [First] msg2
            pipeline1 msg2 0 [Second] msg3
            """;

        // Act
        var result = _parser.ParseLogs(input);

        // Assert
        result.Should().Contain("Pipeline pipeline1");
        result.Should().Contain("msg1| First");
        result.Should().Contain("msg2| Second");
    }

    [Fact]
    public void ParseLogs_WithCircularReferences_ShouldNotInfiniteLoop()
    {
        // Arrange - This creates a circular reference
        var input = """
            pipeline1 msg1 0 [First] msg2
            pipeline1 msg2 0 [Second] msg1
            """;

        // Act & Assert - Should complete without hanging
        var result = _parser.ParseLogs(input);
        result.Should().Contain("Pipeline pipeline1");
    }

    [Theory]
    [InlineData("48656C6C6F", "Hello")]
    [InlineData("576F726C64", "World")]
    [InlineData("54657374", "Test")]
    public void ParseLogs_WithVariousHexEncodings_ShouldDecodeCorrectly(string hex, string expected)
    {
        // Arrange
        var input = $"pipeline1 msg1 1 [{hex}] END";

        // Act
        var result = _parser.ParseLogs(input);

        // Assert
        result.Should().Contain($"msg1| {expected}");
    }
}

// The PipelineParser class is now in its own file: PipelineParser.cs