import { Component, inject, computed } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { SeoService } from '../../core/seo/seo.service';
import { findDica } from '../../core/data/dicas';

@Component({
  selector: 'app-dica',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dica.component.html',
  styleUrl: './dica.component.scss',
})
export class DicaComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly seo = inject(SeoService);

  private readonly params = toSignal(this.route.paramMap, { initialValue: this.route.snapshot.paramMap });
  readonly dica = computed(() => findDica(this.params().get('slug') ?? ''));

  constructor() {
    const d = this.dica();
    if (d) {
      this.seo.atualizarSeo({
        titulo: `${d.titulo} — Prontto`,
        descricao: d.resumo,
        url: `https://prontto.org/dicas/${d.slug}`,
      });
    }
    if (typeof window !== 'undefined') window.scrollTo(0, 0);
  }
}
