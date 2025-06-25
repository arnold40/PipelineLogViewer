using System.Collections.Generic;

namespace PipelineLogViewer.Models;

/// <summary>
/// Represents a pipeline consisting of an ordered list of messages.
/// </summary>
public class Pipeline
{
    /// <summary>
    /// The unique identifier of the pipeline.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// A list of messages belonging to this pipeline, ordered from head to tail.
    /// </summary>
    public List<PipelineMessage> Messages { get; set; } = new();

    /// <summary>
    /// Returns a string representation of the pipeline and its messages.
    /// </summary>
    /// <returns>Formatted string with pipeline ID and ordered message contents.</returns>
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

/// <summary>
/// Represents a single message in a pipeline, including decoding and linkage information.
/// </summary>
/// <param name="PipelineId">The identifier of the pipeline this message belongs to.</param>
/// <param name="Id">The unique identifier of this message within the pipeline.</param>
/// <param name="Body">The decoded message content.</param>
/// <param name="NextId">The identifier of the next message in the pipeline sequence.</param>
/// <param name="Encoding">The encoding type of the original message body (0 = ASCII, 1 = Hexadecimal).</param>
public record PipelineMessage(
    string PipelineId,
    string Id,
    string Body,
    string NextId,
    int Encoding
);