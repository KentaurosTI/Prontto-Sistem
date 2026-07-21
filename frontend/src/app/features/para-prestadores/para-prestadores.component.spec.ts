import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { ParaPrestadoresComponent } from './para-prestadores.component';

describe('ParaPrestadoresComponent', () => {
  let componente: ParaPrestadoresComponent;
  let fixture: ComponentFixture<ParaPrestadoresComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ParaPrestadoresComponent, RouterTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(ParaPrestadoresComponent);
    componente = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar o componente', () => {
    expect(componente).toBeTruthy();
  });

  it('deve exibir 4 benefícios', () => {
    expect(componente.beneficios.length).toBe(4);
  });
});
