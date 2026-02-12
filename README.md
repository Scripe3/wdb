# WDB (Windows Debug Bridge)

WDB is a lightweight command bridge that allows sending commands to a background service running on a Windows machine.  
It is designed for development, debugging, and remote command execution in controlled environments.

## Features

- Send terminal commands to a background service  
- Run commands silently in the background  
- Optional auto-start with Windows  
- Minimal and fast single-file executables  
- Built with .NET 8  
- Designed for local or controlled remote debugging scenarios  

## Components

### `wdb.exe`
Client tool used to send commands.

Example:
```cmd
wdb dir
wdb cd C:\Windows\System32
````

### `wdbd.exe`

Background daemon/service that receives and executes commands.

This runs silently and listens for instructions from `wdb.exe`.

## Usage

1. Start `wdbd.exe` on the target machine.
2. Pair the device with `wdb.exe` app.
3. Use `wdb.exe` to send commands.

Example:

```cmd
wdbd create-new-pairing-code
wdb pair <pairing code>
wdb dir
```

## Auto-Start (Optional)

You can configure the daemon to start automatically with Windows during settings.

Example option:

```
Autorun with hide window
```

If enabled, the daemon will run in the background without opening a visible window.

## Build

Project targets:

```
.NET 8
win-x64
```

Publish example:

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

Output will be in:

```
bin/Release/net8.0/win-x64/publish
```

## Notes

* Intended for **development and debugging purposes**
* Use only on systems you own or have permission to control
* Running background command services without user knowledge may violate policies or laws

## License

Apache License 2.0
