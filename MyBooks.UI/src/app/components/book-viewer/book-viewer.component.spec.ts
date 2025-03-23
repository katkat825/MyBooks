import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BookViewerComponent } from './book-viewer.component';

describe('BookViewerComponent', () => {
  let component: BookViewerComponent;
  let fixture: ComponentFixture<BookViewerComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [BookViewerComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BookViewerComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
