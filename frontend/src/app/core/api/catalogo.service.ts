import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CategoriaBanco } from '../data/menu-categorias';

/** Catálogo público de categorias (menu do site vindo do banco). */
@Injectable({ providedIn: 'root' })
export class CatalogoService {
  private readonly http = inject(HttpClient);

  listarCategorias(): Observable<CategoriaBanco[]> {
    return this.http.get<CategoriaBanco[]>(`${environment.apiUrl}/api/categorias`);
  }
}
