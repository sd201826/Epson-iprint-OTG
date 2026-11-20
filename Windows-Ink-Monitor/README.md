# Epson L3210 Monitor for Windows

A small native Windows Forms application for tracking the Epson EcoTank L3210.

## Build
Requires the .NET 8 SDK on Windows.
1. Open this folder in Visual Studio, or open a terminal here.
2. Run `dotnet restore`
3. Run `dotnet build -c Release`

## What it does
- Lists printers registered in Windows using WMI.
- Lets you record visual estimates for Black, Cyan, Magenta and Yellow.
- Saves estimates under `%LOCALAPPDATA%\EpsonL3210Monitor\levels.json`.
- Provides a place to keep raw waste-ink register readings.

## Important limitations
The L3210 has visible ink reservoirs; its Windows printer interface does not expose reliable electronic percentages for all four colours. The previously supplied InkZero screenshot showed raw hexadecimal bytes and explicitly indicated that a percentage formula was unavailable. Therefore this app does not invent a waste-ink percentage. Automatic waste-pad percentage needs a verified L3210-specific counter definition and service interface.

This application does not reset counters, alter firmware, or issue print commands. Printer discovery only confirms that Windows has a printer entry; it does not mean ink-counter access is available.
