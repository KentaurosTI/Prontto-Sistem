import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { adminGuard } from './core/auth/admin.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/home/home.component').then(m => m.HomeComponent),
  },
  {
    path: 'servicos/novo',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/servicos/criar-servico/criar-servico.component').then(
        m => m.CriarServicoComponent,
      ),
  },
  {
    path: 'servicos',
    loadComponent: () =>
      import('./features/servicos/servicos.component').then(m => m.ServicosComponent),
  },
  // Página de categoria (pública): /servicos/:key e /servicos/:key/:sub
  {
    path: 'servicos/:key/:sub',
    loadComponent: () =>
      import('./features/categoria/categoria.component').then(m => m.CategoriaComponent),
  },
  {
    path: 'servicos/:key',
    loadComponent: () =>
      import('./features/categoria/categoria.component').then(m => m.CategoriaComponent),
  },
  // Listagem de profissionais por serviço (contratante escolhe o prestador)
  {
    path: 'prestadores/:categoriaSlug',
    loadComponent: () =>
      import('./features/prestadores/prestadores.component').then(m => m.PrestadoresComponent),
  },
  {
    path: 'como-funciona',
    loadComponent: () =>
      import('./features/como-funciona/como-funciona.component').then(m => m.ComoFuncionaComponent),
  },
  {
    path: 'para-prestadores',
    loadComponent: () =>
      import('./features/para-prestadores/para-prestadores.component').then(m => m.ParaPrestadoresComponent),
  },
  {
    path: 'entrar',
    loadComponent: () =>
      import('./features/auth/entrar/entrar.component').then(m => m.EntrarComponent),
  },
  {
    path: 'cadastrar',
    loadComponent: () =>
      import('./features/auth/cadastrar/cadastrar.component').then(m => m.CadastrarComponent),
  },
  {
    path: 'minha-area',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/minha-area/minha-area.component').then(m => m.MinhaAreaComponent),
  },
  {
    path: 'admin',
    canActivate: [authGuard, adminGuard],
    loadComponent: () =>
      import('./features/admin/admin.component').then(m => m.AdminComponent),
  },
  {
    path: 'servico/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/servico-detalhe/servico-detalhe.component').then(
        m => m.ServicoDetalheComponent,
      ),
  },
  // Rota canônica SEO: /:cidadeSlug/:categoriaSlug/:slug
  {
    path: ':cidadeSlug/:categoriaSlug/:slug',
    loadComponent: () =>
      import('./features/perfil-prestador/perfil-prestador.component').then(
        m => m.PerfilPrestadorComponent,
      ),
  },
  // Alias mantido para compatibilidade
  {
    path: 'prestador/:slug',
    loadComponent: () =>
      import('./features/perfil-prestador/perfil-prestador.component').then(
        m => m.PerfilPrestadorComponent,
      ),
  },
  {
    path: 'seguranca',
    loadComponent: () =>
      import('./features/seguranca/seguranca.component').then(m => m.SegurancaComponent),
  },
  {
    path: 'dicas/:slug',
    loadComponent: () =>
      import('./features/dica/dica.component').then(m => m.DicaComponent),
  },
  {
    path: 'privacidade',
    loadComponent: () =>
      import('./features/privacidade/privacidade.component').then(m => m.PrivacidadeComponent),
  },
  {
    path: 'regiao-indisponivel',
    loadComponent: () =>
      import('./features/regiao-indisponivel/regiao-indisponivel.component').then(m => m.RegiaoIndisponivelComponent),
  },
  {
    path: 'termos',
    loadComponent: () =>
      import('./features/termos/termos.component').then(m => m.TermosComponent),
  },
  {
    path: 'ajuda',
    loadComponent: () =>
      import('./features/ajuda/ajuda.component').then(m => m.AjudaComponent),
  },
  {
    path: 'caiu-em-golpe',
    loadComponent: () =>
      import('./features/golpe/golpe.component').then(m => m.GolpeComponent),
  },
  {
    path: 'dicas-seguranca',
    loadComponent: () =>
      import('./features/dicas-seguranca/dicas-seguranca.component').then(m => m.DicasSegurancaComponent),
  },
  {
    path: '**',
    loadComponent: () =>
      import('./features/not-found/not-found.component').then(m => m.NotFoundComponent),
  },
];
