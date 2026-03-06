import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { of, throwError } from 'rxjs';
import { StoryListComponent } from './story-list.component';
import { HackerNewsService } from '../../services/hacker-news.service';
import { StoriesResponse } from '../../models/story';

describe('StoryListComponent', () => {
  let component: StoryListComponent;
  let fixture: ComponentFixture<StoryListComponent>;
  let mockService: jasmine.SpyObj<HackerNewsService>;

  const mockResponse: StoriesResponse = {
    stories: [
      { id: 1, title: 'Angular News', url: 'https://angular.io', time: 1709600000, by: 'testuser' },
      { id: 2, title: 'No Link Story', url: null, time: 1709600100, by: 'anotheruser' },
      { id: 3, title: 'TypeScript Update', url: 'https://ts.dev', time: 1709600200, by: 'devuser' }
    ],
    totalCount: 50,
    page: 1,
    pageSize: 20
  };

  beforeEach(async () => {
    mockService = jasmine.createSpyObj('HackerNewsService', ['getNewestStories']);
    mockService.getNewestStories.and.returnValue(of(mockResponse));

    await TestBed.configureTestingModule({
      imports: [StoryListComponent, FormsModule],
      providers: [
        { provide: HackerNewsService, useValue: mockService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(StoryListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load stories on init', () => {
    expect(mockService.getNewestStories).toHaveBeenCalledWith(1, 20, undefined, false);
    expect(component.stories.length).toBe(3);
  });

  it('should display story titles', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const items = compiled.querySelectorAll('.story-item');
    expect(items.length).toBe(3);
  });

  it('should render HN discussion links for all stories', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const items = compiled.querySelectorAll('.story-item');
    items.forEach((item, i) => {
      const link = item.querySelector('a') as HTMLAnchorElement;
      expect(link.href).toContain('news.ycombinator.com/item?id=' + mockResponse.stories[i].id);
      expect(link.target).toBe('_blank');
    });
  });

  it('should render direct links only for stories with URLs', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const directLinks = compiled.querySelectorAll('.direct-link');
    expect(directLinks.length).toBe(2);
    expect((directLinks[0] as HTMLAnchorElement).href).toContain('angular.io');
    expect((directLinks[0] as HTMLAnchorElement).title).toContain('angular.io');
    expect(directLinks[0].textContent).toContain('angular.io');
  });

  it('should calculate total pages correctly', () => {
    expect(component.totalPages).toBe(3);
  });

  it('should go to next page', () => {
    component.nextPage();
    expect(component.currentPage).toBe(2);
    expect(mockService.getNewestStories).toHaveBeenCalledWith(2, 20, undefined, false);
  });

  it('should not go below page 1', () => {
    component.currentPage = 1;
    component.previousPage();
    expect(component.currentPage).toBe(1);
  });

  it('should reset to page 1 on search', () => {
    component.currentPage = 3;
    component.searchTerm = 'angular';
    component.onSearch();
    expect(component.currentPage).toBe(1);
    expect(component.activeSearch).toBe('angular');
    expect(mockService.getNewestStories).toHaveBeenCalledWith(1, 20, 'angular', false);
  });

  it('should show active search indicator', () => {
    component.activeSearch = 'angular';
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const indicator = compiled.querySelector('.active-search');
    expect(indicator).toBeTruthy();
    expect(indicator!.textContent).toContain('angular');
  });

  it('should clear search and reload', () => {
    component.activeSearch = 'angular';
    component.searchTerm = 'angular';
    component.clearSearch();
    expect(component.activeSearch).toBe('');
    expect(component.searchTerm).toBe('');
    expect(component.currentPage).toBe(1);
    expect(mockService.getNewestStories).toHaveBeenCalledWith(1, 20, undefined, false);
  });

  it('should not show active search indicator when no search', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.active-search')).toBeNull();
  });

  it('should display error on failure', () => {
    mockService.getNewestStories.and.returnValue(throwError(() => new Error('fail')));
    component.loadStories();
    expect(component.error).toBe('Failed to load stories. Please try again.');
  });

  it('should show skeleton loading indicator', () => {
    component.loading = true;
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.skeleton-list')).toBeTruthy();
    const skeletonItems = compiled.querySelectorAll('.skeleton-item');
    expect(skeletonItems.length).toBe(20);
  });

  it('should go to specific page via goToPage and scroll to top', () => {
    const scrollSpy = spyOn(window, 'scrollTo');
    component.goToPage(3);
    expect(component.currentPage).toBe(3);
    expect(mockService.getNewestStories).toHaveBeenCalledWith(3, 20, undefined, false);
    expect(scrollSpy).toHaveBeenCalled();
    expect((scrollSpy.calls.mostRecent().args as any[])[0]).toEqual({ top: 0, behavior: 'smooth' });
  });

  it('should not go beyond last page on nextPage', () => {
    // totalCount=50, pageSize=20 => totalPages=3
    component.currentPage = 3;
    component.nextPage();
    expect(component.currentPage).toBe(3);
  });

  it('should truncate long URLs', () => {
    const long = 'https://www.example.com/this/is/a/very/long/path/that/exceeds/forty/characters';
    const result = component.truncateUrl(long);
    expect(result.length).toBeLessThanOrEqual(43); // 40 + '...'
    expect(result).toContain('...');
    expect(result).not.toContain('www.');
  });

  it('should not truncate short URLs', () => {
    const result = component.truncateUrl('https://example.com');
    expect(result).toBe('example.com');
    expect(result).not.toContain('...');
  });

  it('should handle invalid URL in truncateUrl', () => {
    const result = component.truncateUrl('not-a-url');
    expect(result).toBe('not-a-url');
  });

  it('should strip www prefix in truncateUrl', () => {
    const result = component.truncateUrl('https://www.example.com/page');
    expect(result).toBe('example.com/page');
  });

  it('should return all page numbers when total pages <= maxVisiblePages', () => {
    component.totalCount = 60; // 3 pages
    component.maxVisiblePages = 10;
    expect(component.pageNumbers).toEqual([1, 2, 3]);
  });

  it('should include ellipsis when total pages > maxVisiblePages and current page in middle', () => {
    component.totalCount = 400; // 20 pages
    component.maxVisiblePages = 7;
    component.currentPage = 10;
    const pages = component.pageNumbers;
    expect(pages[0]).toBe(1);
    expect(pages[1]).toBe('…');
    expect(pages).toContain(10);
    expect(pages[pages.length - 1]).toBe(20);
    expect(pages[pages.length - 2]).toBe('…');
  });

  it('should show left pages without left ellipsis when near start', () => {
    component.totalCount = 400; // 20 pages
    component.maxVisiblePages = 7;
    component.currentPage = 2;
    const pages = component.pageNumbers;
    expect(pages[0]).toBe(1);
    expect(pages).toContain('…');
    expect(pages[pages.length - 1]).toBe(20);
    // Should not start with ellipsis after 1
    expect(pages[1]).not.toBe('…');
  });

  it('should show right pages without right ellipsis when near end', () => {
    component.totalCount = 400; // 20 pages
    component.maxVisiblePages = 7;
    component.currentPage = 19;
    const pages = component.pageNumbers;
    expect(pages[0]).toBe(1);
    expect(pages[1]).toBe('…');
    expect(pages[pages.length - 1]).toBe(20);
    // Should not have ellipsis before last page
    expect(pages[pages.length - 2]).not.toBe('…');
  });

  it('should set loading to false after error', () => {
    mockService.getNewestStories.and.returnValue(throwError(() => new Error('fail')));
    component.loadStories();
    expect(component.loading).toBeFalse();
  });

  it('should clear error on successful load', () => {
    component.error = 'Previous error';
    component.loadStories();
    expect(component.error).toBe('');
  });
});
