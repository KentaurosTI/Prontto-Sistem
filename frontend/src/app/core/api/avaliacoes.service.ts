import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Avaliacao, ResultadoListaAvaliacoes } from '../models/usuario.model';

@Injectable({ providedIn: 'root' })
export class AvaliacoesService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api`;

  registrar(
    servicoId: string,
    nota: number,
    comentario?: string
  ): Observable<Avaliacao> {
    const corpo: { nota: number; comentario?: string } = { nota };
    if (comentario?.trim()) {
      corpo.comentario = comentario.trim();
    }
    return this.http.post<Avaliacao>(
      `${this.base}/servicos/${servicoId}/avaliacoes`,
      corpo
    );
  }

  listarPorPrestador(
    slug: string,
    page = 1,
    pageSize = 10
  ): Observable<ResultadoListaAvaliacoes> {
    return this.http.get<ResultadoListaAvaliacoes>(
      `${this.base}/prestadores/${slug}/avaliacoes`,
      { params: { page: page.toString(), pageSize: pageSize.toString() } }
    );
  }

  listarPorServico(servicoId: string): Observable<Avaliacao[]> {
    return this.http.get<Avaliacao[]>(
      `${this.base}/servicos/${servicoId}/avaliacoes`
    );
  }
}
