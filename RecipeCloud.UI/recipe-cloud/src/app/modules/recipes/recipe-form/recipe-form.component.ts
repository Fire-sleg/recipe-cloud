import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormArray } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { RecipeService } from '../../../core/services/recipe.service';
import { Recipe } from '../../../core/models/recipe.model';

@Component({
  selector: 'app-recipe-form',
  templateUrl: './recipe-form.component.html',
  styleUrls: ['./recipe-form.component.css']
})
export class RecipeFormComponent implements OnInit {
  recipeForm: FormGroup;
  isEditMode = false;
  recipeId: string | null = null;

  constructor(
    private fb: FormBuilder,
    private recipeService: RecipeService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.recipeForm = this.fb.group({
      title: ['', [Validators.required, Validators.minLength(3)]],
      imageUrl: [''],
      ingredients: this.fb.array([this.fb.control('', Validators.required)]),
      instructions: ['', [Validators.required, Validators.minLength(10)]]
    });
  }

  ngOnInit(): void {
    this.recipeId = this.route.snapshot.paramMap.get('id');
    if (this.recipeId) {
      this.isEditMode = true;
      this.recipeService.getRecipe(this.recipeId).subscribe(recipe => {
        this.recipeForm.patchValue({
          title: recipe.title,
          imageUrl: recipe.imageUrl,
          instructions: recipe.instructions
        });
        this.ingredients.clear();
        recipe.ingredients.forEach(ingredient => {
          this.ingredients.push(this.fb.control(ingredient, Validators.required));
        });
      });
    }
  }

  get ingredients(): FormArray {
    return this.recipeForm.get('ingredients') as FormArray;
  }

  addIngredient(): void {
    this.ingredients.push(this.fb.control('', Validators.required));
  }

  removeIngredient(index: number): void {
    this.ingredients.removeAt(index);
  }

  onSubmit(): void {
    if (this.recipeForm.valid) {
      const recipe: Recipe = this.recipeForm.value;
      let request: Observable<Recipe>;
      if (this.isEditMode && this.recipeId) {
        recipe.id = this.recipeId;
        request = this.recipeService.updateRecipe(recipe);
      } else {
        request = this.recipeService.createRecipe(recipe);
      }
      request.subscribe({
        next: () => {
          this.router.navigate(['/recipes']);
        },
        error: (err) => {
          console.error('Error saving recipe:', err);
        }
      });
    }
  }
    
}