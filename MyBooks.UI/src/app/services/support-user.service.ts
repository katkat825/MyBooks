import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { HttpHeaders } from '@angular/common/http';
import { Observable, map, catchError, throwError, of } from 'rxjs';

export interface CreateReportLogDto {
  dateReceived: string;
  reportedBy: string;
  reportType: string;
  status: string;
  description: string;
  targetType?: string;
  targetId?: number | null;
  targetCreatedBy?: string;
}

export interface UpdateReportLogDto {
  status?: string;
  resolution?: string;
  resolutionNotes?: string;
  reviewNotes?: string;
  dateClosed?: string; 
  targetType?: string;
  targetId?: number | null;
  targetCreatedBy?: string;  
}

export interface ReportLog {
  id: number;
  reportedBy: string;
  reportType: string;
  status: string;
  description: string;
  targetType?: string;
  targetId?: number | null;
  targetCreatedBy?: string;
  dateReceived: string;
  dateClosed?: string;
  resolution?: string;
  resolutionNotes?: string;
  reviewNotes?: string;
}

@Injectable({
  providedIn: 'root'
})
export class SupportUserService {
  private impersonationsUrl = `${environment.authBaseUrl}/support/impersonate`;
  private usersSupportUrl = `${environment.authBaseUrl}/support/users`;
  private globalReviewerUrl = `${environment.authBaseUrl}/support/reviewers`;
  private catalogSupportUrl = `${environment.catalogBaseUrl}/support/books`;
  private fileSupportUrl = `${environment.fileBaseUrl}/support`;
  private tenantSupportUrl = `${environment.tenantBaseUrl}/support`;
  private tenantBaseUrl = environment.tenantBaseUrl;
  private reportLogsUrl = `${environment.supportBaseUrl}/logs/violations`;

  public static statusOptions = ["New", "In Review", "Waiting on Info", "Closed", "Reopened"];
  public static resolutionOptions = ["Item Removed", "No Violation Found", "Duplicate Report", "Invalid Report"];
  public static reportTypes = ["Abuse", "DMCA"];

