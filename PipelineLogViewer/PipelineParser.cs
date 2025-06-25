using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PipelineLogViewer;

/// <summary>
/// Responsible for parsing and reconstructing out-of-order pipeline log messages
/// into coherent, ordered sequences based on their linking IDs.
/// </summary>
public class PipelineParser
{
    /// <summary>
    /// Regular expression for parsing log lines.
    /// Matches format: pipeline_id id encoding [body] next_id
    /// </summary>
    private static readonly Regex LogLineRegex = new(@"^(\S+)\s+(\S+)\s+(\d+)\s+\[([^\]]*)\]\s+(\S+)", RegexOptions.Compiled);

    /// <summary>
    /// Parses raw log input into a formatted, human-readable message per pipeline.
    /// </summary>
    /// <param name="input">Raw multiline log data.</param>
    /// <returns>Formatted string with messages grouped and ordered per pipeline.</returns>
    public string ParseLogs(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var pipelines = ExtractPipelinesFromInput(input);
        return FormatPipelinesOutput(pipelines);
    }

    /// <summary>
    /// Splits the input log lines and builds a mapping of pipelines to their messages.
    /// </summary>
    /// <param name="input">Raw input string containing log lines.</param>
    /// <returns>Dictionary mapping each pipeline ID to its message collection.</returns>
    private Dictionary<string, Dictionary<string, PipelineMessage>> ExtractPipelinesFromInput(string input)
    {
        var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var pipelines = new Dictionary<string, Dictionary<string, PipelineMessage>>();

        foreach (var line in lines)
        {
            var message = ParseLogLine(line);
            if (message == null) continue;

            if (!pipelines.ContainsKey(message.PipelineId))
                pipelines[message.PipelineId] = new Dictionary<string, PipelineMessage>();

            pipelines[message.PipelineId][message.Id] = message;
        }

        return pipelines;
    }

    /// <summary>
    /// Parses a single log line into a <see cref="PipelineMessage"/> object.
    /// </summary>
    /// <param name="line">A single line of log data.</param>
    /// <returns>Parsed <see cref="PipelineMessage"/> or null if parsing fails.</returns>
    private PipelineMessage? ParseLogLine(string line)
    {
        var match = LogLineRegex.Match(line);
        if (!match.Success) return null;

        var pipelineId = match.Groups[1].Value;
        var id = match.Groups[2].Value;
        var encoding = int.Parse(match.Groups[3].Value);
        var bodyEncoded = match.Groups[4].Value;
        var nextId = match.Groups[5].Value;

        var body = DecodeMessageBody(bodyEncoded, encoding);

        return new PipelineMessage(pipelineId, id, body, nextId, encoding);
    }

    /// <summary>
    /// Decodes the message body based on the specified encoding type.
    /// Supports ASCII (plain text) and hexadecimal encoded content.
    /// </summary>
    /// <param name="bodyEncoded">The encoded message body.</param>
    /// <param name="encoding">The encoding type (0 = ASCII, 1 = Hexadecimal).</param>
    /// <returns>Decoded message content as a readable string.</returns>
    private static string DecodeMessageBody(string bodyEncoded, int encoding)
    {
        return encoding switch
        {
            0 => bodyEncoded,
            1 => DecodeHexSafely(bodyEncoded),
            _ => "Invalid encoding"
        };
    }

    /// <summary>
    /// Attempts to decode a hexadecimal string into ASCII text.
    /// Returns an error string if the hex is invalid.
    /// </summary>
    /// <param name="hex">Hexadecimal string to decode.</param>
    /// <returns>Decoded ASCII string or error message.</returns>
    private static string DecodeHexSafely(string hex)
    {
        try
        {
            return Encoding.ASCII.GetString(Convert.FromHexString(hex));
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return "Invalid hex encoding";
        }
    }

    /// <summary>
    /// Formats parsed pipelines into a readable string grouped by pipeline ID.
    /// Orders messages in reverse according to their next_id links.
    /// </summary>
    /// <param name="pipelines">Dictionary of pipeline messages grouped by pipeline ID.</param>
    /// <returns>Formatted string output per pipeline.</returns>
    private static string FormatPipelinesOutput(Dictionary<string, Dictionary<string, PipelineMessage>> pipelines)
    {
        var output = new StringBuilder();
        
        foreach (var (pipelineId, messages) in pipelines)
        {
            output.AppendLine($"Pipeline {pipelineId}");
            
            var orderedMessages = ReconstructMessageChain(messages);
            
            foreach (var message in orderedMessages)
            {
                output.AppendLine($"  {message.Id}| {message.Body}");
            }
        }

        return output.ToString();
    }

    /// <summary>
    /// Reconstructs a sequence of messages within a pipeline based on next_id links.
    /// The chain is ordered from the last message back to the first.
    /// </summary>
    /// <param name="messages">Dictionary of messages for a pipeline.</param>
    /// <returns>List of ordered <see cref="PipelineMessage"/> objects.</returns>
    private static List<PipelineMessage> ReconstructMessageChain(Dictionary<string, PipelineMessage> messages)
    {
        var tailMessage = FindTailMessage(messages);
        if (tailMessage == null) return new List<PipelineMessage>();

        var orderedMessages = BuildMessageChain(messages, tailMessage);
        orderedMessages.Reverse();
        
        return orderedMessages;
    }

    /// <summary>
    /// Finds the tail message in a pipeline (a message not referenced as next by any other).
    /// </summary>
    /// <param name="messages">Pipeline message dictionary.</param>
    /// <returns>The tail <see cref="PipelineMessage"/> or null if not found.</returns>
    private static PipelineMessage? FindTailMessage(Dictionary<string, PipelineMessage> messages)
    {
        var referencedIds = new HashSet<string>();
        
        foreach (var message in messages.Values)
        {
            referencedIds.Add(message.NextId);
        }

        return messages.Values.FirstOrDefault(msg => !referencedIds.Contains(msg.Id));
    }

    /// <summary>
    /// Builds a message chain starting from the tail message by following the next_id links.
    /// </summary>
    /// <param name="messages">Dictionary of all messages in a pipeline.</param>
    /// <param name="startMessage">Message to start the chain from (tail).</param>
    /// <returns>List of messages forming the chain from tail to head.</returns>
    private static List<PipelineMessage> BuildMessageChain(Dictionary<string, PipelineMessage> messages, PipelineMessage startMessage)
    {
        var orderedMessages = new List<PipelineMessage>();
        var visited = new HashSet<string>();
        var currentMessage = startMessage;

        while (currentMessage != null && !visited.Contains(currentMessage.Id))
        {
            visited.Add(currentMessage.Id);
            orderedMessages.Add(currentMessage);
            messages.TryGetValue(currentMessage.NextId, out currentMessage);
        }

        return orderedMessages;
    }
}

/// <summary>
/// Represents a single message in a pipeline.
/// Contains identifiers, message content, encoding information, and linking ID.
/// </summary>
/// <param name="PipelineId">The pipeline this message belongs to.</param>
/// <param name="Id">Unique ID of this message within the pipeline.</param>
/// <param name="Body">Decoded body content of the message.</param>
/// <param name="NextId">ID of the next message in the pipeline sequence.</param>
/// <param name="Encoding">Encoding type used for the body (0 = ASCII, 1 = Hexadecimal).</param>
public record PipelineMessage(
    string PipelineId,
    string Id,
    string Body,
    string NextId,
    int Encoding
);
