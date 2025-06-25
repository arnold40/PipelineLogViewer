using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.Generic;
using PipelineLogViewer.Services;
using PipelineLogViewer.Models;

namespace PipelineLogViewer.ViewModels;

public class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IPipelineService _pipelineService;
    private string _inputText = string.Empty;
    private string _outputText = string.Empty;
    private List<Pipeline> _pipelines = new();

    public MainWindowViewModel() : this(new PipelineService())
    {
    }

    public MainWindowViewModel(IPipelineService pipelineService)
    {
        _pipelineService = pipelineService;
        ParseCommand = new RelayCommand(ParseLogs, CanParseLogs);
    }

    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetField(ref _inputText, value))
            {
                ((RelayCommand)ParseCommand).RaiseCanExecuteChanged();
            }
        }
    }

    public string OutputText
    {
        get => _outputText;
        set => SetField(ref _outputText, value);
    }

    public List<Pipeline> Pipelines
    {
        get => _pipelines;
        set => SetField(ref _pipelines, value);
    }

    public ICommand ParseCommand { get; }

    private void ParseLogs()
    {
        // Parse to structured data
        Pipelines = _pipelineService.ParseLogs(InputText);
        
        // Also update the text output for display
        OutputText = _pipelineService.FormatPipelines(Pipelines);
    }

    private bool CanParseLogs()
    {
        return !string.IsNullOrWhiteSpace(InputText);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}