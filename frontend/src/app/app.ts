import { Component, inject, signal, computed, HostListener } from '@angular/core';
import { RouterOutlet, RouterLink, Router, NavigationEnd } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs/operators';
import { AuthService } from './core/auth/auth.service';
import { CATEGORIAS_MENU, rotaCategoria, mesclarCategorias, CategoriaMenu } from './core/data/menu-categorias';
import { CatalogoService } from './core/api/catalogo.service';
import { CookieConsentComponent } from './core/lgpd/cookie-consent.component';
import { NotificacoesComponent } from './shared/notificacoes/notificacoes.component';

/** Prefixos de rotas "logadas"/app onde a catbar de marketing e o rodapé não aparecem. */
const ROTAS_SEM_MARKETING = ['/entrar', '/cadastrar', '/minha-area', '/admin'];

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, FormsModule, CookieConsentComponent, NotificacoesComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly catalogo = inject(CatalogoService);

  /** Menu de categorias: começa com o estático e é substituído pelo catálogo do banco. */
  readonly categorias = signal<CategoriaMenu[]>(CATEGORIAS_MENU);

  constructor() {
    // Menu do site vem do banco (categorias cadastradas no admin). Fallback: estático.
    this.catalogo.listarCategorias().subscribe({
      next: (db) => { if (db?.length) this.categorias.set(mesclarCategorias(db)); },
      error: () => { /* mantém o menu estático */ },
    });
  }

  readonly menuAberto = signal(false);
  readonly scrollado = signal(false);
  /** Termo da barra de busca do header (contratante). */
  readonly termoBusca = signal('');

  /** True quando o usuário logado é contratante (cliente). */
  readonly ehCliente = computed(() => this.auth.usuario()?.tipoConta === 'cliente');

  /** Submete a busca do header → página de busca de profissionais. */
  buscarNoHeader(): void {
    const q = this.termoBusca().trim();
    this.fecharMenu();
    this.router.navigate(['/buscar'], q ? { queryParams: { q } } : {});
    this.termoBusca.set('');
  }
  /** Índice da categoria com mega-menu aberto, ou null. */
  readonly megaIdx = signal<number | null>(null);
  private timerMega: ReturnType<typeof setTimeout> | null = null;

  private readonly urlAtual = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(e => e.urlAfterRedirects),
      startWith(this.router.url),
    ),
    { initialValue: this.router.url },
  );

  /** Só mostra catbar + rodapé de marketing nas páginas públicas. */
  readonly rotaMarketing = computed(() => {
    const url = this.urlAtual().split('?')[0];
    // Telas logadas/app: criar serviço e detalhe do serviço não têm catbar/rodapé.
    // (as páginas de categoria /servicos/:key são públicas e MOSTRAM a catbar)
    if (url === '/servicos/novo') return false;
    if (/^\/servico\/[^/]+$/.test(url)) return false;
    return !ROTAS_SEM_MARKETING.some(p => url === p || url.startsWith(p + '/'));
  });

  readonly megaAberto = computed(() => this.megaIdx() !== null);

  rota(key: string, item?: string): string[] {
    return rotaCategoria(key, item);
  }

  abrirMega(idx: number): void {
    // No touch/mobile o browser sintetiza `mouseenter` antes do `click`. Se abríssemos
    // o mega aqui, o onClickCatbarItem veria megaIdx já == idx e fecharia na sequência.
    // Por isso, abaixo de 920px a abertura é feita SÓ pelo clique (SCRUM-6 re-fix / PR #14).
    if (typeof window !== 'undefined' && window.innerWidth < 920) return;
    // Categorias novas (do banco, sem submenu) não abrem mega — são link direto.
    if (!this.categorias()[idx]?.grupos?.length) { this.megaIdx.set(null); return; }
    if (this.timerMega) clearTimeout(this.timerMega);
    this.megaIdx.set(idx);
  }

  fecharMega(): void {
    if (this.timerMega) clearTimeout(this.timerMega);
    this.timerMega = setTimeout(() => this.megaIdx.set(null), 140);
  }

  /** Fecha o mega-menu imediatamente (ao navegar por um item — essencial no mobile/touch). */
  fecharMegaImediato(): void {
    if (this.timerMega) clearTimeout(this.timerMega);
    this.megaIdx.set(null);
    this.menuAberto.set(false);
  }

  /**
   * Click em item da catbar.
   * Mobile (< 920px): primeiro toque abre o mega-menu sem navegar; segundo toque navega e fecha.
   * Desktop: fecha o mega imediatamente e deixa o routerLink navegar normalmente.
   */
  onClickCatbarItem(i: number, event: MouseEvent): void {
    if (window.innerWidth < 920 && this.megaIdx() !== i) {
      event.preventDefault();
      if (this.timerMega) clearTimeout(this.timerMega);
      this.megaIdx.set(i);
    } else {
      this.fecharMegaImediato();
    }
  }

  manterMega(): void {
    if (this.timerMega) clearTimeout(this.timerMega);
  }

  alternarMenu(): void {
    this.menuAberto.update(v => !v);
  }

  fecharMenu(): void {
    this.menuAberto.set(false);
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    if (this.megaIdx() !== null) this.megaIdx.set(null);
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    this.menuAberto.set(false);
    this.megaIdx.set(null);
  }

  @HostListener('window:scroll')
  onScroll(): void {
    this.scrollado.set(window.scrollY > 20);
  }
}
