import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BookService } from '../../services/book.service';
import { Router } from '@angular/router';
import { MatStepperModule } from '@angular/material/stepper';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-book-form',
  templateUrl: './book-form.component.html',
  styleUrls: ['./book-form.component.css'],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatStepperModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule
  ]
})
export class BookFormComponent implements OnInit {
  bookForm!: FormGroup;
  selectedFile: File | null = null;
  bookId!: number;
  genres: any[] = [];
  ageCategories: any[] = [];
  seriesList: any[] = [];
  newSeries: boolean = false;

  constructor(private fb: FormBuilder, private bookService: BookService, private router: Router) { }

  ngOnInit(): void {
    this.bookForm = this.fb.group({
      step1: this.fb.group({
        title: ['', Validators.required],
        genreId: ['', Validators.required],
        ageCategoryId: ['', Validators.required]
      }),
      step2: this.fb.group({
        author: [''],
        seriesId: [null],
        seriesPosition: [null],
        isbn: [''],
        description: ['']
      }),
      step3: this.fb.group({ file: [null] })
    });
    this.loadGenres();
    this.loadAgeCategories();
    this.loadSeries();
  }

  get step1(): FormGroup {
    return this.bookForm.get('step1') as FormGroup;
  }

  get step2(): FormGroup {
    return this.bookForm.get('step2') as FormGroup;
  }

  get step3(): FormGroup {
    return this.bookForm.get('step3') as FormGroup;
  }

  loadGenres() {
    this.bookService.getGenres().subscribe({
      next: (data: any[]) => this.genres = data
    })
  }

  loadAgeCategories() {
    this.bookService.getAgeCategories().subscribe({
      next: (data: any[]) => this.ageCategories = data
    })
  }

  loadSeries() {
    this.bookService.getSeries().subscribe({
      next: (data: any[]) => this.seriesList = data
    })
  }

  saveSeries() {
    const seriesName = this.bookForm.value.seriesName.trim();

    if (!seriesName) {
      alert("Series name cannot be empty.");
    }

    const newSeries = { name: seriesName };

    this.bookService.createSeries(newSeries).subscribe({
      next: (createdSeries) => {
        this.seriesList.push(createdSeries);
        this.bookForm.patchValue({ seriesId: createdSeries.id });
      },
      error: (error) => {
        console.error("Error saving series: ", error);
        alert("Failed to save series.");
      }
    })
  }

  toggleNewSeries() {
    this.newSeries = !this.newSeries;
    this.bookForm.patchValue({ seriesName: '' });
  }

  onFileSelected(event: any) {
    this.selectedFile = event.target.files[0] || null;
  }

  uploadFileWithBookId() {
    if (!this.selectedFile || !this.bookId) {
      console.warn("❌ No file selected or bookId missing.");
      return;
    }

    this.bookService.uploadFile(this.selectedFile, this.bookId).subscribe({
      next: (response) => {
        console.log('✅ File uploaded:', response);

        // Now update the book with the fileId
        this.bookService.updateBookFileId(this.bookId!, response.fileId).subscribe({
          next: () => {
            console.log("✅ Book updated with FileId");
            this.router.navigate(['/']); // Navigate after everything is done
          },
          error: (error) => console.error("❌ Failed to update book with FileId", error)
        });
      },
      error: (error) => console.error('❌ File upload failed:', error)
    });
  }

  saveBook() {
    if (this.bookForm.invalid) {
      alert('Please complete all required steps.');
      return;
    }

    const bookData = {
      ...this.bookForm.value.step1,
      ...this.bookForm.value.step2
    };

    this.bookService.createBook(bookData).subscribe({
      next: (response) => {
        console.log('✅ Book saved:', response);
        this.bookId = response.id;  // Store the generated bookId

        // Check if a file is selected before proceeding
        if (this.selectedFile) {
          this.uploadFileWithBookId();
        } else {
          this.router.navigate(['/']); // No file? Just navigate away
        }
      },
      error: (error) => {
        console.error('❌ Error saving book:', error);
        alert('Failed to save book.');
      }
    });
  }
}
