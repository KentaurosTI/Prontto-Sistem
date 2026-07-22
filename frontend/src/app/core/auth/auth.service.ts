import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { tap, finalize, shareReplay } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Usuario } from '../models/usuario.model';

interface RespostaAuth {
  user: any;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly _usuario = signal<Usuario | null>(this.carregarUsuarioLocal());

  readonly usuario = this._usuario.asReadonly();
  readonly estaAutenticado = computed(() => this._usuario() !== null);
  readonly ehAdmin = computed(() => this._usuario()?.papel === 'admin');

  /** Refresh em andamento (compartilhado para evitar renovações simultâneas). */
  private renovacao$: Observable<RespostaAuth> | null = null;

  entrar(email: string, senha: string) {
    return this.http
      .post<RespostaAuth>(`${environment.apiUrl}/api/auth/login`, { email, senha }, { withCredentials: true })
      .pipe(tap(resposta => this.salvarSessao(resposta)));
  }

  cadastrar(dados: {
    nome: string; email: string; senha: string;
    tipoConta: string; telefone?: string; especialidade?: string; cidadeId?: string;
  }) {
    return this.http
      .post<RespostaAuth>(`${environment.apiUrl}/api/auth/register`, {
        nome: dados.nome,
        email: dados.email,
        senha: dados.senha,
        tipoConta: dados.tipoConta,
        telefone: dados.telefone,
        especialidade: dados.especialidade,
        cidadeId: dados.cidadeId,
      }, { withCredentials: true })
      .pipe(tap(resposta => this.salvarSessao(resposta)));
  }

  /** Renova o access token usando o cookie httpOnly de refresh. Compartilhado. */
  renovarSessao(): Observable<RespostaAuth> {
    if (!this.renovacao$) {
      this.renovacao$ = this.http
        .post<RespostaAuth>(`${environment.apiUrl}/api/auth/refresh`, {}, { withCredentials: true })
        .pipe(
          tap(resposta => this.salvarSessao(resposta)),
          finalize(() => (this.renovacao$ = null)),
          shareReplay(1),
        );
    }
    return this.renovacao$;
  }

  sair(): void {
    // Best-effort: encerra a sessão no backend (limpa os cookies de refresh e access).
    this.http
      .post(`${environment.apiUrl}/api/auth/logout`, {}, { withCredentials: true })
      .subscribe({ next: () => {}, error: () => {} });
    this.limparSessaoLocal();
    this.router.navigate(['/entrar']);
  }

  /** Limpa apenas o estado local (usado no logout e ao falhar o refresh). */
  limparSessaoLocal(): void {
    localStorage.removeItem('prontto_usuario');
    this._usuario.set(null);
  }

  /** Sessão realmente expirada (refresh falhou): limpa estado e vai para o login. */
  sessaoExpirada(): void {
    this.limparSessaoLocal();
    this.router.navigate(['/entrar']);
  }

  /** Mantido por compatibilidade — token agora trafega via cookie httpOnly. */
  obterToken(): string | null {
    return null;
  }

  /** Atualiza campos do usuário logado em memória e no localStorage (ex.: foto de perfil). */
  aplicarPatchUsuario(patch: Partial<Usuario>): void {
    const atual = this._usuario();
    if (!atual) return;
    const novo = { ...atual, ...patch };
    localStorage.setItem('prontto_usuario', JSON.stringify(novo));
    this._usuario.set(novo);
  }

  private salvarSessao(resposta: RespostaAuth): void {
    const usuario = this.mapearUsuario(resposta.user);
    localStorage.setItem('prontto_usuario', JSON.stringify(usuario));
    this._usuario.set(usuario);
  }

  private carregarUsuarioLocal(): Usuario | null {
    const dados = localStorage.getItem('prontto_usuario');
    return dados ? JSON.parse(dados) : null;
  }

  private mapearUsuario(dados: any): Usuario {
    return {
      id: dados.id,
      nome: dados.nome,
      email: dados.email,
      telefone: dados.telefone,
      tipoConta: dados.tipoConta,
      papel: dados.papel,
      especialidade: dados.especialidade,
      cidadeId: dados.cidadeId,
      endereco: dados.endereco,
      fotoPerfilUrl: dados.fotoPerfilUrl,
      slug: dados.slug,
      mediaAvaliacoes: dados.mediaAvaliacoes ?? 0,
      totalAvaliacoes: dados.totalAvaliacoes ?? 0,
      criadoEm: dados.criadoEm,
    };
  }
}
