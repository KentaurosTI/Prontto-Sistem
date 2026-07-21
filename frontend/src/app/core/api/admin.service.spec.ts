import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AdminService } from './admin.service';
import { environment } from '../../../environments/environment';

describe('AdminService', () => {
  let servico: AdminService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiUrl}/api/admin`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });

    servico = TestBed.inject(AdminService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('deve criar o serviço', () => {
    expect(servico).toBeTruthy();
  });

  it('obterEstatisticas deve fazer GET em /stats', () => {
    const estatisticasMock = {
      usuarios: { total: 10, clientes: 7, prestadores: 3 },
      servicos: { total: 5, pendentes: 2, emAndamento: 1, concluidos: 2 },
      receita: { ganha: 500, pendente: 100, gmv: 1000 },
    };

    servico.obterEstatisticas().subscribe(dados => {
      expect(dados).toEqual(estatisticasMock);
    });

    const req = httpMock.expectOne(`${base}/stats`);
    expect(req.request.method).toBe('GET');
    req.flush(estatisticasMock);
  });

  it('listarServicos deve fazer GET em /services', () => {
    servico.listarServicos().subscribe();
    const req = httpMock.expectOne(`${base}/services`);
    expect(req.request.method).toBe('GET');
    req.flush({ services: [] });
  });

  it('listarUsuarios deve fazer GET em /users', () => {
    servico.listarUsuarios().subscribe();
    const req = httpMock.expectOne(`${base}/users`);
    expect(req.request.method).toBe('GET');
    req.flush({ users: [] });
  });

  it('atualizarStatusServico deve fazer PATCH com status correto', () => {
    const id = 'uuid-123';
    servico.atualizarStatusServico(id, 'concluido').subscribe();
    const req = httpMock.expectOne(`${base}/services/${id}`);
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ status: 'concluido' });
    req.flush({ service: {} });
  });

  it('listarCobranças deve fazer GET em /charges', () => {
    servico.listarCobranças().subscribe();
    const req = httpMock.expectOne(`${base}/charges`);
    expect(req.request.method).toBe('GET');
    req.flush({ charges: [] });
  });
});
