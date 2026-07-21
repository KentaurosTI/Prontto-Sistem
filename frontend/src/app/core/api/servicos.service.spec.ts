import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ServicosService } from './servicos.service';
import { ResultadoMensagens } from '../models/usuario.model';

describe('ServicosService', () => {
  let service: ServicosService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ServicosService,
      ],
    });
    service = TestBed.inject(ServicosService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });

  describe('listarMensagens (RF-06)', () => {
    const mockResposta: ResultadoMensagens = {
      mensagens: [],
      temMais: false,
      ultimoId: null,
    };

    it('deve chamar endpoint sem afterId quando não informado', () => {
      service.listarMensagens('abc').subscribe();
      const req = httpMock.expectOne('/api/servicos/abc/mensagens?limite=50');
      expect(req.request.method).toBe('GET');
      req.flush(mockResposta);
    });

    it('deve incluir afterId na query string quando informado', () => {
      const cursor = '550e8400-e29b-41d4-a716-446655440000';
      service.listarMensagens('abc', cursor).subscribe();
      const req = httpMock.expectOne(
        `/api/servicos/abc/mensagens?limite=50&afterId=${cursor}`
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockResposta);
    });

    it('deve respeitar limite customizado', () => {
      service.listarMensagens('abc', undefined, 20).subscribe();
      const req = httpMock.expectOne('/api/servicos/abc/mensagens?limite=20');
      expect(req.request.method).toBe('GET');
      req.flush(mockResposta);
    });

    it('deve retornar ResultadoMensagens com temMais e ultimoId', () => {
      const respostaComMais: ResultadoMensagens = {
        mensagens: [
          {
            id: 'msg-1',
            servicoId: 'abc',
            remetenteId: 'user-1',
            remetenteNome: 'Joao',
            papelRemetente: 'cliente',
            tipoMensagem: 'texto',
            conteudo: 'Ola',
            imagemModerada: false,
            criadoEm: '2026-06-14T10:00:00Z',
          },
        ],
        temMais: true,
        ultimoId: 'msg-1',
      };

      let resultado: ResultadoMensagens | undefined;
      service.listarMensagens('abc').subscribe((r) => (resultado = r));

      const req = httpMock.expectOne('/api/servicos/abc/mensagens?limite=50');
      req.flush(respostaComMais);

      expect(resultado).toBeDefined();
      expect(resultado!.temMais).toBeTrue();
      expect(resultado!.ultimoId).toBe('msg-1');
      expect(resultado!.mensagens.length).toBe(1);
    });
  });
});
