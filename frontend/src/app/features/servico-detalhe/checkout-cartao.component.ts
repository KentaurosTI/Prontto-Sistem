import {
  Component, Input, Output, EventEmitter, inject, signal, ElementRef, ViewChild,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { loadStripe, Stripe, StripeElements } from '@stripe/stripe-js';
import { environment } from '../../../environments/environment';

/**
 * Checkout de cartão (Stripe Payment Element). Busca o client_secret no backend,
 * monta o formulário de pagamento e confirma. Emite (pago) no sucesso.
 */
@Component({
  selector: 'app-checkout-cartao',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './checkout-cartao.component.html',
  styleUrl: './checkout-cartao.component.scss',
})
export class CheckoutCartaoComponent {
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

  private stripe: Stripe | null = null;
  private elements: StripeElements | null = null;

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
      // Aguarda o container renderizar (display:block) para montar o Payment Element.
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
      this.erro.set(error.message ?? 'Falha no pagamento. Verifique os dados do cartão.');
    } else if (paymentIntent && (paymentIntent.status === 'succeeded' || paymentIntent.status === 'processing')) {
      this.sucesso.set(true);
      this.pago.emit();
    } else {
      this.erro.set('Pagamento não concluído. Tente novamente.');
    }
  }
}
