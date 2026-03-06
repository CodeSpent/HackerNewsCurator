import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { HackerNewsService } from './hacker-news.service';
import { StoriesResponse } from '../models/story';

describe('HackerNewsService', () => {
  let service: HackerNewsService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        HackerNewsService
      ]
    });
    service = TestBed.inject(HackerNewsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch newest stories with page and pageSize params', () => {
    const mockResponse: StoriesResponse = {
      stories: [{ id: 1, title: 'Test Story', url: 'https://test.com', time: 1709600000, by: 'testuser' }],
      totalCount: 1,
      page: 1,
      pageSize: 20
    };

    service.getNewestStories(1, 20).subscribe(response => {
      expect(response.stories.length).toBe(1);
      expect(response.totalCount).toBe(1);
    });

    const req = httpMock.expectOne(r =>
      r.url === '/api/stories/newest' &&
      r.params.get('page') === '1' &&
      r.params.get('pageSize') === '20'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should include search param when provided', () => {
    const mockResponse: StoriesResponse = {
      stories: [],
      totalCount: 0,
      page: 1,
      pageSize: 20
    };

    service.getNewestStories(1, 20, 'angular').subscribe(response => {
      expect(response.totalCount).toBe(0);
    });

    const req = httpMock.expectOne(r =>
      r.url === '/api/stories/newest' &&
      r.params.get('search') === 'angular'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should include nocache param when true', () => {
    const mockResponse: StoriesResponse = {
      stories: [],
      totalCount: 0,
      page: 1,
      pageSize: 20
    };

    service.getNewestStories(1, 20, undefined, true).subscribe(response => {
      expect(response.totalCount).toBe(0);
    });

    const req = httpMock.expectOne(r =>
      r.url === '/api/stories/newest' &&
      r.params.get('nocache') === 'true'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should not include nocache param when false', () => {
    const mockResponse: StoriesResponse = {
      stories: [],
      totalCount: 0,
      page: 1,
      pageSize: 20
    };

    service.getNewestStories(1, 20, undefined, false).subscribe(response => {
      expect(response.totalCount).toBe(0);
    });

    const req = httpMock.expectOne(r =>
      r.url === '/api/stories/newest' &&
      !r.params.has('nocache')
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should not include search param when not provided', () => {
    const mockResponse: StoriesResponse = {
      stories: [],
      totalCount: 0,
      page: 1,
      pageSize: 20
    };

    service.getNewestStories(1, 20).subscribe(response => {
      expect(response.stories.length).toBe(0);
    });

    const req = httpMock.expectOne(r =>
      r.url === '/api/stories/newest' &&
      !r.params.has('search')
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });
});
