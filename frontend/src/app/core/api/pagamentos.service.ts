import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface StatusConnect {
  possui: boolean;
  chargesEnabled: boolean;
  detailsSubmitted: boolean;
  payoutsEnabled: boolean;
}

@Injectable({ providedIn: 'root' })
export class PagamentosService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/pagamentos`;

  /** Cria/continua o onboarding do prestador no Stripe Connect e retorna o link. */
  criarOnboardingConnect(): Observable<{ url: string }> {
    return this.http.post<{ url: string }>(`${this.base}/connect/onboarding`, {});
  }

  statusConnect(): Observable<StatusConnect> {
    return this.http.get<StatusConnect>(`${this.base}/connect/status`);
  }
}
