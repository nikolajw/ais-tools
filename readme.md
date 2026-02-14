# AisReplay

A C# .NET application that replays Automatic Identification System (AIS) vessel tracking data from CSV files or downloads. It converts AIS records into NMEA 0183 marine communication protocol format and broadcasts them via UDP for marine navigation and monitoring systems.

## What It Does

AisReplay allows you to:

- **Replay vessel data** from CSV files containing AIS records (timestamp, MMSI, location, speed, heading, etc.)
- **Download AIS data** automatically from [aisdata.ais.dk](http://aisdata.ais.dk/) by date
- **Convert to NMEA standards** - Encodes data into NMEA 0183 format or GPS GPRMC format
- **Send via UDP** - Broadcasts data to localhost:10110 for marine software integration
- **Filter vessels** - Optionally filter by MMSI (Maritime Mobile Service Identity) number
- **Adjust playback speed** - Multiply replay speed by any factor (1x, 2x, 10x, etc.)
- **Skip moored vessels** - Optionally exclude stationary vessels from the replay
- **Cache downloads** - Automatically caches downloaded data for reuse

This is useful for:
- Testing marine navigation software
- Simulating vessel traffic scenarios
- Developing AIS data processing applications
- Training and demonstration purposes

## Projects in This Repository

This repository contains two complementary command-line tools:

### 1. AisReplay
The main replay tool that broadcasts AIS data via UDP to marine navigation systems.

**Features:**
- Load AIS data from CSV files or download from aisdata.ais.dk
- Broadcast to UDP endpoint (customizable host/port)
- Filter by vessel MMSI
- Convert to NMEA 0183 or GPS GPRMC format
- Adjust playback speed
- AOT compiled for fast startup and low memory usage

### 2. AisFileLoader
A utility tool for preprocessing and filtering AIS CSV data before replay.

**Features:**
- Filter CSV files by MMSI (vessel) numbers
- Load MMSI lists from files, command-line, or stdin
- Download and filter data from aisdata.ais.dk
- Output to file or stdout (can be piped to other tools)
- Exclude mode (inverse filtering)
- Unix-like stdin/stdout design for chaining commands

**Example workflow:**
```bash
# Filter AIS data and pipe directly to replay tool
cat vessel_list.txt | \
  aisfileloader -d 2024-01-15 | \
  aisreplay --gps
```

## Getting Started

### Requirements

- .NET 6 or later
- A local machine with UDP network capability

### Installation

#### Option 1: Homebrew (macOS)

The easiest way to install on macOS:

```bash
brew tap nikolajw/aisreplay
brew install aisreplay
```

Then run:
```bash
aisreplay --file data.csv
```

#### Option 2: Pre-built Binaries

Download pre-built binaries for Linux, macOS, or Windows from [GitHub Releases](https://github.com/nikolajw/ais-replay/releases).

Extract and run:
```bash
./AisReplay --file data.csv
```

#### Option 3: Build from Source

1. Clone the repository:
   ```bash
   git clone git@github.com:nikolajw/ais-replay.git
   cd ais-replay
   ```

2. Build the project:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run --project . -- [arguments]
   ```

## Usage

### Basic Commands

#### Replay from a Local CSV File
```bash
dotnet run -- --file path/to/data.csv
```

#### Download and Replay from a Specific Date
```bash
dotnet run -- --date 2024-01-15
```

Data will be automatically downloaded from `aisdata.ais.dk` and cached in your temp directory.

### Options

| Short | Long | Description | Example |
|-------|------|-------------|---------|
| `-f` | `--file <path>` | Path to a CSV file with AIS records (required if --date not provided) | `-f data.csv` |
| `-d` | `--date <YYYY-MM-DD>` | Download data for a specific date (required if --file not provided) | `-d 2024-01-15` |
| `-m` | `--mmsi <mmsi>` | Filter to a specific vessel (optional) | `-m 220382000` |
| `-x` | `--x-speed <multiplier>` | Playback speed multiplier (optional, default: 1) | `-x 10` |
| `-g` | `--gps` | Output GPS format (GPRMC) instead of NMEA 0183 (optional) | `-g` |
| `-s` | `--skip-moored` | Skip moored/stationary vessels (optional) | `-s` |
| `-c` | `--purge-cache` | Clear cached downloads and exit | `-c` |
| `-h` | `--host <ip>` | UDP host/IP address to send events to (default: 127.0.0.1) | `-h 192.168.1.100` |
| `-p` | `--port <port>` | UDP port to send events to (default: 10110) | `-p 5000` |

### Examples

```bash
# Replay all vessels from a CSV at normal speed
aisreplay --file shipdata.csv

# Replay one vessel at 10x speed (using short flags)
aisreplay -f shipdata.csv -m 220382000 -x 10

# Download and replay data from a specific date, skip moored vessels
aisreplay --date 2024-01-15 --skip-moored

# Replay in GPS format for GPS-compatible receivers
aisreplay -f shipdata.csv -g

# Send events to a different host and port
aisreplay -f shipdata.csv -h 192.168.1.100 -p 5000

# Combine multiple options
aisreplay -d 2024-01-15 -m 220382000 -x 10 -s -h 192.168.1.100 -p 5000

# Clear cached data
aisreplay --purge-cache
```

## CSV File Format

The application expects a CSV file with the following structure (with header):

```
Timestamp,Vessel Name,MMSI,Latitude,Longitude,Navigational Status,ROT,SOG,COG,Heading
dd/MM/yyyy HH:mm:ss,Name,123456789,55.1234,12.5678,under way using engine,0.5,12.3,180.5,185
```

**Fields:**
- **Timestamp**: dd/MM/yyyy HH:mm:ss format
- **Vessel Name**: Ignored in current implementation
- **MMSI**: Maritime Mobile Service Identity (9-digit number)
- **Latitude**: Decimal degrees (positive = North)
- **Longitude**: Decimal degrees (positive = East)
- **Navigational Status**: Vessel status (e.g., "under way using engine", "at anchor", "moored")
- **ROT**: Rate of Turn (degrees per minute)
- **SOG**: Speed Over Ground (knots)
- **COG**: Course Over Ground (degrees)
- **Heading**: True heading (degrees)

## Output

The application broadcasts NMEA 0183 sentences via UDP to `127.0.0.1:10110`.

### NMEA 0183 Format
```
!AIVDM,1,1,,A,15MOK=PP001G?tO`K>2IbDpUkP0S,0*5C
```
This is the standard marine vessel tracking protocol used by navigation systems worldwide.

### GPS GPRMC Format (with --gps flag)
```
$GPRMC,123519,A,4807.038,N,01131.000,E,022.4,084.4,230394,003.1,W*6A
```
This is the GPS Recommended Minimum format, compatible with GPS receivers.

## Releases

Pre-built binaries are available for:
- **Linux** (x64)
- **macOS** (Intel x64 and Apple Silicon ARM64)
- **Windows** (x64)

Download from [GitHub Releases](https://github.com/nikolajw/ais-replay/releases).

### macOS Security Warning

If you download the binary directly from GitHub Releases on macOS, you may see a security warning: "Apple could not verify 'AisReplay' is free of malware."

**To avoid this, use Homebrew** (see Installation section above) - it handles all security certificates automatically.

**If using a downloaded binary**, bypass the warning with:

```bash
xattr -d com.apple.quarantine /path/to/AisReplay
./AisReplay --file data.csv
```

Or right-click the executable and click "Open" to bypass the warning once.

## Development

### Project Structure
```
.
├── README.md
├── AisReplay.slnx          # Solution file
└── src/
    ├── AisReplay/          # Main replay tool
    │   ├── Program.cs      # Entry point and CLI argument parsing
    │   ├── Options.cs      # Command-line options
    │   ├── AisRecord.cs    # Data model for AIS records
    │   ├── CsvParser.cs    # CSV parsing logic
    │   ├── NmeaEncoder.cs  # NMEA 0183 and GPS encoding logic
    │   └── AisReplay.csproj
    │
    └── AisFileLoader/      # CSV filtering tool
        ├── Program.cs      # Entry point and filtering logic
        ├── Options.cs      # Command-line options
        ├── AisRecord.cs    # Data model (shared)
        ├── CsvParser.cs    # CSV parsing (shared)
        └── AisFileLoader.csproj
```

### Building & Testing

Build both projects:
```bash
dotnet build
```

Run AisReplay with test data:
```bash
aisreplay --file test.csv
```

Run AisFileLoader:
```bash
aisfileloader -i test.csv -m vessels.txt > filtered.csv
```

## AisFileLoader Usage

AisFileLoader is a preprocessing tool to filter AIS CSV data by vessel MMSI numbers.

### Basic Usage

**Filter by MMSI list:**
```bash
aisfileloader -i input.csv -l "220382000,210409000" > output.csv
```

**Filter by MMSI file:**
```bash
aisfileloader -i input.csv -m vessels.txt > output.csv
```

**Pipe MMSI numbers from stdin:**
```bash
cat vessels.txt | aisfileloader -i input.csv

# Or explicitly:
aisfileloader -i input.csv --mmsi-stdin < vessels.txt
```

**Download and filter:**
```bash
aisfileloader -d 2024-01-15 -m vessels.txt > filtered.csv
```

**Exclude vessels (inverse filter):**
```bash
aisfileloader -i input.csv -m exclude_list.txt -e > output.csv
```

### Multiple Files and Dates

AisFileLoader can combine data from multiple input sources in a single operation:

**Multiple input files:**
```bash
aisfileloader -i file1.csv -i file2.csv -i file3.csv -m vessels.txt -o combined.csv
```

**Multiple dates from ais.dk:**
```bash
aisfileloader -d 2024-01-15 -d 2024-01-16 -d 2024-01-17 -m vessels.txt > combined.csv
```

**Mix files and dates:**
```bash
aisfileloader -i local_data.csv -d 2024-01-15 -d 2024-01-16 -m 220382000,210409000
```

All records from the specified sources are combined into a single output stream with:
- Header written only once (no duplication)
- MMSI filtering applied to all sources
- Combined statistics at the end

### Piping Between Tools

Combine AisFileLoader with AisReplay for powerful workflows:

```bash
# Filter single date and replay
cat vessel_list.txt | aisfileloader -d 2024-01-15 | \
  aisreplay -f /dev/stdin -h 192.168.1.100 -p 5000

# Filter multiple dates and replay
aisfileloader -d 2024-01-15 -d 2024-01-16 -d 2024-01-17 -m vessels.txt | \
  aisreplay -f /dev/stdin -x 10

# Filter multiple files and save to disk
aisfileloader -i file1.csv -i file2.csv -i file3.csv -m 220382000 > filtered.csv
```

## Cache

Downloaded files are cached in the system temp directory under `AisReplay/`:
- Location: `%TEMP%/AisReplay/` (Windows) or `/tmp/AisReplay/` (Linux/macOS)
- Use `--purge-cache` to clear cached files
