# Pipeline Log Viewer

A cross-platform desktop application built with .NET 9 and Avalonia UI for parsing and visualizing pipeline execution logs. The application reconstructs pipeline execution chains from log entries and displays them in a human-readable format.

## Features

- **Log Parsing**: Parses structured log entries with pipeline IDs, message IDs, and execution chains
- **Chain Reconstruction**: Automatically reconstructs the execution order from distributed log entries
- **Multiple Encodings**: Supports plain text (encoding 0) and hexadecimal ASCII (encoding 1)
- **Multiple Pipelines**: Handles multiple independent pipeline executions in a single log file
- **Cross-Platform**: Runs on Windows, macOS, and Linux
- **Clean UI**: Modern, user-friendly interface with syntax highlighting

## Log Format

The application expects log entries in the following format:
```
<pipeline_id> <message_id> <encoding> [<body>] <next_message_id>
```

Where:
- `pipeline_id`: Unique identifier for the pipeline execution
- `message_id`: Unique identifier for this specific log message
- `encoding`: 0 for plain text, 1 for hexadecimal ASCII, other values show error
- `body`: The actual log message content
- `next_message_id`: ID of the next message in the execution chain, or -1 for terminal messages

### Example Input
```
2 3 1 [4F4B] -1
1 0 0 [some text] 1
1 1 0 [another text] 2
2 99 1 [4F4B] 3
1 2 1 [626F6479] -1
```

### Example Output
```
Pipeline 2
  3| OK
  99| OK
Pipeline 1
  2| body
  1| another text
  0| some text
```

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Any platform supported by .NET 9 (Windows, macOS, Linux)

### Building and Running

1. **Clone or download the project**
2. **Build the solution:**
   ```bash
   dotnet build
   ```
3. **Run the application:**
   ```bash
   dotnet run --project PipelineLogViewer
   ```
4. **Run tests:**
   ```bash
   dotnet test
   ```

### Using the Application

1. **Launch the application**
2. **Paste your log data** into the top text area
3. **Click "Parse Logs"** to process the data
4. **View the reconstructed pipeline** in the bottom text area

## Project Structure

```
PipelineLogViewer/
├── PipelineLogViewer/              # Main application
│   ├── App.axaml                   # Application configuration
│   ├── MainWindow.axaml            # Main window UI definition
│   ├── MainWindow.axaml.cs         # Main window code-behind
│   ├── PipelineParser.cs           # Core parsing logic
│   ├── Program.cs                  # Application entry point
│   └── PipelineLogViewer.csproj    # Project file
├── PipelineLogViewer.Tests/        # Unit tests
│   ├── PipelineParserTests.cs      # Parser unit tests
│   └── PipelineLogViewer.Tests.csproj
├── PipelineLogViewer.sln           # Solution file
├── .gitignore                      # Git ignore rules
└── README.md                       # This file
```
