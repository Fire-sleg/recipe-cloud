import { RouterModule, Routes } from "@angular/router";
import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { CategoryComponent } from "./category/category.component";
import { CategoryItemComponent } from "./category-item/category-item.component";
import { RecipeListComponent } from "../recipes/recipe-list/recipe-list.component";
import { RecipeItemComponent } from "../recipes/recipe-item/recipe-item.component";
import { SharedModule } from "../shared/shared.module";

const routes: Routes = [
  { path: '', component: CategoryComponent, 
    children: [
    {
      path: ':subcategory',
      component: CategoryItemComponent
    }
  ] }
  
];

@NgModule({
  declarations: [
    CategoryComponent,
    CategoryItemComponent
  ],
  imports: [
    FormsModule,
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    SharedModule,
    RouterModule.forChild(routes)
  ]
})
export class CategoryModule { }