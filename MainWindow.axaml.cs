using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PipelineLogViewer;

public partial class MainWindow : Window
{
    private TextBox _inputBox;
    private TextBlock _outputBlock;
    private Button _parseButton;

    public MainWindow()
    {
        InitializeComponent();
        _inputBox = this.FindControl<TextBox>("InputBox");
        _outputBlock = this.FindControl<TextBlock>("OutputBlock");
        _parseButton = this.FindControl<Button>("ParseButton");

        _parseButton.Click += (_, _) =>
        {
            var input = _inputBox.Text;
            var output = ParseLogs(input);
            _outputBlock.Text = output;
        };
    }

    private string ParseLogs(string input)
    {
        var lines = input.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var pipelines = new Dictionary<string, Dictionary<string, (string, string, int)>>();

        foreach (var line in lines)
        {
            var match = Regex.Match(line, @"^(\S+)\s+(\S+)\s+(\d+)\s+\[([^\]]*)\]\s+(\S+)");
            if (!match.Success) continue;

            var pipelineId = match.Groups[1].Value;
            var id = match.Groups[2].Value;
            var encoding = int.Parse(match.Groups[3].Value);
            var bodyEncoded = match.Groups[4].Value;
            var nextId = match.Groups[5].Value;

            string body = encoding switch
            {
                0 => bodyEncoded,
                1 => Encoding.ASCII.GetString(Convert.FromHexString(bodyEncoded)),
                _ => "Invalid encoding"
            };

            if (!pipelines.ContainsKey(pipelineId))
                pipelines[pipelineId] = new();

            pipelines[pipelineId][id] = (body, nextId, encoding);
        }

        var output = new StringBuilder();
        foreach (var (pipelineId, messages) in pipelines)
        {
            output.AppendLine($"Pipeline {pipelineId}");
            // Find tail
            string tailId = null;
            var nextIdSet = new HashSet<string>();
            foreach (var (_, v) in messages)
                nextIdSet.Add(v.Item2);
            foreach (var id in messages.Keys)
                if (!nextIdSet.Contains(id))
                    tailId = id;

            // Reconstruct
            var ordered = new List<string>();
            while (tailId != null && messages.ContainsKey(tailId))
            {
                ordered.Add(tailId);
                tailId = messages[tailId].Item2;
            }

            ordered.Reverse();
            foreach (var id in ordered)
            {
                var (body, _, _) = messages[id];
                output.AppendLine($"  {id}| {body}");
            }

            output.AppendLine();
        }

        return output.ToString();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}