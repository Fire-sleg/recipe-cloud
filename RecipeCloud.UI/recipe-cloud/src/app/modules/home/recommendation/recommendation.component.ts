import { Component, OnInit } from '@angular/core';
import { Recipe } from '../../../core/models/recipe.model';
import { RecommendationService } from '../../../core/services/recommendation.service';

@Component({
  selector: 'app-recommendation',
  templateUrl: './recommendation.component.html',
  styleUrls: ['./recommendation.component.css']
})
export class RecommendationComponent implements OnInit {
[x: string]: any;
  
  recommendations: Recipe[] = [];
  showNotFound = false;
  
  currentIndex = 0;
  translateX = 0;
  isAnimating = false;
  
  readonly itemsVisible = 5; // кількість видимих елементів
  readonly itemsToScroll = 1; // кількість елементів для прокручування
  
  itemWidth = 0;
  maxIndex = 0;
  indicators: number[] = [];

  

  constructor(private recomService: RecommendationService) { }

  ngOnInit(): void {
    setTimeout(() => {
      if (!this.recommendations || !this.recommendations.length) {
        this.showNotFound = true;
      }
    }, 8000);

    this.recomService.getRecommendations(20).subscribe((recommendationRecipes: Recipe[])=>{
      this.recommendations = recommendationRecipes;
       this.calculateDimensions();
       this.updateIndicators();
    });
  }
  mathFoor(currentIndex: number, itemsToScroll: number): number{
    return Math.floor(currentIndex / itemsToScroll);
  }

  onImageError(event: Event): void {
    const img = event.target as HTMLImageElement;
    img.src = 'assets/temp.jpg';
  }

  ngAfterViewInit() {
    // Розрахунок ширини елементів після рендерингу
    setTimeout(() => {
      this.calculateDimensions();
    });
  }

  calculateDimensions() {
    // Припускаємо ширину елемента + gap
    this.itemWidth = 280; // налаштуйте згідно вашого дизайну
    this.maxIndex = Math.max(0, this.recommendations.length - this.itemsVisible);
  }

  updateIndicators() {
    const indicatorCount = Math.ceil((this.recommendations.length - this.itemsVisible + 1) / this.itemsToScroll) || 1;
    this.indicators = Array(indicatorCount).fill(0).map((_, i) => i);
  }

  scrollLeft() {
    if (this.currentIndex > 0) {
      this.currentIndex = Math.max(0, this.currentIndex - this.itemsToScroll);
      this.updateTransform();
    }
  }

  scrollRight() {
    if (this.currentIndex < this.maxIndex) {
      this.currentIndex = Math.min(this.maxIndex, this.currentIndex + this.itemsToScroll);
      this.updateTransform();
    }
  }

  goToSlide(indicatorIndex: number) {
    this.currentIndex = Math.min(indicatorIndex * this.itemsToScroll, this.maxIndex);
    this.updateTransform();
  }

  private updateTransform() {
    this.isAnimating = true;
    this.translateX = -this.currentIndex * this.itemWidth;
    
    // Вимкнути анімацію після завершення
    setTimeout(() => {
      this.isAnimating = false;
    }, 300);
  }

  trackByRecipeId(index: number, recipe: Recipe): string {
    return recipe.id;
  }
}