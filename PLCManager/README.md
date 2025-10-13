# Nodus/IP - PLC Connection Manager

A modern Windows desktop application for managing PLC connections and IP address switching.

## Features

- **DataGrid View**: Spreadsheet-like interface to manage multiple PLC connections
- **Quick Connect**: Right-click context menu with Connect, Ping, and Delete options
- **IP Address Management**: Automatically change your local IP address to connect to different PLCs on various networks
- **Ping Testing**: Test connectivity to PLCs before connecting
- **Save/Load Lists**: Store and import PLC connection lists as JSON files
- **Status Bar**: Real-time feedback on operations and connection status
- **No Command Windows**: IP changes happen silently without popup windows
- **Auto-save**: Connections are automatically saved to AppData
- **Modern UI**: Clean, compact interface with dark theme toolbar

## Usage

### Adding a PLC Connection

1. Click "Add New" in the toolbar
2. Edit the connection details directly in the grid:
   - **Name**: Descriptive name for the PLC
   - **PLC IP Address**: The IP address of the PLC
   - **My IP Address**: The IP you want your computer to use
   - **Subnet Mask**: Network subnet mask (defaults to 255.255.255.0)
   - **Gateway**: Optional gateway address
   - **Network Adapter**: Select your network adapter from the list

### Connecting to a PLC

1. Select a connection from the list
2. Right-click and choose "Connect" (or use the Connect button)
3. The app will change your IP address to match the configuration
4. Check the status bar for confirmation

### Testing Connectivity

1. Select a connection
2. Right-click and choose "Ping" (or use the Ping button)
3. Status will show if the PLC is reachable

### Managing Lists

- **Save**: Automatically saves to `%AppData%\NodusIP\plc-connections.json`
- **Open**: Load a saved JSON file
- **Save As**: Export your connections to a specific file
- **Delete**: Remove unwanted connections

## Requirements

- Windows operating system
- .NET 9.0 or later
- Administrator privileges (required for changing IP addresses)

## Running the Application

```bash
cd PLCManager
dotnet run
```

## Building for Release

```bash
cd PLCManager
dotnet publish -c Release -r win-x64 --self-contained
```

The executable will be in `bin\Release\net9.0\win-x64\publish\`

## Important Notes

- The application requires administrator privileges to change IP addresses
- Run as administrator for full functionality
- Your existing connections are automatically saved when modified
- The app uses `netsh` internally for IP configuration (no visible command windows)

## Keyboard Shortcuts

- **Double-click** a row to view details in the bottom panel
- **Right-click** for context menu
- Edit cells directly in the grid

## Technical Details

- Built with Avalonia UI for modern cross-platform UI
- MVVM architecture using CommunityToolkit.Mvvm
- Network operations use System.Net.NetworkInformation and System.Management
- Silent IP changes via netsh with CreateNoWindow
