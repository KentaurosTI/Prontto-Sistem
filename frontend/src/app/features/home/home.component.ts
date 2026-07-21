import {
  Component,
  OnInit,
  AfterViewInit,
  OnDestroy,
  ElementRef,
  inject,
  signal,
  computed,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { SeoService } from '../../core/seo/seo.service';
import { buscarServico, slugificar, sugestoesBusca } from '../../core/data/menu-categorias';
import { DICAS } from '../../core/data/dicas';

/** Normaliza texto para busca: minúsculo, sem acento. */
function normalizar(s: string): string {
  return s.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '').trim();
}
/** Filtra uma lista pelo termo (contém), retornando no máximo 5. */
function filtrar(lista: string[], termo: string): string[] {
  const t = normalizar(termo);
  if (!t) return [];
  return lista.filter(x => normalizar(x).includes(t)).slice(0, 5);
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly seoService = inject(SeoService);
  private readonly router = inject(Router);
  private readonly host = inject(ElementRef) as ElementRef<HTMLElement>;
  private observer?: IntersectionObserver;

  readonly termoBusca = signal('');
  readonly cidadeBusca = signal('São Paulo, SP');

  /** Fonte das sugestões de serviço (categorias + subcategorias). */
  private readonly fonteSugestoes = sugestoesBusca();

  /** Controle de foco (dropdown só aparece com o campo em foco). */
  readonly focoTermo = signal(false);
  readonly focoCidade = signal(false);

  /** Sugestões visíveis (máx. 5, só ao digitar). */
  readonly sugestoesTermo = computed(() =>
    this.focoTermo() ? filtrar(this.fonteSugestoes, this.termoBusca()) : [],
  );
  readonly sugestoesCidade = computed(() =>
    this.focoCidade() ? filtrar(this.regioesSp, this.cidadeBusca()) : [],
  );

  selecionarTermo(s: string): void {
    this.termoBusca.set(s);
    this.focoTermo.set(false);
  }
  selecionarCidade(s: string): void {
    this.cidadeBusca.set(s);
    this.focoCidade.set(false);
  }
  /** Fecha o dropdown após o blur (com atraso para o clique registrar). */
  fecharTermo(): void {
    setTimeout(() => this.focoTermo.set(false), 150);
  }
  fecharCidade(): void {
    setTimeout(() => this.focoCidade.set(false), 150);
  }

  /** A região informada está dentro da área atendida (São Paulo / Grande SP)? */
  private regiaoAtendida(cidade: string): boolean {
    const c = normalizar(cidade);
    if (!c) return true; // vazio = usa padrão São Paulo
    if (c.includes('sao paulo') || /\bsp\b/.test(c)) return true;
    return this.regioesSp.some(r => {
      const n = normalizar(r);
      return n.includes(c) || c.includes(normalizar(r.split(',')[0]));
    });
  }

  /** Busca serviço; valida a região antes. */
  buscar(): void {
    const cidade = this.cidadeBusca().trim();
    if (cidade && !this.regiaoAtendida(cidade)) {
      this.router.navigate(['/regiao-indisponivel'], { queryParams: { regiao: cidade } });
      return;
    }
    const termo = this.termoBusca().trim();
    if (!termo) {
      this.router.navigate(['/servicos']);
      return;
    }
    const rota = buscarServico(termo);
    if (rota) {
      this.router.navigate(rota);
    } else {
      // termo sem correspondência → tela de "serviço não encontrado"
      this.router.navigate(['/servicos', slugificar(termo)], { queryParams: { q: termo } });
    }
  }

  /** Campos do bloco "Cadastre-se como Profissional". */
  readonly pjEmail = signal('');
  readonly pjTelefone = signal('');

  /** Leva o profissional para o cadastro completo, já como prestador. */
  cadastrarProfissional(): void {
    this.router.navigate(['/cadastrar'], {
      queryParams: {
        tipo: 'prestador',
        email: this.pjEmail() || null,
        telefone: this.pjTelefone() || null,
      },
    });
  }

  /** Regiões atendidas (São Paulo e Grande SP) — autocomplete do campo de cidade. */
  readonly regioesSp = [
    'São Paulo, SP',
    'Zona Norte, São Paulo',
    'Zona Sul, São Paulo',
    'Zona Leste, São Paulo',
    'Zona Oeste, São Paulo',
    'Centro, São Paulo',
    'Vila Mariana, São Paulo',
    'Pinheiros, São Paulo',
    'Moema, São Paulo',
    'Tatuapé, São Paulo',
    'Santana, São Paulo',
    'Itaquera, São Paulo',
    'Butantã, São Paulo',
    'Morumbi, São Paulo',
    'Ipiranga, São Paulo',
    'Lapa, São Paulo',
    'Santo Amaro, São Paulo',
    'Perdizes, São Paulo',
    'Guarulhos, SP',
    'Osasco, SP',
    'Santo André, SP',
    'São Bernardo do Campo, SP',
    'São Caetano do Sul, SP',
    'Diadema, SP',
    'Barueri, SP',
    'Cotia, SP',
    'Taboão da Serra, SP',
    'Mauá, SP',
    'Carapicuíba, SP',
    'Itaquaquecetuba, SP',
  ];

  readonly servicosDestaque = [
    { titulo: 'Montagem de móveis', img: '/img/montagem-de-moveis.jpg' },
    { titulo: 'Mudanças e Carretos', img: '/img/mudancas-e-carretos.jpg' },
    { titulo: 'Serviço de Pedreiro', img: '/img/servico-de-pedreiro.jpg' },
    { titulo: 'Instalação elétrica', img: '/img/instalacao-eletrica.jpg' },
    { titulo: 'Pintura residencial', img: '/img/pintura-residencial.jpg' },
    { titulo: 'Limpeza pós-obra', img: '/img/limpeza-pos-obra.jpg' },
    { titulo: 'Conserto hidráulico', img: '/img/conserto-hidraulico.jpg' },
    { titulo: 'Instalação de ar-condicionado', img: '/img/ar-condicionado.jpg' },
  ];

  readonly pedidosFrequentes = [
    { icone: 'ri-flashlight-line', categoria: 'Reformas e Reparos', titulo: 'Instalação elétrica', nota: '4.8', data: '12 de maio de 2026' },
    { icone: 'ri-home-heart-line', categoria: 'Serviços Domésticos', titulo: 'Diarista', nota: '4.9', data: '3 de maio de 2026' },
    { icone: 'ri-drop-line', categoria: 'Reformas e Reparos', titulo: 'Conserto hidráulico', nota: '4.7', data: '28 de abril de 2026' },
    { icone: 'ri-brush-line', categoria: 'Pintura', titulo: 'Pintura residencial', nota: '4.8', data: '19 de abril de 2026' },
    { icone: 'ri-temp-cold-line', categoria: 'Climatização', titulo: 'Instalação de ar-condicionado', nota: '4.9', data: '7 de abril de 2026' },
    { icone: 'ri-truck-line', categoria: 'Mudança', titulo: 'Mudanças e Carretos', nota: '4.7', data: '31 de março de 2026' },
  ];

  readonly dicas = DICAS;

  ngOnInit(): void {
    this.seoService.atualizarSeo({
      titulo: 'Prontto — Encontre o profissional certo',
      descricao:
        'Descreva o serviço, receba orçamentos de profissionais verificados e contrate com segurança. Tudo em poucos minutos.',
      url: 'https://prontto.org/',
    });
  }

  ngAfterViewInit(): void {
    const alvos = this.host.nativeElement.querySelectorAll<HTMLElement>('.reveal');
    this.observer = new IntersectionObserver(
      entradas => {
        entradas.forEach(e => {
          if (e.isIntersecting) {
            e.target.classList.add('in');
            this.observer?.unobserve(e.target);
          }
        });
      },
      { threshold: 0.12 },
    );
    alvos.forEach(el => this.observer!.observe(el));
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  /** Avança/retrocede uma página inteira do carrossel. */
  mover(trilho: HTMLElement, direcao: 1 | -1, gap = 22): void {
    trilho.scrollBy({ left: direcao * (trilho.clientWidth + gap), behavior: 'smooth' });
  }
}
