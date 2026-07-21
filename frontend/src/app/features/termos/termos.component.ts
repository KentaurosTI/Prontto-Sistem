import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '../../core/seo/seo.service';

@Component({
  selector: 'app-termos',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './termos.component.html',
  styleUrl: './termos.component.scss',
})
export class TermosComponent implements OnInit {
  private readonly seo = inject(SeoService);

  ngOnInit(): void {
    this.seo.atualizarSeo({
      titulo: 'Termos de Uso — Prontto',
      descricao: 'Termos de uso do Prontto: modelo de intermediação, proteção de dados (LGPD), responsabilidades do contratante e do profissional.',
      url: 'https://prontto.org/termos',
    });
    if (typeof window !== 'undefined') window.scrollTo(0, 0);
  }
}
