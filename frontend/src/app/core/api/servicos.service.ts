import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Servico, MensagemServico, Disputa, ResultadoMensagens } from '../models/usuario.model';
import { environment } from '../../../environments/environment';

export interface ComandoCriarServico {
  titulo: string;
  descricao?: string | null;
  categoriaId: string;
  cidadeId?: string | null;
  endereco?: string | null;
  agendadoEm?: string | null;
  prestadorId?: string | null;
}

@Injectable({ providedIn: 'root' })
export class ServicosService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/servicos`;

  criarSolicitacao(dados: ComandoCriarServico): Observable<{ servico: Servico }> {
    return this.http.post<{ servico: Servico }>(this.baseUrl, dados);
  }

  listarMeusServicos(): Observable<{ servicos: Servico[] }> {
    return this.http.get<{ servicos: Servico[] }>(this.baseUrl);
  }

  listarDisponiveis(): Observable<{ servicos: Servico[] }> {
    return this.http.get<{ servicos: Servico[] }>(`${this.baseUrl}/disponiveis`);
  }

  obterServico(id: string): Observable<{ servico: Servico }> {
    return this.http.get<{ servico: Servico }>(`${this.baseUrl}/${id}`);
  }

  listarMensagens(id: string, afterId?: string, limite = 50): Observable<ResultadoMensagens> {
    let params = `limite=${limite}`;
    if (afterId) params += `&afterId=${afterId}`;
    return this.http.get<ResultadoMensagens>(`${this.baseUrl}/${id}/mensagens?${params}`);
  }

  vincularPrestador(id: string): Observable<{ servico: Servico }> {
    return this.http.post<{ servico: Servico }>(`${this.baseUrl}/${id}/vincular`, {});
  }

  enviarMensagem(id: string, conteudo: string): Observable<{ mensagem: MensagemServico }> {
    return this.http.post<{ mensagem: MensagemServico }>(`${this.baseUrl}/${id}/mensagem`, { conteudo });
  }

  enviarProposta(id: string, valor: number): Observable<{ mensagem: MensagemServico }> {
    return this.http.post<{ mensagem: MensagemServico }>(`${this.baseUrl}/${id}/proposta`, { valor });
  }

  aceitarProposta(id: string, mensagemId: string): Observable<{ servico: Servico }> {
    return this.http.patch<{ servico: Servico }>(
      `${this.baseUrl}/${id}/proposta/${mensagemId}/aceitar`,
      {}
    );
  }

  marcarConcluido(id: string): Observable<{ servico: Servico }> {
    return this.http.patch<{ servico: Servico }>(`${this.baseUrl}/${id}/concluir`, {});
  }

  confirmarConclusao(id: string): Observable<{ servico: Servico }> {
    return this.http.patch<{ servico: Servico }>(`${this.baseUrl}/${id}/confirmar`, {});
  }

  cancelar(id: string, motivo?: string): Observable<{ servico: Servico }> {
    return this.http.patch<{ servico: Servico }>(`${this.baseUrl}/${id}/cancelar`, { motivo });
  }

  abrirDisputa(
    id: string,
    motivo: string,
    descricao?: string
  ): Observable<{ disputa: Disputa }> {
    return this.http.post<{ disputa: Disputa }>(`${this.baseUrl}/${id}/disputa`, { motivo, descricao });
  }
}
