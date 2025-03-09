import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap, catchError, throwError, from, Observable } from 'rxjs';
import { environment } from '../../environments/environment';


@Injectable({
  providedIn: 'root'
})
export class BookService {
  private apiUrl = `${environment.apiUrl}/books`;

  constructor(private http: HttpClient) { }

  getAllBooks(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
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
    return this.http.get<any[]>(`${this.apiUrl}/genres`);
  }

  getAgeCategories(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/agecategories`);
  }

  getSeries(): Observable<string[]> {
    return this.http.get<string[]>(`${this.apiUrl}/series`);
  }

  createSeries(series: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/series`, series);
  }

  createGenre(genre: any): Observable<any> {
    return this.http.post(`${this.apiUrl}/genres`, genre);
  }
}
