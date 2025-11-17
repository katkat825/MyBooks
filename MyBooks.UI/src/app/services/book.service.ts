import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { map, tap, catchError, throwError, from, Observable } from 'rxjs';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BookService {
  private genresUrl = `${environment.catalogBaseUrl}/genres`;
  private booksUrl = `${environment.catalogBaseUrl}/books`;
  private ageUrl = `${environment.catalogBaseUrl}/age-ratings`;
  private seriesUrl = `${environment.catalogBaseUrl}/series`;
  public fileBaseUrl =  `${environment.fileBaseUrl}`;

  constructor(private http: HttpClient) { }

  private getAuthHeaders(): HttpHeaders {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Content-Type': 'application/json',
      Authorization: `Bearer ${token}`,
    });
  }

  getRecentlyRead(count: number = 10): Observable<any[]> {
      return this.http.get<any>(`${this.booksUrl}/recently-read?count=${count}`, { headers: this.getAuthHeaders() }).pipe(
        map(response => {
          const list = response.$values ?? response;
          return list;
        }),
        catchError(error => {
          console.error("Error fetching recently read books: ", error);
          return throwError(() => error);
        })
      );
    }

  getBooks(page: number, pageSize: number = 20): Observable<any> {
    return this.http.get<any>(`${this.booksUrl}?page=${page}&pageSize=${pageSize}`, { headers: this.getAuthHeaders() }).pipe(
      map(response => {
        // handle $values (JSON.NET or EF weird serialization)
        const resultsRaw = response.results ?? response.Results ?? response.$values ?? response.Results?.$values;
        let results = Array.isArray(resultsRaw)
          ? resultsRaw
          : Array.isArray(response.results?.$values)
          ? response.results.$values
          : Array.isArray(response.Results?.$values)
          ? response.Results.$values
          : [];

        if (Array.isArray(results)) {
          results = results.sort((a, b) =>
            (a?.title || '').localeCompare(b?.title || '')
          );
        } else {
          console.warn('Unexpected results shape:', response);
          results = [];
        }

        return {
          totalCount: response.totalCount ?? response.TotalCount ?? results.length,
          page: response.page ?? response.Page ?? page,
          pageSize: response.pageSize ?? response.PageSize ?? pageSize,
          results
        };
      }),
      catchError(error => {
        console.error("Error fetching paginated books:", error);
        return throwError(() => error);
      })
    );
  }

  getAllBooks(): Observable<any[]> {
    return this.http.get<any>(this.booksUrl, { headers: this.getAuthHeaders() }).pipe(
      map(response => {
        const list = response.$values ?? response;
        return Array.isArray(list)
          ? list.sort((a, b) => a.title.localeCompare(b.title))
          : list;
      }),
      catchError(error => {
        console.error("Error fetching books:", error);
        return throwError(() => error);
      })
    );
  }

  deleteBook(id: number): Observable<void> {
    return this.http.delete<void>(`${this.booksUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  createBook(book: any): Observable<any> {
    return this.http.post(this.booksUrl, book, { headers: this.getAuthHeaders() }).pipe(
      tap(() => console.log("Book created successfully.")),
      catchError((error) => {
        console.error("Error creating book:", error);
        return throwError(() => error);
      })
    );
  }

  updateBook(id: number, book: any): Observable<any> {
    const updatedBook = {
      ...book, id};
    return this.http.put(`${this.booksUrl}/${id}`, updatedBook, { headers: this.getAuthHeaders() });
  }

  updateBookFileId(bookId: number, fileId: number): Observable<any> {
    return this.http.patch(`${this.booksUrl}/${bookId}/file`, { fileId }, { headers: this.getAuthHeaders() }).pipe(
      catchError(error => {
        console.error("❌ Error updating book with FileId:", error);
        return throwError(() => new Error("Failed to update book with FileId"));
      })
    );
  }

  getBook(id: number): Observable<any> {
    return this.http.get<any>(`${this.booksUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  getGenres(): Observable<any[]> {
    return this.http.get<any>(`${this.genresUrl}`, { headers: this.getAuthHeaders() }).pipe(
      map(response => {
        const list = response && typeof response === 'object' && '$values' in response
          ? response.$values
          : response;
        return Array.isArray(list)
          ? list.sort((a, b) => a.name.localeCompare(b.name))
          : list;
      }),
      catchError(error => {
        console.error("Error fetching genre:", error);
        return throwError(() => error);
      })
    );
  }

  createGenre(genre: any): Observable<any> {
    return this.http.post(`${this.genresUrl}`, genre, { headers: this.getAuthHeaders() });
  }

  updateGenre(id: number, genre: any): Observable<any> {
    return this.http.put(`${this.genresUrl}/${id}`, genre, { headers: this.getAuthHeaders() });
  }

  deleteGenre(id: number): Observable<any> {
    return this.http.delete<void>(`${this.genresUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  getAgeCategories(): Observable<any[]> {
    return this.http.get<any>(this.ageUrl, { headers: this.getAuthHeaders() }).pipe(
      map(response => {
        return response && typeof response === 'object' && '$values' in response
          ? response.$values
          : response;
      }),
      catchError((error) => {
        console.error("Error fetching genre:", error);
        return throwError(() => error);
      })
    );
  }

  getSeries(): Observable<any[]> {
    return this.http.get<any>(this.seriesUrl, { headers: this.getAuthHeaders() }).pipe(
      map(response => {
        const list = response && typeof response === 'object' && '$values' in response
          ? response.$values
          : response;
        return Array.isArray(list)
          ? list.sort((a, b) => a.name.localeCompare(b.name))
          : list;
      }),
      catchError(error => {
        console.error("Error fetching series:", error);
        return throwError(() => error);
      })
    );
  }

  createSeries(series: any): Observable<any> {
    return this.http.post(this.seriesUrl, series, { headers: this.getAuthHeaders() });
  }

  updateSeries(id: number, series: any): Observable<any> {
    return this.http.put(`${this.seriesUrl}/${id}`, series, { headers: this.getAuthHeaders() });
  }

  deleteSeries(id: number): Observable<any> {
    return this.http.delete<void>(`${this.seriesUrl}/${id}`, { headers: this.getAuthHeaders() });
  }

  uploadFile(file: File, bookId: number, bookTitle: string): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('bookId', bookId.toString());
    formData.append('bookTitle', bookTitle);

    return this.http.post(`&{this.fileBaseUrl}/upload`, formData, {
      headers: new HttpHeaders({
        Authorization: `Bearer ${localStorage.getItem('token')}`,
      }),
    });
  }

  downloadFile(fileId: number, inline: boolean = false): Observable<Blob> {
    return this.http.get(`${this.fileBaseUrl}/${fileId}?inline=${inline}`, { responseType: 'blob', headers: this.getAuthHeaders() });
  }

  deleteFile(fileId: number) {
    return this.http.delete(`${this.fileBaseUrl}/${fileId}`, { headers: this.getAuthHeaders() });
  }

  getFileMetadata(fileId: number): Observable<any> {
    return this.http.get(`${this.fileBaseUrl}/metadata/${fileId}`, { headers: this.getAuthHeaders() });
  }

  getReadingProgress(fileId: number): Observable<any> {
    return this.http.get(`${this.fileBaseUrl}/progress/${fileId}`, { headers: this.getAuthHeaders() });
  }

  updateReadingProgress(fileId: number, progress: number): Observable<any> {
    return this.http.post(`${this.fileBaseUrl}/progress/${fileId}`, { ProgressPercent: progress }, { headers: this.getAuthHeaders() });
  }
}
