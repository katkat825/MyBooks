import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { map, tap, catchError, throwError, from, Observable } from 'rxjs';
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class BookService {
  private apiUrl = `${environment.apiUrl}/books`;
  private fileApiUrl = environment.fileApiUrl;

  constructor(private http: HttpClient) { }

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    });
  }

  getAllBooks(): Observable<any[]> {
    return this.http.get<any>(this.apiUrl, {headers: this.getAuthHeaders()}).pipe(
      map(response => response.$values ?? response), 
      tap((data) => console.log("Fetched books in UI:", data)), 
      catchError((error) => {
        console.error("Error fetching books:", error);
        return throwError(error);
      })
    );
  }

  deleteBook(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  createBook(book: any): Observable<any> {
    console.log("sending book data: ", book);
    return this.http.post(this.apiUrl, book, { headers: this.getAuthHeaders() }).pipe(
      tap(() => console.log("Book created successfully.")),
      catchError((error) => {
        console.error("Error creating book:", error);
        return throwError(error);
      })
    );
  }

  updateBook(id: number, book: any): Observable<any> {
    const updatedBook = {
      ...book, id};
    return this.http.put(`${this.apiUrl}/${id}`, updatedBook, { headers: this.getAuthHeaders() });
  }

  updateBookFileId(bookId: number, fileId: number): Observable<any> {
    return this.http.patch(`${environment.apiUrl}/books/${bookId}/file`, { fileId }, { headers: this.getAuthHeaders() }).pipe(
      tap(() => console.log('successfully updated bookId: ${bookId} with fileId: ${fileId}')),
      catchError(error => {
        console.error("❌ Error updating book with FileId:", error);
        return throwError(() => new Error("Failed to update book with FileId"));
      })
    );
  }

  getBook(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  getGenres(): Observable<any[]> {
    return this.http.get<any>(`${this.apiUrl}/genres`, { headers: this.getAuthHeaders() }).pipe(
      map(response => {
        // ✅ Check if $values exists and is an array
        return response && typeof response === 'object' && '$values' in response
          ? response.$values
          : response;
      }),
      catchError((error) => {
        console.error("Error fetching genre:", error);
        return throwError(error);
      })
    );
  }

  createGenre(genre: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/genres`, genre, { headers: this.getAuthHeaders() });
  }

  updateGenre(id: number, genre: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/genres/${id}`, genre, { headers: this.getAuthHeaders() });
  }

  deleteGenre(id: number): Observable<any> {
    return this.http.delete<void>(`${this.apiUrl}/genres/${id}`, { headers: this.getAuthHeaders() });
  }

  getAgeCategories(): Observable<any[]> {
    return this.http.get<any>(`${this.apiUrl}/agecategories`, { headers: this.getAuthHeaders() }).pipe(
      map(response => {
        return response && typeof response === 'object' && '$values' in response
          ? response.$values
          : response;
      }),
      catchError((error) => {
        console.error("Error fetching genre:", error);
        return throwError(error);
      })
    );
  }

  getSeries(): Observable<string[]> {
    return this.http.get<any>(`${this.apiUrl}/series`, { headers: this.getAuthHeaders() }).pipe(
      map(response => {
        // ✅ Check if $values exists and is an array
        return response && typeof response === 'object' && '$values' in response
          ? response.$values
          : response;
      }),
      catchError((error) => {
        console.error("Error fetching series:", error);
        return throwError(error);
      })
    );
  }

  createSeries(series: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/series`, series, { headers: this.getAuthHeaders() });
  }

  updateSeries(id: number, series: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/series/${id}`, series, { headers: this.getAuthHeaders() });
  }

  deleteSeries(id: number): Observable<any> {
    return this.http.delete<void>(`${this.apiUrl}/series/${id}`, { headers: this.getAuthHeaders() });
  }

  uploadFile(file: File, bookId: number): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('bookId', bookId.toString());

    formData.forEach((value, key) => console.log(`📝 ${key}:`, value));

    return this.http.post(`${this.fileApiUrl}/upload`, formData, {
      headers: new HttpHeaders({
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      }),
    });
  }

  downloadFile(fileId: number): Observable<Blob> {
    return this.http.get(`${this.fileApiUrl}/${fileId}`, { responseType: 'blob', headers: this.getAuthHeaders() });
  }

  deleteFile(fileId: number) {
    return this.http.delete(`${this.fileApiUrl}/${fileId}`, { headers: this.getAuthHeaders() });
  }
}
