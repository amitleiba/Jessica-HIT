import { Injectable } from '@angular/core';
import { jsPDF } from 'jspdf';
import autoTable from 'jspdf-autotable';
import { MetricEntry, MetricsStats } from './metrics.service';

@Injectable({ providedIn: 'root' })
export class ReportService {

  exportCsv(history: MetricEntry[], rangeLabel: string): void {
    const headers = [
      'Timestamp', 'Solar Voltage (V)', 'Obstacle Distance (cm)',
      'Safety Code', 'Safety State', 'Mode Code', 'Mode'
    ];

    const rows = history.map(e => [
      e.timestamp,
      e.solarVoltage.toFixed(3),
      e.distance.toString(),
      e.safety.toString(),
      this.safetyLabel(e.safety),
      e.mode.toString(),
      this.modeName(e.mode)
    ]);

    const csv = [headers, ...rows]
      .map(row => row.map(cell => `"${String(cell).replace(/"/g, '""')}"`).join(','))
      .join('\r\n');

    const filename = `jessica-metrics-${rangeLabel.replace(/\s+/g, '_')}-${this.dateStamp()}.csv`;
    this.triggerDownload(new Blob(['﻿' + csv], { type: 'text/csv;charset=utf-8;' }), filename);
  }

  exportPdf(history: MetricEntry[], stats: MetricsStats | null, rangeLabel: string): void {
    const doc = new jsPDF({ orientation: 'portrait', unit: 'mm', format: 'a4' });
    const pageW = doc.internal.pageSize.getWidth();
    const pageH = doc.internal.pageSize.getHeight();
    const margin = 14;

    // ── Header Banner ────────────────────────────────────────────────────────
    doc.setFillColor(15, 23, 42);
    doc.rect(0, 0, pageW, 38, 'F');

    // Accent line
    doc.setFillColor(59, 130, 246);
    doc.rect(0, 38, pageW, 1.5, 'F');

    doc.setTextColor(255, 255, 255);
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(22);
    doc.text('JESSICA', margin, 16);

    doc.setFont('helvetica', 'normal');
    doc.setFontSize(11);
    doc.setTextColor(148, 163, 184);
    doc.text('Telemetry Analytics Report', margin, 24);

    doc.setFontSize(9);
    doc.text(`Time Range: ${rangeLabel}`, margin, 31);
    doc.text(`Generated: ${new Date().toLocaleString()}`, margin, 36);

    doc.setFont('helvetica', 'bold');
    doc.setFontSize(10);
    doc.setTextColor(59, 130, 246);
    doc.text(`${history.length} records`, pageW - margin, 31, { align: 'right' });
    doc.setTextColor(148, 163, 184);
    doc.setFont('helvetica', 'normal');

    let y = 50;

    // ── Summary Statistics ───────────────────────────────────────────────────
    if (stats) {
      y = this.sectionHeader(doc, 'Summary Statistics', y, pageW, margin);

      autoTable(doc, {
        startY: y,
        head: [['Metric', 'Value']],
        body: [
          ['Average Solar Voltage', `${stats.averageSolarVoltage.toFixed(3)} V`],
          ['Max Obstacle Distance', `${stats.maxDistance} cm`],
          ['Min Obstacle Distance', `${stats.minDistance} cm`],
          ['Average Obstacle Distance', `${stats.averageDistance.toFixed(1)} cm`],
          ['Safety Incidents (Hazard)', stats.safetyIncidentCount.toString()],
          ['Total Samples', stats.totalCount.toString()],
        ],
        styles: { fontSize: 10, cellPadding: 4 },
        headStyles: { fillColor: [30, 41, 59], textColor: [255, 255, 255], fontStyle: 'bold' },
        alternateRowStyles: { fillColor: [245, 247, 250] },
        columnStyles: { 1: { halign: 'right', fontStyle: 'bold' } },
        margin: { left: margin, right: margin },
        tableWidth: 'auto',
      });

      y = (doc as any).lastAutoTable.finalY + 10;

      // ── Mode Distribution ──────────────────────────────────────────────────
      y = this.sectionHeader(doc, 'Operating Mode Distribution', y, pageW, margin);

      const modeRows = Object.keys(stats.modeDistribution).sort().map(k => {
        const count = stats.modeDistribution[k];
        const pct = stats.totalCount > 0 ? Math.round((count / stats.totalCount) * 100) : 0;
        return [this.modeName(parseInt(k, 10)), count.toString(), `${pct}%`];
      });

      autoTable(doc, {
        startY: y,
        head: [['Mode', 'Count', 'Share']],
        body: modeRows,
        styles: { fontSize: 10, cellPadding: 4 },
        headStyles: { fillColor: [30, 41, 59], textColor: [255, 255, 255], fontStyle: 'bold' },
        alternateRowStyles: { fillColor: [245, 247, 250] },
        columnStyles: { 1: { halign: 'right' }, 2: { halign: 'right', fontStyle: 'bold' } },
        margin: { left: margin, right: margin },
      });

      y = (doc as any).lastAutoTable.finalY + 10;
    }

    // ── Telemetry Data Table ─────────────────────────────────────────────────
    y = this.sectionHeader(doc, 'Telemetry Data', y, pageW, margin);

    autoTable(doc, {
      startY: y,
      head: [['Timestamp', 'Solar Voltage', 'Distance', 'Safety', 'Mode']],
      body: history.map(e => [
        new Date(e.timestamp).toLocaleString(),
        `${e.solarVoltage.toFixed(3)} V`,
        `${e.distance} cm`,
        this.safetyLabel(e.safety),
        this.modeName(e.mode),
      ]),
      styles: { fontSize: 8.5, cellPadding: 3 },
      headStyles: { fillColor: [30, 41, 59], textColor: [255, 255, 255], fontStyle: 'bold' },
      alternateRowStyles: { fillColor: [245, 247, 250] },
      columnStyles: {
        0: { cellWidth: 45 },
        1: { halign: 'right' },
        2: { halign: 'right' },
        3: { halign: 'center' },
        4: { halign: 'center' },
      },
      margin: { left: margin, right: margin },
      didParseCell: (data) => {
        if (data.section !== 'body' || data.column.index !== 3) return;
        const v = data.cell.raw as string;
        if (v === 'Safe') data.cell.styles.textColor = [16, 185, 129];
        else if (v === 'Warning') data.cell.styles.textColor = [180, 110, 0];
        else if (v === 'Hazard') data.cell.styles.textColor = [220, 50, 50];
      },
    });

    // ── Page Footers ─────────────────────────────────────────────────────────
    const totalPages = doc.getNumberOfPages();
    for (let i = 1; i <= totalPages; i++) {
      doc.setPage(i);
      doc.setFillColor(245, 247, 250);
      doc.rect(0, pageH - 12, pageW, 12, 'F');
      doc.setFontSize(8);
      doc.setFont('helvetica', 'normal');
      doc.setTextColor(100, 116, 139);
      doc.text('Jessica — Solar Agricultural Robot', margin, pageH - 4.5);
      doc.text(`Page ${i} of ${totalPages}`, pageW - margin, pageH - 4.5, { align: 'right' });
    }

    doc.save(`jessica-report-${this.dateStamp()}.pdf`);
  }

  // ── Private helpers ──────────────────────────────────────────────────────

  private sectionHeader(doc: jsPDF, title: string, y: number, pageW: number, margin: number): number {
    doc.setFont('helvetica', 'bold');
    doc.setFontSize(12);
    doc.setTextColor(30, 41, 59);
    doc.text(title, margin, y);
    doc.setDrawColor(59, 130, 246);
    doc.setLineWidth(0.4);
    doc.line(margin, y + 2, pageW - margin, y + 2);
    return y + 7;
  }

  private safetyLabel(safety: number): string {
    return ['Safe', 'Warning', 'Hazard'][safety] ?? 'Unknown';
  }

  private modeName(mode: number): string {
    return ['Idle', 'Manual', 'Autonomous', 'Charging'][mode] ?? `Mode ${mode}`;
  }

  private dateStamp(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private triggerDownload(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }
}
