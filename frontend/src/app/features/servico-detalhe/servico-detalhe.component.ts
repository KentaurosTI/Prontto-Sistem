import {
  Component,
  inject,
  signal,
  computed,
  OnInit,
  OnDestroy,
  ElementRef,
  ViewChild,
} from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { interval, Subscription } from 'rxjs';
import { switchMap } from 'rxjs/operators';
import type { Stripe, StripeCardElement, StripeElements } from '@stripe/stripe-js';
import { AuthService } from '../../core/auth/auth.service';
import { ServicosService } from '../../core/api/servicos.service';
import { CobrancaService } from '../../core/api/cobranca.service';
import { Servico, MensagemServico, Cobranca, DtoInicioCartao } from '../../core/models/usuario.model';

@Component({
  selector: 'app-servico-detalhe',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './servico-detalhe.component.html',
  styleUrl: './servico-detalhe.component.scss',
})
export class ServicoDetalheComponent implements OnInit, OnDestroy {
  @ViewChild('stripeCardElement') stripeCardRef?: ElementRef<HTMLDivElement>;
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly servicosService = inject(ServicosService);
  private readonly cobrancaService = inject(CobrancaService);

  readonly usuario = this.auth.usuario;
  readonly servico = signal<Servico | null>(null);
  readonly mensagens = signal<MensagemServico[]>([]);
  readonly cobranca = signal<Cobranca | null>(null);
  readonly carregando = signal(true);
  readonly erro = signal<string | null>(null);
  readonly enviando = signal(false);
  readonly mensagemFeedback = signal<string | null>(null);

  // Cursor-based pagination (RF-06)
  readonly ultimoId = signal<string | null>(null);
  readonly temMaisAnteriores = signal(false);

  // Formulários
  conteudoMensagem = '';
  valorProposta: number | null = null;
  motivoDisputa = '';
  descricaoDisputa = '';
  motivoCancelamento = '';

  // UI state
  readonly mostrarFormProposta = signal(false);
  readonly mostrarFormDisputa = signal(false);
  readonly mostrarFormCancelamento = signal(false);

  // Pagamento por cartão (Stripe)
  readonly metodoPagamento = signal<'pix' | 'cartao'>('pix');
  readonly inicioCartao = signal<DtoInicioCartao | null>(null);
  readonly carregandoCartao = signal(false);
  readonly confirmandoCartao = signal(false);
  readonly erroCartao = signal<string | null>(null);
  readonly cartaoPago = signal(false);
  parcelas = 1;
  private stripeInstance: Stripe | null = null;
  private stripeElements: StripeElements | null = null;
  private stripeCard: StripeCardElement | null = null;

  private servicoId = '';
  private pollingSubscription?: Subscription;

  // ── Computeds ─────────────────────────────────────────────────────────────

  readonly eCliente = computed(
    () => this.usuario()?.tipoConta === 'cliente'
  );

  readonly ePrestador = computed(
    () => this.usuario()?.tipoConta === 'prestador'
  );

  readonly eAdmin = computed(() => this.usuario()?.papel === 'admin');

  readonly ehClienteDoServico = computed(() => {
    const u = this.usuario();
    const s = this.servico();
    return u && s ? s.clienteId === u.id : false;
  });

  readonly ehPrestadorDoServico = computed(() => {
    const u = this.usuario();
    const s = this.servico();
    return u && s ? s.prestadorId === u.id : false;
  });

  readonly podeEnviarMensagem = computed(() => {
    const s = this.servico();
    if (!s) return false;
    const statusBloqueados: string[] = ['concluido', 'cancelado', 'em_disputa'];
    return !statusBloqueados.includes(s.status);
  });

  readonly podeEnviarProposta = computed(() => {
    const s = this.servico();
    if (!s) return false;
    return s.status === 'em_negociacao';
  });

  readonly podeVincular = computed(() => {
    const s = this.servico();
    if (!s) return false;
    return (
      this.ePrestador() &&
      s.status === 'em_negociacao' &&
      !s.prestadorId
    );
  });

  readonly podeConcluir = computed(() => {
    const s = this.servico();
    if (!s) return false;
    return this.ehPrestadorDoServico() && s.status === 'em_andamento';
  });

  readonly podeConfirmar = computed(() => {
    const s = this.servico();
    if (!s) return false;
    return (
      this.ehClienteDoServico() &&
      s.status === 'aguardando_confirmacao_cliente'
    );
  });

  readonly podeAbrirDisputa = computed(() => {
    const s = this.servico();
    if (!s) return false;
    return (
      this.ehClienteDoServico() &&
      s.status === 'aguardando_confirmacao_cliente'
    );
  });

