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
    private readonly PipelineParser _parser;

    public MainWindow()
    {
        _parser = new PipelineParser();
        InitializeComponent();
        _inputBox = this.FindControl<TextBox>("InputBox");
        _outputBlock = this.FindControl<TextBlock>("OutputBlock");
        _parseButton = this.FindControl<Button>("ParseButton");

        _parseButton.Click += (_, _) =>
        {
            var input = _inputBox.Text;
            var output =  _parser.ParseLogs(input);
            _outputBlock.Text = output;
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}