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
  pickerAccessToken?: string;
}

@Injectable({
  providedIn: 'root'
})

export class BulkImportService {
  private readonly importUrl = `${environment.fileBaseUrl}/import`;
  private readonly fileBaseUrl = environment.fileBaseUrl;

  constructor(private http: HttpClient) {}

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
        'Content-Type': 'application/json',
        Authorization: `Bearer ${token}`,
    });
  }

  startBulkImport(dto: BulkImportStartDto): Observable<void> {
    return this.http.post<void>(`${this.importUrl}/start`, dto, {
        headers: this.getAuthHeaders(),
    });
  }

  getJobs(): Observable<any[]> {
      return this.http.get<any[]>(`${this.importUrl}/jobs`, {
        headers: this.getAuthHeaders(),
      });
  }

  getJobStatus(jobId: number): Observable<any> {
    return this.http.get<any>(`${this.importUrl}/${jobId}/status`, {
      headers: this.getAuthHeaders(),
    });
  }

  getExistingFileIds(integrationId: number): Observable<string[]> {
      return this.http.get<string[]>(`${this.fileBaseUrl}/ids/integration/${integrationId}`, {
        headers: this.getAuthHeaders(),
      });
  }
}