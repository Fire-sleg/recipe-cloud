import { Component, Input, OnInit } from '@angular/core';
import { Recipe } from '../../../core/models/recipe.model';


@Component({
  selector: 'app-recipe-item',
  templateUrl: './recipe-item.component.html',
  styleUrls: ['./recipe-item.component.css']
})
export class RecipeItemComponent   {
  @Input() recipe: Recipe | undefined;
  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = 'assets/temp.jpg';
  }
}


