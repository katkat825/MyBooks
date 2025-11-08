import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface BulkImportFileOverrideDto {
  fileId: string;
  genreId?: number;
  ageCategoryId?: number;
}

export interface BulkImportStartDto {
  fileIds: string[];
  genreId: number;
  ageCategoryId: number;
  integrationId: number;
  overrides?: BulkImportFileOverrideDto[];
}

@Injectable({
  providedIn: 'root'
})

export class BulkImportService {
  private readonly baseUrl = `${environment.fileBaseUrl}/api/bulk-import`;
  private readonly filesUrl = `${environment.fileBaseUrl}/api/files`;

  constructor(private http: HttpClient) {}

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
    });
  }

  startBulkImport(dto: BulkImportStartDto): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/start`, dto, {
        headers: this.getAuthHeaders(),
    });
  }

  getJobs(): Observable<any[]> {
      return this.http.get<any[]>(`${this.baseUrl}/jobs`, {
        headers: this.getAuthHeaders(),
      });
  }

  getJobStatus(jobId: number): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/status/${jobId}`, {
      headers: this.getAuthHeaders(),
    });
  }

  getExistingFileIds(integrationId: number): Observable<string[]> {
      return this.http.get<string[]>(`${this.filesUrl}/ids/${integrationId}`, {
        headers: this.getAuthHeaders(),
      });
  }
}