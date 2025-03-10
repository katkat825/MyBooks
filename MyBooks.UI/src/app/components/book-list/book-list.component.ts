import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { Component, OnInit } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { BookService } from '../../services/book.service';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-book-list',
  standalone: true,
  templateUrl: './book-list.component.html',
  styleUrl: './book-list.component.css',
  imports: [CommonModule, MatIconModule, MatButtonModule, HttpClientModule, MatFormFieldModule, MatInputModule, FormsModule]
})
export class BookListComponent {
  books: any[] = [];
  filteredBooks: any[] = [];
  searchQuery: string = '';

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
        console.log("Books received in UI: ", data);
        this.books = data;
        this.filteredBooks = data;
      },
      error: (error) => console.error('Error loading books', error),
      complete: () => console.log("Books fetch completed.")
    });
  }

  filterBooks() {
    const query = this.searchQuery.toLowerCase().trim();
    this.filteredBooks = this.books.filter(book =>
      book.title.toLowerCase().includes(query) ||
      (book.author && book.author.toLowerCase().includes(query))
    );
  }

  addBook() {
    this.router.navigate(['/create']);
  }
}
