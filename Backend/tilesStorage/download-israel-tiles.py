#!/usr/bin/env python3
"""
Download high zoom level satellite tiles for Israel and create MBTiles file.
This script downloads tiles from free tile servers and creates an MBTiles file.
"""

import sqlite3
import requests
import mercantile
import os
from tqdm import tqdm
import time

# Israel bounds [min_lon, min_lat, max_lon, max_lat]
ISRAEL_BOUNDS = [34.0, 29.0, 36.0, 33.5]

# Zoom levels to download
ZOOM_LEVELS = [14, 15, 16, 17, 18]  # Adjust as needed

# Tile server templates (free/public sources)
TILE_SERVERS = {
    'esri': 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
    'google': 'https://mt1.google.com/vt/lyrs=s&x={x}&y={y}&z={z}',
    # Note: Google may require API key or have usage limits
}

def create_mbtiles(output_file, tile_format='jpg', zoom_levels=None):
    """Create a new MBTiles database."""
    if zoom_levels is None:
        zoom_levels = ZOOM_LEVELS
    
    # Remove existing file if it exists to avoid lock issues
    if os.path.exists(output_file):
        try:
            os.remove(output_file)
        except Exception as e:
            print(f"Warning: Could not remove existing file: {e}")
        
    conn = sqlite3.connect(output_file)
    # Set timeout to handle potential locks
    conn.execute('PRAGMA journal_mode=WAL')  # Use WAL mode for better concurrency
    cursor = conn.cursor()
    
    # Create tiles table
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS tiles (
            zoom_level INTEGER,
            tile_column INTEGER,
            tile_row INTEGER,
            tile_data BLOB
        )
    ''')
    
    # Create metadata table
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS metadata (
            name TEXT,
            value TEXT
        )
    ''')
    
    # Clear any existing metadata
    cursor.execute('DELETE FROM metadata')
    
    # Insert metadata
    bounds_str = ','.join(map(str, ISRAEL_BOUNDS))
    min_zoom = str(min(zoom_levels))
    max_zoom = str(max(zoom_levels))
    
    cursor.execute('''
        INSERT INTO metadata (name, value) VALUES
        ('name', 'Israel Satellite Tiles'),
        ('format', ?),
        ('bounds', ?),
        ('minzoom', ?),
        ('maxzoom', ?),
        ('type', 'baselayer'),
        ('description', 'High resolution satellite tiles for Israel')
    ''', (tile_format, bounds_str, min_zoom, max_zoom))
    
    conn.commit()
    return conn, cursor

def download_tile(url, retries=3):
    """Download a single tile with retries."""
    for attempt in range(retries):
        try:
            response = requests.get(url, timeout=15, headers={'User-Agent': 'Mozilla/5.0'})
            if response.status_code == 200:
                return response.content
            elif response.status_code == 404:
                # Tile doesn't exist at this location
                return None
        except Exception as e:
            if attempt == retries - 1:
                return None
            time.sleep(0.5)  # Brief delay before retry
    return None

def download_tiles_for_israel(output_file='israel-satellite-high-zoom.mbtiles', 
                              tile_server='esri', 
                              max_tiles=None,
                              zoom_levels=None):
    """
    Download tiles for Israel region and create MBTiles file.
    
    Args:
        output_file: Output MBTiles filename
        tile_server: Which tile server to use ('esri' or 'google')
        max_tiles: Maximum number of tiles to download (None for unlimited)
        zoom_levels: List of zoom levels to download
    """
    if zoom_levels is None:
        zoom_levels = ZOOM_LEVELS
        
    print(f"Creating MBTiles file: {output_file}")
    # Esri returns JPEG, Google returns PNG
    tile_format = 'jpg' if tile_server == 'esri' else 'png'
    conn, cursor = create_mbtiles(output_file, tile_format, zoom_levels)
    
    server_url = TILE_SERVERS[tile_server]
    total_tiles = 0
    downloaded = 0
    
    # Calculate total tiles
    for z in zoom_levels:
        tiles = list(mercantile.tiles(*ISRAEL_BOUNDS, z))
        total_tiles += len(tiles)
        if max_tiles and total_tiles > max_tiles:
            break
    
    print(f"Total tiles to download: {total_tiles}")
    print(f"Estimated file size: ~{total_tiles * 50 / 1024 / 1024:.1f} MB (rough estimate)")
    
    if max_tiles:
        print(f"Limiting to {max_tiles} tiles")
    
    # Download tiles
    for z in zoom_levels:
        print(f"\nDownloading zoom level {z}...")
        tiles = list(mercantile.tiles(*ISRAEL_BOUNDS, z))
        
        for tile in tqdm(tiles, desc=f"Zoom {z}"):
            if max_tiles and downloaded >= max_tiles:
                break
                
            url = server_url.format(x=tile.x, y=tile.y, z=tile.z)
            tile_data = download_tile(url)
            
            if tile_data:
                # Convert TMS Y to XYZ Y (MBTiles uses TMS)
                tms_y = (2 ** tile.z) - 1 - tile.y
                cursor.execute('''
                    INSERT INTO tiles (zoom_level, tile_column, tile_row, tile_data)
                    VALUES (?, ?, ?, ?)
                ''', (tile.z, tile.x, tms_y, sqlite3.Binary(tile_data)))
                downloaded += 1
                # Commit every 100 tiles for safety
                if downloaded % 100 == 0:
                    conn.commit()
            
            # Small delay to avoid rate limiting (reduced for faster download)
            time.sleep(0.05)
            
            if max_tiles and downloaded >= max_tiles:
                break
        
        if max_tiles and downloaded >= max_tiles:
            break
    
    # Final commit and close connection (this will merge WAL into main file)
    conn.commit()
    conn.close()
    
    # Wait a moment for WAL to merge
    time.sleep(1)
    
    # Check if WAL file still exists (it should be merged by now)
    wal_file = output_file + '-wal'
    if os.path.exists(wal_file):
        # Force checkpoint to merge WAL
        conn = sqlite3.connect(output_file)
        conn.execute('PRAGMA wal_checkpoint(FULL)')
        conn.close()
        time.sleep(0.5)
    
    print(f"\n[SUCCESS] Download complete!")
    print(f"  Downloaded: {downloaded} tiles")
    print(f"  Output file: {output_file}")
    if os.path.exists(output_file):
        print(f"  File size: {os.path.getsize(output_file) / 1024 / 1024:.1f} MB")
    print(f"\nPlace this file in Backend/tilesStorage/ and restart TileServer GL")

if __name__ == '__main__':
    import argparse
    
    parser = argparse.ArgumentParser(description='Download high zoom satellite tiles for Israel')
    parser.add_argument('--output', '-o', default='israel-satellite-high-zoom.mbtiles',
                       help='Output MBTiles filename')
    parser.add_argument('--server', '-s', choices=['esri', 'google'], default='esri',
                       help='Tile server to use')
    parser.add_argument('--max-tiles', '-m', type=int, default=None,
                       help='Maximum number of tiles to download (for testing)')
    parser.add_argument('--zoom', '-z', nargs='+', type=int, default=ZOOM_LEVELS,
                       help='Zoom levels to download (default: 14-18)')
    
    args = parser.parse_args()
    zoom_levels = args.zoom
    
    print("=" * 60)
    print("Israel High Zoom Level Satellite Tile Downloader")
    print("=" * 60)
    print(f"Bounds: {ISRAEL_BOUNDS}")
    print(f"Zoom levels: {zoom_levels}")
    print(f"Tile server: {args.server}")
    print("=" * 60)
    
    download_tiles_for_israel(args.output, args.server, args.max_tiles, zoom_levels)

