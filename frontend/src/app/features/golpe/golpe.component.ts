import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '../../core/seo/seo.service';

const EMAIL_SUPORTE = 'prontto.org@gmail.com';

@Component({
  selector: 'app-golpe',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './golpe.component.html',
  styleUrl: './golpe.component.scss',
})
export class GolpeComponent implements OnInit {
  private readonly seo = inject(SeoService);

  readonly nome = signal('');
  readonly email = signal('');
  readonly telefone = signal('');
  readonly descricao = signal('');
  readonly enviado = signal(false);

  ngOnInit(): void {
    this.seo.atualizarSeo({
      titulo: 'Caiu em um golpe? Veja o que fazer — Prontto',
      descricao: 'Passo a passo do que fazer se você foi vítima de um golpe, e canal para denunciar ao Prontto.',
      url: 'https://prontto.org/caiu-em-golpe',
    });
    if (typeof window !== 'undefined') window.scrollTo(0, 0);
  }

  get formValido(): boolean {
    return (
      this.nome().trim().length >= 2 &&
      (this.email().trim().length >= 5 || this.telefone().trim().length >= 8) &&
      this.descricao().trim().length >= 10
    );
  }

  enviar(): void {
    if (!this.formValido) return;
    const assunto = `Denúncia de golpe — ${this.nome().trim()}`;
    const corpo =
      `Nome: ${this.nome().trim()}\n` +
      `E-mail: ${this.email().trim() || '—'}\n` +
      `Telefone: ${this.telefone().trim() || '—'}\n\n` +
      `Descrição do ocorrido:\n${this.descricao().trim()}`;
    window.location.href = `mailto:${EMAIL_SUPORTE}?subject=${encodeURIComponent(assunto)}&body=${encodeURIComponent(corpo)}`;
    this.enviado.set(true);
  }
}
