import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { BookService } from '../../services/book.service';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-admin-series',
  standalone: true,
  templateUrl: './admin-series.component.html',
  styleUrls: ['./admin-series.component.css'],
  imports: [FormsModule, MatFormFieldModule, MatInputModule, CommonModule, MatTableModule, MatIconModule, ReactiveFormsModule]
})

export class AdminSeriesComponent implements OnInit {
  series: any[] = [];
  editForm!: FormGroup;  
  editingSeries: any = null;
  createForm!: FormGroup;
  addingSeries: boolean = false;

  constructor(private bookService: BookService, private fb: FormBuilder) { }

  ngOnInit(): void {
    this.loadSeries();
    this.editForm = this.fb.group({
      name: ['', Validators.required]
    });
    this.createForm = this.fb.group({
      name: ['', Validators.required]
    });
  }

  loadSeries() {
    this.bookService.getSeries().subscribe({
      next: (data) => this.series = data,
      error: (error) => console.error('Error fetching series:', error)
    });
  }

  startEdit(series: any) {
    this.editingSeries = series;
    this.editForm.patchValue({ name: series.name });
  }

  cancelEdit() {
    this.editingSeries = null;
    this.editForm.reset();
  }

  saveEdit() {
    if (!this.editingSeries) return;

    const updatedSeries = { ...this.editingSeries, name: this.editForm.value.name };

    this.bookService.updateSeries(updatedSeries.id, updatedSeries).subscribe({
      next: () => {
        this.loadSeries();
        this.cancelEdit();
      },
      error: (error) => {
        console.error("Error updating series: ", error);
        alert("Failed to update series.");
      }
    });
  }

  cancelCreate() {
    this.addingSeries = false;
    this.createForm.reset();
  }

  addSeries() {
    this.addingSeries = true;
    this.createForm.reset();
  }

  saveCreate() {
    if (this.createForm.invalid) {
      alert("Series name cannot be empty.");
      return;
    }

    const newSeries = { name: this.createForm.value.name.trim() };

    this.bookService.createSeries(newSeries).subscribe({
      next: (createdSeries) => {
        this.series.push(createdSeries);
        this.addingSeries = false;
        this.createForm.reset();
        this.loadSeries();
      },
      error: (error) => {
        console.error("error adding series: ", error);
        alert("Failed to add series.");
      }
    })
  }

  deleteSeries(id: number) {
    if (confirm('Are you sure you want to delete this series?')) {
      this.bookService.deleteSeries(id).subscribe({
        next: () => this.loadSeries(),
        error: (error) => {
          console.error('Error deleting series:', error)
          if (error.status === 409) {
            alert("This series cannot be deleted because it contains books.");
          } else {
            alert("An error occurred while deleting the series.");
          }
        }
      });
    }
  }
}
