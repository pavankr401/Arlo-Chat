import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { guestGuard } from './guards/guest.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  {
    path: 'login',
    loadComponent: () => import('./components/login/login.component').then(m => m.LoginComponent),
    canActivate: [guestGuard]
  },
  {
    path: 'create-account',
    loadComponent: () => import('./components/create-account/create-account.component').then(m => m.CreateAccountComponent),
    canActivate: [guestGuard]
  },
  {
    path: 'home',
    loadComponent: () => import('./components/home/home.component').then(m => m.HomeComponent),
    canActivate: [authGuard]
  },
  {
    path: 'manage-friends',
    loadComponent: () => import('./components/manage-friends/manage-friends.component').then(m => m.ManageFriendsComponent),
    canActivate: [authGuard]
  },
  { path: '**', redirectTo: 'login' }
];
