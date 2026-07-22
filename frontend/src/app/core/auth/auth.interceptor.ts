import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
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

export const authInterceptor: HttpInterceptorFn = (requisicao, proximo) => {
  const auth = inject(AuthService);

  // Envia cookies httpOnly automaticamente em todas as requisições.
  const req = requisicao.clone({ withCredentials: true });

  return proximo(req).pipe(
    catchError((erro: unknown) => {
      const status = erro instanceof HttpErrorResponse ? erro.status : 0;

      // Só tenta renovar em 401 de rotas protegidas (não nos próprios endpoints de auth).
      if (status !== 401 || ehEndpointAuth(req.url)) {
        return throwError(() => erro);
      }

      // Access token expirou: renova via cookie de refresh e refaz a requisição.
      return auth.renovarSessao().pipe(
        switchMap(() => proximo(req.clone({ withCredentials: true }))),
        catchError(() => {
          // Refresh falhou (sessão realmente expirada): desloga e manda pro login.
          auth.sessaoExpirada();
          return throwError(() => erro);
        }),
      );
    }),
  );
};
