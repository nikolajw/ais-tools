# ais-tools

A pair of command-line tools for working with Automatic Identification System (AIS) vessel tracking data. Download AIS records from public data sources, filter by vessel, and broadcast them via UDP in marine communication standards (NMEA 0183 or GPS GPRMC).

## Tools

### AisLoader
Downloads AIS records from [aisdata.ais.dk](http://aisdata.ais.dk/) and filters by vessel MMSI numbers.

**Features:**
- Download AIS data by date (single or date ranges)
- Filter records by one or more MMSI (Maritime Mobile Service Identity) numbers
- Output to stdout for piping to other tools
- Built-in caching for faster re-runs

### AisStreamer
Converts AIS records to marine communication formats and broadcasts them via UDP.

**Features:**
- Read AIS CSV records from file or stdin
- Convert to NMEA 0183 Type 1 Position Reports or GPS GPRMC format
- Broadcast via UDP to any host/port
- Adjustable playback speed (1x, 10x, etc.)
- Efficient line-by-line processing

## Prerequisites

- **.NET 10.0+** - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com)

## Installation

### Build from Source

```bash
# Clone the repository
git clone https://github.com/nikolajw/ais-tools.git
cd ais-tools

# Build the solution
dotnet build

# Build for release (optimized)
dotnet build --configuration Release
```

### Download Pre-Built Binaries

Pre-built binaries are available on the [GitHub Releases page](https://github.com/nikolajw/ais-tools/releases) for:
- Linux (x64)
- macOS (Intel and Apple Silicon)
- Windows (x64)

## Quick Start

### Using AisStreamer with a Local CSV File

```bash
# Stream data from a CSV file at normal speed
dotnet run --project src/AisStreamer -- --input-file shipdata.csv

# Stream at 10x speed in GPS format
dotnet run --project src/AisStreamer -- --input-file shipdata.csv --x-speed 10 --gps
```

### Using AisLoader to Download and Filter Data

```bash
# Download AIS data for a specific date
dotnet run --project src/AisLoader -- --date 2024-01-15

# Filter for specific vessel(s)
dotnet run --project src/AisLoader -- --date 2024-01-15 --mmsi 220382000 --mmsi 219055000
```

### Pipe AisLoader to AisStreamer

Combine downloading and streaming in a single pipeline:

```bash
# Download data and stream it live
dotnet run --project src/AisLoader -- --date 2024-01-15 | \
  dotnet run --project src/AisStreamer -- --x-speed 10
```

## Usage

### AisLoader Options

```
--date <YYYY-MM-DD>     Date to download (repeatable, e.g., --date 2024-01-15 --date 2024-01-16)
--mmsi <number>         MMSI filter to include in output (repeatable, e.g., --mmsi 220382000 --mmsi 219055000)
```

**Examples:**
```bash
# Single date
aisloader --date 2024-01-15

# Multiple dates
aisloader --date 2024-01-15 --date 2024-01-16

# Filter by MMSI
aisloader --date 2024-01-15 --mmsi 220382000

# Multiple filters
aisloader --date 2024-01-15 --mmsi 220382000 --mmsi 219055000
```

### AisStreamer Options

```
--host <hostname>           Target hostname or IP (default: 127.0.0.1)
-p, --port <port>          UDP port number (default: 10110)
-x, --x-speed <factor>      Playback speed multiplier (default: 1)
-g, --gps                   Output GPS GPRMC format instead of NMEA 0183
-f, --input-file <path>     Input CSV file (reads from stdin if not specified)
```

**Examples:**
```bash
# Stream at default settings
aisstreamer --input-file shipdata.csv

# Stream at 10x speed to custom host/port
aisstreamer --input-file shipdata.csv --x-speed 10 --host 192.168.1.100 --port 5000

# Output in GPS format
aisstreamer --input-file shipdata.csv --gps

# Pipe from stdin
cat shipdata.csv | aisstreamer --x-speed 5
```

## CSV Format

The input CSV format is:
```
Timestamp,Vessel Name,MMSI,Latitude,Longitude,NavigationalStatus,ROT,SOG,COG,Heading
```

Example:
```
15/01/2024 10:30:45,VESSEL NAME,220382000,55.1234,-2.5678,Under way using engine,0,12.5,90.0,180
```

## Output Formats

### NMEA 0183 (default)
Standard maritime communication format. Example:
```
!AIVDM,1,1,,A,15MvkT001G?ut47P0D@23wpe00Sa,0*5C
```

### GPS GPRMC
Standard GPS format with recommended minimum data. Example:
```
$GPRMC,103045,A,5507.4040,N,00234.068,W,12.5,90.0,150124,,,A*7F
```

## Project Structure

```
ais-tools/
├── src/
│   ├── AisLoader/           # Download and filter tool
│   └── AisStreamer/         # Convert and broadcast tool
│       ├── AisRecord.cs     # Data model
│       ├── CsvParser.cs     # CSV parsing utility
│       └── NmeaEncoder.cs   # NMEA/GPS encoding
├── CLAUDE.md                # Developer documentation
└── README.md                # This file
```

## Testing UDP Output

To test AisStreamer's UDP output:

**macOS/Linux:**
```bash
# Terminal 1: Listen for UDP packets
nc -u -l 10110

# Terminal 2: Run AisStreamer
aisstreamer --input-file shipdata.csv
```

**Wireshark:**
Use Wireshark to capture UDP packets on port 10110 for detailed analysis.

## Development

### Build Commands

```bash
# Build solution
dotnet build

# Build in Release configuration
dotnet build --configuration Release

# Build specific project
dotnet build src/AisStreamer/AisStreamer.csproj
dotnet build src/AisLoader/AisLoader.csproj
```

### Publishing Self-Contained Binaries

```bash
# Linux x64
dotnet publish --configuration Release --runtime linux-x64 --self-contained --output ./publish/linux-x64

# macOS (Intel)
dotnet publish --configuration Release --runtime osx-x64 --self-contained --output ./publish/osx-x64

# macOS (Apple Silicon)
dotnet publish --configuration Release --runtime osx-arm64 --self-contained --output ./publish/osx-arm64

# Windows x64
dotnet publish --configuration Release --runtime win-x64 --self-contained --output ./publish/win-x64
```

## Cache

AisLoader caches downloaded data in:
- **Windows:** `%TEMP%/AisStreamer/`
- **Linux/macOS:** `/tmp/AisStreamer/`

Delete cached files manually to clear them, or re-run AisLoader to refresh.

## Data Source

AIS data is downloaded from [aisdata.ais.dk](http://aisdata.ais.dk/), a public repository of historical AIS vessel tracking data.

## License

See the repository for license information.

## More Information

For detailed developer documentation, see [CLAUDE.md](CLAUDE.md).