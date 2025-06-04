import { Component, Input, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Category } from '../../../core/models/category.model';

@Component({
  selector: 'app-category-item',
  templateUrl: './category-item.component.html',
  styleUrls: ['./category-item.component.css']
})
export class CategoryItemComponent {
  @Input() category: Category | undefined;

  constructor(private router: Router, private route: ActivatedRoute,) { }

  navigateToSubCategory(subCategoryTransliteratedName: string): void {
      const newPath = `category/${subCategoryTransliteratedName}`;
      this.router.navigateByUrl(newPath);

      // this.router.navigate([subCategoryTransliteratedName], { relativeTo: this.route });

    }
  
}
