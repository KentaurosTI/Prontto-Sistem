import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { AdminComponent } from './admin.component';

describe('AdminComponent', () => {
  let componente: AdminComponent;
  let fixture: ComponentFixture<AdminComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminComponent, HttpClientTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminComponent);
    componente = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar o componente', () => {
    expect(componente).toBeTruthy();
  });

  it('aba padrão deve ser estatísticas', () => {
    expect(componente.abaSelecionada()).toBe('stats');
  });

  it('rotularStatus deve retornar rótulo correto', () => {
    expect(componente.rotularStatus('aguardando_pagamento')).toBe('Aguardando pagamento');
    expect(componente.rotularStatus('em_andamento')).toBe('Em andamento');
    expect(componente.rotularStatus('concluido')).toBe('Concluído');
    expect(componente.rotularStatus('cancelado')).toBe('Cancelado');
  });

  it('deve trocar de aba ao clicar', () => {
    componente.abaSelecionada.set('servicos');
    expect(componente.abaSelecionada()).toBe('servicos');
  });
});
