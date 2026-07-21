import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/** Endpoints de autenticação não devem disparar tentativa de refresh. */
function ehEndpointAuth(url: string): boolean {
  return url.includes('/api/auth/login')
    || url.includes('/api/auth/register')
    || url.includes('/api/auth/refresh')
    || url.includes('/api/auth/logout');
}

function comToken(requisicao: HttpRequest<unknown>, token: string | null) {
  return token
    ? requisicao.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : requisicao;
}

export const authInterceptor: HttpInterceptorFn = (requisicao, proximo) => {
  const auth = inject(AuthService);

  return proximo(comToken(requisicao, auth.obterToken())).pipe(
    catchError((erro: unknown) => {
      const status = erro instanceof HttpErrorResponse ? erro.status : 0;

      // Só tenta renovar em 401 de rotas protegidas (não nos próprios endpoints de auth).
      if (status !== 401 || ehEndpointAuth(requisicao.url)) {
        return throwError(() => erro);
      }

      // Access token expirou: renova via cookie de refresh e refaz a requisição.
      return auth.renovarSessao().pipe(
        switchMap(() => proximo(comToken(requisicao, auth.obterToken()))),
        catchError(() => {
          // Refresh falhou (sessão realmente expirada): desloga e manda pro login.
          auth.sessaoExpirada();
          return throwError(() => erro);
        }),
      );
    }),
  );
};
