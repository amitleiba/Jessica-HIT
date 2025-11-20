# Israel High Zoom Satellite Tile Downloader

This script downloads high-resolution satellite tiles for Israel and creates an MBTiles file compatible with TileServer GL.

## What It Does

Downloads satellite imagery tiles from free tile servers (Esri World Imagery) for Israel at specified zoom levels and packages them into a standard MBTiles file that can be used with TileServer GL.

## Requirements

- Python 3.7 or higher
- Required Python packages:
  - `sqlite3` (built-in)
  - `requests`
  - `mercantile`
  - `tqdm`

## Installation

1. Install Python dependencies:
   ```bash
   pip install requests mercantile tqdm
   ```

   Or create a requirements file and install:
   ```bash
   pip install -r requirements.txt
   ```

## Usage

### Basic Usage

Download tiles for default zoom levels (14-18):
```bash
python download-israel-tiles.py
```

### Download Specific Zoom Levels

Download zoom levels 14-16:
```bash
python download-israel-tiles.py --zoom 14 15 16
```

### Custom Output File

Specify a custom output filename:
```bash
python download-israel-tiles.py --output my-israel-tiles.mbtiles
```

### Limit Download (for Testing)

Limit to first 1000 tiles (useful for testing):
```bash
python download-israel-tiles.py --max-tiles 1000
```

### Use Different Tile Server

Use Google satellite tiles (may require API key):
```bash
python download-israel-tiles.py --server google
```

## Command-Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--output` | `-o` | Output MBTiles filename | `israel-satellite-high-zoom.mbtiles` |
| `--server` | `-s` | Tile server (`esri` or `google`) | `esri` |
| `--max-tiles` | `-m` | Maximum tiles to download (for testing) | Unlimited |
| `--zoom` | `-z` | Zoom levels to download (space-separated) | `14 15 16 17 18` |

## Examples

### Download zoom levels 14-16 for Israel:
```bash
python download-israel-tiles.py --zoom 14 15 16 --output israel-zoom14-16.mbtiles
```

### Test download (first 500 tiles):
```bash
python download-israel-tiles.py --zoom 14 --max-tiles 500 --output test-tiles.mbtiles
```

### Full download for high-resolution coverage:
```bash
python download-israel-tiles.py --zoom 14 15 16 17 18 --output israel-full-high-zoom.mbtiles
```

## Output

The script creates an MBTiles file (SQLite database) containing:
- Satellite imagery tiles in JPEG format (from Esri)
- Proper metadata (bounds, zoom levels, format)
- TMS coordinate system (MBTiles standard)

### During Download

You may see temporary files:
- `.mbtiles` - Main database file
- `.mbtiles-wal` - Write-Ahead Log (temporary, contains data during download)
- `.mbtiles-shm` - Shared Memory file (temporary)

**These are normal!** When the download completes, the WAL file will be automatically merged into the main `.mbtiles` file, and the temporary files will be removed.

## Using with TileServer GL

1. **Place the MBTiles file** in the `tilesStorage` folder (or wherever TileServer GL is configured to look)

2. **Restart TileServer GL** (or restart your Aspire AppHost if using Aspire)

3. **Access the tiles** via TileServer GL web interface (typically `http://localhost:8080`)

4. **Use in your application** - TileServer GL will automatically detect the MBTiles file and serve tiles at the correct zoom levels

## Israel Bounds

The script downloads tiles for Israel using these bounds:
- **Min Longitude**: 34.0
- **Min Latitude**: 29.0
- **Max Longitude**: 36.0
- **Max Latitude**: 33.5

## Tile Count Estimates

Approximate tile counts for Israel:
- **Zoom 14**: ~1,000 tiles
- **Zoom 15**: ~4,000 tiles
- **Zoom 16**: ~16,000 tiles
- **Zoom 17**: ~64,000 tiles
- **Zoom 18**: ~256,000 tiles

**Total for zoom 14-16**: ~21,000 tiles (~1-3 GB depending on compression)

## Download Time

Download time depends on:
- Your internet connection speed
- Number of zoom levels
- Tile server response time

**Estimated times:**
- Zoom 14-16: 30-60 minutes
- Zoom 14-18: 2-4 hours

The script includes:
- Progress bars for each zoom level
- Rate limiting to avoid overwhelming the tile server
- Automatic retries for failed downloads
- Graceful handling of missing tiles (404 errors)

## Troubleshooting

### Database Locked Error

If you see "database is locked":
1. Make sure no other process is using the file
2. Kill any running Python processes: `Get-Process python | Stop-Process -Force`
3. Delete the existing file and restart

### Missing Tiles (404 Errors)

Some tiles may not exist at certain locations. The script handles this gracefully and continues downloading. This is normal.

### Slow Download

The script includes rate limiting (0.05 second delay between tiles) to avoid overwhelming the tile server. You can modify the delay in the script if needed, but be respectful of the free tile servers.

## Notes

- The script uses **Esri World Imagery** by default (free, no API key required)
- Tiles are downloaded in **JPEG format** (from Esri)
- The MBTiles file uses **TMS coordinate system** (MBTiles standard)
- The script automatically converts XYZ coordinates to TMS for MBTiles compatibility
- All tiles are stored in a single SQLite database file

## License

This script is provided as-is for downloading publicly available satellite imagery tiles.
