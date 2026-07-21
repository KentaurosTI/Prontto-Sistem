import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { PerfilPrestadorService } from '../../core/api/perfil-prestador.service';
import { PrestadorBusca, Categoria, Cidade } from '../../core/models/usuario.model';
import { SeoService } from '../../core/seo/seo.service';
import { AuthService } from '../../core/auth/auth.service';
import { resolverUrlImagem } from '../../core/util/url-imagem';

@Component({
  selector: 'app-prestadores',
  standalone: true,
  imports: [RouterLink, DecimalPipe],
  templateUrl: './prestadores.component.html',
  styleUrl: './prestadores.component.scss',
})
export class PrestadoresComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly perfilService = inject(PerfilPrestadorService);
  private readonly seo = inject(SeoService);
  private readonly auth = inject(AuthService);

  readonly prestadores = signal<PrestadorBusca[]>([]);
  readonly cidades = signal<Cidade[]>([]);
  readonly carregando = signal(true);
  readonly categoriaSlug = signal('');
  readonly categoriaNome = signal('');
  readonly cidadeSlug = signal<string>('');

  urlImagem(url: string | null | undefined): string {
    return resolverUrlImagem(url);
  }

  ngOnInit(): void {
    this.categoriaSlug.set(this.route.snapshot.paramMap.get('categoriaSlug') ?? '');
    this.cidadeSlug.set(this.route.snapshot.queryParamMap.get('cidade') ?? '');

    // Nome amigável da categoria + lista de cidades (filtro)
    this.perfilService.listarCategorias().subscribe({
      next: (cats: Categoria[]) => {
        const cat = cats.find((c) => c.slug === this.categoriaSlug());
        this.categoriaNome.set(cat?.nome ?? this.categoriaSlug());
        this.seo.atualizarSeo({
          titulo: `Profissionais de ${this.categoriaNome()} — Prontto`,
          descricao: `Escolha um profissional de ${this.categoriaNome()} e envie sua solicitação de serviço.`,
          url: `https://prontto.org/prestadores/${this.categoriaSlug()}`,
        });
      },
    });
    this.perfilService.listarCidades().subscribe({
      next: (cids) => {
        this.cidades.set(cids);
        // Proximidade: se não veio cidade na URL, usa a cidade do contratante logado.
        if (!this.cidadeSlug()) {
          const cidadeIdUsuario = this.auth.usuario()?.cidadeId;
          const cidade = cidadeIdUsuario ? cids.find((c) => c.id === cidadeIdUsuario) : null;
          if (cidade) {
            this.cidadeSlug.set(cidade.slug);
            this.buscar();
          }
        }
      },
    });

    this.buscar();
  }

  buscar(): void {
    this.carregando.set(true);
    this.perfilService
      .buscarPrestadores(this.categoriaSlug(), this.cidadeSlug() || undefined, 1, 50)
      .subscribe({
        next: (res) => {
          this.prestadores.set(res.items);
          this.carregando.set(false);
        },
        error: () => {
          this.prestadores.set([]);
          this.carregando.set(false);
        },
      });
  }

  aoTrocarCidade(valor: string): void {
    this.cidadeSlug.set(valor);
    // Reflete o filtro na URL (compartilhável) sem recarregar o componente.
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { cidade: valor || null },
      queryParamsHandling: 'merge',
    });
    this.buscar();
  }

  estrelas(media: number): ('cheia' | 'vazia')[] {
    return Array.from({ length: 5 }, (_, i) => (i < Math.round(media) ? 'cheia' : 'vazia'));
  }
}
