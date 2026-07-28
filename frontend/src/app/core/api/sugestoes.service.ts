import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Sugestao {
  id: string;
  email: string;
  telefone: string;
  titulo: string;
  descricao: string;
  atendida: boolean;
  criadoEm: string;
}

export interface EnvioSugestao {
  email: string;
  telefone?: string;
  titulo: string;
  descricao?: string;
}

@Injectable({ providedIn: 'root' })
export class SugestoesService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api`;

  enviar(dados: EnvioSugestao): Observable<{ ok: boolean }> {
    return this.http.post<{ ok: boolean }>(`${this.base}/sugestoes`, dados);
  }

  listarAdmin(): Observable<{ sugestoes: Sugestao[] }> {
    return this.http.get<{ sugestoes: Sugestao[] }>(`${this.base}/admin/sugestoes`);
  }
}
