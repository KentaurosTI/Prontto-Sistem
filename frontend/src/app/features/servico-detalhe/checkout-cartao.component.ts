import {
  Component, Input, Output, EventEmitter, inject, signal, ElementRef, ViewChild, OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { loadStripe, Stripe, StripeElements, PaymentIntent } from '@stripe/stripe-js';
import { environment } from '../../../environments/environment';

/**
 * Checkout com Stripe Payment Element. Suporta cartão (instantâneo) e boleto (assíncrono).
 * - Cartão: confirma na hora → (pago).
 * - Boleto: gera o comprovante e fica "aguardando confirmação" (o webhook confirma depois).
 * Também trata o retorno pós-redirect (?payment_intent_client_secret=...).
 */
@Component({
  selector: 'app-checkout-cartao',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './checkout-cartao.component.html',
  styleUrl: './checkout-cartao.component.scss',
})
export class CheckoutCartaoComponent implements OnInit {
  private readonly http = inject(HttpClient);

  @Input({ required: true }) servicoId!: string;
  @Input() valor = 0;
  @Output() pago = new EventEmitter<void>();

  @ViewChild('paymentElement') paymentElementRef!: ElementRef<HTMLDivElement>;

  readonly carregando = signal(false);
  readonly processando = signal(false);
  readonly erro = signal<string | null>(null);
  readonly sucesso = signal(false);
  readonly mostrarForm = signal(false);
  readonly aguardando = signal<string | null>(null);
  readonly boletoUrl = signal<string | null>(null);

  private stripe: Stripe | null = null;
  private elements: StripeElements | null = null;

  async ngOnInit(): Promise<void> {
    // Retorno de um redirect (ex.: boleto): checa o status do PaymentIntent.
    const cs = new URLSearchParams(window.location.search).get('payment_intent_client_secret');
    if (!cs) return;
    this.carregando.set(true);
    try {
      this.stripe = await loadStripe(environment.stripePublishableKey);
      const { paymentIntent } = await this.stripe!.retrievePaymentIntent(cs);
      this.tratarStatus(paymentIntent ?? undefined);
    } catch {
      /* silencioso — o usuário pode reiniciar o pagamento */
    } finally {
      this.carregando.set(false);
    }
  }

  async iniciarPagamento(): Promise<void> {
    this.erro.set(null);
    this.carregando.set(true);
    try {
      this.stripe = await loadStripe(environment.stripePublishableKey);
      if (!this.stripe) throw new Error('Não foi possível carregar o Stripe.');

      const res = await firstValueFrom(
        this.http.post<{ clientSecret: string }>(
          `${environment.apiUrl}/api/servicos/${this.servicoId}/checkout/stripe`, {},
        ),
      );

      this.elements = this.stripe.elements({
        clientSecret: res.clientSecret,
        locale: 'pt-BR',
        appearance: { theme: 'stripe', variables: { colorPrimary: '#f97316', borderRadius: '10px' } },
      });

      this.mostrarForm.set(true);
      setTimeout(() => {
        const el = this.elements!.create('payment');
        el.mount(this.paymentElementRef.nativeElement);
        this.carregando.set(false);
      });
    } catch (e: unknown) {
      this.carregando.set(false);
      const err = e as { error?: { error?: string }; message?: string };
      this.erro.set(err?.error?.error ?? err?.message ?? 'Não foi possível iniciar o pagamento.');
    }
  }

  async pagar(): Promise<void> {
    if (!this.stripe || !this.elements) return;
    this.processando.set(true);
    this.erro.set(null);

    const { error, paymentIntent } = await this.stripe.confirmPayment({
      elements: this.elements,
      confirmParams: { return_url: window.location.href },
      redirect: 'if_required',
    });

    this.processando.set(false);

    if (error) {
      this.erro.set(error.message ?? 'Falha no pagamento. Verifique os dados.');
      return;
    }
    this.tratarStatus(paymentIntent);
  }

  /** Interpreta o status do PaymentIntent (cartão instantâneo vs boleto/assíncrono). */
  private tratarStatus(pi?: PaymentIntent): void {
    if (!pi) return;
    switch (pi.status) {
      case 'succeeded':
        this.sucesso.set(true);
        this.pago.emit();
        break;
      case 'processing':
        this.mostrarForm.set(false);
        this.aguardando.set('Pagamento em processamento. Você será avisado por e-mail assim que for confirmado.');
        break;
      case 'requires_action': {
        // Boleto: exibe o comprovante e fica aguardando o pagamento.
        const voucher = (pi.next_action as { boleto_display_details?: { hosted_voucher_url?: string } } | null | undefined)
          ?.boleto_display_details?.hosted_voucher_url;
        this.mostrarForm.set(false);
        if (voucher) this.boletoUrl.set(voucher);
        this.aguardando.set('Boleto gerado. Efetue o pagamento pelo código para confirmar o serviço. Pode levar 1–2 dias úteis.');
        break;
      }
      default:
        this.erro.set('Pagamento não concluído. Tente novamente.');
    }
  }
}
