import { Component, OnInit, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '../../core/seo/seo.service';

@Component({
  selector: 'app-dicas-seguranca',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './dicas-seguranca.component.html',
  styleUrl: './dicas-seguranca.component.scss',
})
export class DicasSegurancaComponent implements OnInit {
  private readonly seo = inject(SeoService);

  ngOnInit(): void {
    this.seo.atualizarSeo({
      titulo: 'Dicas de Segurança — Prontto',
      descricao: 'Dicas práticas para contratar e pagar com segurança, verificar profissionais e evitar golpes no Prontto.',
      url: 'https://prontto.org/dicas-seguranca',
    });
    if (typeof window !== 'undefined') window.scrollTo(0, 0);
  }
}
