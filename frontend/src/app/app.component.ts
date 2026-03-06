import { Component } from '@angular/core';
import { StoryListComponent } from './components/story-list/story-list.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [StoryListComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss'
})
export class AppComponent {
  title = 'Hacker News Reader';
}
