import { RouterModule, Routes } from "@angular/router";
import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { CategoryComponent } from "./category/category.component";
import { CategoryItemComponent } from "./category-item/category-item.component";

const routes: Routes = [
  { path: '', component: CategoryComponent }
];

@NgModule({
  declarations: [
    CategoryComponent,
    CategoryItemComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    RouterModule.forChild(routes)
  ]
})
export class CategoryModule { }