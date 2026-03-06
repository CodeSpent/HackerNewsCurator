import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { StoriesResponse } from '../models/story';

@Injectable({
  providedIn: 'root'
})
export class HackerNewsService {
  private apiUrl = '/api/stories';

  constructor(private http: HttpClient) {}

  getNewestStories(page: number, pageSize: number, search?: string, nocache?: boolean): Observable<StoriesResponse> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search) {
      params = params.set('search', search);
    }

    if (nocache) {
      params = params.set('nocache', 'true');
    }

    return this.http.get<StoriesResponse>(`${this.apiUrl}/newest`, { params });
  }
}
