import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Category } from '../../../core/models/category.model';
import { CategoryService } from '../../../core/services/category.service';


@Component({
  selector: 'app-category',
  templateUrl: './category.component.html',
  styleUrls: ['./category.component.css']
})
export class CategoryComponent implements OnInit {
  showNotFound = false;
  categoryTransliteratedName: string | null = null;
  category: Category | null = null;

  constructor(private route: ActivatedRoute, private router: Router, private categoryService: CategoryService) { }

  ngOnInit(): void {

    setTimeout(() => {
      if (!this.category) {
        this.showNotFound = true;
      }
    }, 1000);

    
    this.route.paramMap.subscribe(params => {
      const transliteratedName = params.get('transliteratedName') || params.get('subTransliteratedName') || params.get('subSubTransliteratedName');
      if (transliteratedName && transliteratedName !== this.categoryTransliteratedName) {
        this.categoryTransliteratedName = transliteratedName;
        this.loadCategoryDetails(this.categoryTransliteratedName);
      }
    });
  }

  loadCategoryDetails(transliteratedName: string): void {
    this.categoryService.getCategoryByTransliteratedName(transliteratedName).subscribe(data => {
      debugger;
      this.category = data;
      if(!this.category?.subCategories?.length){
        this.router.navigate(['recipes', this.categoryTransliteratedName]);
      }
      // if(this.category?.recipes && this.category.recipes.length){
      //   this.router.navigate(['recipes', this.categoryTransliteratedName]);
      // }
    });
    
  }

  navigateToSubCategory(subCategoryId: string): void {
    this.router.navigate(['category', subCategoryId]);
    
  }

}
