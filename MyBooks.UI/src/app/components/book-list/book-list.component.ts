import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { BookService } from '../../services/book.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-book-list',
  standalone: true,
  templateUrl: './book-list.component.html',
  styleUrl: './book-list.component.css',
  encapsulation: ViewEncapsulation.None,
  imports: [
    CommonModule, 
    MatIconModule, 
    MatButtonModule, 
    HttpClientModule, 
    MatFormFieldModule, 
    MatInputModule, 
    FormsModule, 
    MatProgressSpinner
  ]
})
export class BookListComponent {
  books: any[] = [];
  filteredBooks: any[] = [];
  searchQuery: string = '';
  isFinalizing: boolean = true;

  constructor(private bookService: BookService, private router: Router) { }

  ngOnInit(): void {
    this.loadBooks();
  }

  viewBookDetails(book: any) {
    this.router.navigate(['/book', book.id]);
  }

  loadBooks() {
    this.bookService.getAllBooks().subscribe({
      next: (data) => {
        this.books = data.sort((a,b) => a.title.localeCompare(b.title));
        this.filteredBooks = [...this.books];
        this.isFinalizing = false;
      },
      error: (error) => console.error('Error loading books', error)
    });
  }

  filterBooks() {
    const query = this.searchQuery.toLowerCase().trim();
    this.filteredBooks = this.books
      .filter(book =>
        book.title.toLowerCase().includes(query) ||
        (book.author && book.author.toLowerCase().includes(query)) ||
        (book.series && book.series.name && book.series.name.toLowerCase().includes(query))
      )
      .sort((a, b) => a.title.localeCompare(b.title));
  }

  addBook() {
    this.router.navigate(['/create']);
  }
}
