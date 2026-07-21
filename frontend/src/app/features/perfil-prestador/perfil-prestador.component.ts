import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { PerfilPrestadorService } from '../../core/api/perfil-prestador.service';
import { AvaliacoesService } from '../../core/api/avaliacoes.service';
import { AuthService } from '../../core/auth/auth.service';
import { Avaliacao, PerfilPublico } from '../../core/models/usuario.model';
import { SeoService } from '../../core/seo/seo.service';
import { resolverUrlImagem } from '../../core/util/url-imagem';

@Component({
  selector: 'app-perfil-prestador',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './perfil-prestador.component.html',
  styleUrl: './perfil-prestador.component.scss',
})
export class PerfilPrestadorComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly perfilService = inject(PerfilPrestadorService);
  private readonly avaliacoesService = inject(AvaliacoesService);
  private readonly seoService = inject(SeoService);
  readonly auth = inject(AuthService);

  readonly perfil = signal<PerfilPublico | null>(null);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);

  // ── Avaliações (RF-08) ───────────────────────────────────────────────────────
  readonly avaliacoes = signal<Avaliacao[]>([]);
  readonly carregandoAvaliacoes = signal(false);
  readonly paginaAvaliacoes = signal(1);
  readonly totalAvaliacoes = signal(0);
  readonly totalPaginasAvaliacoes = signal(0);

  readonly temMaisAvaliacoes = computed(
    () => this.paginaAvaliacoes() < this.totalPaginasAvaliacoes()
  );

  private slugAtual = '';

  /** Resolve a URL da imagem para exibição (uploads locais vêm da API). */
  urlImagem(url: string | null | undefined): string {
    return resolverUrlImagem(url);
  }

  ngOnInit(): void {
    const slug = this.route.snapshot.paramMap.get('slug');
    if (!slug) {
      this.router.navigate(['/not-found']);
      return;
    }

    this.slugAtual = slug;

    this.perfilService.obterPerfilPublico(slug).subscribe({
      next: (dados) => {
        this.perfil.set(dados);
        this.carregando.set(false);
        const cidadeNome = dados.cidades?.[0]?.nome ?? 'sua cidade';
        this.seoService.atualizarSeo({
          titulo: `${dados.nome} — Prestador`,
          descricao: `${dados.especialidade ?? 'Prestador de serviços'} em ${cidadeNome}. Confira avaliações e solicite um serviço.`,
          url: `https://prontto.org/prestador/${slug}`,
        });
        this.carregarAvaliacoes();
      },
      error: (err) => {
        this.carregando.set(false);
        if (err.status === 404) {
          this.erro.set('Prestador não encontrado.');
        } else {
          this.erro.set('Ocorreu um erro ao carregar o perfil. Tente novamente.');
        }
      },
    });
  }

  carregarAvaliacoes(): void {
    if (this.carregandoAvaliacoes()) return;
    this.carregandoAvaliacoes.set(true);

    this.avaliacoesService
      .listarPorPrestador(this.slugAtual, this.paginaAvaliacoes(), 10)
      .subscribe({
        next: (res) => {
          if (this.paginaAvaliacoes() === 1) {
            this.avaliacoes.set(res.items);
          } else {
            this.avaliacoes.update((atual) => [...atual, ...res.items]);
          }
          this.totalAvaliacoes.set(res.total);
          this.totalPaginasAvaliacoes.set(res.totalPaginas);
          this.carregandoAvaliacoes.set(false);
        },
        error: () => {
          this.carregandoAvaliacoes.set(false);
        },
      });
  }

  verMaisAvaliacoes(): void {
    this.paginaAvaliacoes.update((p) => p + 1);
    this.carregarAvaliacoes();
  }

  readonly mensagemContratacao = signal<string | null>(null);

  /** True quando o usuário logado é prestador (não pode contratar). */
  readonly ehPrestadorLogado = computed(
    () => this.auth.usuario()?.tipoConta === 'prestador'
  );

  contratar(): void {
    const usuario = this.auth.usuario();

    if (!usuario) {
      const returnUrl = this.router.url;
      this.router.navigate(['/entrar'], { queryParams: { returnUrl } });
      return;
    }

    if (usuario.tipoConta === 'prestador') {
      this.mensagemContratacao.set('Prestadores não podem contratar serviços.');
      setTimeout(() => this.mensagemContratacao.set(null), 4000);
      return;
    }

    // Cliente — navega para criação de serviço com prestador pré-selecionado
    const p = this.perfil();
    if (!p) return;

    this.router.navigate(['/servicos/novo'], {
      queryParams: {
        // Precisa ser o ID (GUID) do prestador — o backend não aceita o slug.
        prestadorId: p.id,
        prestadorNome: encodeURIComponent(p.nome),
      },
    });
  }

  get estrelas(): number[] {
    return Array.from({ length: 5 }, (_, i) => i + 1);
  }

  estralaAtiva(index: number): boolean {
    return index <= Math.round(this.perfil()?.mediaAvaliacoes ?? 0);
  }

  estrelasAvaliacao(nota: number): number[] {
    return Array.from({ length: 5 }, (_, i) => i + 1);
  }

  estrelaAvaliacaoAtiva(index: number, nota: number): boolean {
    return index <= nota;
  }

  formatarData(dataIso: string): string {
    const d = new Date(dataIso);
    return d.toLocaleDateString('pt-BR', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    });
  }
}
