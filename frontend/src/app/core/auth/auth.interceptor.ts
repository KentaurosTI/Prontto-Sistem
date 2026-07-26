import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

/** Endpoints de autenticação não devem disparar tentativa de refresh. */
function ehEndpointAuth(url: string): boolean {
  return url.includes('/api/auth/login')
    || url.includes('/api/auth/register')
    || url.includes('/api/auth/refresh')
    || url.includes('/api/auth/logout');
}

/**
 * SCRUM-22: autenticação por cookie httpOnly. Toda requisição para a API é enviada
 * com `withCredentials: true` para que o cookie `prontto_access_token` acompanhe —
 * não há mais header Authorization/Bearer no frontend.
 */
function comCredenciais(requisicao: HttpRequest<unknown>): HttpRequest<unknown> {
  return requisicao.url.startsWith(environment.apiUrl)
    ? requisicao.clone({ withCredentials: true })
    : requisicao;
}

export const authInterceptor: HttpInterceptorFn = (requisicao, proximo) => {
  const auth = inject(AuthService);

  return proximo(comCredenciais(requisicao)).pipe(
    catchError((erro: unknown) => {
      const status = erro instanceof HttpErrorResponse ? erro.status : 0;

      // Só tenta renovar em 401 de rotas protegidas (não nos próprios endpoints de auth).
      if (status !== 401 || ehEndpointAuth(requisicao.url)) {
        return throwError(() => erro);
      }

      // Access token expirou: renova via cookie de refresh e refaz a requisição.
      return auth.renovarSessao().pipe(
        switchMap(() => proximo(comCredenciais(requisicao))),
        catchError(() => {
          // Refresh falhou (sessão realmente expirada): desloga e manda pro login.
          auth.sessaoExpirada();
          return throwError(() => erro);
        }),
      );
    }),
  );
};
