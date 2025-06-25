using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PipelineLogViewer.Models;

namespace PipelineLogViewer;

/// <summary>
/// Responsible for parsing and reconstructing unordered pipeline log messages
/// into structured, ordered representations of pipeline data.
/// </summary>
public class PipelineParser
{
    /// <summary>
    /// Regular expression used to parse log lines with the format:
    /// pipeline_id id encoding [body] next_id
    /// </summary>
    private static readonly Regex LogLineRegex = new(@"^(\S+)\s+(\S+)\s+(\d+)\s+\[([^\]]*)\]\s+(\S+)", RegexOptions.Compiled);

    /// <summary>
    /// Parses raw log input and returns structured pipeline data.
    /// </summary>
    /// <param name="input">Multiline string containing raw log entries.</param>
    /// <returns>
    /// A list of <see cref="Pipeline"/> objects, each containing ordered messages
    /// reconstructed from the input.
    /// </returns>
    public static List<Pipeline> ParseLogsToStructuredData(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new List<Pipeline>();

        var messagesGrouped = ExtractPipelinesFromInput(input);
        var pipelines = new List<Pipeline>();

        foreach (var (pipelineId, messages) in messagesGrouped)
        {
            var orderedMessages = ReconstructMessageChain(messages);

            var pipeline = new Pipeline
            {
                Id = pipelineId,
                Messages = orderedMessages
            };

            pipelines.Add(pipeline);
        }

        return pipelines;
    }

    /// <summary>
    /// Extracts pipeline messages from raw input and groups them by pipeline ID.
    /// </summary>
    /// <param name="input">The raw input string containing log lines.</param>
    /// <returns>
    /// A dictionary mapping pipeline IDs to their corresponding message dictionaries.
    /// </returns>
    private static Dictionary<string, Dictionary<string, PipelineMessage>> ExtractPipelinesFromInput(string input)
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
    /// Parses a single log line into a <see cref="PipelineMessage"/> instance.
    /// </summary>
    /// <param name="line">A log line in the expected format.</param>
    /// <returns>
    /// A <see cref="PipelineMessage"/> if parsing is successful; otherwise, null.
    /// </returns>
    private static PipelineMessage? ParseLogLine(string line)
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
    /// Decodes the message body using the specified encoding.
    /// </summary>
    /// <param name="bodyEncoded">The encoded message body.</param>
    /// <param name="encoding">The encoding type (0 = ASCII, 1 = Hexadecimal).</param>
    /// <returns>The decoded message body as a string.</returns>
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
    /// Safely decodes a hexadecimal string to ASCII.
    /// </summary>
    /// <param name="hex">Hexadecimal string.</param>
    /// <returns>The ASCII string or an error message if decoding fails.</returns>
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
    /// Reconstructs the correct message order for a pipeline by resolving links via NextId.
    /// </summary>
    /// <param name="messages">Dictionary of message IDs to <see cref="PipelineMessage"/> instances.</param>
    /// <returns>List of messages ordered from head to tail.</returns>
    private static List<PipelineMessage> ReconstructMessageChain(Dictionary<string, PipelineMessage> messages)
    {
        var tailMessage = FindTailMessage(messages);
        if (tailMessage == null) return new List<PipelineMessage>();

        var orderedMessages = BuildMessageChain(messages, tailMessage);
        orderedMessages.Reverse();

        return orderedMessages;
    }

    /// <summary>
    /// Identifies the tail message (not referenced by any other message's NextId).
    /// </summary>
    /// <param name="messages">The set of messages within a pipeline.</param>
    /// <returns>The tail <see cref="PipelineMessage"/>, or null if none is found.</returns>
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
    /// Follows NextId links to build a message chain from tail to head.
    /// </summary>
    /// <param name="messages">Dictionary of all pipeline messages.</param>
    /// <param name="startMessage">The tail message to begin the chain.</param>
    /// <returns>A list representing the message chain.</returns>
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
