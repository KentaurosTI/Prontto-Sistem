import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { PerfilPrestadorComponent } from './perfil-prestador.component';
import { PerfilPrestadorService } from '../../core/api/perfil-prestador.service';
import { AuthService } from '../../core/auth/auth.service';
import { PerfilPublico } from '../../core/models/usuario.model';

const perfilMock: PerfilPublico = {
  id: 'uuid-1',
  nome: 'João Silva',
  fotoPerfilUrl: null,
  slug: 'joao-silva-a1b2',
  descricao: 'Especialista em instalações elétricas.',
  especialidade: 'Eletricista',
  mediaAvaliacoes: 4.5,
  totalAvaliacoes: 12,
  categorias: [{ id: 'cat-1', nome: 'Eletricista', slug: 'eletricista' }],
  cidades: [{ id: 'cid-1', nome: 'Itapevi', estado: 'SP', slug: 'itapevi' }],
  imagensPortfolio: [],
};

describe('PerfilPrestadorComponent', () => {
  let component: PerfilPrestadorComponent;
  let fixture: ComponentFixture<PerfilPrestadorComponent>;
  let obterPerfilPublicoSpy: ReturnType<typeof vi.fn>;
  let navigateSpy: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    obterPerfilPublicoSpy = vi.fn().mockReturnValue(of(perfilMock));
    navigateSpy = vi.fn();

    const serviceMock = {
      obterPerfilPublico: obterPerfilPublicoSpy,
    } as unknown as PerfilPrestadorService;

    const routerMock = {
      navigate: navigateSpy,
      url: '/prestador/joao-silva-a1b2',
    } as unknown as Router;

    await TestBed.configureTestingModule({
      imports: [PerfilPrestadorComponent],
      providers: [
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'joao-silva-a1b2' } } },
        },
        { provide: Router, useValue: routerMock },
        { provide: PerfilPrestadorService, useValue: serviceMock },
        {
          provide: AuthService,
          useValue: { usuario: () => null },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PerfilPrestadorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar o componente', () => {
    expect(component).toBeTruthy();
  });

  it('deve exibir o perfil após carregar', () => {
    expect(component.perfil()).toEqual(perfilMock);
    expect(component.carregando()).toBe(false);
    expect(component.erro()).toBeNull();
  });

  it('deve exibir erro 404 quando prestador não for encontrado', () => {
    obterPerfilPublicoSpy.mockReturnValue(throwError(() => ({ status: 404 })));
    component.ngOnInit();
    expect(component.erro()).toBe('Prestador não encontrado.');
    expect(component.perfil()).toBeNull();
  });

  it('deve redirecionar para /entrar ao contratar sem autenticação', () => {
    component.contratar();
    expect(navigateSpy).toHaveBeenCalledWith(
      ['/entrar'],
      expect.objectContaining({ queryParams: expect.objectContaining({ returnUrl: expect.any(String) }) }),
    );
  });

  it('deve calcular estrelas corretamente', () => {
    expect(component.estrelas.length).toBe(5);
    // Média 4.5 → arredonda para 5 estrelas ativas
    expect(component.estralaAtiva(5)).toBe(true);
    expect(component.estralaAtiva(1)).toBe(true);
  });
});
