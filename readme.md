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

## Getting Started

### Requirements

- .NET 6 or later
- A local machine with UDP network capability

### Installation

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

| Option | Description | Example |
|--------|-------------|---------|
| `--file <path>` | Path to a CSV file with AIS records (required if --date not provided) | `--file data.csv` |
| `--date <YYYY-MM-DD>` | Download data for a specific date (required if --file not provided) | `--date 2024-01-15` |
| `--mmsi <mmsi>` | Filter to a specific vessel (optional) | `--mmsi 220382000` |
| `--x-speed <multiplier>` | Playback speed multiplier (optional, default: 1) | `--x-speed 10` |
| `--gps` | Output GPS format (GPRMC) instead of NMEA 0183 (optional) | `--gps` |
| `--skip-moored` | Skip moored/stationary vessels (optional) | `--skip-moored` |
| `--purge-cache` | Clear cached downloads and exit | `--purge-cache` |

### Examples

```bash
# Replay all vessels from a CSV at normal speed
dotnet run -- --file shipdata.csv

# Replay one vessel at 10x speed
dotnet run -- --file shipdata.csv --mmsi 220382000 --x-speed 10

# Download and replay today's data, skip moored vessels
dotnet run -- --date 2024-01-15 --skip-moored

# Replay in GPS format for GPS-compatible receivers
dotnet run -- --file shipdata.csv --gps

# Clear cached data
dotnet run -- --purge-cache
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

## Development

### Project Structure
```
.
├── readme.md
├── AisReplay.slnx          # Solution file
└── src/
    ├── Program.cs          # Entry point and CLI argument parsing
    ├── AisRecord.cs        # Data model for AIS records
    ├── CsvParser.cs        # CSV parsing logic
    ├── NmeaEncoder.cs      # NMEA 0183 and GPS encoding logic
    └── AisReplay.csproj    # Project configuration
```

### Building & Testing
```bash
dotnet build       # Build the project
dotnet run -- --file test.csv  # Run with test data
```

## Cache

Downloaded files are cached in the system temp directory under `AisReplay/`:
- Location: `%TEMP%/AisReplay/` (Windows) or `/tmp/AisReplay/` (Linux/macOS)
- Use `--purge-cache` to clear cached files

## License

[Add your license information here]

## Contributing

[Add contribution guidelines here]
