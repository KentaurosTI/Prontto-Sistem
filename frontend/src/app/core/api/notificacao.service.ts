import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface Notificacao {
  id: string;
  titulo: string;
  mensagem: string;
  lida: boolean;
  tipo: string;
  referenciaId?: string | null;
  criadoEm: string;
}

interface RespostaLista {
  naoLidas: number;
  notificacoes: Notificacao[];
}

@Injectable({ providedIn: 'root' })
export class NotificacaoService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/notificacoes`;

  readonly notificacoes = signal<Notificacao[]>([]);
  readonly naoLidas = signal(0);

  /** Carrega a lista completa + contagem (ao abrir o dropdown). */
  carregar(): void {
    this.http.get<RespostaLista>(this.base).subscribe({
      next: (res) => {
        this.notificacoes.set(res.notificacoes);
        this.naoLidas.set(res.naoLidas);
      },
      error: () => {},
    });
  }

  /** Só a contagem — para o polling leve do badge. */
  atualizarContagem(): void {
    this.http.get<{ total: number }>(`${this.base}/nao-lidas`).subscribe({
      next: (res) => this.naoLidas.set(res.total),
      error: () => {},
    });
  }

  marcarLida(id: string): void {
    this.http.post(`${this.base}/${id}/lida`, {}).subscribe({
      next: () => {
        this.notificacoes.update((lista) =>
          lista.map((n) => (n.id === id ? { ...n, lida: true } : n)),
        );
        this.naoLidas.update((n) => Math.max(0, n - 1));
      },
      error: () => {},
    });
  }

  marcarTodasLidas(): void {
    this.http.post(`${this.base}/lidas`, {}).subscribe({
      next: () => {
        this.notificacoes.update((lista) => lista.map((n) => ({ ...n, lida: true })));
        this.naoLidas.set(0);
      },
      error: () => {},
    });
  }

  limpar(): void {
    this.notificacoes.set([]);
    this.naoLidas.set(0);
  }
}
