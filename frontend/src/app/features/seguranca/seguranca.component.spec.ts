import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SegurancaComponent } from './seguranca.component';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';

describe('SegurancaComponent', () => {
  let component: SegurancaComponent;
  let fixture: ComponentFixture<SegurancaComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SegurancaComponent],
      providers: [provideRouter([]), provideHttpClient()],
    }).compileComponents();

    fixture = TestBed.createComponent(SegurancaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('deve ter 4 itens de FAQ por padrão', () => {
    expect(component.faqItems().length).toBe(4);
  });

  it('deve alternar abertura do FAQ', () => {
    expect(component.faqItems()[0].aberta).toBeFalse();
    component.alternarFaq(0);
    expect(component.faqItems()[0].aberta).toBeTrue();
    component.alternarFaq(0);
    expect(component.faqItems()[0].aberta).toBeFalse();
  });

  it('deve fechar outros itens ao abrir um FAQ', () => {
    component.alternarFaq(0);
    component.alternarFaq(1);
    expect(component.faqItems()[0].aberta).toBeFalse();
    expect(component.faqItems()[1].aberta).toBeTrue();
  });
});
