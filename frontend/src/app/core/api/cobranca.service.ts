import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Cobranca, DtoInicioCartao } from '../models/usuario.model';

@Injectable({ providedIn: 'root' })
export class CobrancaService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/servicos`;

  obterCobranca(servicoId: string) {
    return this.http.get<{ cobranca: Cobranca }>(`${this.base}/${servicoId}/cobranca`);
  }

  iniciarPagamentoCartao(servicoId: string, parcelas: number) {
    return this.http.post<DtoInicioCartao>(
      `${this.base}/${servicoId}/pagamento-cartao`,
      { parcelas }
    );
  }
}
