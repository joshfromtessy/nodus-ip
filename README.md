# Nodus/IP - PLC Connection Manager

A Windows desktop application for managing PLC connections across different networks. Built with Avalonia UI.

## Features

- Organize PLCs by building location (West, East, South, North, Skaneateles, Auburn)
- Store connection details: PLC IP, local IP, subnet mask, gateway, network adapter
- Quickly change your network adapter's IP settings to connect to different PLCs
- Ping PLCs to check connectivity
- Save/load connection lists as JSON files
- Collapsible building sections for easy organization
- Discord-inspired dark theme

## Requirements

- Windows 10 or later
- .NET 8.0 SDK
- Administrator privileges (required for changing IP addresses)

## Installation

1. Clone this repository
2. Open the solution in Visual Studio or Rider
3. Build and run the project

```bash
cd PLCManager
dotnet run
```

## Usage

1. **Add a PLC**: Click the "Add New" button in the detail panel or use the top toolbar button
2. **Select Building**: Assign each PLC to a building using the dropdown
3. **Configure Connection**: Enter PLC IP, your desired local IP, subnet mask, gateway, and network adapter
4. **Set IP**: Click "Set IP" to change your network adapter's IP (requires admin)
5. **Ping**: Test connectivity to the PLC
6. **Save/Load**: Save your connection list for later use

## Project Structure

```
PLCManager/
├── Models/           # Data models (PlcConnection)
├── ViewModels/       # MVVM view models
├── Views/            # Avalonia XAML views
├── Services/         # Network and data services
└── App.axaml        # Application entry point
```

## Notes

- The app requires administrator privileges to change network adapter IP addresses
- All connection data is stored in JSON format at `%AppData%\NodusIP\plc-connections.json`
- Only active physical network adapters are shown (virtual adapters are filtered out)

## License

MIT
