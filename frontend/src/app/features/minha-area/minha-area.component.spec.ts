import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { RouterTestingModule } from '@angular/router/testing';
import { signal } from '@angular/core';
import { MinhaAreaComponent } from './minha-area.component';
import { AuthService } from '../../core/auth/auth.service';
import { BankingService } from '../../core/api/banking.service';
import { PerfilPrestadorService } from '../../core/api/perfil-prestador.service';
import { of, throwError } from 'rxjs';

describe('MinhaAreaComponent', () => {
  let componente: MinhaAreaComponent;
  let fixture: ComponentFixture<MinhaAreaComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MinhaAreaComponent, ReactiveFormsModule, HttpClientTestingModule, RouterTestingModule],
    }).compileComponents();

    fixture = TestBed.createComponent(MinhaAreaComponent);
    componente = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar o componente', () => {
    expect(componente).toBeTruthy();
  });

  it('deve exibir a sidebar', () => {
    const elemento: HTMLElement = fixture.nativeElement;
    expect(elemento.querySelector('.sidebar')).toBeTruthy();
  });

  it('deve exibir abas mobile', () => {
    const elemento: HTMLElement = fixture.nativeElement;
    expect(elemento.querySelector('.abas-mobile')).toBeTruthy();
  });

  it('deve iniciar na aba de serviços para clientes', () => {
    expect(componente.abaAtiva()).toBe('servicos');
  });
});

describe('MinhaAreaComponent — aba portfólio', () => {
  let componente: MinhaAreaComponent;
  let fixture: ComponentFixture<MinhaAreaComponent>;
  let perfilService: jasmine.SpyObj<PerfilPrestadorService>;

  beforeEach(async () => {
    const perfilSpy = jasmine.createSpyObj('PerfilPrestadorService', [
      'uploadImagem',
      'removerImagem',
      'obterPerfilPublico',
      'obterMinhasImagens',
      'listarCategorias',
      'listarCidades',
    ]);
    perfilSpy.listarCategorias.and.returnValue(of([]));
    perfilSpy.listarCidades.and.returnValue(of([]));
    perfilSpy.obterMinhasImagens.and.returnValue(of({ imagens: [] }));
    perfilSpy.obterPerfilPublico.and.returnValue(
      of({ id: '1', nome: 'Test', imagensPortfolio: [], mediaAvaliacoes: 0, totalAvaliacoes: 0, categorias: [], cidades: [] })
    );

    const authSpy = jasmine.createSpyObj('AuthService', ['sair'], {
      usuario: signal({
        id: '1',
        nome: 'Teste',
        email: 'teste@teste.com',
        tipoConta: 'prestador',
        papel: 'usuario',
        mediaAvaliacoes: 0,
        totalAvaliacoes: 0,
        criadoEm: '2024-01-01',
        slug: 'teste-prestador',
      }),
      estaAutenticado: signal(true),
      ehAdmin: signal(false),
    });

    await TestBed.configureTestingModule({
      imports: [MinhaAreaComponent, ReactiveFormsModule, HttpClientTestingModule, RouterTestingModule],
      providers: [
        { provide: AuthService, useValue: authSpy },
        { provide: PerfilPrestadorService, useValue: perfilSpy },
      ],
    }).compileComponents();

    perfilService = TestBed.inject(PerfilPrestadorService) as jasmine.SpyObj<PerfilPrestadorService>;

    fixture = TestBed.createComponent(MinhaAreaComponent);
    componente = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve iniciar com imagens vazia e uploadEmAndamento false', () => {
    expect(componente.imagens()).toEqual([]);
    expect(componente.uploadEmAndamento()).toBeFalse();
    expect(componente.erroUpload()).toBeNull();
    expect(componente.previewUrl()).toBeNull();
  });

  it('deve incluir "portfolio" no union type da aba ativa', () => {
    componente.mudarAba('portfolio');
    expect(componente.abaAtiva()).toBe('portfolio');
  });

  it('deve rejeitar arquivo muito grande e definir erroUpload', () => {
    const arquivoGrande = new File(['x'.repeat(6 * 1024 * 1024)], 'grande.jpg', { type: 'image/jpeg' });
    const eventoFake = { target: { files: [arquivoGrande], value: '' } } as unknown as Event;
    componente.onArquivoSelecionado(eventoFake);
    expect(componente.erroUpload()).toBe('Arquivo muito grande. O limite é 5 MB.');
    expect(componente.previewUrl()).toBeNull();
  });

  it('deve rejeitar tipo de arquivo não permitido', () => {
    const arquivoInvalido = new File(['data'], 'foto.gif', { type: 'image/gif' });
    const eventoFake = { target: { files: [arquivoInvalido], value: '' } } as unknown as Event;
    componente.onArquivoSelecionado(eventoFake);
    expect(componente.erroUpload()).toBe('Tipo de arquivo não permitido. Use JPG, PNG ou WebP.');
  });

  it('deve adicionar imagem à lista após upload bem-sucedido', () => {
    const respostaUpload = { id: 'img-1', url: '/uploads/2024/01/foto.jpg' };
    perfilService.uploadImagem.and.returnValue(of(respostaUpload));

    const arquivoFake = new File(['data'], 'foto.jpg', { type: 'image/jpeg' });
    componente.arquivoSelecionado.set(arquivoFake);
    componente.uploadImagem();

    expect(perfilService.uploadImagem).toHaveBeenCalledWith(arquivoFake, 0);
    expect(componente.imagens().length).toBe(1);
    expect(componente.imagens()[0].id).toBe('img-1');
    expect(componente.previewUrl()).toBeNull();
    expect(componente.uploadEmAndamento()).toBeFalse();
  });

  it('deve definir erroUpload quando upload falha com 400', () => {
    perfilService.uploadImagem.and.returnValue(throwError(() => ({ status: 400 })));

    const arquivoFake = new File(['data'], 'foto.jpg', { type: 'image/jpeg' });
    componente.arquivoSelecionado.set(arquivoFake);
    componente.uploadImagem();

    expect(componente.erroUpload()).toBe('Arquivo inválido. Verifique o tipo e o tamanho (máx 5 MB).');
    expect(componente.uploadEmAndamento()).toBeFalse();
  });

  it('deve remover imagem da lista local após DELETE bem-sucedido', () => {
    componente.imagens.set([
      { id: 'img-1', url: '/uploads/2024/01/foto1.jpg', ordem: 0 },
      { id: 'img-2', url: '/uploads/2024/01/foto2.jpg', ordem: 1 },
    ]);
    perfilService.removerImagem.and.returnValue(of(undefined));

    componente.removerImagem('img-1');

    expect(componente.imagens().length).toBe(1);
    expect(componente.imagens()[0].id).toBe('img-2');
  });

  it('não deve remover imagem da lista quando DELETE falha', () => {
    componente.imagens.set([{ id: 'img-1', url: '/uploads/foto.jpg', ordem: 0 }]);
    perfilService.removerImagem.and.returnValue(throwError(() => ({ status: 500 })));

    componente.removerImagem('img-1');

    expect(componente.imagens().length).toBe(1);
  });

  it('deve chamar carregarImagens ao mudar para aba portfolio quando lista vazia', () => {
    spyOn(componente, 'carregarImagens');
    componente.mudarAba('portfolio');
    expect(componente.carregarImagens).toHaveBeenCalled();
  });
});

