import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Product, CreateProductRequest, UpdateProductRequest } from '../models/product.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/products`;

  readonly products = signal<Product[]>([]);
  readonly selectedProduct = signal<Product | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  getAll(): Observable<Product[]> {
    this.loading.set(true);
    this.error.set(null);
    return this.http.get<Product[]>(this.apiUrl).pipe(
      tap({
        next: (products) => {
          this.products.set(products);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err.message);
          this.loading.set(false);
        }
      })
    );
  }

  getById(id: number): Observable<Product> {
    this.loading.set(true);
    this.error.set(null);
    return this.http.get<Product>(`${this.apiUrl}/${id}`).pipe(
      tap({
        next: (product) => {
          this.selectedProduct.set(product);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err.message);
          this.loading.set(false);
        }
      })
    );
  }

  create(product: CreateProductRequest): Observable<Product> {
    this.loading.set(true);
    this.error.set(null);
    return this.http.post<Product>(this.apiUrl, product).pipe(
      tap({
        next: (newProduct) => {
          this.products.update(products => [...products, newProduct]);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err.message);
          this.loading.set(false);
        }
      })
    );
  }

  update(id: number, product: UpdateProductRequest): Observable<Product> {
    this.loading.set(true);
    this.error.set(null);
    return this.http.put<Product>(`${this.apiUrl}/${id}`, product).pipe(
      tap({
        next: (updatedProduct) => {
          this.products.update(products =>
            products.map(p => p.id === id ? updatedProduct : p)
          );
          this.selectedProduct.set(updatedProduct);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err.message);
          this.loading.set(false);
        }
      })
    );
  }

  delete(id: number): Observable<void> {
    this.loading.set(true);
    this.error.set(null);
    return this.http.delete<void>(`${this.apiUrl}/${id}`).pipe(
      tap({
        next: () => {
          this.products.update(products => products.filter(p => p.id !== id));
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err.message);
          this.loading.set(false);
        }
      })
    );
  }
}
