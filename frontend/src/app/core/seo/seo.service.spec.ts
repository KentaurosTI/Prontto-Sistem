import { TestBed } from '@angular/core/testing';
import { Title, Meta } from '@angular/platform-browser';
import { SeoService } from './seo.service';

describe('SeoService', () => {
  let service: SeoService;
  let titleService: Title;
  let metaService: Meta;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SeoService, Title, Meta],
    });
    service = TestBed.inject(SeoService);
    titleService = TestBed.inject(Title);
    metaService = TestBed.inject(Meta);
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });

  it('deve atualizar o título com sufixo "| Prontto"', () => {
    service.atualizarSeo({
      titulo: 'Encontre Prestadores de Serviços',
      descricao: 'Prontto conecta você com prestadores.',
    });

    expect(titleService.getTitle()).toBe('Encontre Prestadores de Serviços | Prontto');
  });

  it('deve atualizar a meta description', () => {
    const descricao = 'Prontto conecta você com prestadores de serviços domésticos de confiança.';
    service.atualizarSeo({ titulo: 'Home', descricao });

    const tag = metaService.getTag('name="description"');
    expect(tag?.content).toBe(descricao);
  });

  it('deve atualizar as meta tags Open Graph', () => {
    service.atualizarSeo({
      titulo: 'Serviços',
      descricao: 'Explore prestadores na sua cidade.',
      url: 'https://prontto.org/servicos',
    });

    const ogTitle = metaService.getTag('property="og:title"');
    const ogDesc = metaService.getTag('property="og:description"');
    const ogUrl = metaService.getTag('property="og:url"');

    expect(ogTitle?.content).toBe('Serviços | Prontto');
    expect(ogDesc?.content).toBe('Explore prestadores na sua cidade.');
    expect(ogUrl?.content).toBe('https://prontto.org/servicos');
  });

  it('deve atualizar as meta tags Twitter Card', () => {
    service.atualizarSeo({
      titulo: 'Como Funciona',
      descricao: 'Veja como funciona a Prontto.',
    });

    const twitterCard = metaService.getTag('name="twitter:card"');
    const twitterTitle = metaService.getTag('name="twitter:title"');

    expect(twitterCard?.content).toBe('summary_large_image');
    expect(twitterTitle?.content).toBe('Como Funciona | Prontto');
  });

  it('deve definir og:image e twitter:image quando imagem é fornecida', () => {
    const imagem = 'https://prontto.org/assets/og-image.png';
    service.atualizarSeo({
      titulo: 'Prestador',
      descricao: 'Perfil do prestador.',
      imagem,
    });

    const ogImage = metaService.getTag('property="og:image"');
    const twitterImage = metaService.getTag('name="twitter:image"');

    expect(ogImage?.content).toBe(imagem);
    expect(twitterImage?.content).toBe(imagem);
  });
});