  readonly podeCancelar = computed(() => {
    const s = this.servico();
    if (!s) return false;
    if (s.status === 'concluido' || s.status === 'cancelado') return false;
    if (this.eAdmin()) return true;
    if (this.ehClienteDoServico() || this.ehPrestadorDoServico()) {
      return s.status === 'em_negociacao';
    }
    return false;
  });

  readonly propostaPendente = computed(() => {
    return this.mensagens().find(
      (m) =>
        m.tipoMensagem === 'proposta' && m.statusProposta === 'pendente'
    ) ?? null;
  });

  // ── Lifecycle ──────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.servicoId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.servicoId) {
      this.router.navigate(['/minha-area']);
      return;
    }

    this.carregarServico();
    this.iniciarPolling();
  }

  ngOnDestroy(): void {
    this.pollingSubscription?.unsubscribe();
  }

  // ── Carregamento ───────────────────────────────────────────────────────────

  private carregarServico(): void {
    this.carregando.set(true);
    this.servicosService.obterServico(this.servicoId).subscribe({
      next: (res) => {
        this.servico.set(res.servico);
        this.carregarMensagens();
        this.carregarCobranca();
        this.carregando.set(false);
      },
      error: () => {
        this.erro.set('Serviço não encontrado ou acesso negado.');
        this.carregando.set(false);
      },
    });
  }

  private carregarMensagens(): void {
    this.servicosService.listarMensagens(this.servicoId).subscribe({
      next: (res) => {
        this.mensagens.set(res.mensagens);
        this.ultimoId.set(res.ultimoId);
        this.temMaisAnteriores.set(res.temMais);
        this.rolarParaFinalChat();
      },
    });
  }

  private rolarParaFinalChat(): void {
    setTimeout(() => {
      const chatEl = document.querySelector('.chat-mensagens');
      if (chatEl) chatEl.scrollTop = chatEl.scrollHeight;
    }, 50);
  }

  private carregarCobranca(): void {
    const s = this.servico();
    if (!s || !['aguardando_pagamento', 'pago', 'em_andamento', 'aguardando_confirmacao_cliente', 'em_disputa', 'concluido'].includes(s.status)) return;
    this.cobrancaService.obterCobranca(this.servicoId).subscribe({
      next: (res) => this.cobranca.set(res.cobranca),
      error: () => { /* cobrança pode não existir ainda */ }
    });
  }

  /** Rola a página até o card de checkout (PIX) após aceitar a proposta. */
  private rolarParaCheckout(): void {
    // Aguarda o *ngIf da seção PIX renderizar (status + cobrança carregados).
    let tentativas = 0;
    const tentar = () => {
      const el = document.querySelector('.pix-container');
      if (el) {
        el.scrollIntoView({ behavior: 'smooth', block: 'center' });
      } else if (tentativas++ < 20) {
        setTimeout(tentar, 150);
      }
    };
    setTimeout(tentar, 150);
  }

  copiarPix(): void {
    const c = this.cobranca();
    if (c?.pixCopiaCola) {
      navigator.clipboard.writeText(c.pixCopiaCola).then(() => {
        this.mensagemFeedback.set('Código PIX copiado!');
        setTimeout(() => this.mensagemFeedback.set(null), 3000);
      });
    }
  }

  selecionarMetodoPagamento(metodo: 'pix' | 'cartao'): void {
    this.metodoPagamento.set(metodo);
    this.erroCartao.set(null);
    if (metodo === 'cartao') {
      this.prepararFormularioCartao();
    } else {
      this.destruirStripeElement();
    }
  }

  private async prepararFormularioCartao(): Promise<void> {
    if (this.inicioCartao()) {
      // PaymentIntent já criado — só monta o element
      setTimeout(() => this.montarStripeElement(), 50);
      return;
    }

    this.carregandoCartao.set(true);
    this.erroCartao.set(null);

    this.cobrancaService.iniciarPagamentoCartao(this.servicoId, this.parcelas).subscribe({
      next: async (dto) => {
        this.inicioCartao.set(dto);
        this.carregandoCartao.set(false);
        // Aguarda o DOM renderizar o container antes de montar o element
        setTimeout(() => this.montarStripeElement(), 50);
      },
      error: (err) => {
        this.erroCartao.set(err?.error?.error ?? 'Erro ao iniciar pagamento por cartão.');
        this.carregandoCartao.set(false);
      },
    });
  }

  private async montarStripeElement(): Promise<void> {
    const dto = this.inicioCartao();
    if (!dto || !this.stripeCardRef?.nativeElement) return;

    if (!this.stripeInstance) {
      const { loadStripe } = await import('@stripe/stripe-js');
      this.stripeInstance = await loadStripe(dto.publishableKey);
    }

    if (!this.stripeInstance) {
      this.erroCartao.set('Erro ao carregar Stripe.');
      return;
    }

    if (!this.stripeElements) {
      this.stripeElements = this.stripeInstance.elements();
    }

    if (!this.stripeCard) {
      this.stripeCard = this.stripeElements.create('card', {
        style: {
          base: {
            fontSize: '16px',
            color: '#374151',
            fontFamily: '"Inter", sans-serif',
            '::placeholder': { color: '#9ca3af' },
          },
        },
        hidePostalCode: true,
      });
      this.stripeCard.mount(this.stripeCardRef.nativeElement);
    }
  }

  private destruirStripeElement(): void {
    if (this.stripeCard) {
      this.stripeCard.destroy();
      this.stripeCard = null;
      this.stripeElements = null;
    }
  }

  async confirmarPagamentoCartao(): Promise<void> {
    const dto = this.inicioCartao();
    if (!dto || !this.stripeInstance || !this.stripeCard) return;

    this.confirmandoCartao.set(true);
    this.erroCartao.set(null);

    const result = await this.stripeInstance.confirmCardPayment(dto.clientSecret, {
      payment_method: { card: this.stripeCard },
    });

    this.confirmandoCartao.set(false);

    if (result.error) {
      this.erroCartao.set(result.error.message ?? 'Pagamento recusado. Verifique os dados do cartão.');
    } else if (result.paymentIntent?.status === 'succeeded') {
      this.cartaoPago.set(true);
      this.destruirStripeElement();
      this.exibirFeedback('Pagamento aprovado! Aguardando confirmação do sistema.');
      // Aguarda alguns segundos e recarrega o serviço para refletir o novo status
      setTimeout(() => this.carregarServico(), 4000);
    }
  }

  private iniciarPolling(): void {
    // Polling incremental a cada 10 segundos — só busca mensagens novas após o cursor (ADR §11.4)
    this.pollingSubscription = interval(10_000)
      .pipe(
        switchMap(() =>
          this.servicosService.listarMensagens(
            this.servicoId,
            this.ultimoId() ?? undefined
          )
        )
      )
      .subscribe({
        next: (res) => {
          if (res.mensagens.length > 0) {
            this.anexarMensagens(res.mensagens);
            this.ultimoId.set(res.ultimoId);
            this.rolarParaFinalChat();
          }
        },
      });
  }

  /** Anexa mensagens ignorando as que já estão na lista (evita duplicidade). */
  private anexarMensagens(novas: MensagemServico[]): void {
    this.mensagens.update((atual) => {
      const idsExistentes = new Set(atual.map((m) => m.id));
      const filtradas = novas.filter((m) => !idsExistentes.has(m.id));
      return filtradas.length ? [...atual, ...filtradas] : atual;
    });
  }

  // ── Ações ──────────────────────────────────────────────────────────────────

  enviarMensagem(): void {
    if (!this.conteudoMensagem.trim()) return;
    this.enviando.set(true);
    this.servicosService.enviarMensagem(this.servicoId, this.conteudoMensagem).subscribe({
      next: (res) => {
        this.anexarMensagens([res.mensagem]);
        // Avança o cursor para a mensagem recém-enviada, senão o polling a re-buscaria (duplicidade).
        this.ultimoId.set(res.mensagem.id);
        this.conteudoMensagem = '';
        this.enviando.set(false);
        this.rolarParaFinalChat();
      },
      error: (err) => {
        this.exibirFeedback(err?.error?.error ?? 'Erro ao enviar mensagem.');
        this.enviando.set(false);
      },
    });
  }

  enviarProposta(): void {
    if (!this.valorProposta || this.valorProposta <= 0) {
      this.exibirFeedback('Informe um valor válido para a proposta.');
      return;
    }
    this.enviando.set(true);
    this.servicosService.enviarProposta(this.servicoId, this.valorProposta).subscribe({
      next: (res) => {
        this.anexarMensagens([res.mensagem]);
        this.ultimoId.set(res.mensagem.id);
        this.valorProposta = null;
        this.mostrarFormProposta.set(false);
        this.enviando.set(false);
      },
      error: (err) => {
        this.exibirFeedback(err?.error?.error ?? 'Erro ao enviar proposta.');
        this.enviando.set(false);
      },
    });
  }

  aceitarProposta(mensagemId: string): void {
    this.enviando.set(true);
    this.servicosService.aceitarProposta(this.servicoId, mensagemId).subscribe({
      next: (res) => {
        this.servico.set(res.servico);
        this.carregarMensagens();
        // Carrega a cobrança/PIX gerada e leva o cliente direto ao checkout.
        this.carregarCobranca();
        this.exibirFeedback('Proposta aceita! Finalize o pagamento para agendar o serviço.');
        this.enviando.set(false);
        this.rolarParaCheckout();
      },
      error: (err) => {
        this.exibirFeedback(err?.error?.error ?? 'Erro ao aceitar proposta.');
        this.enviando.set(false);
      },
    });
  }

  vincularPrestador(): void {
    this.enviando.set(true);
    this.servicosService.vincularPrestador(this.servicoId).subscribe({
      next: (res) => {
        this.servico.set(res.servico);
        this.exibirFeedback('Você foi vinculado ao serviço!');
        this.enviando.set(false);
      },
      error: (err) => {
        this.exibirFeedback(err?.error?.error ?? 'Erro ao vincular.');
        this.enviando.set(false);
      },
    });
  }

  marcarConcluido(): void {
    this.enviando.set(true);
    this.servicosService.marcarConcluido(this.servicoId).subscribe({
      next: (res) => {
        this.servico.set(res.servico);
        this.exibirFeedback('Serviço marcado como concluído. Aguardando confirmação do cliente.');
        this.enviando.set(false);
      },
      error: (err) => {
        this.exibirFeedback(err?.error?.error ?? 'Erro ao marcar como concluído.');
        this.enviando.set(false);
      },
    });
  }

  confirmarConclusao(): void {
    this.enviando.set(true);
    this.servicosService.confirmarConclusao(this.servicoId).subscribe({
      next: (res) => {
        this.servico.set(res.servico);
        this.exibirFeedback('Conclusão confirmada. Pagamento liberado ao prestador!');
        this.enviando.set(false);
      },
      error: (err) => {
        this.exibirFeedback(err?.error?.error ?? 'Erro ao confirmar conclusão.');
        this.enviando.set(false);
      },
    });
  }

  cancelar(): void {
    if (!this.motivoCancelamento.trim()) {
      this.exibirFeedback('Informe o motivo do cancelamento.');
      return;
    }
    this.enviando.set(true);
    this.servicosService.cancelar(this.servicoId, this.motivoCancelamento).subscribe({
      next: (res) => {
        this.servico.set(res.servico);
        this.mostrarFormCancelamento.set(false);
        this.exibirFeedback('Serviço cancelado.');
        this.enviando.set(false);
      },
      error: (err) => {
        this.exibirFeedback(err?.error?.error ?? 'Erro ao cancelar.');
        this.enviando.set(false);
      },
    });
  }

  abrirDisputa(): void {
    if (!this.motivoDisputa.trim()) {
      this.exibirFeedback('Informe o motivo da disputa.');
      return;
    }
    this.enviando.set(true);
    this.servicosService
      .abrirDisputa(this.servicoId, this.motivoDisputa, this.descricaoDisputa || undefined)
      .subscribe({
        next: () => {
          this.carregarServico();
          this.mostrarFormDisputa.set(false);
          this.exibirFeedback('Disputa aberta. O admin irá analisar o caso.');
          this.enviando.set(false);
        },
        error: (err) => {
          this.exibirFeedback(err?.error?.error ?? 'Erro ao abrir disputa.');
          this.enviando.set(false);
        },
      });
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  private exibirFeedback(mensagem: string): void {
    this.mensagemFeedback.set(mensagem);
    setTimeout(() => this.mensagemFeedback.set(null), 4000);
  }

  corStatus(status: string): string {
    const mapa: Record<string, string> = {
      em_negociacao: 'badge-negociacao',
      aguardando_pagamento: 'badge-pagamento',
      pago: 'badge-pago',
      em_andamento: 'badge-andamento',
      aguardando_confirmacao_cliente: 'badge-aguardando',
      em_disputa: 'badge-disputa',
      concluido: 'badge-concluido',
      cancelado: 'badge-cancelado',
    };
    return mapa[status] ?? 'badge-default';
  }

  labelStatus(status: string): string {
    const mapa: Record<string, string> = {
      em_negociacao: 'Em Negociação',
      aguardando_pagamento: 'Aguardando Pagamento',
      pago: 'Pago',
      em_andamento: 'Em Andamento',
      aguardando_confirmacao_cliente: 'Aguardando Confirmação',
      em_disputa: 'Em Disputa',
      concluido: 'Concluído',
      cancelado: 'Cancelado',
    };
    return mapa[status] ?? status;
  }

  ehProposta(msg: MensagemServico): boolean {
    return msg.tipoMensagem === 'proposta';
  }

  propostaPodeSerAceita(msg: MensagemServico): boolean {
    if (!this.ehProposta(msg)) return false;
    if (msg.statusProposta !== 'pendente') return false;
    // Não pode aceitar a própria proposta
    const u = this.usuario();
    return u ? msg.remetenteId !== u.id : false;
  }
}
