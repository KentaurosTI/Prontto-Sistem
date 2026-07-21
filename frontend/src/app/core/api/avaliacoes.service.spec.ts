import { TestBed } from '@angular/core/testing';
import {
  HttpClientTestingModule,
  HttpTestingController,
} from '@angular/common/http/testing';
import { AvaliacoesService } from './avaliacoes.service';
import { Avaliacao, ResultadoListaAvaliacoes } from '../models/usuario.model';
import { environment } from '../../../environments/environment';

describe('AvaliacoesService', () => {
  let service: AvaliacoesService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiUrl}/api`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(AvaliacoesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });

  it('deve registrar avaliação com nota e comentário via POST', () => {
    const avaliacaoMock: Avaliacao = {
      id: 'aval-001',
      servicoId: 'serv-001',
      nomeAvaliador: 'João Silva',
      nota: 5,
      comentario: 'Ótimo serviço!',
      criadoEm: '2026-06-13T10:00:00Z',
    };

    service.registrar('serv-001', 5, 'Ótimo serviço!').subscribe((res) => {
      expect(res).toEqual(avaliacaoMock);
    });

    const req = httpMock.expectOne(`${base}/servicos/serv-001/avaliacoes`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ nota: 5, comentario: 'Ótimo serviço!' });
    req.flush(avaliacaoMock);
  });

  it('deve registrar avaliação sem comentário (comentário omitido no corpo)', () => {
    const avaliacaoMock: Avaliacao = {
      id: 'aval-002',
      servicoId: 'serv-002',
      nomeAvaliador: 'Maria Santos',
      nota: 4,
      criadoEm: '2026-06-13T11:00:00Z',
    };

    service.registrar('serv-002', 4).subscribe((res) => {
      expect(res).toEqual(avaliacaoMock);
    });

    const req = httpMock.expectOne(`${base}/servicos/serv-002/avaliacoes`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ nota: 4 });
    expect(req.request.body.comentario).toBeUndefined();
    req.flush(avaliacaoMock);
  });

  it('deve listar avaliações de um prestador com paginação via GET', () => {
    const resultadoMock: ResultadoListaAvaliacoes = {
      items: [
        {
          id: 'aval-010',
          servicoId: 'serv-010',
          nomeAvaliador: 'Carlos Oliveira',
          nota: 5,
          comentario: 'Excelente!',
          criadoEm: '2026-06-10T08:00:00Z',
        },
      ],
      total: 1,
      pagina: 1,
      totalPaginas: 1,
    };

    service.listarPorPrestador('prestador-slug', 1, 10).subscribe((res) => {
      expect(res.items.length).toBe(1);
      expect(res.total).toBe(1);
      expect(res.totalPaginas).toBe(1);
    });

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${base}/prestadores/prestador-slug/avaliacoes` &&
        r.params.get('page') === '1' &&
        r.params.get('pageSize') === '10'
    );
    expect(req.request.method).toBe('GET');
    req.flush(resultadoMock);
  });

  it('deve listar avaliações de um serviço via GET', () => {
    const avaliacoesMock: Avaliacao[] = [
      {
        id: 'aval-020',
        servicoId: 'serv-020',
        nomeAvaliador: 'Ana Lima',
        nota: 3,
        criadoEm: '2026-06-12T14:00:00Z',
      },
    ];

    service.listarPorServico('serv-020').subscribe((res) => {
      expect(res.length).toBe(1);
      expect(res[0].nota).toBe(3);
    });

    const req = httpMock.expectOne(`${base}/servicos/serv-020/avaliacoes`);
    expect(req.request.method).toBe('GET');
    req.flush(avaliacoesMock);
  });
});
