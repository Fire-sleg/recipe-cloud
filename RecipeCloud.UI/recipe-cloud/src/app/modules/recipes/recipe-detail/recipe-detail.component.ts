import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { map, Observable } from 'rxjs';
import { Recipe } from '../../../core/models/recipe.model';
import { RecipeService } from '../../../core/services/recipe.service';
import { User } from '../../../core/models/user.model';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-recipe-detail',
  templateUrl: './recipe-detail.component.html',
  styleUrls: ['./recipe-detail.component.css']
})
export class RecipeDetailComponent implements OnInit {
  recipe$!: Observable<Recipe>;
  isAuthenticated$: Observable<boolean> | undefined;
  currentUser$: Observable<User | null> | undefined;

  constructor(
    private authService: AuthService,
    private route: ActivatedRoute,
    private recipeService: RecipeService
  ) {}

  ngOnInit(): void {
    this.isAuthenticated$ = this.authService.currentUser$.pipe(map(user => !!user));
    this.currentUser$ = this.authService.currentUser$;
    const id = this.route.snapshot.paramMap.get('id')!;
    this.recipe$ = this.recipeService.getRecipe(id);
  }
}