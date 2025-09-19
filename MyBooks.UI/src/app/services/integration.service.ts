import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class IntegrationService {
  private apiUrl = `${environment.integrationApiUrl}/google-integrations`;

  constructor(private http: HttpClient) {}

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    });
  }

  getAuthorizeUrl(): Observable<any> {
    return this.http.get(`${this.apiUrl}/authorize-url`, { headers: this.getAuthHeaders() })
      .pipe(
        catchError(err => {
          console.error("Error getting authorize URL:", err);
          return throwError(() => err);
        })
      );
  }

  getIntegrations(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl, { headers: this.getAuthHeaders() });
  }

  deleteIntegration(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  getFolders(integrationId: number): Observable<any[]> {
    return this.http.get<any[]>(
      `${this.apiUrl}/${integrationId}/folders`,
      { headers: this.getAuthHeaders() }
    );
  }

  updateFolders(integrationId: number, folderIds: string[]): Observable<void> {
    return this.http.put<void>(
      `${this.apiUrl}/${integrationId}/folders`,
      folderIds,
      { headers: this.getAuthHeaders() }
    );
  }
}
