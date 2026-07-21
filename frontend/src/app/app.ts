import { Component, inject, signal, computed, HostListener } from '@angular/core';
import { RouterOutlet, RouterLink, Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs/operators';
import { AuthService } from './core/auth/auth.service';
import { CATEGORIAS_MENU, rotaCategoria } from './core/data/menu-categorias';
import { CookieConsentComponent } from './core/lgpd/cookie-consent.component';
import { NotificacoesComponent } from './shared/notificacoes/notificacoes.component';

/** Prefixos de rotas "logadas"/app onde a catbar de marketing e o rodapé não aparecem. */
const ROTAS_SEM_MARKETING = ['/entrar', '/cadastrar', '/minha-area', '/admin'];

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, CookieConsentComponent, NotificacoesComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly categorias = CATEGORIAS_MENU;

  readonly menuAberto = signal(false);
  readonly scrollado = signal(false);
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

  manterMega(): void {
    if (this.timerMega) clearTimeout(this.timerMega);
  }

  alternarMenu(): void {
    this.menuAberto.update(v => !v);
  }

  fecharMenu(): void {
    this.menuAberto.set(false);
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
