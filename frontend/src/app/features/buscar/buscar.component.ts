import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { SeoService } from '../../core/seo/seo.service';
import { sugestoesBusca, buscarServico, CATEGORIAS_MENU } from '../../core/data/menu-categorias';
import { SugestoesService } from '../../core/api/sugestoes.service';

@Component({
  selector: 'app-buscar',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './buscar.component.html',
  styleUrl: './buscar.component.scss',
})
export class BuscarComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly seo = inject(SeoService);
  private readonly sugestoesService = inject(SugestoesService);

  readonly termo = signal('');
  readonly focado = signal(false);
  readonly semResultado = signal(false);

  // ── Popup "sugerir serviço" ──────────────────────────────────────────────
  readonly mostrarSugestao = signal(false);
  readonly enviandoSugestao = signal(false);
  readonly sugestaoEnviada = signal(false);
  readonly sugestaoErro = signal<string | null>(null);
  sugTitulo = '';
  sugDescricao = '';
  sugEmail = '';
  sugTelefone = '';

  abrirSugestao(): void {
    // Pré-preenche o título com o termo pesquisado.
    this.sugTitulo = this.termo().trim();
    this.sugestaoEnviada.set(false);
    this.sugestaoErro.set(null);
    this.mostrarSugestao.set(true);
  }

  fecharSugestao(): void {
    this.mostrarSugestao.set(false);
  }

  enviarSugestao(): void {
    const email = this.sugEmail.trim();
    const titulo = this.sugTitulo.trim();
    if (!titulo) { this.sugestaoErro.set('Informe o título do serviço.'); return; }
    if (!email || !email.includes('@')) { this.sugestaoErro.set('Informe um e-mail válido.'); return; }

    this.enviandoSugestao.set(true);
    this.sugestaoErro.set(null);
    this.sugestoesService.enviar({
      email,
      telefone: this.sugTelefone.trim() || undefined,
      titulo,
      descricao: this.sugDescricao.trim() || undefined,
    }).subscribe({
      next: () => {
        this.enviandoSugestao.set(false);
        this.sugestaoEnviada.set(true);
        this.sugTitulo = ''; this.sugDescricao = ''; this.sugEmail = ''; this.sugTelefone = '';
      },
      error: (err) => {
        this.enviandoSugestao.set(false);
        this.sugestaoErro.set(err?.error?.error ?? 'Erro ao enviar. Tente novamente.');
      },
    });
  }

  private readonly todas = sugestoesBusca();

  /** Sugestões filtradas pelo texto digitado (máx. 8). */
  readonly sugestoes = computed(() => {
    const t = this.termo().trim().toLowerCase();
    if (t.length < 2) return [];
    return this.todas.filter((s) => s.toLowerCase().includes(t)).slice(0, 8);
  });

  /** Categorias populares para atalho quando o campo está vazio. */
  readonly categorias = CATEGORIAS_MENU;

  ngOnInit(): void {
    this.seo.atualizarSeo({
      titulo: 'Buscar profissional — Prontto',
      descricao: 'Pesquise o serviço que precisa e veja os profissionais disponíveis na sua região.',
      url: 'https://prontto.org/buscar',
    });
    const q = this.route.snapshot.queryParamMap.get('q');
    if (q) {
      this.termo.set(q);
      this.buscar();
    }
  }

  selecionar(sugestao: string): void {
    this.termo.set(sugestao);
    this.irParaResultados(sugestao);
  }

  irParaCategoria(key: string): void {
    this.router.navigate(['/prestadores', key]);
  }

  buscar(): void {
    const t = this.termo().trim();
    if (!t) return;
    this.irParaResultados(t);
  }

  private irParaResultados(termo: string): void {
    // Resolve o termo para uma categoria e leva às boxes de profissionais.
    const rota = buscarServico(termo); // ['/servicos', key] | ['/servicos', key, sub] | null
    if (rota && rota.length >= 2) {
      this.semResultado.set(false);
      this.focado.set(false);
      this.router.navigate(['/prestadores', rota[1]]);
    } else {
      this.semResultado.set(true);
    }
  }
}