describe('MinhaAreaComponent — banking erro em vermelho (SCRUM-9)', () => {
  let componente: MinhaAreaComponent;
  let fixture: ComponentFixture<MinhaAreaComponent>;
  let bankingService: jasmine.SpyObj<BankingService>;

  beforeEach(async () => {
    const bankingSpy = jasmine.createSpyObj('BankingService', ['obterDadosBancarios', 'salvarDadosBancarios']);
    bankingSpy.obterDadosBancarios.and.returnValue(of({ banking: null }));

    const perfilSpy = jasmine.createSpyObj('PerfilPrestadorService', [
      'listarCategorias', 'listarCidades', 'obterPerfilPublico',
    ]);
    perfilSpy.listarCategorias.and.returnValue(of([]));
    perfilSpy.listarCidades.and.returnValue(of([]));
    perfilSpy.obterPerfilPublico.and.returnValue(
      of({ id: '1', nome: 'T', imagensPortfolio: [], mediaAvaliacoes: 0, totalAvaliacoes: 0, categorias: [], cidades: [] })
    );

    const authSpy = jasmine.createSpyObj('AuthService', ['sair'], {
      usuario: signal({
        id: '1', nome: 'T', email: 't@t.com', tipoConta: 'prestador',
        papel: 'usuario', mediaAvaliacoes: 0, totalAvaliacoes: 0,
        criadoEm: '2024-01-01', slug: 'slug-t',
      }),
      estaAutenticado: signal(true),
      ehAdmin: signal(false),
    });

    await TestBed.configureTestingModule({
      imports: [MinhaAreaComponent, ReactiveFormsModule, HttpClientTestingModule, RouterTestingModule],
      providers: [
        { provide: AuthService, useValue: authSpy },
        { provide: BankingService, useValue: bankingSpy },
        { provide: PerfilPrestadorService, useValue: perfilSpy },
      ],
    }).compileComponents();

    bankingService = TestBed.inject(BankingService) as jasmine.SpyObj<BankingService>;
    fixture = TestBed.createComponent(MinhaAreaComponent);
    componente = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve iniciar erroBanking como null', () => {
    expect(componente.erroBanking()).toBeNull();
  });

  it('deve definir mensagem de sucesso e limpar erroBanking ao salvar com sucesso', () => {
    const banking = { id: '1', usuarioId: '1', tipoChavePix: 'cpf', chavePix: '123', nomeCompleto: 'T', cpfCnpj: '11111111111', criadoEm: '', atualizadoEm: '' } as any;
    bankingService.salvarDadosBancarios.and.returnValue(of({ banking }));

    componente.salvarBanking();

    expect(componente.mensagem()).toBe('Dados bancários salvos com sucesso!');
    expect(componente.erroBanking()).toBeNull();
  });

  it('deve definir erroBanking em vermelho e limpar mensagem ao salvar com erro', () => {
    bankingService.salvarDadosBancarios.and.returnValue(throwError(() => ({ status: 400, error: { error: 'CPF inválido' } })));

    componente.salvarBanking();

    expect(componente.erroBanking()).toContain('CPF inválido');
    expect(componente.mensagem()).toBeNull();
  });
});
