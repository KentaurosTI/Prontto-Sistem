import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { ComoFuncionaComponent } from './como-funciona.component';

describe('ComoFuncionaComponent', () => {
  let componente: ComoFuncionaComponent;
  let fixture: ComponentFixture<ComoFuncionaComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ComoFuncionaComponent, RouterTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(ComoFuncionaComponent);
    componente = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar o componente', () => {
    expect(componente).toBeTruthy();
  });

  it('deve exibir 4 passos', () => {
    expect(componente.passos.length).toBe(4);
    const elemento: HTMLElement = fixture.nativeElement;
    expect(elemento.querySelectorAll('.passo').length).toBe(4);
  });
});
