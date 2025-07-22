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
import { RecipeListComponent } from './recipe-list/recipe-list.component';
import { SharedModule } from '../shared/shared.module';
import { AuthGuard } from '../../core/guards/auth.guard';
import { RecipeItemComponent } from './recipe-item/recipe-item.component';
import { RecipeFilterComponent } from './recipe-filter/recipe-filter.component';
import { RecipeDetailComponent } from './recipe-detail/recipe-detail.component';
import { BreadcrumbComponent } from '../shared/breadcrumb/breadcrumb.component';
import { RecipeSharedModule } from './recipe-shared.module';
import { RecipeFormComponent } from './recipe-form/recipe-form.component';

const routes: Routes = [
  { path: ':transliteratedName', component: RecipeFilterComponent},
  { path: 'detail/:transliteratedName', component: RecipeDetailComponent }
];



@NgModule({
  declarations: [
    RecipeFilterComponent,
    RecipeListComponent,
    RecipeListComponent,
    RecipeDetailComponent
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
    RecipeSharedModule,
    RouterModule.forChild(routes),

  ]
})
export class RecipesModule { }