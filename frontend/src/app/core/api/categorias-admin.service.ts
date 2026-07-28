import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CategoriaAdmin {
  id: string;
  nome: string;
  slug: string;
  descricao?: string | null;
  imagem?: string | null;
  ativa: boolean;
  ordem: number;
}

export interface CriarCategoria {
  nome: string;
  descricao?: string | null;
  imagem?: string | null;
  ordem?: number | null;
}

export interface EditarCategoria {
  nome: string;
  descricao?: string | null;
  imagem?: string | null;
  ativa: boolean;
  ordem?: number | null;
}

@Injectable({ providedIn: 'root' })
export class CategoriasAdminService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/admin/categorias`;

  listar(): Observable<{ categorias: CategoriaAdmin[] }> {
    return this.http.get<{ categorias: CategoriaAdmin[] }>(this.base);
  }

  criar(dados: CriarCategoria): Observable<{ categoria: CategoriaAdmin }> {
    return this.http.post<{ categoria: CategoriaAdmin }>(this.base, dados);
  }

  editar(id: string, dados: EditarCategoria): Observable<{ categoria: CategoriaAdmin }> {
    return this.http.put<{ categoria: CategoriaAdmin }>(`${this.base}/${id}`, dados);
  }

  alternarAtiva(id: string): Observable<{ categoria: CategoriaAdmin }> {
    return this.http.patch<{ categoria: CategoriaAdmin }>(`${this.base}/${id}/alternar-ativa`, {});
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  uploadImagem(arquivo: File): Observable<{ url: string }> {
    const form = new FormData();
    form.append('arquivo', arquivo);
    return this.http.post<{ url: string }>(`${this.base}/imagem/upload`, form);
  }
}
