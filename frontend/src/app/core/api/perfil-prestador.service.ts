import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  PerfilPublico,
  Categoria,
  Cidade,
  ImagemPortfolio,
  ComandoAtualizarPerfil,
  PrestadorBusca,
  ResultadoPaginado,
} from '../models/usuario.model';

@Injectable({ providedIn: 'root' })
export class PerfilPrestadorService {
  private readonly http = inject(HttpClient);
  private readonly baseAuth = `${environment.apiUrl}/api/auth`;
  private readonly baseApi = environment.apiUrl;

  /**
   * Obtém o perfil público de um prestador pelo slug.
   * Rota: GET /{cidadeSlug}/{categoriaSlug}/{slug}
   * Para V1 simplificada: GET /prestador-publico/{slug} via proxy ou slug direto.
   * O backend usa /{cidadeSlug}/{categoriaSlug}/{slug} mas pode ser chamado com valores dummy.
   */
  obterPerfilPublico(slug: string): Observable<PerfilPublico> {
    // Usa valores canônicos da URL pública. Em V1 o slug é suficiente para localizar o prestador.
    return this.http.get<PerfilPublico>(`${this.baseApi}/v/v/${slug}`);
  }

  listarCategorias(): Observable<Categoria[]> {
    return this.http.get<Categoria[]>(`${this.baseApi}/api/categorias`);
  }

  listarCidades(): Observable<Cidade[]> {
    return this.http.get<Cidade[]>(`${this.baseApi}/api/cidades`);
  }

  /**
   * Busca paginada de prestadores por categoria (obrigatório) e cidade (opcional).
   * Rota: GET /api/prestadores?categoriaSlug=&cidadeSlug=&page=&pageSize=
   * Pública — não requer autenticação (RN-01).
   */
  buscarPrestadores(
    categoriaSlug: string,
    cidadeSlug?: string,
    page = 1,
    pageSize = 20,
  ): Observable<ResultadoPaginado<PrestadorBusca>> {
    let params = new HttpParams()
      .set('categoriaSlug', categoriaSlug)
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (cidadeSlug) {
      params = params.set('cidadeSlug', cidadeSlug);
    }

    return this.http.get<ResultadoPaginado<PrestadorBusca>>(
      `${this.baseApi}/api/prestadores`,
      { params },
    );
  }

  /**
   * Atualiza o perfil público do prestador autenticado.
   * Requer token Bearer (injetado pelo authInterceptor).
   */
  atualizarPerfil(dados: ComandoAtualizarPerfil): Observable<{ perfil: PerfilPublico }> {
    return this.http.put<{ perfil: PerfilPublico }>(`${this.baseAuth}/perfil`, dados);
  }

  /**
   * Registra uma imagem no portfólio após upload direto ao Cloudinary (ADR-03).
   * @deprecated Usar uploadImagem() que faz upload local ao servidor.
   */
  adicionarImagem(
    url: string,
    cloudinaryPublicId: string,
    ordem = 0,
  ): Observable<{ imagem: ImagemPortfolio & { status: string } }> {
    return this.http.post<{ imagem: ImagemPortfolio & { status: string } }>(
      `${this.baseAuth}/portfolio`,
      { url, cloudinaryPublicId, ordem },
    );
  }

  /**
   * Faz upload de uma imagem ao portfólio via multipart/form-data.
   * Rota: POST /api/auth/portfolio/upload
   * Tipos aceitos: jpg, jpeg, png, webp — máx 5 MB.
   * NÃO definir Content-Type manualmente — o browser define o boundary.
   */
  uploadImagem(arquivo: File, ordem = 0): Observable<{ id: string; url: string }> {
    const formData = new FormData();
    formData.append('arquivo', arquivo);
    formData.append('ordem', ordem.toString());
    return this.http.post<{ id: string; url: string }>(
      `${this.baseAuth}/portfolio/upload`,
      formData,
    );
  }

  /**
   * Remove uma imagem do portfólio (soft delete).
   * Rota: DELETE /api/auth/portfolio/{id}
   */
  removerImagem(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseAuth}/portfolio/${id}`);
  }

  /**
   * Faz upload da foto de perfil (hospedada na própria Prontto).
   * Rota: POST /api/auth/perfil/foto/upload — retorna a URL relativa.
   */
  uploadFotoPerfil(arquivo: File): Observable<{ url: string }> {
    const formData = new FormData();
    formData.append('arquivo', arquivo);
    return this.http.post<{ url: string }>(`${this.baseAuth}/perfil/foto/upload`, formData);
  }

  /** Obtém o cadastro atual do usuário logado (nome, telefone, cidade, endereço). */
  obterMeuCadastro(): Observable<{ user: any }> {
    return this.http.get<{ user: any }>(`${this.baseAuth}/me`);
  }

  /** Atualiza o cadastro do próprio usuário (aba "Meu Perfil" do cliente). */
  atualizarMeuCadastro(dados: {
    nome?: string; telefone?: string; cidadeId?: string | null; endereco?: string;
  }): Observable<{ user: any }> {
    return this.http.put<{ user: any }>(`${this.baseAuth}/meu-cadastro`, dados);
  }
}
