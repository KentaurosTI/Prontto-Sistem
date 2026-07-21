import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ActivatedRoute } from '@angular/router';
import { of, throwError } from 'rxjs';

import { CriarServicoComponent } from './criar-servico.component';
import { ServicosService } from '../../../core/api/servicos.service';
import { PerfilPrestadorService } from '../../../core/api/perfil-prestador.service';
import { AuthService } from '../../../core/auth/auth.service';
import { signal } from '@angular/core';

describe('CriarServicoComponent', () => {
  let component: CriarServicoComponent;
  let fixture: ComponentFixture<CriarServicoComponent>;
  let servicosServiceSpy: jasmine.SpyObj<ServicosService>;
  let perfilServiceSpy: jasmine.SpyObj<PerfilPrestadorService>;
  let authServiceSpy: { usuario: ReturnType<typeof signal> };

  beforeEach(async () => {
    servicosServiceSpy = jasmine.createSpyObj('ServicosService', ['criarSolicitacao']);
    perfilServiceSpy = jasmine.createSpyObj('PerfilPrestadorService', [
      'listarCategorias',
      'listarCidades',
    ]);
    authServiceSpy = { usuario: signal(null) };

    perfilServiceSpy.listarCategorias.and.returnValue(
      of([{ id: 'cat-1', nome: 'Elétrica', slug: 'eletrica' }])
    );
    perfilServiceSpy.listarCidades.and.returnValue(
      of([{ id: 'cid-1', nome: 'São Paulo', estado: 'SP', slug: 'sao-paulo' }])
    );

    await TestBed.configureTestingModule({
      imports: [
        CriarServicoComponent,
        ReactiveFormsModule,
        RouterTestingModule,
        HttpClientTestingModule,
      ],
      providers: [
        { provide: ServicosService, useValue: servicosServiceSpy },
        { provide: PerfilPrestadorService, useValue: perfilServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              queryParamMap: {
                get: (key: string) => null,
              },
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CriarServicoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar o componente', () => {
    expect(component).toBeTruthy();
  });

  it('deve carregar categorias e cidades na inicialização', () => {
    expect(perfilServiceSpy.listarCategorias).toHaveBeenCalled();
    expect(perfilServiceSpy.listarCidades).toHaveBeenCalled();
    expect(component.categorias().length).toBe(1);
    expect(component.cidades().length).toBe(1);
  });

  it('deve marcar formulário como inválido quando vazio', () => {
    component.submeter();
    expect(component.formulario.invalid).toBeTrue();
  });

  it('deve marcar campos obrigatórios como tocados ao submeter sem preencher', () => {
    component.submeter();
    expect(component.campoInvalido('titulo')).toBeTrue();
    expect(component.campoInvalido('categoriaId')).toBeTrue();
  });

  it('deve iniciar enviando=false', () => {
    expect(component.enviando()).toBeFalse();
  });

  it('deve detectar campo inválido corretamente', () => {
    const campo = component.formulario.get('titulo')!;
    campo.markAsTouched();
    expect(component.campoInvalido('titulo')).toBeTrue();
  });

  it('deve exibir erro ao falhar ao criar solicitação', () => {
    servicosServiceSpy.criarSolicitacao.and.returnValue(
      throwError(() => ({ error: { mensagem: 'Erro de servidor' } }))
    );

    component.formulario.patchValue({
      titulo: 'Instalação elétrica completa',
      descricao: 'Preciso instalar tomadas e disjuntores na casa',
      categoriaId: 'cat-1',
    });

    component.submeter();

    expect(component.erro()).toBe('Erro de servidor');
    expect(component.enviando()).toBeFalse();
  });

  it('deve exibir data mínima de hoje no campo agendadoEm', () => {
    const hoje = new Date().toISOString().split('T')[0];
    expect(component.hoje).toBe(hoje);
  });
});
