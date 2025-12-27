import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/products',
    pathMatch: 'full'
  },
  {
    path: 'products',
    children: [
      {
        path: '',
        loadComponent: () => import('./features/products/product-list/product-list.component')
          .then(m => m.ProductListComponent)
      },
      {
        path: 'new',
        loadComponent: () => import('./features/products/product-form/product-form.component')
          .then(m => m.ProductFormComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./features/products/product-detail/product-detail.component')
          .then(m => m.ProductDetailComponent)
      },
      {
        path: ':id/edit',
        loadComponent: () => import('./features/products/product-form/product-form.component')
          .then(m => m.ProductFormComponent)
      }
    ]
  },
  {
    path: 'orders',
    children: [
      {
        path: '',
        loadComponent: () => import('./features/orders/order-list/order-list.component')
          .then(m => m.OrderListComponent)
      },
      {
        path: 'new',
        loadComponent: () => import('./features/orders/order-form/order-form.component')
          .then(m => m.OrderFormComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./features/orders/order-detail/order-detail.component')
          .then(m => m.OrderDetailComponent)
      },
      {
        path: ':id/edit',
        loadComponent: () => import('./features/orders/order-form/order-form.component')
          .then(m => m.OrderFormComponent)
      }
    ]
  },
  {
    path: '**',
    redirectTo: '/products'
  }
];
