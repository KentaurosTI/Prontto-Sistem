import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { ServicoDetalheComponent } from './servico-detalhe.component';
import { AuthService } from '../../core/auth/auth.service';

describe('ServicoDetalheComponent', () => {
  let component: ServicoDetalheComponent;
  let fixture: ComponentFixture<ServicoDetalheComponent>;

  const mockAuthService = {
    usuario: signal(null),
  };

  const mockRouter = {
    navigate: jasmine.createSpy('navigate'),
    url: '/servicos/123',
  };

  const mockRoute = {
    snapshot: {
      paramMap: convertToParamMap({ id: '123' }),
    },
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ServicoDetalheComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockRoute },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ServicoDetalheComponent);
    component = fixture.componentInstance;
  });

  it('deve criar o componente', () => {
    expect(component).toBeTruthy();
  });

  it('deve iniciar com estado de carregamento', () => {
    expect(component.carregando()).toBeTrue();
    expect(component.servico()).toBeNull();
    expect(component.erro()).toBeNull();
  });

  it('deve iniciar com mensagens vazias', () => {
    expect(component.mensagens()).toEqual([]);
    expect(component.cobranca()).toBeNull();
  });

  it('deve iniciar signals de cursor no estado neutro (RF-06)', () => {
    expect(component.ultimoId()).toBeNull();
    expect(component.temMaisAnteriores()).toBeFalse();
  });

  it('deve iniciar com formulários de ação fechados', () => {
    expect(component.mostrarFormProposta()).toBeFalse();
    expect(component.mostrarFormDisputa()).toBeFalse();
    expect(component.mostrarFormCancelamento()).toBeFalse();
  });

  it('deve mapear status corretamente para classe CSS', () => {
    expect(component.corStatus('concluido')).toBe('badge-concluido');
    expect(component.corStatus('cancelado')).toBe('badge-cancelado');
    expect(component.corStatus('em_negociacao')).toBe('badge-negociacao');
  });

  it('deve mapear status corretamente para label', () => {
    expect(component.labelStatus('concluido')).toBe('Concluído');
    expect(component.labelStatus('em_andamento')).toBe('Em Andamento');
    expect(component.labelStatus('aguardando_pagamento')).toBe('Aguardando Pagamento');
  });
});
