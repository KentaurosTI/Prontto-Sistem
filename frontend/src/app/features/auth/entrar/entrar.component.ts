import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { SeoService } from '../../../core/seo/seo.service';

@Component({
  selector: 'app-entrar',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './entrar.component.html',
  styleUrl: './entrar.component.scss',
})
export class EntrarComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly roteador = inject(Router);
  private readonly seoService = inject(SeoService);

  ngOnInit(): void {
    this.seoService.atualizarSeo({
      titulo: 'Entrar',
      descricao: 'Acesse sua conta Prontto.',
      url: 'https://prontto.org/entrar',
    });
  }

  readonly formulario = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required, Validators.minLength(8)]],
  });

  readonly carregando = signal(false);
  readonly erro = signal<string | null>(null);
  readonly senhaVisivel = signal(false);

  entrar(): void {
    if (this.formulario.invalid) return;

    const { email, senha } = this.formulario.value;
    this.carregando.set(true);
    this.erro.set(null);

    this.auth.entrar(email!, senha!).subscribe({
      next: () => this.roteador.navigate([this.destinoPosLogin()]),
      error: (resposta) => {
        this.erro.set(resposta.error?.error ?? 'Erro ao entrar');
        this.carregando.set(false);
      },
    });
  }

  /** Destino após o login: admin → /admin; contratante → /buscar; prestador → /minha-area. */
  private destinoPosLogin(): string {
    if (this.auth.ehAdmin()) return '/admin';
    return this.auth.usuario()?.tipoConta === 'cliente' ? '/buscar' : '/minha-area';
  }
}
