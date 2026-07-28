import { Component, inject, signal, OnInit, HostListener } from '@angular/core';
import { AdminService } from '../../core/api/admin.service';
import { SugestoesService, Sugestao } from '../../core/api/sugestoes.service';
import { CategoriasAdminService, CategoriaAdmin } from '../../core/api/categorias-admin.service';
import { EstatisticasAdmin, Servico, StatusServico, Cobranca, Usuario } from '../../core/models/usuario.model';
import { DecimalPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

interface EdicaoUsuario { tipo: 'usuario'; id: string; nome: string; telefone: string; }
interface EdicaoServico { tipo: 'servico'; id: string; titulo: string; preco: number; }

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [DecimalPipe, DatePipe, RouterLink, FormsModule],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss',
})
export class AdminComponent implements OnInit {
  private readonly adminService = inject(AdminService);
  private readonly sugestoesService = inject(SugestoesService);
  private readonly categoriasService = inject(CategoriasAdminService);

  readonly estatisticas = signal<EstatisticasAdmin | null>(null);
  readonly servicos = signal<Servico[]>([]);
  readonly usuarios = signal<Usuario[]>([]);
  readonly listaCobrancas = signal<Cobranca[]>([]);
  readonly sugestoes = signal<Sugestao[]>([]);
  readonly categorias = signal<CategoriaAdmin[]>([]);
  readonly abaSelecionada = signal<'stats' | 'servicos' | 'usuarios' | 'financeiro' | 'sugestoes' | 'categorias'>('stats');

  // ── Formulário de categoria (incluir/editar) ────────────────────────────────
  readonly catFormAberto = signal(false);
  readonly catEditandoId = signal<string | null>(null);
  readonly catSalvando = signal(false);
  readonly catEnviandoImagem = signal(false);
  readonly catErro = signal<string | null>(null);
  catNome = '';
  catDescricao = '';
  catImagem = '';
  catAtiva = true;
  readonly carregando = signal(false);

  /** id da linha com o menu de ações "..." aberto (ou null). */
  readonly menuAberto = signal<string | null>(null);
  /** Estado do modal de edição. */
  readonly edicao = signal<EdicaoUsuario | EdicaoServico | null>(null);

  alternarMenu(id: string, ev: Event): void {
    ev.stopPropagation();
    this.menuAberto.update(a => (a === id ? null : id));
  }

  @HostListener('document:click')
  fecharMenu(): void {
    this.menuAberto.set(null);
  }

  // ── Usuários ──────────────────────────────────────────────
  abrirEditarUsuario(u: Usuario): void {
    this.menuAberto.set(null);
    this.edicao.set({ tipo: 'usuario', id: u.id, nome: u.nome ?? '', telefone: u.telefone ?? '' });
  }

  excluirUsuario(u: Usuario): void {
    this.menuAberto.set(null);
    if (!confirm(`Excluir o usuário "${u.nome}"? Essa ação remove o acesso dele.`)) return;
    this.adminService.excluirUsuario(u.id).subscribe({
      next: () => { this.carregarUsuarios(); this.carregarEstatisticas(); },
    });
  }

  // ── Serviços ──────────────────────────────────────────────
  abrirEditarServico(s: Servico): void {
    this.menuAberto.set(null);
    this.edicao.set({ tipo: 'servico', id: s.id, titulo: s.titulo, preco: s.preco });
  }

  excluirServico(s: Servico): void {
    this.menuAberto.set(null);
    if (!confirm(`Excluir o serviço "${s.titulo}"?`)) return;
    this.adminService.excluirServico(s.id).subscribe({
      next: () => { this.carregarServicos(); this.carregarEstatisticas(); },
    });
  }

  // ── Edição (modal) ────────────────────────────────────────
  atualizarCampoEdicao(campo: string, valor: string): void {
    this.edicao.update(e => (e ? { ...e, [campo]: campo === 'preco' ? Number(valor) : valor } as any : e));
  }

  salvarEdicao(): void {
    const e = this.edicao();
    if (!e) return;
    if (e.tipo === 'usuario') {
      this.adminService.editarUsuario(e.id, e.nome, e.telefone).subscribe({
        next: () => { this.carregarUsuarios(); this.edicao.set(null); },
      });
    } else {
      this.adminService.editarServico(e.id, e.titulo, e.preco).subscribe({
        next: () => { this.carregarServicos(); this.edicao.set(null); },
      });
    }
  }

  fecharEdicao(): void {
    this.edicao.set(null);
  }

  ngOnInit(): void {
    this.carregarEstatisticas();
    this.carregarServicos();
    this.carregarUsuarios();
    this.carregarCobrancas();
    this.carregarSugestoes();
    this.carregarCategorias();
  }

