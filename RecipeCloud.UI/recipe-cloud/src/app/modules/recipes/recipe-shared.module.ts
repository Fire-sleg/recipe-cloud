import { NgModule } from "@angular/core";
import { RecipeItemComponent } from "./recipe-item/recipe-item.component";
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
import { RecipeFormComponent } from "./recipe-form/recipe-form.component";

@NgModule({
  declarations: [RecipeItemComponent, RecipeFormComponent],
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
],
  exports: [RecipeItemComponent, RecipeFormComponent]
})
export class RecipeSharedModule {}
