import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let servico: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });

    servico = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('deve criar o serviço', () => {
    expect(servico).toBeTruthy();
  });

  it('deve iniciar sem usuário autenticado', () => {
    expect(servico.estaAutenticado()).toBe(false);
    expect(servico.usuario()).toBeNull();
  });

  it('deve identificar admin corretamente', () => {
    expect(servico.ehAdmin()).toBe(false);
  });

  it('deve limpar sessão ao sair', () => {
    servico.sair();
    expect(servico.estaAutenticado()).toBe(false);
    expect(servico.usuario()).toBeNull();
    expect(localStorage.getItem('prontto_token')).toBeNull();
    expect(localStorage.getItem('prontto_usuario')).toBeNull();
  });

  it('obterToken deve retornar null sem sessão ativa', () => {
    expect(servico.obterToken()).toBeNull();
  });
});
