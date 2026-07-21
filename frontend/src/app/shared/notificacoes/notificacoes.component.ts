import { Component, inject, signal, HostListener, OnInit, OnDestroy } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { NotificacaoService } from '../../core/api/notificacao.service';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-notificacoes',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './notificacoes.component.html',
  styleUrl: './notificacoes.component.scss',
})
export class NotificacoesComponent implements OnInit, OnDestroy {
  private readonly service = inject(NotificacaoService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly aberto = signal(false);
  readonly notificacoes = this.service.notificacoes;
  readonly naoLidas = this.service.naoLidas;

  private intervalo?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    if (this.auth.estaAutenticado()) {
      this.service.atualizarContagem();
      // Polling leve do badge a cada 30s.
      this.intervalo = setInterval(() => {
        if (this.auth.estaAutenticado()) this.service.atualizarContagem();
      }, 30_000);
    }
  }

  ngOnDestroy(): void {
    if (this.intervalo) clearInterval(this.intervalo);
  }

  alternar(ev: Event): void {
    ev.stopPropagation();
    const novo = !this.aberto();
    this.aberto.set(novo);
    if (novo) this.service.carregar();
  }

  @HostListener('document:click')
  fechar(): void {
    this.aberto.set(false);
  }

  abrir(n: { id: string; referenciaId?: string | null; lida: boolean }): void {
    if (!n.lida) this.service.marcarLida(n.id);
    this.aberto.set(false);
    if (n.referenciaId) {
      this.router.navigate(['/servico', n.referenciaId]);
    }
  }

  marcarTodas(ev: Event): void {
    ev.stopPropagation();
    this.service.marcarTodasLidas();
  }
}
