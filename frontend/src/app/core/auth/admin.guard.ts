import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const roteador = inject(Router);

  if (!auth.ehAdmin()) {
    roteador.navigate(['/minha-area']);
    return false;
  }
  return true;
};
