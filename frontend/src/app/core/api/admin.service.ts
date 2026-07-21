import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Cobranca, EstatisticasAdmin, MensagemServico, Servico, StatusServico, Usuario } from '../models/usuario.model';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/admin`;

  obterEstatisticas() {
    return this.http.get<EstatisticasAdmin>(`${this.base}/stats`);
  }

  listarUsuarios() {
    return this.http.get<{ users: any[] }>(`${this.base}/users`);
  }

  listarServicos() {
    return this.http.get<{ services: Servico[] }>(`${this.base}/services`);
  }

  atualizarStatusServico(id: string, status: StatusServico) {
    return this.http.patch<{ service: Servico }>(`${this.base}/services/${id}`, { status });
  }

  editarServico(id: string, titulo: string, preco: number) {
    return this.http.patch<{ service: Servico }>(`${this.base}/services/${id}/editar`, { titulo, preco });
  }

  excluirServico(id: string) {
    return this.http.delete<{ message: string }>(`${this.base}/services/${id}`);
  }

  editarUsuario(id: string, nome: string, telefone: string) {
    return this.http.patch<{ user: Usuario }>(`${this.base}/users/${id}`, { nome, telefone });
  }

  excluirUsuario(id: string) {
    return this.http.delete<{ message: string }>(`${this.base}/users/${id}`);
  }

  listarMensagens(servicoId: string) {
    return this.http.get<{ messages: MensagemServico[] }>(`${this.base}/services/${servicoId}/messages`);
  }

  enviarMensagem(servicoId: string, conteudo: string) {
    return this.http.post<{ message: MensagemServico }>(`${this.base}/services/${servicoId}/messages`, { content: conteudo });
  }

  listarCobranças() {
    return this.http.get<{ charges: Cobranca[] }>(`${this.base}/charges`);
  }
}