  constructor(private http: HttpClient) {}

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    });
  }

  // impersonation
  impersonate(userId: number): Observable<{ token: string; logId: number }> {
    const currentToken = localStorage.getItem('token');
    if(currentToken) {
      localStorage.setItem('originalToken', currentToken);
    }

    return this.http.post<{ token: string; logId: number }>(
      `${this.impersonationsUrl}/${userId}`, {}, { headers: this.getAuthHeaders() }
    );
  }

  stopImpersonation(logId: number): Observable<void> {
    return this.http.post<void>(
      `${this.impersonationsUrl}/stop/${logId}`, {}, { headers: this.getAuthHeaders() }
    );
  }

  impersonateAccount(tenantId: number): Observable<{ token: string; logId: number }> {
    const currentToken = localStorage.getItem('token');
    if(currentToken) {
      localStorage.setItem('originalToken', currentToken);
    }

    return this.http.post<{ token: string; logId: number }>(
      `${this.impersonationsUrl}/tenants/${tenantId}`, {}, { headers: this.getAuthHeaders() }
    );
  }

  //book-viewer component requirements
  getBook(id: number): Observable<any> {
    return this.http.get<any>(`${this.catalogSupportUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  downloadFile(fileId: number): Observable<Blob> {
    return this.http.get(`${this.fileSupportUrl}/${fileId}`, { responseType: 'blob', headers: this.getAuthHeaders() });
  }

  getFileMetadata(fileId: number) {
    return this.http.get(`${this.fileSupportUrl}/metadata/${fileId}`, { headers: this.getAuthHeaders() });
  }

  getReadingProgress(fileId: number): Observable<any> {
    return this.http.get(`${this.fileSupportUrl}/progress/${fileId}`, { headers: this.getAuthHeaders() });
  }

  updateReadingProgress(fileId: number, progress: number): Observable<any> {
    return this.http.post(`${this.fileSupportUrl}/progress/${fileId}`, { ProgressPercent: progress }, { headers: this.getAuthHeaders() });
  }

  // books
  getAllBooks(): Observable<any> {
    return this.http.get<any>(this.catalogSupportUrl, { headers: this.getAuthHeaders() });
  }

  toggleBookRestricted(id: number, restricted: boolean): Observable<void> {
    return this.http.patch<void>(
      `${this.catalogSupportUrl}/${id}/restricted?restricted=${restricted}`, {}, { headers: this.getAuthHeaders() }
    );
  }

  updateBookFileLink(bookId: number, fileId: number): Observable<void> {
    return this.http.patch<void>(
      `${this.catalogSupportUrl}/${bookId}/file`,
      { fileId },
      { headers: this.getAuthHeaders() }
    );
  }

  // files
  getAllFilesForBook(bookId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.fileSupportUrl}/book/${bookId}`, { headers: this.getAuthHeaders() });
  }

  activateFile(fileId: number): Observable<void> {
    return this.http.patch<void>(
      `${this.fileSupportUrl}/${fileId}/activate`, {}, { headers: this.getAuthHeaders() }
    );
  }

  // tenants
  getAllTenants(): Observable<any> {
    return this.http.get<any[]>(this.tenantSupportUrl, { headers: this.getAuthHeaders() });
  }

  getTenantById(id: number): Observable<any> {
    return this.http.get<any>(`${this.tenantSupportUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  createTenant(dto: any): Observable<any> {
    return this.http.post<any>(this.tenantSupportUrl, dto, { headers: this.getAuthHeaders() });
  }

  updateTenant(id: number, dto: any): Observable<void> {
    return this.http.put<void>(`${this.tenantBaseUrl}/${id}`, dto, { headers: this.getAuthHeaders() });
  }

  toggleTenantActiveStatus(id: number, isActive: boolean): Observable<void> {
    return this.http.patch<void>(`${this.tenantSupportUrl}/${id}/status`, isActive, { headers: this.getAuthHeaders() });
  }

  // users
  getAllUsers(): Observable<any[]> {
    return this.http.get<any[]>(`${this.usersSupportUrl}/all`, {
      headers: this.getAuthHeaders(),
    });
  }

  // abuse reports
  getAllReports(): Observable<ReportLog[]> {
    return this.http.get<ReportLog[]>(this.reportLogsUrl, { headers: this.getAuthHeaders() });
  }

  getReportById(id: number): Observable<ReportLog> {
    return this.http.get<ReportLog>(`${this.reportLogsUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  createReport(dto: CreateReportLogDto): Observable<ReportLog> {
    return this.http.post<ReportLog>(this.reportLogsUrl, dto, { headers: this.getAuthHeaders() });
  }

  updateReport(id: number, dto: UpdateReportLogDto): Observable<void> {
    return this.http.put<void>(`${this.reportLogsUrl}/${id}`, dto,  { headers: this.getAuthHeaders() });
  }

  // global reviewer access
  getGlobalReviewers(): Observable<any[]> {
    return this.http.get<any[]>(this.globalReviewerUrl, { headers: this.getAuthHeaders() });
  }

  grantGlobalReviewerAccess(userId: number): Observable<any> {
    return this.http.post<any>(
      `${this.globalReviewerUrl}/${userId}`,
      {},
      { headers: this.getAuthHeaders() }
    );
  }

  hasGlobalReviewerAccess(): Observable<boolean> {
    const token = localStorage.getItem('token');
    if(!token)
      return of(false);
    let currentUserId: number | null = null;

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      currentUserId = Number(payload['nameId']);
    }
    catch (err) {
      return of(false);
    }

    return this.getGlobalReviewers().pipe(
      map(reviewers => {
        return reviewers.some((r: any) => r.userId === currentUserId && r.isActive);
      }),
      catchError(err => {
        console.error('Error checking global reviewer access', err);
        return of(false);
      })
    )
  }

  revokeGlobalReviewerAccess(userId: number): Observable<any> {
    return this.http.delete<any>(
      `${this.globalReviewerUrl}/${userId}`,
      { headers: this.getAuthHeaders() }
    );
  }

  switchToReviewerPortal(): Observable<any> {
    const currentToken = localStorage.getItem('token');
    if(currentToken) {
      localStorage.setItem('originalToken', currentToken);
    }
    
    return this.http.post<any>(
      `${this.globalReviewerUrl}/switch`,
      {},
      { headers: this.getAuthHeaders() }
    );
  }  
}
