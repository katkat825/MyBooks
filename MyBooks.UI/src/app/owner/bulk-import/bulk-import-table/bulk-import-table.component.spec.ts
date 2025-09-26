import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BulkImportTableComponent } from './bulk-import-table.component';

describe('BulkImportTableComponent', () => {
  let component: BulkImportTableComponent;
  let fixture: ComponentFixture<BulkImportTableComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [BulkImportTableComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(BulkImportTableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