  carregarSugestoes(): void {
    this.sugestoesService.listarAdmin().subscribe(res => this.sugestoes.set(res.sugestoes));
  }

  // ── Catálogo de categorias ──────────────────────────────────────────────────
  carregarCategorias(): void {
    this.categoriasService.listar().subscribe(res => this.categorias.set(res.categorias));
  }

  abrirNovaCategoria(): void {
    this.catEditandoId.set(null);
    this.catNome = ''; this.catDescricao = ''; this.catImagem = ''; this.catAtiva = true;
    this.catErro.set(null);
    this.catFormAberto.set(true);
  }

  abrirEdicaoCategoria(c: CategoriaAdmin): void {
    this.catEditandoId.set(c.id);
    this.catNome = c.nome; this.catDescricao = c.descricao ?? '';
    this.catImagem = c.imagem ?? ''; this.catAtiva = c.ativa;
    this.catErro.set(null);
    this.catFormAberto.set(true);
  }

  fecharFormCategoria(): void {
    this.catFormAberto.set(false);
  }

  onImagemCategoriaSelecionada(evento: Event): void {
    const input = evento.target as HTMLInputElement;
    const arquivo = input.files?.[0];
    input.value = '';
    if (!arquivo) return;
    this.catEnviandoImagem.set(true);
    this.catErro.set(null);
    this.categoriasService.uploadImagem(arquivo).subscribe({
      next: (res) => { this.catImagem = res.url; this.catEnviandoImagem.set(false); },
      error: () => { this.catErro.set('Erro ao enviar a imagem.'); this.catEnviandoImagem.set(false); },
    });
  }

  salvarCategoria(): void {
    if (!this.catNome.trim()) { this.catErro.set('Informe o nome do serviço.'); return; }
    this.catSalvando.set(true);
    this.catErro.set(null);
    const id = this.catEditandoId();
    if (id) {
      this.categoriasService.editar(id, {
        nome: this.catNome.trim(),
        descricao: this.catDescricao.trim() || null,
        imagem: this.catImagem || null,
        ativa: this.catAtiva,
      }).subscribe({
        next: () => { this.catSalvando.set(false); this.catFormAberto.set(false); this.carregarCategorias(); },
        error: (e) => { this.catSalvando.set(false); this.catErro.set(e?.error?.error ?? 'Erro ao salvar.'); },
      });
    } else {
      this.categoriasService.criar({
        nome: this.catNome.trim(),
        descricao: this.catDescricao.trim() || null,
        imagem: this.catImagem || null,
      }).subscribe({
        next: () => { this.catSalvando.set(false); this.catFormAberto.set(false); this.carregarCategorias(); },
        error: (e) => { this.catSalvando.set(false); this.catErro.set(e?.error?.error ?? 'Erro ao salvar.'); },
      });
    }
  }

  alternarAtivaCategoria(c: CategoriaAdmin): void {
    this.categoriasService.alternarAtiva(c.id).subscribe({ next: () => this.carregarCategorias() });
  }

  excluirCategoria(c: CategoriaAdmin): void {
    if (!confirm(`Excluir o serviço "${c.nome}"? Esta ação não pode ser desfeita.`)) return;
    this.categoriasService.excluir(c.id).subscribe({ next: () => this.carregarCategorias() });
  }

  carregarEstatisticas(): void {
    this.adminService.obterEstatisticas().subscribe(dados => this.estatisticas.set(dados));
  }

  carregarServicos(): void {
    this.adminService.listarServicos().subscribe(res => this.servicos.set(res.services));
  }

  carregarUsuarios(): void {
    this.adminService.listarUsuarios().subscribe(res => this.usuarios.set(res.users));
  }

  carregarCobrancas(): void {
    this.adminService.listarCobranças().subscribe(res => this.listaCobrancas.set(res.charges));
  }

  atualizarStatus(servicoId: string, status: StatusServico): void {
    this.adminService.atualizarStatusServico(servicoId, status).subscribe({
      next: () => this.carregarServicos(),
    });
  }

  rotularStatus(status: StatusServico): string {
    const rotulos: Record<StatusServico, string> = {
      em_negociacao: 'Em negociação',
      aguardando_pagamento: 'Aguardando pagamento',
      pago: 'Pago',
      em_andamento: 'Em andamento',
      aguardando_confirmacao_cliente: 'Aguardando confirmação',
      em_disputa: 'Em disputa',
      concluido: 'Concluído',
      cancelado: 'Cancelado',
    };
    return rotulos[status];
  }
}
