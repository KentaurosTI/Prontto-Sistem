import { Component, computed, inject, signal, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { SeoService } from '../../core/seo/seo.service';
import { sugestoesBusca, buscarServico, CATEGORIAS_MENU } from '../../core/data/menu-categorias';

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

  readonly termo = signal('');
  readonly focado = signal(false);
  readonly semResultado = signal(false);

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
