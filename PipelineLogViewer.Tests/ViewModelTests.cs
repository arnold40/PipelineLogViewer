using FluentAssertions;
using PipelineLogViewer.Services;
using PipelineLogViewer.ViewModels;
using PipelineLogViewer.Models;

namespace PipelineLogViewer.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void InputText_WhenSet_ShouldNotifyPropertyChanged()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        var propertyChangedEventRaised = false;
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.InputText))
                propertyChangedEventRaised = true;
        };

        // Act
        viewModel.InputText = "test input";

        // Assert
        propertyChangedEventRaised.Should().BeTrue();
        viewModel.InputText.Should().Be("test input");
    }

    [Fact]
    public void ParseCommand_WithEmptyInput_ShouldNotBeExecutable()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act & Assert
        viewModel.ParseCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void ParseCommand_WithValidInput_ShouldBeExecutable()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();

        // Act
        viewModel.InputText = "1 0 0 [test] -1";

        // Assert
        viewModel.ParseCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void ParseCommand_WhenExecuted_ShouldUpdateBothPipelinesAndOutputText()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        viewModel.InputText = "1 0 0 [test] -1";

        // Act
        viewModel.ParseCommand.Execute(null);

        // Assert
        viewModel.Pipelines.Should().HaveCount(1);
        viewModel.Pipelines[0].Id.Should().Be("1");
        viewModel.Pipelines[0].Messages.Should().HaveCount(1);
        viewModel.Pipelines[0].Messages[0].Body.Should().Be("test");
        
        viewModel.OutputText.Should().NotBeEmpty();
        viewModel.OutputText.Should().Contain("Pipeline 1");
        viewModel.OutputText.Should().Contain("0| test");
    }
}

public class PipelineServiceTests
{
    [Fact]
    public void ParseLogs_ShouldReturnStructuredPipelineData()
    {
        // Arrange
        var service = new PipelineService();
        var input = """
            1 0 0 [Start] 1
            1 1 0 [End] -1
            """;

        // Act
        var result = service.ParseLogs(input);

        // Assert
        result.Should().HaveCount(1);
        result[0].Id.Should().Be("1");
        result[0].Messages.Should().HaveCount(2);
        result[0].Messages[0].Body.Should().Be("End");  // Reversed order
        result[0].Messages[1].Body.Should().Be("Start");
    }

    [Fact]
    public void FormatPipelines_ShouldReturnFormattedString()
    {
        // Arrange
        var service = new PipelineService();
        var pipelines = new List<Pipeline>
        {
            new Pipeline
            {
                Id = "1",
                Messages = new List<PipelineMessage>
                {
                    new PipelineMessage("1", "0", "test", "-1", 0)
                }
            }
        };

        // Act
        var result = service.FormatPipelines(pipelines);

        // Assert
        result.Should().Contain("Pipeline 1");
        result.Should().Contain("0| test");
    }
}

public class PipelineModelTests
{
    [Fact]
    public void Pipeline_ToString_ShouldFormatCorrectly()
    {
        // Arrange
        var pipeline = new Pipeline
        {
            Id = "1",
            Messages = new List<PipelineMessage>
            {
                new PipelineMessage("1", "0", "First", "1", 0),
                new PipelineMessage("1", "1", "Second", "-1", 0)
            }
        };

        // Act
        var result = pipeline.ToString();

        // Assert
        result.Should().Contain("Pipeline 1");
        result.Should().Contain("0| First");
        result.Should().Contain("1| Second");
    }
}