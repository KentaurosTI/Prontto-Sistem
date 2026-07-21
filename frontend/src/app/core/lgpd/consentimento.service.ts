import { Injectable, signal } from '@angular/core';

export type EscolhaCookies = 'aceito' | 'essenciais';
const CHAVE = 'prontto_cookie_consent';

/**
 * Serviço de consentimento de cookies (LGPD — Lei 13.709/2018).
 * Guarda a escolha do usuário e permite reabrir as preferências.
 */
@Injectable({ providedIn: 'root' })
export class ConsentimentoService {
  /** Escolha atual (null = ainda não decidiu → mostrar banner). */
  readonly escolha = signal<EscolhaCookies | null>(this.carregar());

  private carregar(): EscolhaCookies | null {
    if (typeof localStorage === 'undefined') return null;
    const v = localStorage.getItem(CHAVE);
    return v === 'aceito' || v === 'essenciais' ? v : null;
  }

  registrar(escolha: EscolhaCookies): void {
    try {
      localStorage.setItem(CHAVE, escolha);
      localStorage.setItem(CHAVE + '_data', new Date().toISOString());
    } catch {
      /* localStorage indisponível — segue sem persistir */
    }
    this.escolha.set(escolha);
  }

  /** Reabre o banner para o usuário rever as preferências. */
  reabrir(): void {
    this.escolha.set(null);
  }

  aceitouTodos(): boolean {
    return this.escolha() === 'aceito';
  }
}
