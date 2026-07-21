import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { EntrarComponent } from './entrar.component';

describe('EntrarComponent', () => {
  let componente: EntrarComponent;
  let fixture: ComponentFixture<EntrarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EntrarComponent, ReactiveFormsModule, RouterTestingModule, HttpClientTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(EntrarComponent);
    componente = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar o componente', () => {
    expect(componente).toBeTruthy();
  });

  it('formulário deve ser inválido quando vazio', () => {
    expect(componente.formulario.invalid).toBe(true);
  });

  it('formulário deve ser inválido com e-mail incorreto', () => {
    componente.formulario.patchValue({ email: 'nao-e-email', senha: 'senha123' });
    expect(componente.formulario.invalid).toBe(true);
  });

  it('formulário deve ser válido com dados corretos', () => {
    componente.formulario.patchValue({ email: 'test@test.com', senha: 'senha123' });
    expect(componente.formulario.valid).toBe(true);
  });

  it('senha menor que 8 caracteres deve ser inválida', () => {
    componente.formulario.patchValue({ email: 'test@test.com', senha: '1234' });
    expect(componente.formulario.get('senha')?.invalid).toBe(true);
  });
});
