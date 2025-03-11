import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, tap, catchError, throwError, from, Observable } from 'rxjs';
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class BookService {
  private apiUrl = `${environment.apiUrl}/books`;
  private fileApiUrl = `https://localhost:7142/api/files`;

  constructor(private http: HttpClient) { }

  getAllBooks(): Observable<any[]> {
    return this.http.get<any>(`${this.apiUrl}`).pipe(
      map(response => response.$values ?? response), // ✅ Extracts $values if present
      tap((data) => console.log("Fetched books in UI:", data)), // ✅ Debugging log
      catchError((error) => {
        console.error("Error fetching books:", error);
        return throwError(error);
      })
    );
  }

  deleteBook(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  createBook(book: any): Observable<any> {
    console.log("sending book data: ", book);
    return this.http.post(this.apiUrl, book).pipe(
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
    return this.http.put(`${this.apiUrl}/${id}`, updatedBook);
  }

  updateBookFileId(bookId: number, fileId: number): Observable<any> {
    console.log('request to update book ${bookId} with FileId: ${fileId}');
    return this.http.patch(`${environment.apiUrl}/books/${bookId}/file`, { fileId }).pipe(
      tap(() => console.log('successfulle updated bookId: ${bookId} with fileId: ${fileId}')),
      catchError(error => {
        console.error("❌ Error updating book with FileId:", error);
        return throwError(() => new Error("Failed to update book with FileId"));
      })
    );
  }

  getBook(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  getGenres(): Observable<any[]> {
    return this.http.get<any>(`${this.apiUrl}/genres`).pipe(
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
    return this.http.post(`${this.apiUrl}/genres`, genre);
  }

  updateGenre(id: number, genre: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/genres/${id}`, genre);
  }

  deleteGenre(id: number): Observable<any> {
    return this.http.delete<void>(`${this.apiUrl}/genres/${id}`);
  }

  getAgeCategories(): Observable<any[]> {
    return this.http.get<any>(`${this.apiUrl}/agecategories`).pipe(
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
    return this.http.get<any>(`${this.apiUrl}/series`).pipe(
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
    return this.http.post(`${this.apiUrl}/series`, series);
  }

  updateSeries(id: number, series: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/series/${id}`, series);
  }

  deleteSeries(id: number): Observable<any> {
    return this.http.delete<void>(`${this.apiUrl}/series/${id}`);
  }

  uploadFile(file: File, bookId?: number): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    if (bookId) { 
      formData.append('bookId', bookId.toString());
    }
    return this.http.post(`${this.fileApiUrl}/upload`, formData);
  }

  downloadFile(fileId: number): Observable<Blob> {
    return this.http.get(`${this.fileApiUrl}/${fileId}`, { responseType: 'blob' });
  }
}
