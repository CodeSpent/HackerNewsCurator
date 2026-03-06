import { Component, OnInit, ViewChild, ElementRef, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HackerNewsService } from '../../services/hacker-news.service';
import { Story } from '../../models/story';

@Component({
  selector: 'app-story-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './story-list.component.html',
  styleUrl: './story-list.component.scss'
})
export class StoryListComponent implements OnInit {
  @ViewChild('paginationContainer') paginationContainer!: ElementRef;

  stories: Story[] = [];
  searchTerm = '';
  activeSearch = '';
  currentPage = 1;
  pageSize = 20;
  totalCount = 0;
  loading = false;
  error = '';
  maxVisiblePages = 10;
  skeletonItems = Array(20);
  nocache = new URLSearchParams(window.location.search).has('nocache');

  constructor(private hackerNewsService: HackerNewsService) {}

  ngOnInit(): void {
    this.loadStories();
    this.updateMaxVisiblePages();
  }

  @HostListener('window:resize')
  onResize(): void {
    this.updateMaxVisiblePages();
  }

  private updateMaxVisiblePages(): void {
    const containerWidth = Math.min(window.innerWidth - 40, 800);
    const prevNextWidth = 160;
    const pageButtonWidth = 40;
    this.maxVisiblePages = Math.max(5, Math.floor((containerWidth - prevNextWidth) / pageButtonWidth));
  }

  loadStories(): void {
    this.loading = true;
    this.error = '';

    this.hackerNewsService.getNewestStories(this.currentPage, this.pageSize, this.activeSearch || undefined, this.nocache)
      .subscribe({
        next: (response) => {
          this.stories = response.stories;
          this.totalCount = response.totalCount;
          this.loading = false;
        },
        error: () => {
          this.error = 'Failed to load stories. Please try again.';
          this.loading = false;
        }
      });
  }

  onSearch(): void {
    this.activeSearch = this.searchTerm;
    this.currentPage = 1;
    this.loadStories();
  }

  clearSearch(): void {
    this.searchTerm = '';
    this.activeSearch = '';
    this.currentPage = 1;
    this.loadStories();
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.loadStories();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.goToPage(this.currentPage + 1);
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.goToPage(this.currentPage - 1);
    }
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  get pageNumbers(): (number | string)[] {
    const total = this.totalPages;
    const max = this.maxVisiblePages;

    if (total <= max) {
      return Array.from({ length: total }, (_, i) => i + 1);
    }

    const current = this.currentPage;
    const sideCount = Math.floor((max - 5) / 2);
    const leftEnd = current - sideCount;
    const rightStart = current + sideCount;

    if (leftEnd <= 2) {
      const leftPages = max - 2;
      return [
        ...Array.from({ length: leftPages }, (_, i) => i + 1),
        '…',
        total,
      ];
    }

    if (rightStart >= total - 1) {
      const rightPages = max - 2;
      return [
        1,
        '…',
        ...Array.from({ length: rightPages }, (_, i) => total - rightPages + 1 + i),
      ];
    }

    const middle: number[] = [];
    for (let i = leftEnd; i <= rightStart; i++) {
      middle.push(i);
    }
    return [1, '…', ...middle, '…', total];
  }

  truncateUrl(url: string): string {
    try {
      const parsed = new URL(url);
      const host = parsed.hostname.replace(/^www\./, '');
      const path = parsed.pathname === '/' ? '' : parsed.pathname;
      const full = host + path;
      return full.length > 40 ? full.substring(0, 40) + '...' : full;
    } catch {
      return url.length > 40 ? url.substring(0, 40) + '...' : url;
    }
  }

}
