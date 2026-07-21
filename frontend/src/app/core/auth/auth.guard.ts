import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const roteador = inject(Router);

  if (!auth.estaAutenticado()) {
    roteador.navigate(['/entrar']);
    return false;
  }
  return true;
};
