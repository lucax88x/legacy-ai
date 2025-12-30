import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Order, CreateOrderRequest, UpdateOrderRequest, PagedResult } from '../models/order.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/orders`;

  readonly orders = signal<Order[]>([]);
  readonly selectedOrder = signal<Order | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly currentPage = signal(1);
  readonly pageSize = signal(10);
  readonly totalCount = signal(0);
  readonly totalPages = signal(0);
  readonly hasPreviousPage = signal(false);
  readonly hasNextPage = signal(false);

  getAll(page: number = 1, pageSize: number = 10): Observable<PagedResult<Order>> {
    this.loading.set(true);
    this.error.set(null);

    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResult<Order>>(this.apiUrl, { params }).pipe(
      tap({
        next: (result) => {
          this.orders.set(result.items);
          this.currentPage.set(result.page);
          this.pageSize.set(result.pageSize);
          this.totalCount.set(result.totalCount);
          this.totalPages.set(result.totalPages);
          this.hasPreviousPage.set(result.hasPreviousPage);
          this.hasNextPage.set(result.hasNextPage);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err.message);
          this.loading.set(false);
        }
      })
    );
  }

  getById(id: number): Observable<Order> {
    this.loading.set(true);
    this.error.set(null);
    return this.http.get<Order>(`${this.apiUrl}/${id}`).pipe(
      tap({
        next: (order) => {
          this.selectedOrder.set(order);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err.message);
          this.loading.set(false);
        }
      })
    );
  }

  create(order: CreateOrderRequest): Observable<Order> {
    this.loading.set(true);
    this.error.set(null);
    return this.http.post<Order>(this.apiUrl, order).pipe(
      tap({
        next: (newOrder) => {
          this.orders.update(orders => [...orders, newOrder]);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err.message);
          this.loading.set(false);
        }
      })
    );
  }

  update(id: number, order: UpdateOrderRequest): Observable<Order> {
    this.loading.set(true);
    this.error.set(null);
    return this.http.put<Order>(`${this.apiUrl}/${id}`, order).pipe(
      tap({
        next: (updatedOrder) => {
          this.orders.update(orders =>
            orders.map(o => o.id === id ? updatedOrder : o)
          );
          this.selectedOrder.set(updatedOrder);
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
          this.orders.update(orders => orders.filter(o => o.id !== id));
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
