using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PipelineLogViewer;

public partial class MainWindow : Window
{
    private TextBox _inputBox;
    private TextBox _outputBox;
    private Button _parseButton;
    private readonly PipelineParser _parser;

    public MainWindow()
    {
        _parser = new PipelineParser();
        InitializeComponent();
        _inputBox = this.FindControl<TextBox>("InputBox");
        _outputBox = this.FindControl<TextBox>("OutputBox");
        _parseButton = this.FindControl<Button>("ParseButton");

        _parseButton.Click += (_, _) =>
        {
            var input = _inputBox.Text;
            var output =  _parser.ParseLogs(input);
            _outputBox.Text = output;
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}