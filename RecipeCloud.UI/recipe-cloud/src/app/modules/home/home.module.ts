import { RouterModule, Routes } from "@angular/router";
import { HomeComponent } from "./home-part/home.component";
import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { ReactiveFormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { BannerCarouselComponent } from "./banner-carousel/banner-carousel.component";
import { RecommendationComponent } from "./recommendation/recommendation.component";
import { RecipeItemComponent } from "../recipes/recipe-item/recipe-item.component";
import { RecipesModule } from "../recipes/recipes.module";
import { RecipeSharedModule } from "../recipes/recipe-shared.module";

const routes: Routes = [
  { path: '', component: HomeComponent }
];

@NgModule({
  declarations: [
    HomeComponent,
    BannerCarouselComponent,
    RecommendationComponent
  ],
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    RecipeSharedModule,
    RouterModule.forChild(routes)
  ]
})
export class HomeModule { }