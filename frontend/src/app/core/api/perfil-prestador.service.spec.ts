import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PerfilPrestadorService } from './perfil-prestador.service';
import { environment } from '../../../environments/environment';
import { PerfilPublico, Categoria, Cidade, PrestadorBusca, ResultadoPaginado } from '../models/usuario.model';

const perfilMock: PerfilPublico = {
  id: 'uuid-1',
  nome: 'João Silva',
  slug: 'joao-silva-a1b2',
  fotoPerfilUrl: null,
  descricao: null,
  especialidade: 'Eletricista',
  mediaAvaliacoes: 4.5,
  totalAvaliacoes: 8,
  categorias: [],
  cidades: [],
  imagensPortfolio: [],
};

describe('PerfilPrestadorService', () => {
  let service: PerfilPrestadorService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
    });
    service = TestBed.inject(PerfilPrestadorService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });

  it('deve obter perfil público pelo slug', () => {
    service.obterPerfilPublico('joao-silva-a1b2').subscribe((p) => {
      expect(p).toEqual(perfilMock);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/v/v/joao-silva-a1b2`);
    expect(req.request.method).toBe('GET');
    req.flush(perfilMock);
  });

  it('deve listar categorias ativas', () => {
    const categorias: Categoria[] = [{ id: 'c1', nome: 'Encanador', slug: 'encanador' }];
    service.listarCategorias().subscribe((cats) => {
      expect(cats).toEqual(categorias);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/api/categorias`);
    expect(req.request.method).toBe('GET');
    req.flush(categorias);
  });

  it('deve listar cidades ativas', () => {
    const cidades: Cidade[] = [{ id: 'd1', nome: 'Itapevi', estado: 'SP', slug: 'itapevi' }];
    service.listarCidades().subscribe((cs) => {
      expect(cs).toEqual(cidades);
    });

    const req = httpMock.expectOne(`${environment.apiUrl}/api/cidades`);
    expect(req.request.method).toBe('GET');
    req.flush(cidades);
  });

  it('deve atualizar o perfil do prestador autenticado', () => {
    service
      .atualizarPerfil({ nome: 'Novo Nome', categoriaIds: ['c1'], cidadeIds: ['d1'] })
      .subscribe((res) => {
        expect(res.perfil.nome).toBe('Novo Nome');
      });

    const req = httpMock.expectOne(`${environment.apiUrl}/api/auth/perfil`);
    expect(req.request.method).toBe('PUT');
    req.flush({ perfil: { ...perfilMock, nome: 'Novo Nome' } });
  });

  it('deve buscar prestadores por categoria sem cidade', () => {
    const resultado: ResultadoPaginado<PrestadorBusca> = {
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    };

    service.buscarPrestadores('encanador').subscribe((res) => {
      expect(res).toEqual(resultado);
    });

    const req = httpMock.expectOne(
      (r) => r.url === `${environment.apiUrl}/api/prestadores` &&
        r.params.get('categoriaSlug') === 'encanador' &&
        !r.params.has('cidadeSlug'),
    );
    expect(req.request.method).toBe('GET');
    req.flush(resultado);
  });

  it('deve buscar prestadores por categoria e cidade', () => {
    const resultado: ResultadoPaginado<PrestadorBusca> = {
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 20,
    };

    service.buscarPrestadores('eletricista', 'itapevi', 2, 20).subscribe((res) => {
      expect(res).toEqual(resultado);
    });

    const req = httpMock.expectOne(
      (r) =>
        r.url === `${environment.apiUrl}/api/prestadores` &&
        r.params.get('categoriaSlug') === 'eletricista' &&
        r.params.get('cidadeSlug') === 'itapevi' &&
        r.params.get('page') === '2',
    );
    expect(req.request.method).toBe('GET');
    req.flush(resultado);
  });
});
