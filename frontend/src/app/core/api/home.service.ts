import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DadosHome } from '../models/usuario.model';

@Injectable({ providedIn: 'root' })
export class HomeService {
  private readonly http = inject(HttpClient);

  obterDadosHome(): Observable<DadosHome> {
    return this.http.get<DadosHome>(`${environment.apiUrl}/api/home`);
  }
}
