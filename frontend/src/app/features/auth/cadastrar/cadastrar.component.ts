import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { SeoService } from '../../../core/seo/seo.service';

@Component({
  selector: 'app-cadastrar',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './cadastrar.component.html',
  styleUrl: './cadastrar.component.scss',
})
export class CadastrarComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly roteador = inject(Router);
  private readonly rota = inject(ActivatedRoute);
  private readonly seoService = inject(SeoService);

  ngOnInit(): void {
    this.seoService.atualizarSeo({
      titulo: 'Criar Conta — Prontto',
      descricao: 'Crie sua conta Prontto gratuitamente e comece a solicitar ou oferecer serviços hoje mesmo.',
      url: 'https://prontto.org/cadastrar',
    });

    // Veio do bloco "Cadastre-se como Profissional" da Home: já entra como prestador.
    const p = this.rota.snapshot.queryParamMap;
    if (p.get('tipo') === 'prestador') {
      this.selecionarTipoConta('prestador');
      this.formulario.patchValue({
        email: p.get('email') ?? '',
        telefone: p.get('telefone') ?? '',
      });
    } else if (p.get('tipo') === 'cliente') {
      this.selecionarTipoConta('cliente');
    } else {
      // Padrão: cliente pré-selecionado (formulário em página única).
      this.selecionarTipoConta('cliente');
    }
  }

  readonly formulario = this.fb.group({
    nome: ['', [Validators.required, Validators.minLength(2)]],
    email: ['', [Validators.required, Validators.email]],
    senha: ['', [Validators.required, Validators.minLength(8)]],
    tipoConta: ['', Validators.required],
    telefone: [''],
    especialidade: [''],
  });

  readonly carregando = signal(false);
  readonly erro = signal<string | null>(null);
  readonly ehPrestador = signal(false);
  readonly senhaVisivel = signal(false);

  readonly tipoSelecionado = signal<'' | 'cliente' | 'prestador'>('');

  selecionarTipoConta(tipo: 'cliente' | 'prestador'): void {
    this.formulario.patchValue({ tipoConta: tipo });
    this.ehPrestador.set(tipo === 'prestador');
    this.tipoSelecionado.set(tipo);
  }

  cadastrar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    const { nome, email, senha, tipoConta, telefone, especialidade } = this.formulario.value;
    this.carregando.set(true);
    this.erro.set(null);

    this.auth.cadastrar({
      nome: nome!, email: email!, senha: senha!, tipoConta: tipoConta!,
      telefone: telefone ?? undefined, especialidade: especialidade ?? undefined,
    }).subscribe({
      next: () => this.roteador.navigate([this.auth.ehAdmin() ? '/admin' : '/minha-area']),
      error: (resposta) => {
        this.erro.set(resposta.error?.error ?? 'Erro ao cadastrar');
        this.carregando.set(false);
      },
    });
  }
}
