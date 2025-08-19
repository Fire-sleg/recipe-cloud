import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../shared/shared.module';
import { AuthGuard } from '../../core/guards/auth.guard';
import { CollectionFormComponent } from './collection-form/collection-form.component';
import { CollectionSharedModule } from './collection-shared.module';

@NgModule({
  declarations: [
    
  ],
  imports: [
    FormsModule,
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatGridListModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatListModule,
    SharedModule,
    CollectionSharedModule
  ]
})
export class CollectionsModule { }