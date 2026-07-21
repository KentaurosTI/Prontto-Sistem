import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ConsentimentoService } from './consentimento.service';

@Component({
  selector: 'app-cookie-consent',
  standalone: true,
  imports: [RouterLink],
  template: `
    @if (consent.escolha() === null) {
      <div class="cookie" role="dialog" aria-live="polite" aria-label="Aviso de cookies">
        <div class="cookie__in">
          <div class="cookie__ic" aria-hidden="true"><i class="ri-shield-check-line"></i></div>
          <div class="cookie__txt">
            <b>Nós usamos cookies</b>
            <p>
              Utilizamos cookies para manter você conectado, lembrar preferências e melhorar sua
              experiência. Cookies essenciais são necessários para o site funcionar; os demais dependem
              do seu consentimento, conforme a <b>LGPD</b>. Saiba mais na
              <a routerLink="/privacidade">Política de Privacidade</a>.
            </p>
          </div>
          <div class="cookie__acoes">
            <button type="button" class="btn btn-out" (click)="consent.registrar('essenciais')">Apenas essenciais</button>
            <button type="button" class="btn btn-laranja" (click)="consent.registrar('aceito')">Aceitar todos</button>
          </div>
        </div>
      </div>
    }
  `,
  styleUrl: './cookie-consent.component.scss',
})
export class CookieConsentComponent {
  readonly consent = inject(ConsentimentoService);
}
