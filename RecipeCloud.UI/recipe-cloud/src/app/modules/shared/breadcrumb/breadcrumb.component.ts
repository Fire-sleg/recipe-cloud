import { Component, Input, OnChanges, OnInit, SimpleChanges} from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { filter } from 'rxjs';
import { Recipe } from '../../../core/models/recipe.model';
import { Category } from '../../../core/models/category.model';
import { BreadcrumbItem } from '../../../core/models/breadcrumb.model';


@Component({
  selector: 'app-breadcrumb',
  templateUrl: './breadcrumb.component.html',
  styleUrl: './breadcrumb.component.css'
})

export class BreadcrumbComponent implements OnChanges{
  @Input() category: Category | null | undefined;
  @Input() recipe: Recipe | null | undefined;

  breadcrumbs: BreadcrumbItem[] = [];

  constructor(private router: Router) {}

  
  ngOnChanges(changes: SimpleChanges): void {
    if(this.category != null){
      if(this.category.breadcrumbPath != null){
        this.breadcrumbs = this.category.breadcrumbPath;
      }
    }
    else if(this.recipe != null){
      if(this.recipe.breadcrumbPath != null){
        this.breadcrumbs = this.recipe.breadcrumbPath;
      }
    }
  }


}
/*breadcrumbs: BreadcrumbItem[] = [];

  constructor(private router: Router, private activatedRoute: ActivatedRoute) {}

  ngOnInit() {
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe(() => {
      this.breadcrumbs = this.createBreadcrumbs(this.activatedRoute.root);
    });
  }

  private createBreadcrumbs(route: ActivatedRoute, url: string = '', breadcrumbs: BreadcrumbItem[] = []): BreadcrumbItem[] {
    const children: ActivatedRoute[] = route.children;

    if (children.length === 0) {
      return breadcrumbs;
    }

    for (const child of children) {
      const routeURL: string = child.snapshot.url.map(segment => segment.path).join('/');
      if (routeURL !== '') {
        url += `/${routeURL}`;
      }

      const label = child.snapshot.data['breadcrumb'] || this.getDefaultLabel(routeURL);
      if (label) {
        breadcrumbs.push({ label, url });
      }

      return this.createBreadcrumbs(child, url, breadcrumbs);
    }

    return breadcrumbs;
  }

  private getDefaultLabel(path: string): string {
    if (path.startsWith('category/')) {
      return 'Category';
    } else if (path.startsWith('product/')) {
      return 'Product';
    }
    return path;
  } */