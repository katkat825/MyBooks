import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { BookService } from '../../services/book.service';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { HttpClientModule } from '@angular/common/http';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

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
  readingMode: boolean = false;
  readingUrl: SafeResourceUrl = '';
  readingProgress: number = 0;

  constructor(
    private route: ActivatedRoute,
    private bookService: BookService,
    private router: Router,
    private sanitizer: DomSanitizer
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

      if (book.fileId) {
        this.bookService.deleteFile(book.fileId).subscribe({
          next: () => {
            console.log("File deleted successfully, proceeding with book deletion...");
            this.deleteBookRecord(id);
          },
          error: (error) => console.error("Error deleting file", error),
        });
      } else {
        console.log("else called - no fileId detected");
        this.deleteBookRecord(id);
      }
    }
  }

  private deleteBookRecord(id: number) {
    this.bookService.deleteBook(id).subscribe({
      next: () => {
        console.log("book deleted successfully");
        this.router.navigate(['/']);
      },
      error: (error) => console.error("Error deleting book: ", error),
    });
  }

  downloadBookFile(fileId: number) {
    this.bookService.downloadFile(fileId).subscribe({
      next: (fileBlob) => {
        const blobUrl = window.URL.createObjectURL(fileBlob);
        const a = document.createElement('a');
        a.href = blobUrl;
        a.download = this.book.title || 'book-file';
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
      },
      error: (error) => console.error('Error downloading file', error)
    });
  }

  readBookNewWindow(fileId: number) {
    this.bookService.downloadFile(fileId).subscribe({
      next: (fileBlob) => {
        const blobUrl = window.URL.createObjectURL(fileBlob);
        window.open(blobUrl, '_blank');
      },
      error: (error) => console.error('Error opening book in new window', error)
    });
  }

  onReaderLoad(event: any) {
    const iframe = event.target;
    const doc = iframe.contentDocument || iframe.contentWindow.document;

    if (this.readingProgress > 0) {
      const totalScrollable = doc.documentElement.scrollHeight - doc.documentElement.clientHeight;
      const scrollToPosition = (this.readingProgress / 100) * totalScrollable;
      iframe.contentWindow.scrollTo(0, scrollToPosition);
    }

    iframe.contentWindow.addEventListener('scroll', () => {
      const scrollTop = iframe.contentWindow.scrollY;
      const totalScrollable = doc.documentElement.scrollHeight - doc.documentElement.clientHeight;
      const progress = totalScrollable > 0 ? (scrollTop / totalScrollable) * 100 : 0;
      this.readingProgress = progress;

      this.bookService.updateReadingProgress(this.book.fileId, progress).subscribe({
        next: (res) => console.log('Progress updated:', res),
        error: (err) => console.error('Error updating progress:', err)
      });
    });
  }
}
