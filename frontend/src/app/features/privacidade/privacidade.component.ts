import { Component, OnInit, inject } from '@angular/core';
import { SeoService } from '../../core/seo/seo.service';
import { ConsentimentoService } from '../../core/lgpd/consentimento.service';

@Component({
  selector: 'app-privacidade',
  standalone: true,
  templateUrl: './privacidade.component.html',
  styleUrl: './privacidade.component.scss',
})
export class PrivacidadeComponent implements OnInit {
  private readonly seo = inject(SeoService);
  readonly consent = inject(ConsentimentoService);

  ngOnInit(): void {
    this.seo.atualizarSeo({
      titulo: 'Política de Privacidade e Cookies — Prontto',
      descricao: 'Como o Prontto coleta, usa e protege seus dados pessoais, em conformidade com a LGPD (Lei 13.709/2018).',
      url: 'https://prontto.org/privacidade',
    });
    if (typeof window !== 'undefined') window.scrollTo(0, 0);
  }
}
