import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TabsModule } from 'primeng/tabs';
import { CreateRecordingComponent } from './components/create-recording/create-recording.component';
import { ReplayRecordingComponent } from './components/replay-recording/replay-recording.component';
import { DeleteRecordingComponent } from './components/delete-recording/delete-recording.component';

/**
 * RecorderPanelComponent — main page for the recording feature.
 *
 * Contains 3 tabs:
 *   1. Create  — record a new driving session
 *   2. Replay  — play back existing recordings (with loop support)
 *   3. Delete  — remove recordings you no longer need
 */
@Component({
  selector: 'app-recorder-panel',
  standalone: true,
  imports: [
    CommonModule,
    TabsModule,
    CreateRecordingComponent,
    ReplayRecordingComponent,
    DeleteRecordingComponent,
  ],
  templateUrl: './recorder-panel.component.html',
  styleUrl: './recorder-panel.component.scss',
})
export class RecorderPanelComponent {
  activeTab: string | number = '0';

  onTabChange(index: string | number): void {
    this.activeTab = index;
    console.log(`[RecorderPanel] Tab switched to index ${index}`);
  }
}

