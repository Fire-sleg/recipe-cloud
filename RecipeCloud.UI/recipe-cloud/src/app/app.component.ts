import { Component, OnInit } from '@angular/core';
import { CollectionService } from './core/services/collection.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {
  title = 'RecipeCloud';

  constructor(private collectionService: CollectionService) {}

  ngOnInit(): void {
    this.collectionService.successMessage$.subscribe(msg => {
      if (msg) {
        alert(msg); // краще замінити на toast/snackbar
        this.collectionService.successMessage$.next(null);
      }
    });

    this.collectionService.errorMessage$.subscribe(msg => {
      if (msg) {
        alert(msg);
        this.collectionService.errorMessage$.next(null);
      }
    });
  }
}
