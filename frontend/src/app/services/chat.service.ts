import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  timestamp: Date;
}

export interface ChatRequest {
  message: string;
}

export interface ChatResponse {
  response: string;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/chat`;

  readonly messages = signal<ChatMessage[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  sendMessage(message: string): Observable<ChatResponse> {
    this.loading.set(true);
    this.error.set(null);

    this.messages.update(msgs => [...msgs, {
      role: 'user',
      content: message,
      timestamp: new Date()
    }]);

    return this.http.post<ChatResponse>(this.apiUrl, { message } as ChatRequest).pipe(
      tap({
        next: (response) => {
          this.messages.update(msgs => [...msgs, {
            role: 'assistant',
            content: response.response,
            timestamp: new Date()
          }]);
          this.loading.set(false);
        },
        error: (err) => {
          this.error.set(err.message || 'Failed to send message');
          this.loading.set(false);
        }
      })
    );
  }

  clearMessages(): void {
    this.messages.set([]);
    this.error.set(null);
  }
}
