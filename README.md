# PPBM - Processor Performance Boost Mode Manager

A Windows desktop application for inspecting, controlling, and persisting the hidden "Processor Performance Boost Mode" power setting.

## The Problem

Windows ships with the processor boost mode set to **Aggressive** by default. On multi-monitor setups this is forced on permanently, causing the CPU to run at elevated clocks even at idle and increasing idle temperatures by 20-40 degrees Celsius with no meaningful performance benefit for most workloads.

## The Solution

PPBM surfaces this hidden setting and lets you switch to cooler modes. It also detects your connected monitor refresh rates (high-refresh monitors keep the iGPU busy, adding additional heat) and provides a simple interface to optimize your system for cooler, quieter operation.

## Features

- **Detect and display** the current Processor Performance Boost Mode with color-coded status
- **Five predefined power profiles** with one-click switching:
  - **Cool and Quiet** (Disabled) -- recommended for productivity and daily use
  - **Balanced** (Enabled) -- general purpose
  - **Aggressive** (Factory Default) -- generates excessive heat, not recommended
  - **Gaming Optimized** (Efficient Enabled) -- good performance with moderate thermals
  - **Rendering / Compilation** (Efficient Aggressive) -- for heavy sustained workloads
- **Auto-detect and fix** the aggressive hot mode with a single click
- **CPU temperature monitoring** with real-time polling every 2 seconds
- **CPU load monitoring** via WMI
- **Max CPU Frequency slider** -- limit the maximum processor frequency from 50% to 100%; setting to 99% effectively disables turbo boost
- **Connected monitor detection** -- lists all displays with current refresh rates and warns about monitors running above 60 Hz
- **Unhide the boost setting** -- make the hidden power setting visible in Control Panel's Power Options
- **Survive Windows updates** -- creates a scheduled task that re-applies your settings at every login with a 30-second delay, ensuring persistence across feature updates
- **Debug logging** -- all powercfg interactions are logged to `%LOCALAPPDATA%\PPBM\debug.log`

## Requirements

- Windows 10 or later (x64)
- Administrator privileges (required for powercfg commands)

## Installation

### Download

Download the latest release from the [Releases](https://github.com/AlexKven/PPBM/releases) page. Extract the zip and run `PPBM.exe`.

### Build from Source

Prerequisites: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```bash
dotnet restore PPBM/PPBM.csproj
dotnet build PPBM/PPBM.csproj -c Release
```

To create a self-contained single-file executable:

```bash
dotnet publish PPBM/PPBM.csproj -c Release -r win-x64 --self-contained true -o ./publish
```

The output will be in the `publish/` directory.

## Usage

1. Run `PPBM.exe` as Administrator (the application manifest enforces this automatically)
2. The main window shows your current boost mode, CPU temperature, and connected monitors
3. Click a profile card to apply it -- the boost mode changes immediately
4. Adjust the Max CPU Frequency slider if desired (99% disables turbo boost)
5. Enable **Survive Updates** to persist settings across reboots and Windows updates
6. Use **Unhide Boost Setting** to make the setting visible in Control Panel

## Tech Stack

| Layer | Technology |
|---|---|
| Language | C# |
| Framework | .NET 10 (net10.0-windows) |
| UI | WPF + Windows Forms |
| Dependencies | System.Management (WMI), TaskScheduler (persistence) |
| Target | Windows x64, self-contained single-file |

## Project Structure

```
PPBM/
├── Models/       BoostMode, PowerProfile, MonitorInfo
├── ViewModels/   MainViewModel (polling, commands, bindings)
├── Services/     PowerConfigService, MonitorService, ScheduledTaskService
├── Converters/   WPF value converters
├── App.xaml      Application entry point
└── MainWindow.xaml   Full application UI
```

## License

MIT
