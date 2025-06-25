using System.Collections.Generic;
using System.Linq;
using PipelineLogViewer.Models;

namespace PipelineLogViewer.Services;

public interface IPipelineService
{
    List<Pipeline> ParseLogs(string input);
    string FormatPipelines(List<Pipeline> pipelines);
}

public class PipelineService : IPipelineService
{
    private readonly PipelineParser _parser;

    public PipelineService()
    {
        _parser = new PipelineParser();
    }

    public List<Pipeline> ParseLogs(string input)
    {
        return PipelineParser.ParseLogsToStructuredData(input);
    }

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