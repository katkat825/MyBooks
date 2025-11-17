import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../environments/environment';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class EmailService {
    private emailViolationsUrl = `${environment.emailBaseUrl}/abuse/report`;

    constructor(private http: HttpClient) {}

    private getAuthHeaders(): HttpHeaders {
        const token = localStorage.getItem('token');
        return new HttpHeaders({
            'Content-Type': 'application/json',
            Authorization: `Bearer ${token}`,
        });
    }

    sendViolationReport(dto: { description: string; contactEmail: string }): Observable<void> {
        return this.http.post<void>(this.emailViolationsUrl, dto, { headers: this.getAuthHeaders() });
    }
}