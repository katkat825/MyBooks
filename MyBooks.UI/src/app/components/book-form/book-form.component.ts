import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { BookService } from '../../services/book.service';
import { Router, ActivatedRoute } from '@angular/router';
import { MatStepperModule } from '@angular/material/stepper';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { HttpClientModule } from '@angular/common/http';

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
    MatIconModule,
    HttpClientModule
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
  fileId?: number;

  constructor(
    private fb: FormBuilder,
    private bookService: BookService,
    private router: Router,
    private route: ActivatedRoute
  ) { }

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

    this.route.paramMap.subscribe(params => {
      const id = Number(params.get('id'));
      if (id) {
        this.bookId = id;
        this.loadBook(this.bookId);
      }
    });
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

  loadBook(id: number) {
    this.bookService.getBook(id).subscribe({
      next: (book) => {
        if (book) {
          this.bookForm.patchValue({ step1: book, step2: book });
          this.bookId = book.id;
          this.fileId = book.fileId;
        }
      },
      error: (error) => console.error('Error loading book: ', error)
    });
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

  toggleNewSeries() {
    this.newSeries = !this.newSeries;
    this.bookForm.patchValue({ seriesName: '' });
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

  saveStep1() {
    if (this.step1.invalid) {
      alert('Please fill in all required fields.');
      return;
    }

    const bookData = { ...this.step1.value };

    if (this.bookId) {
      this.bookService.updateBook(this.bookId, bookData).subscribe({
        next: () => console.log("Step 1: book updated successfully"),
        error: (error) => console.error("Error updating book: ", error)
      });
    } else {
      this.bookService.createBook(bookData).subscribe({
        next: (response) => {
          console.log("Step 1: book created successfully");
          this.bookId = response.id;
        },
        error: (error) => console.error("Error creating book: ", error)
      });
    }
  }

  saveStep2() {
    if (!this.bookId) return;

    const bookData = {
      id: this.bookId,
      ...this.step1.value,
      ...this.step2.value,
      fileId: this.fileId ?? null
    };

    console.log("updating book ID: ", this.bookId);
    console.log("book data sent:", bookData);

    this.bookService.updateBook(this.bookId, bookData).subscribe({
      next: () => console.log('Step 2: book updated successfully'),
      error: (error) => console.error('Error updating book: ', error)
    });
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

  updateBookWithFileId() {
    if (!this.fileId || !this.bookId) return;

    this.bookService.updateBookFileId(this.bookId, this.fileId).subscribe({
      next: () => console.log("Step 4: book updated with FileId"),
      error: (error) => console.error("Failed to update book with FileId", error)
    });
  }
}
