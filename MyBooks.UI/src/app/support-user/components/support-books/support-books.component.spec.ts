import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SupportBooksComponent } from './support-books.component';

describe('SupportBooksComponent', () => {
  let component: SupportBooksComponent;
  let fixture: ComponentFixture<SupportBooksComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [SupportBooksComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SupportBooksComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
