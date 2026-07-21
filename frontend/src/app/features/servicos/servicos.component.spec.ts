import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, ActivatedRoute } from '@angular/router';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { ServicosComponent } from './servicos.component';
import { PerfilPrestadorService } from '../../core/api/perfil-prestador.service';
import { Categoria, Cidade, PrestadorBusca, ResultadoPaginado } from '../../core/models/usuario.model';

const categoriasMock: Categoria[] = [
  { id: 'c1', nome: 'Encanador', slug: 'encanador' },
  { id: 'c2', nome: 'Eletricista', slug: 'eletricista' },
];

const cidadesMock: Cidade[] = [
  { id: 'd1', nome: 'Itapevi', estado: 'SP', slug: 'itapevi' },
];

const prestadorMock: PrestadorBusca = {
  id: 'p1',
  nome: 'Carlos Silva',
  fotoPerfilUrl: null,
  slug: 'carlos-silva-ab12',
  mediaAvaliacoes: 4.5,
  totalAvaliacoes: 10,
  categorias: [{ id: 'c1', nome: 'Encanador', slug: 'encanador' }],
  cidades: [{ id: 'd1', nome: 'Itapevi', estado: 'SP', slug: 'itapevi' }],
};

const resultadoMock: ResultadoPaginado<PrestadorBusca> = {
  items: [prestadorMock],
  totalCount: 1,
  page: 1,
  pageSize: 20,
};

const resultadoVazioMock: ResultadoPaginado<PrestadorBusca> = {
  items: [],
  totalCount: 0,
  page: 1,
  pageSize: 20,
};

function criarServiceMock(overrides?: Partial<PerfilPrestadorService>): PerfilPrestadorService {
  return {
    listarCategorias: vi.fn().mockReturnValue(of(categoriasMock)),
    listarCidades: vi.fn().mockReturnValue(of(cidadesMock)),
    buscarPrestadores: vi.fn().mockReturnValue(of(resultadoMock)),
    obterPerfilPublico: vi.fn(),
    atualizarPerfil: vi.fn(),
    adicionarImagem: vi.fn(),
    removerImagem: vi.fn(),
    ...overrides,
  } as unknown as PerfilPrestadorService;
}

function criarFixture(
  queryParams: Record<string, string> = {},
  serviceMock?: PerfilPrestadorService,
): { fixture: ComponentFixture<ServicosComponent>; componente: ServicosComponent; mock: PerfilPrestadorService } {
  const mock = serviceMock ?? criarServiceMock();

  TestBed.configureTestingModule({
    imports: [ServicosComponent, HttpClientTestingModule],
    providers: [
      provideRouter([]),
      { provide: PerfilPrestadorService, useValue: mock },
      {
        provide: ActivatedRoute,
        useValue: { queryParams: of(queryParams) },
      },
    ],
  });

  const fixture = TestBed.createComponent(ServicosComponent);
  const componente = fixture.componentInstance;
  // detectChanges aciona ngOnInit; como os observables usam `of()`, emitem sincronamente
  fixture.detectChanges();

  return { fixture, componente, mock };
}

