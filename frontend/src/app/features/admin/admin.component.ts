import { Component, inject, signal, OnInit, HostListener } from '@angular/core';
import { AdminService } from '../../core/api/admin.service';
import { EstatisticasAdmin, Servico, StatusServico, Cobranca, Usuario } from '../../core/models/usuario.model';
import { DecimalPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';

interface EdicaoUsuario { tipo: 'usuario'; id: string; nome: string; telefone: string; }
interface EdicaoServico { tipo: 'servico'; id: string; titulo: string; preco: number; }

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [DecimalPipe, DatePipe, RouterLink],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss',
})
export class AdminComponent implements OnInit {
  private readonly adminService = inject(AdminService);

  readonly estatisticas = signal<EstatisticasAdmin | null>(null);
  readonly servicos = signal<Servico[]>([]);
  readonly usuarios = signal<Usuario[]>([]);
  readonly listaCobrancas = signal<Cobranca[]>([]);
  readonly abaSelecionada = signal<'stats' | 'servicos' | 'usuarios' | 'financeiro'>('stats');
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
