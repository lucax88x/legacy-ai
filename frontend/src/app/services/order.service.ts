import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { Order, CreateOrderRequest, UpdateOrderRequest } from '../models/order.model';
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

  getAll(): Observable<Order[]> {
    this.loading.set(true);
    this.error.set(null);
    return this.http.get<Order[]>(this.apiUrl).pipe(
      tap({
        next: (orders) => {
          this.orders.set(orders);
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
