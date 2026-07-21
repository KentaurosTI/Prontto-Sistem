import { Component, inject, signal, OnInit } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ServicosService } from '../../../core/api/servicos.service';
import { PerfilPrestadorService } from '../../../core/api/perfil-prestador.service';
import { AuthService } from '../../../core/auth/auth.service';
import { Categoria, Cidade } from '../../../core/models/usuario.model';
import { CATEGORIAS_MENU } from '../../../core/data/menu-categorias';

@Component({
  selector: 'app-criar-servico',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule, RouterLink],
  templateUrl: './criar-servico.component.html',
  styleUrl: './criar-servico.component.scss',
})
export class CriarServicoComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly servicosService = inject(ServicosService);
  private readonly perfilService = inject(PerfilPrestadorService);
  readonly auth = inject(AuthService);

  readonly categorias = signal<Categoria[]>([]);
  readonly cidades = signal<Cidade[]>([]);
  readonly enviando = signal(false);
  readonly erro = signal<string | null>(null);
  readonly carregandoListas = signal(true);

  // Prestador pré-selecionado via query param
  readonly prestadorIdParam = signal<string | null>(null);
  readonly prestadorNomeParam = signal<string | null>(null);

  // Data mínima para o campo de agendamento
  readonly hoje = new Date().toISOString().split('T')[0];

  readonly formulario = this.fb.group({
    titulo: ['', [Validators.required, Validators.minLength(5), Validators.maxLength(120)]],
    descricao: ['', [Validators.required, Validators.minLength(10)]],
    categoriaId: ['', Validators.required],
    subcategoria: [''],
    cidadeId: [''],
    endereco: [''],
    agendadoEm: [''],
  });

  /** Subcategorias da categoria selecionada (a partir do menu estático, casado pelo slug). */
  subcategoriasDisponiveis(): string[] {
    const catId = this.formulario.get('categoriaId')?.value;
    if (!catId) return [];
    const cat = this.categorias().find((c) => c.id === catId);
    if (!cat) return [];
    const menu = CATEGORIAS_MENU.find((m) => m.key === cat.slug);
    if (!menu) return [];
    return menu.grupos.flatMap((g) => g.itens);
  }

  aoTrocarCategoria(): void {
    // Limpa a subcategoria ao mudar de categoria (as opções mudam).
    this.formulario.patchValue({ subcategoria: '' });
  }

  ngOnInit(): void {
    const prestadorId = this.route.snapshot.queryParamMap.get('prestadorId');
    const prestadorNome = this.route.snapshot.queryParamMap.get('prestadorNome');

    if (prestadorId) this.prestadorIdParam.set(prestadorId);
    if (prestadorNome) this.prestadorNomeParam.set(decodeURIComponent(prestadorNome));

    // Pré-preenche cidade e endereço com o cadastro do cliente (se houver).
    const u = this.auth.usuario();
    if (u) {
      this.formulario.patchValue({
        cidadeId: u.cidadeId ?? '',
        endereco: u.endereco ?? '',
      });
    }

    this.perfilService.listarCategorias().subscribe({
      next: (cats) => {
        this.categorias.set(cats);
        this.verificarCarregamento();
      },
      error: () => this.verificarCarregamento(),
    });

    this.perfilService.listarCidades().subscribe({
      next: (cids) => {
        this.cidades.set(cids);
        this.verificarCarregamento();
      },
      error: () => this.verificarCarregamento(),
    });
  }

  private carregouCategorias = false;
  private carregouCidades = false;

  private verificarCarregamento(): void {
    // Marca cada lista como carregada alternadamente
    if (!this.carregouCategorias) {
      this.carregouCategorias = true;
    } else {
      this.carregouCidades = true;
      this.carregandoListas.set(false);
    }
  }

  submeter(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    this.enviando.set(true);
    this.erro.set(null);

    const v = this.formulario.value;

    // Inclui a subcategoria escolhida no início da descrição (o backend não tem campo próprio).
    let descricao = v.descricao || '';
    if (v.subcategoria) {
      descricao = `Subcategoria: ${v.subcategoria}\n\n${descricao}`;
    }

    this.servicosService
      .criarSolicitacao({
        titulo: v.titulo!,
        descricao: descricao || null,
        categoriaId: v.categoriaId!,
        cidadeId: v.cidadeId || null,
        endereco: v.endereco || null,
        agendadoEm: v.agendadoEm || null,
        prestadorId: this.prestadorIdParam() || null,
      })
      .subscribe({
        next: () => {
          this.router.navigate(['/minha-area']);
        },
        error: (err) => {
          const msg =
            err?.error?.error ||
            err?.error?.mensagem ||
            err?.error?.message ||
            'Erro ao criar solicitação. Tente novamente.';
          this.erro.set(msg);
          this.enviando.set(false);
        },
      });
  }

  campoInvalido(nome: string): boolean {
    const campo = this.formulario.get(nome);
    return !!(campo && campo.invalid && campo.touched);
  }

  erroCampo(nome: string, tipo: string): boolean {
    const campo = this.formulario.get(nome);
    return !!(campo && campo.hasError(tipo) && campo.touched);
  }
}
