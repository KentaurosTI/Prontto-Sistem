import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { RouterTestingModule } from '@angular/router/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { CadastrarComponent } from './cadastrar.component';

describe('CadastrarComponent', () => {
  let componente: CadastrarComponent;
  let fixture: ComponentFixture<CadastrarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CadastrarComponent, ReactiveFormsModule, RouterTestingModule, HttpClientTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(CadastrarComponent);
    componente = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar o componente', () => {
    expect(componente).toBeTruthy();
  });

  it('formulário deve ser inválido quando vazio', () => {
    expect(componente.formulario.invalid).toBe(true);
  });

  it('formulário deve ser válido com dados mínimos', () => {
    componente.formulario.patchValue({ nome: 'João Silva', email: 'joao@test.com', senha: 'senha123', tipoConta: 'cliente' });
    expect(componente.formulario.valid).toBe(true);
  });

  it('prestador deve exibir campo de especialidade', () => {
    componente.ehPrestador.set(true);
    fixture.detectChanges();
    const elemento: HTMLElement = fixture.nativeElement;
    expect(elemento.querySelector('#especialidade')).toBeTruthy();
  });

  it('cliente não deve exibir campo de especialidade', () => {
    componente.ehPrestador.set(false);
    fixture.detectChanges();
    const elemento: HTMLElement = fixture.nativeElement;
    expect(elemento.querySelector('#especialidade')).toBeNull();
  });
});
