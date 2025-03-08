import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { BookService } from '../../services/book.service';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButton, MatButtonModule } from '@angular/material/button';
import { HttpClientModule } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

@Component({
  selector: 'app-book-form',
  standalone: true,
  templateUrl: './book-form.component.html',
  styleUrls: ['./book-form.component.css'],
  imports: [
    ReactiveFormsModule,
    CommonModule,
    MatButtonModule,
    MatIconModule,
    HttpClientModule,
    MatCardModule,
    MatButtonModule,
    MatInputModule,
    MatSelectModule,
  ],
})
export class BookFormComponent implements OnInit {
  bookForm!: FormGroup;
  bookId!: number;
  genres: any[] = [];
  ageCategories: any[] = [];
  series: any[] = [];
  newSeries: boolean = false;
  newSeriesName: string = '';

  constructor(
    private fb: FormBuilder,
    private bookService: BookService,
    public router: Router,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    this.bookForm = this.fb.group({
      title: ['', Validators.required],
      author: [''],
      seriesId: [''],
      seriesName: [''],
      genreId: ['', Validators.required],
      publishedDate: [''],
      genre: [null],
      description: [''],
      isbn: [''],
      location: [''],
      tagInput: [''],
      ageCategoryId: ['']
    });
    this.loadGenres();
    this.loadAgeCategories();
    this.loadSeries();

    this.route.paramMap.subscribe(params => {
      const id = Number(params.get('id'));
      if (id) {
        this.bookId = id;
        this.loadBook(this.bookId);
      }
    });
  }

  loadBook(id: number) {
    this.bookService.getBook(id).subscribe({
      next: (book) => {
        if (book) {
          console.log("Loaded book: ", book);
          this.bookForm.patchValue(book);
        }
      },
      error: (error) => console.error('Error loading book: ', error),
      complete: () => console.log('Book load completed')      
    });    
  }

  loadGenres() {
    this.bookService.getGenres().subscribe({
      next: (data: any[]) => this.genres = data
    });
  }

  loadAgeCategories() {
    this.bookService.getAgeCategories().subscribe({
      next: (data: any[]) => this.ageCategories = data
    });
  }

  loadSeries() {
    this.bookService.getSeries().subscribe({
      next: (data: any[]) => this.series = data
    });
  }

  toggleNewSeries() {
    this.newSeries = !this.newSeries;
    this.newSeriesName = '';
  }

  saveSeries() {
    const seriesName = this.bookForm.value.seriesName.trim();

    console.log("series name before sending: ", seriesName);

    if (!seriesName) {
      alert("Series name cannot be empty.");
      return;
    }

    const newSeries = { name: seriesName };

    this.bookService.createSeries(newSeries).subscribe({
      next: (createdSeries) => {
        console.log("series created: ", createdSeries);
        this.series.push(createdSeries);
        this.bookForm.patchValue({ seriesId: createdSeries.id });
      },
      error: (error) => {
        console.error("Error saving series: ", error);
        alert("Failed to save series.");
      }
    });
  }

  saveBook() {
    if (this.bookForm.invalid) {
      alert('Please fill in all required fields.');
      return;
    }

    const formData = { ...this.bookForm.value };

    if (formData.genreId) {
      const selectedGenre = this.genres.find((g) => g.id === formData.genreId);
      formData.Genre = selectedGenre
    }

    if (this.newSeries && formData.series) {
      this.series.push({ name: formData.series });
    }
    console.log("Submitting book form: ", formData);

    if (this.bookId) {
      formData.id = this.bookId;
      this.bookService.updateBook(this.bookId, formData).subscribe({
        next: () => {
          this.router.navigate(['/']);
        },
        error: (error) => {
          console.error('Error updating book:', error);
          alert('Failed to update book.');
        },
        complete: () => console.log('Book update completed')
      });
    } else {
      this.bookService.createBook(this.bookForm.value).subscribe({
        next: () => {
          this.router.navigate(['/']);
        },
        error: (error) => {
          console.error('Error creating book:', error);
          console.error('Full error:', error.error);
          console.error('Error message:', error.error.message);
          console.error('Error error error:', error.error.errors);
          alert('Failed to add book.');
        },
        complete: () => console.log('Book creation completed')
      });
    }
  }
}
