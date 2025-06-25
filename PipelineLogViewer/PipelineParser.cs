using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PipelineLogViewer.Models;

namespace PipelineLogViewer;

public class PipelineParser
{
    private static readonly Regex LogLineRegex = new(@"^(\S+)\s+(\S+)\s+(\d+)\s+\[([^\]]*)\]\s+(\S+)", RegexOptions.Compiled);

    // Updated method that returns structured data
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

    private static string DecodeMessageBody(string bodyEncoded, int encoding)
    {
        return encoding switch
        {
            0 => bodyEncoded,
            1 => DecodeHexSafely(bodyEncoded),
            _ => "Invalid encoding"
        };
    }

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

    private static List<PipelineMessage> ReconstructMessageChain(Dictionary<string, PipelineMessage> messages)
    {
        // Find the tail message (not referenced by any other message)
        var tailMessage = FindTailMessage(messages);
        if (tailMessage == null) return new List<PipelineMessage>();

        // Build the chain from tail to head
        var orderedMessages = BuildMessageChain(messages, tailMessage);
        
        // Reverse to get head-to-tail order
        orderedMessages.Reverse();
        
        return orderedMessages;
    }

    private static PipelineMessage? FindTailMessage(Dictionary<string, PipelineMessage> messages)
    {
        var referencedIds = new HashSet<string>();
        
        // Collect all IDs that are referenced as "next"
        foreach (var message in messages.Values)
        {
            referencedIds.Add(message.NextId);
        }

        // Find the message that is not referenced by anyone (the tail)
        return messages.Values.FirstOrDefault(msg => !referencedIds.Contains(msg.Id));
    }

    private static List<PipelineMessage> BuildMessageChain(Dictionary<string, PipelineMessage> messages, PipelineMessage startMessage)
    {
        var orderedMessages = new List<PipelineMessage>();
        var visited = new HashSet<string>(); // Prevent infinite loops in circular references
        var currentMessage = startMessage;

        while (currentMessage != null && !visited.Contains(currentMessage.Id))
        {
            visited.Add(currentMessage.Id);
            orderedMessages.Add(currentMessage);
            
            // Move to the next message in the chain
            messages.TryGetValue(currentMessage.NextId, out currentMessage);
        }

        return orderedMessages;
    }
}