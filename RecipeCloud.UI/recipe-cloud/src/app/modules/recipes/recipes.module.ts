import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { RouterModule, Routes } from '@angular/router';
import { RecipeListComponent } from './recipe-list/recipe-list.component';
import { RecipeDetailComponent } from './recipe-detail/recipe-detail.component';
import { RecipeFormComponent } from './recipe-form/recipe-form.component';
//import { RecipeCardComponent } from './recipe-card/recipe-card.component';
import { SharedModule } from '../shared/shared.module';
import { AuthGuard } from '../../core/guards/auth.guard';

const routes: Routes = [
  { path: '', component: RecipeListComponent },
  { path: ':id', component: RecipeDetailComponent },
  { path: 'new', component: RecipeFormComponent, canActivate: [AuthGuard] },
  { path: 'edit/:id', component: RecipeFormComponent, canActivate: [AuthGuard] }
];

@NgModule({
  declarations: [
    RecipeListComponent,
    RecipeDetailComponent,
    RecipeFormComponent,
    //RecipeCardComponent
  ],
  imports: [
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
export class RecipesModule { }