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
import { UserProfileComponent } from './user-profile/user-profile.component';
import { SharedModule } from '../shared/shared.module';

const routes: Routes = [
  { path: '', component: UserProfileComponent }
];



@NgModule({
  declarations: [
    UserProfileComponent
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
    RouterModule.forChild(routes)
  ]
})
export class UserModule { }