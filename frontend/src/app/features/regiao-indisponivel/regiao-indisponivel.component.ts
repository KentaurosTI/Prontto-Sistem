import { Component, inject, computed } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { SeoService } from '../../core/seo/seo.service';

@Component({
  selector: 'app-regiao-indisponivel',
  standalone: true,
  imports: [RouterLink],
  template: `
    <main class="auth-wrap">
      <div class="auth-box">
        <div class="auth-ic" aria-hidden="true"><i class="ri-map-pin-line" style="font-size:42px;color:var(--laranja)"></i></div>
        <h1>Ainda não atendemos essa região</h1>
        <p class="auth-box__sub">
          @if (regiao()) { Não encontramos profissionais em <b>“{{ regiao() }}”</b>. }
          No momento, o Prontto atende <b>São Paulo e a Grande São Paulo</b>. Estamos crescendo — em breve
          chegaremos a mais cidades!
        </p>
        <a routerLink="/servicos" class="btn btn-laranja">Ver serviços em São Paulo</a>
        <a routerLink="/" class="btn btn-out">Voltar ao início</a>
      </div>
    </main>
  `,
})
export class RegiaoIndisponivelComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly seo = inject(SeoService);
  private readonly query = toSignal(this.route.queryParamMap, { initialValue: this.route.snapshot.queryParamMap });
  readonly regiao = computed(() => this.query().get('regiao') ?? '');

  constructor() {
    this.seo.atualizarSeo({
      titulo: 'Região não atendida — Prontto',
      descricao: 'No momento o Prontto atende São Paulo e a Grande São Paulo.',
      url: 'https://prontto.org/regiao-indisponivel',
    });
    if (typeof window !== 'undefined') window.scrollTo(0, 0);
  }
}
