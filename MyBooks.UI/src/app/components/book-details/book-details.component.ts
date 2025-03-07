import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { BookService } from '../../services/book.service';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { HttpClientModule } from '@angular/common/http';

@Component({
  selector: 'app-book-details',
  standalone: true,
  templateUrl: './book-details.component.html',
  styleUrls: ['./book-details.component.css'],
  imports: [CommonModule, MatIconModule, MatButtonModule, RouterModule, HttpClientModule, MatCardModule],
})
export class BookDetailsComponent implements OnInit {
  book: any = null;
  bookCards: any[] = [];

  constructor(
    private route: ActivatedRoute,
    private bookService: BookService,
    private router: Router
  ) { }

  ngOnInit(): void {
    const bookId = Number(this.route.snapshot.paramMap.get('id'));
    if (bookId) {
      this.bookService.getBook(bookId).subscribe({
        next: (data) => {
          this.book = data;
        },
        error: (error) => console.error('Error fetching book details:', error),
        complete: () => console.log('Book fetch completed.')
      });
    }
  }

  editBook(book: any) {
    this.router.navigate(['/create/', book.id])
  }

  deleteBook(book: any) {
    if (confirm("Are you sure you want to delete this book?")) {
      const id = book.id;
      this.bookService.deleteBook(id).subscribe({
        next: () => {
          console.log('Book deleted successfully');
          this.bookCards = this.bookCards.filter(book => book.id !== id);
          this.router.navigate(['/']);
        },
        error: (error) => console.error('Error deleting book', error),
        complete: () => console.log("Delete completed.")
      });
    }
  }
}
