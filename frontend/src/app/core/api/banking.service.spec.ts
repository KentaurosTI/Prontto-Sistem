import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { BankingService } from './banking.service';
import { environment } from '../../../environments/environment';

describe('BankingService', () => {
  let servico: BankingService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiUrl}/api/auth`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });

    servico = TestBed.inject(BankingService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('deve criar o serviço', () => {
    expect(servico).toBeTruthy();
  });

  it('obterDadosBancarios deve fazer GET em /banking', () => {
    servico.obterDadosBancarios().subscribe();
    const req = httpMock.expectOne(`${base}/banking`);
    expect(req.request.method).toBe('GET');
    req.flush({ banking: null });
  });

  it('salvarDadosBancarios deve fazer POST em /banking com payload mapeado', () => {
    const dados = {
      tipoChavePix: 'cpf' as const,
      chavePix: '123.456.789-00',
      nomeCompleto: 'João Silva',
      cpfCnpj: '12345678900',
      nomeBanco: 'Nubank',
      agencia: '0001',
      numeroConta: '123456-7',
      tipoConta: 'corrente',
    };

    servico.salvarDadosBancarios(dados).subscribe();

    const req = httpMock.expectOne(`${base}/banking`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      pixKeyType: 'cpf',
      pixKey: '123.456.789-00',
      fullName: 'João Silva',
      cpfCnpj: '12345678900',
      bankName: 'Nubank',
      agency: '0001',
      accountNumber: '123456-7',
      bankAccountType: 'corrente',
    });

    req.flush({ banking: {} });
  });
});
