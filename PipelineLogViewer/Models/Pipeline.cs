using System.Collections.Generic;

namespace PipelineLogViewer.Models;

public class Pipeline
{
    public string Id { get; set; } = string.Empty;
    public List<PipelineMessage> Messages { get; set; } = new();
    
    public override string ToString()
    {
        var result = $"Pipeline {Id}\n";
        foreach (var message in Messages)
        {
            result += $"  {message.Id}| {message.Body}\n";
        }
        return result;
    }
}

public record PipelineMessage(
    string PipelineId,
    string Id,
    string Body,
    string NextId,
    int Encoding
);