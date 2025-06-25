using System.Collections.Generic;
using System.Linq;
using PipelineLogViewer.Models;

namespace PipelineLogViewer.Services;

/// <summary>
/// Defines the contract for a pipeline log processing service.
/// Provides methods to parse raw log input and format structured pipeline data.
/// </summary>
public interface IPipelineService
{
    /// <summary>
    /// Parses raw pipeline log input into a structured list of <see cref="Pipeline"/> objects.
    /// </summary>
    /// <param name="input">Multiline raw log data as a string.</param>
    /// <returns>List of parsed and ordered pipelines.</returns>
    List<Pipeline> ParseLogs(string input);

    /// <summary>
    /// Formats a list of <see cref="Pipeline"/> objects into a human-readable string.
    /// </summary>
    /// <param name="pipelines">List of structured pipeline data.</param>
    /// <returns>A formatted string representing the pipelines and their messages.</returns>
    string FormatPipelines(List<Pipeline> pipelines);
}

/// <summary>
/// Implements the <see cref="IPipelineService"/> interface.
/// Uses <see cref="PipelineParser"/> to process and format pipeline logs.
/// </summary>
public class PipelineService : IPipelineService
{
    private readonly PipelineParser _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineService"/> class.
    /// </summary>
    public PipelineService()
    {
        _parser = new PipelineParser();
    }

    /// <inheritdoc/>
    public List<Pipeline> ParseLogs(string input)
    {
        return PipelineParser.ParseLogsToStructuredData(input);
    }

    /// <inheritdoc/>
    public string FormatPipelines(List<Pipeline> pipelines)
    {
        if (!pipelines.Any())
            return string.Empty;

        var result = string.Empty;
        foreach (var pipeline in pipelines)
        {
            result += pipeline.ToString();
        }
        return result;
    }
}