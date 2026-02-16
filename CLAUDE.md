# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**ais-tools** is a C# .NET project containing two command-line tools for working with Automatic Identification System (AIS) vessel tracking data:

- **AisLoader**: Downloads AIS records from [aisdata.ais.dk](http://aisdata.ais.dk/) and filters by MMSI to stdout
- **AisStreamer**: Converts AIS records (from CSV) to marine communication standards (NMEA 0183 or GPS GPRMC) and broadcasts them via UDP for integration with marine navigation software

## Architecture

### AisLoader
A simple tool for downloading and filtering AIS data:
- Downloads ZIP files from aisdata.ais.dk by date
- Extracts and parses CSV records
- Filters by MMSI (can specify multiple --mmsi values)
- Outputs filtered CSV to stdout
- Caches downloaded files for reuse

**Options:**
- `--date` (repeatable): Date or date range in YYYY-MM-DD format (can be specified multiple times)
- `--mmsi` (repeatable): MMSI numbers to include in output (can be specified multiple times)

**Usage:**
```bash
# Download data for a specific date
aisloader --date 2024-01-15

# Download multiple dates
aisloader --date 2024-01-15 --date 2024-01-16

# Filter by specific MMSI numbers
aisloader --date 2024-01-15 --mmsi 220382000 --mmsi 219055000

# Pipe to AisStreamer
aisloader --date 2024-01-15 | aisstreamer --x-speed 10
```

### AisStreamer
Reads CSV records and broadcasts as NMEA 0183 or GPS GPRMC sentences:
- Reads CSV from stdin or file
- Parses AIS records line-by-line (memory efficient)
- Converts to NMEA 0183 Type 1 Position Report or GPS GPRMC format
- Broadcasts via UDP with adjustable playback speed
- Logs records to stderr

**Options:**
- `--host`: Hostname or IP address (default: 127.0.0.1)
- `-p, --port`: UDP port number (default: 10110)
- `-x, --x-speed`: Playback speed multiplier (default: 1)
- `-g, --gps`: Output GPS GPRMC format instead of NMEA 0183
- `-f, --input-file`: Input CSV file path (reads from stdin if not specified)

**Usage:**
```bash
# Stream from local CSV file at normal speed
aisstreamer --input-file shipdata.csv

# Stream at 10x speed in GPS format
aisstreamer --input-file shipdata.csv --x-speed 10 --gps

# Use custom host and port
aisstreamer --input-file shipdata.csv --host 192.168.1.100 --port 5000

# Pipe from AisLoader
aisloader --date 2024-01-15 | aisstreamer --x-speed 10

# Combine downloading, filtering, and streaming in one command
aisloader --date 2024-01-15 --mmsi 220382000 | aisstreamer --x-speed 5 --gps
```

### Shared Components (AisStreamer only)
- **AisRecord.cs**: C# record representing a single AIS position report
  - Fields: Timestamp, MMSI, Latitude, Longitude, NavigationalStatus, ROT, SOG, COG, Heading

- **CsvParser.cs**: Static utility parsing CSV lines into AisRecord objects
  - Parses timestamps in `dd/MM/yyyy HH:mm:ss` format
  - Graceful handling of missing/invalid numeric values (returns 0 or -1)

- **NmeaEncoder.cs**: Static utility for encoding AIS records into marine communication formats
  - `ToNmea0183()`: Encodes Type 1 Position Report (168-bit payload) into NMEA 0183 sentence with checksum
  - `ToGprmc()`: Encodes records into GPS GPRMC (Recommended Minimum) format
  - Bit-packing logic for NMEA encoding with AIS armoring (ASCII-encoded base 64)
  - Navigation status mapping (e.g., "under way using engine" → code 0)

## Common Commands

```bash
# Build the entire solution
dotnet build

# Build in Release configuration
dotnet build --configuration Release

# Build specific project
dotnet build src/AisStreamer/AisStreamer.csproj
dotnet build src/AisLoader/AisLoader.csproj

# Run AisStreamer with local CSV file
dotnet run --project src/AisStreamer -- --input-file shipdata.csv

# Run AisStreamer with custom host and port
dotnet run --project src/AisStreamer -- --input-file shipdata.csv --host 192.168.1.100 --port 5000

# Run AisStreamer at 10x speed in GPS format
dotnet run --project src/AisStreamer -- --input-file shipdata.csv --x-speed 10 --gps

# Run AisLoader to download and filter data
dotnet run --project src/AisLoader -- --date 2024-01-15 --mmsi 220382000

# Pipe AisLoader output to AisStreamer
dotnet run --project src/AisLoader -- --date 2024-01-15 | dotnet run --project src/AisStreamer -- --x-speed 10

# Publish for distribution (Linux, macOS, Windows)
dotnet publish --configuration Release --runtime linux-x64 --self-contained --output ./publish/linux-x64
dotnet publish --configuration Release --runtime osx-arm64 --self-contained --output ./publish/osx-arm64
dotnet publish --configuration Release --runtime win-x64 --self-contained --output ./publish/win-x64
```

## Key Implementation Details

### CSV Format (Both Applications)
- Input format: `Timestamp,Vessel Name,MMSI,Latitude,Longitude,NavigationalStatus,ROT,SOG,COG,Heading`
- Timestamp parsing is strict: `dd/MM/yyyy HH:mm:ss` (use `CultureInfo.InvariantCulture`)
- Name field (index 1) is parsed but not used currently

### NMEA 0183 Type 1 Encoding (AisStreamer)
- 168-bit payload (28 bytes) including:
  - Message Type (6 bits) = 1
  - Repeat Indicator (2 bits)
  - MMSI (30 bits)
  - Status Code (4 bits, mapped from navigational status string)
  - ROT (8 bits, signed, encoded as √(ROT/4.733))
  - SOG (10 bits, 0.1 knot resolution)
  - Longitude/Latitude (28/27 bits, 1/600000 degree resolution)
  - COG (12 bits, 0.1 degree resolution)
  - Heading (9 bits, true heading or 511 if unavailable)
  - Spare bits and second timestamp
- Payload encoded with "armoring" (ASCII-friendly 6-bit encoding)
- Final format: `!AIVDM,1,1,,A,<payload>,<fillBits>*<checksum>`

**GPS GPRMC Format**
- Standard GPS format: `$GPRMC,time,status,lat,N/S,lon,E/W,sog,cog,date,magvar,magvar_dir*checksum`
- Coordinates in `DDMM.MMMM` format (degrees and decimal minutes)

**AisLoader Data Flow**
1. Parse command-line arguments (dates, MMSI filters)
2. For each date: download ZIP from aisdata.ais.dk
3. Extract CSV from ZIP to cache directory
4. Read CSV line-by-line (memory efficient)
5. Filter by MMSI (if filters specified)
6. Write matching records to stdout
7. Log statistics to stderr

**AisStreamer Data Flow**
1. Parse command-line arguments (host, port, speed, format)
2. Open input: stdin or specified file
3. Read CSV line-by-line (memory efficient)
4. For each record:
   - Parse CSV to AisRecord
   - Apply timing: sleep based on timestamp delta and speed multiplier
   - Encode to NMEA 0183 or GPS GPRMC format
   - Send via UDP to host:port
   - Log record to stderr

## Release Process

Automated via GitHub Actions on push to `main`:
1. Builds in Release configuration
2. Publishes self-contained binaries for Linux (x64), macOS (x64), Windows (x64)
3. Creates tar.gz and zip archives
4. Creates GitHub release with version tag: `v<YYYY.MM.DD>-<short-commit-sha>`
5. Uploads archives as release artifacts

## Complete Release Workflow

When releasing a new version (from code change to Homebrew update):

### 1. Make Code Changes and Commit
```bash
# Make your code changes
# Test locally to verify
git add <files>
git commit -m "Fix/Feature description"
git push origin main
```

### 2. Bump Version Numbers
```bash
# Update version in both project files
sed -i '' 's/<Version>OLD<\/Version>/<Version>NEW<\/Version>/g' \
  src/AisStreamer/AisStreamer.csproj src/AisLoader/AisLoader.csproj
```

### 3. Commit Version Bump
```bash
git add src/AisStreamer/AisStreamer.csproj src/AisLoader/AisLoader.csproj
git commit -m "Bump version to X.Y.Z"
```

### 4. Create and Push Tag
```bash
git tag -a vX.Y.Z -m "Release X.Y.Z - description"
git push origin main vX.Y.Z
```

### 5. Wait for GitHub Actions
- Wait 2 minutes for release artifacts to be created on GitHub
- Verify all 4 artifacts exist (linux-x64, macos-arm64, macos-x64, windows-x64)

### 6. Get SHA256 Hashes (via gh CLI, no download needed)
```bash
gh release view vX.Y.Z --repo nikolajw/ais-tools --json assets \
  --jq '.assets[] | select(.name | contains("macos")) | "\(.name): \(.digest)"'
```

### 7. Update Homebrew Formulas
If Homebrew formulas exist for these tools, update them in the appropriate Homebrew repository:

Update **aisloader.rb**:
- Change `version` from old to new
- Update macOS URLs to reference new tag (vX.Y.Z)
- Update SHA256 hashes with values from step 6

Update **aisstreamer.rb**:
- Same changes as aisloader.rb

### 8. Commit and Push Homebrew Changes
```bash
cd homebrew-aistools
git add Formula/aisloader.rb Formula/aisstreamer.rb
git commit -m "Update Homebrew formulas to vX.Y.Z"
git push origin main
```

## Testing Notes

- No formal test project; manual testing is the primary validation method

**AisLoader Testing:**
- Test with `--date` arguments pointing to dates with known data
- Verify CSV output format is correct
- Test `--mmsi` filtering with multiple values
- Verify cache directory handling on different platforms

**AisStreamer Testing:**
- Can test locally with `--input-file` argument pointing to sample CSV
- UDP output can be monitored with tools like `nc -u -l 10110` (macOS/Linux) or Wireshark
- Test NMEA 0183 output and GPS GPRMC output modes
- Test various `--x-speed` values to verify timing logic
- Test piping from AisLoader to AisStreamer
- Verify error handling for missing/invalid files

## .NET Configuration

- **Target Framework**: net10.0 (requires .NET 10.0+)
- **Nullable**: Enabled (strict null-checking)
- **Type**: Console executable
- **Language Features**: C# top-level statements, records, local functions, newer target-typed new expressions

## Cache Location

- **AisLoader** caches downloaded ZIP files and extracted CSVs in: `%TEMP%/AisStreamer/` (Windows) or `/tmp/AisStreamer/` (Linux/macOS)
- Delete files manually from the cache directory to clear them, or re-run AisLoader with the same dates to refresh from the server
