import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, tap, catchError, throwError, from, Observable } from 'rxjs';
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class BookService {
  private apiUrl = `${environment.apiUrl}/books`;

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
}