describe('ServicosComponent', () => {
  afterEach(() => TestBed.resetTestingModule());

  // ── Grade de categorias ────────────────────────────────────────────────────

  it('deve criar o componente', () => {
    const { componente } = criarFixture();
    expect(componente).toBeTruthy();
  });

  it('deve carregar categorias da API ao inicializar', () => {
    const { componente, mock } = criarFixture();
    expect(mock.listarCategorias).toHaveBeenCalled();
    expect(componente.categorias().length).toBe(2);
  });

  it('deve exibir grade de categorias quando não há categoria selecionada', () => {
    const { fixture } = criarFixture();
    fixture.detectChanges();

    const elemento: HTMLElement = fixture.nativeElement;
    const cartoes = elemento.querySelectorAll('.card-categoria');
    expect(cartoes.length).toBe(2);
  });

  it('deve retornar ícone emoji para slugs conhecidos', () => {
    const { componente } = criarFixture();
    expect(componente.iconeCategoria('encanador')).toBe('🔧');
    expect(componente.iconeCategoria('eletricista')).toBe('⚡');
  });

  it('deve retornar ícone padrão para slugs desconhecidos', () => {
    const { componente } = criarFixture();
    expect(componente.iconeCategoria('desconhecido')).toBe('🔨');
  });

  // ── Busca de prestadores ───────────────────────────────────────────────────

  it('deve executar busca quando query param categoria está presente', () => {
    const { mock } = criarFixture({ categoria: 'encanador' });
    expect(mock.buscarPrestadores).toHaveBeenCalledWith('encanador', undefined, 1, 20);
  });

  it('deve passar cidadeSlug na busca quando fornecida', () => {
    const { mock } = criarFixture({ categoria: 'encanador', cidade: 'itapevi' });
    expect(mock.buscarPrestadores).toHaveBeenCalledWith('encanador', 'itapevi', 1, 20);
  });

  it('deve exibir prestadores retornados pela API', () => {
    const { componente } = criarFixture({ categoria: 'encanador' });
    expect(componente.prestadores().length).toBe(1);
    expect(componente.prestadores()[0].nome).toBe('Carlos Silva');
  });

  it('deve exibir lista vazia quando nenhum prestador é encontrado', () => {
    const mock = criarServiceMock({
      buscarPrestadores: vi.fn().mockReturnValue(of(resultadoVazioMock)),
    });
    const { componente } = criarFixture({ categoria: 'pintor' }, mock);
    expect(componente.prestadores().length).toBe(0);
    expect(componente.totalCount()).toBe(0);
  });

  it('deve ativar erroCategoria ao receber erro 404 da busca', () => {
    const mock = criarServiceMock({
      buscarPrestadores: vi.fn().mockReturnValue(throwError(() => ({ status: 404 }))),
    });
    const { componente } = criarFixture({ categoria: 'invalida' }, mock);
    expect(componente.erroCategoria()).toBe(true);
    expect(componente.prestadores().length).toBe(0);
  });

  it('deve não executar busca quando categoria está ausente dos query params', () => {
    const { mock } = criarFixture({});
    expect(mock.buscarPrestadores).not.toHaveBeenCalled();
  });

  // ── Paginação ──────────────────────────────────────────────────────────────

  it('deve calcular totalPaginas corretamente para 45 resultados', () => {
    const resultado45: ResultadoPaginado<PrestadorBusca> = {
      items: [],
      totalCount: 45,
      page: 1,
      pageSize: 20,
    };
    const mock = criarServiceMock({
      buscarPrestadores: vi.fn().mockReturnValue(of(resultado45)),
    });
    const { componente } = criarFixture({ categoria: 'encanador' }, mock);
    expect(componente.totalPaginas()).toBe(3);
  });

  it('temPaginaAnterior deve ser falso na primeira página', () => {
    const { componente } = criarFixture({ categoria: 'encanador' });
    expect(componente.temPaginaAnterior()).toBe(false);
  });

  it('temProximaPagina deve ser verdadeiro quando há mais páginas', () => {
    const resultadoMultiplas: ResultadoPaginado<PrestadorBusca> = {
      items: [prestadorMock],
      totalCount: 50,
      page: 1,
      pageSize: 20,
    };
    const mock = criarServiceMock({
      buscarPrestadores: vi.fn().mockReturnValue(of(resultadoMultiplas)),
    });
    const { componente } = criarFixture({ categoria: 'encanador' }, mock);
    expect(componente.temProximaPagina()).toBe(true);
  });

  // ── Utilitários ────────────────────────────────────────────────────────────

  it('deve formatar estrelas corretamente', () => {
    const { componente } = criarFixture();
    expect(componente.formatarEstrelas(4)).toBe('★★★★☆');
    expect(componente.formatarEstrelas(5)).toBe('★★★★★');
    expect(componente.formatarEstrelas(0)).toBe('☆☆☆☆☆');
  });

  it('deve formatar lista de múltiplas cidades', () => {
    const { componente } = criarFixture();
    const prestadorMultiCidades = {
      ...prestadorMock,
      cidades: [
        { id: 'd1', nome: 'Itapevi', estado: 'SP', slug: 'itapevi' },
        { id: 'd2', nome: 'Osasco', estado: 'SP', slug: 'osasco' },
      ],
    };
    expect(componente.nomeCidades(prestadorMultiCidades)).toBe('Itapevi, Osasco');
  });

  it('deve retornar "—" quando prestador não tem cidades', () => {
    const { componente } = criarFixture();
    expect(componente.nomeCidades({ ...prestadorMock, cidades: [] })).toBe('—');
  });

  // ── Estado de carregamento ─────────────────────────────────────────────────

  it('carregandoCategorias deve ser false após receber resposta da API', () => {
    const { componente } = criarFixture();
    // Após detectChanges + resposta síncrona do `of()`, carregandoCategorias deve ser false
    expect(componente.carregandoCategorias()).toBe(false);
  });
});
