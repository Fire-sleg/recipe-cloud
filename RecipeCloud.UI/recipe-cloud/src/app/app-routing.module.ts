import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './modules/home/home-part/home.component';

const routes: Routes = [
  { path: '', loadChildren: () => import('./modules/home/home.module').then(m => m.HomeModule) },
  { path: 'category/:transliteratedName', loadChildren: () => import('./modules/categories/category.module').then(m => m.CategoryModule) },
  { path: 'auth', loadChildren: () => import('./modules/auth/auth.module').then(m => m.AuthModule) },
  { path: 'recipes', loadChildren: () => import('./modules/recipes/recipes.module').then(m => m.RecipesModule) },
  // { path: 'category/:transliteratedName', component: CategoryComponent },
  /*
  ,
  { path: 'auth', loadChildren: () => import('./modules/auth/auth.module').then(m => m.AuthModule) },
  { path: 'recipes', loadChildren: () => import('./modules/recipes/recipes.module').then(m => m.RecipesModule) },
  { path: 'profile', loadChildren: () => import('./modules/profile/profile.module').then(m => m.ProfileModule) },
  { path: 'admin', loadChildren: () => import('./modules/admin/admin.module').then(m => m.AdminModule) },
  { path: 'subscriptions-feed', loadChildren: () => import('./modules/subscriptions-feed/subscriptions-feed.module').then(m => m.SubscriptionsFeedModule) },
  { path: 'notifications', loadChildren: () => import('./modules/notifications/notifications.module').then(m => m.NotificationsModule) }

  */
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }