import { Component, AfterViewInit, OnDestroy } from '@angular/core';
import * as L from 'leaflet';

@Component({
    selector: 'app-map-display',
    standalone: true,  // This makes the component standalone!
    imports: [],
    templateUrl: './map-display.component.html',
    styleUrl: './map-display.component.scss',
})
export class MapDisplayComponent implements AfterViewInit, OnDestroy {
    // The map object
    private map!: L.Map;

    // The car marker on the map
    private carMarker!: L.Marker;

    // Israel center coordinates (approximately Tel Aviv area)
    private readonly ISRAEL_CENTER: L.LatLngTuple = [31.5, 34.75];

    // How much the car moves with each button press (in degrees)
    private readonly MOVE_STEP = 0.01;

    // Custom car icon using a div with emoji (no image file needed!)
    private carIcon = L.divIcon({
        html: '<div class="car-marker">🚗</div>',  // Car emoji
        iconSize: [40, 40],           // Size of the icon
        iconAnchor: [20, 20],         // Center of the icon
        className: 'car-icon-container'  // CSS class for styling
    });

    /**
     * AfterViewInit - Called after the HTML is ready
     * This is where we initialize the map
     */
    ngAfterViewInit(): void {
        this.initMap();
    }

    /**
     * OnDestroy - Called when the component is removed
     * This cleans up the map to prevent memory leaks
     */
    ngOnDestroy(): void {
        if (this.map) {
            this.map.remove();
        }
    }

    /**
     * Initialize the Leaflet map
     */
    private initMap(): void {
        // Create the map and attach it to the 'map' div
        this.map = L.map('map', {
            center: this.ISRAEL_CENTER,
            zoom: 8,  // Zoom level (higher = more zoomed in)
        });

        // Add the map tiles (the actual map images)
        // We use OpenStreetMap - it's free!
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap contributors'
        }).addTo(this.map);

        // Add the car marker to the center of Israel
        this.carMarker = L.marker(this.ISRAEL_CENTER, {
            icon: this.carIcon
        }).addTo(this.map);

        // Add a popup to the car
        this.carMarker.bindPopup('🚗 Car is here!');
    }

    /**
     * Move the car UP (north)
     * Called from the control panel
     */
    moveUp(): void {
        const currentPos = this.carMarker.getLatLng();
        const newPos: L.LatLngTuple = [currentPos.lat + this.MOVE_STEP, currentPos.lng];
        this.carMarker.setLatLng(newPos);
        console.log('[Map] Car moved UP to:', newPos);
    }

    /**
     * Move the car DOWN (south)
     * Called from the control panel
     */
    moveDown(): void {
        const currentPos = this.carMarker.getLatLng();
        const newPos: L.LatLngTuple = [currentPos.lat - this.MOVE_STEP, currentPos.lng];
        this.carMarker.setLatLng(newPos);
        console.log('[Map] Car moved DOWN to:', newPos);
    }

    /**
     * Move the car LEFT (west)
     * Called from the control panel
     */
    moveLeft(): void {
        const currentPos = this.carMarker.getLatLng();
        const newPos: L.LatLngTuple = [currentPos.lat, currentPos.lng - this.MOVE_STEP];
        this.carMarker.setLatLng(newPos);
        console.log('[Map] Car moved LEFT to:', newPos);
    }

    /**
     * Move the car RIGHT (east)
     * Called from the control panel
     */
    moveRight(): void {
        const currentPos = this.carMarker.getLatLng();
        const newPos: L.LatLngTuple = [currentPos.lat, currentPos.lng + this.MOVE_STEP];
        this.carMarker.setLatLng(newPos);
        console.log('[Map] Car moved RIGHT to:', newPos);
    }
}
