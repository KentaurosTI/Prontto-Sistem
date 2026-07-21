import { Component, inject, computed, signal, effect } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { SeoService } from '../../core/seo/seo.service';
import {
  CATEGORIAS_MENU,
  findCat,
  findSub,
  itensPlanos,
  imagemHero,
  rotaCategoria,
} from '../../core/data/menu-categorias';

const CIDADES = [
  'São Paulo', 'Rio de Janeiro', 'Belo Horizonte', 'Brasília', 'Curitiba', 'Porto Alegre',
  'Salvador', 'Fortaleza', 'Campinas', 'Goiânia', 'Recife', 'Manaus',
];
const NOMES = [
  'Lara', 'Vitor Gabriel', 'Esther', 'Lavínia', 'Cauã', 'Thiago',
  'João Miguel', 'Yuri', 'Ana Paula', 'Roberto', 'Bernardo', 'Helena',
];
const RW_NOMES = ['Ana Paula', 'Roberto Silva', 'Bernardo Fernandes'];
const RW_PARA = ['Antônio Santos', 'Vanessa Silva', 'Adriana Prado'];
const RW_TEXTOS = [
  'Fui muito bem atendida, com um trabalho de qualidade. Valeu a pena, orçamento grátis e não é caro. Obrigada!',
  'Excelentes profissionais, rápidos, honestos e com bom preço. Recomendo muito.',
  'O profissional é excelente, atendeu com rapidez, atenção e solucionou o problema. Recomendo!',
];
const PRECOS = [[500, 300, 800], [300, 450, 600], [300, 200, 1500], [150, 200, 400], [200, 1600, 3000]];

interface CostCard { valor: string; servico: string; menor: string; maior: string; }
interface ReqCard { nome: string; texto: string; }
interface FaqItem { q: string; a: string[]; }
interface LinkItem { texto: string; rota: string[]; }

function brl(n: number): string {
  return 'R$ ' + n.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

@Component({
  selector: 'app-categoria',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './categoria.component.html',
  styleUrl: './categoria.component.scss',
})
export class CategoriaComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly seo = inject(SeoService);

  private readonly params = toSignal(this.route.paramMap, { initialValue: this.route.snapshot.paramMap });
  private readonly query = toSignal(this.route.queryParamMap, { initialValue: this.route.snapshot.queryParamMap });

  /** Categoria encontrada (ou undefined → tela de "não encontrado"). */
  readonly catRaw = computed(() => findCat(this.params().get('key') ?? ''));
  readonly encontrado = computed(() => this.catRaw() !== undefined);
  /** Termo buscado (para a tela de não encontrado). */
  readonly termoBuscado = computed(() => this.query().get('q') ?? this.params().get('key') ?? '');

  readonly cat = computed(() => this.catRaw() ?? CATEGORIAS_MENU[0]);
  readonly sub = computed(() => findSub(this.cat(), this.params().get('sub')));
  readonly foco = computed(() => this.sub() ?? this.cat().label);
  readonly focoBaixo = computed(() => {
    const f = this.foco();
    return f.charAt(0).toLowerCase() + f.slice(1);
  });
  readonly flat = computed(() => itensPlanos(this.cat()));
  readonly heroImg = computed(() => imagemHero(this.cat(), this.sub()));

  /** Índice do item de FAQ aberto (ou null). */
  readonly faqAberto = signal<number | null>(null);

  readonly reviews = RW_NOMES.map((nome, k) => ({
    nome, texto: RW_TEXTOS[k], para: RW_PARA[k],
  }));

  readonly custos = computed<CostCard[]>(() => {
    const flat = this.flat();
    return Array.from({ length: 4 }, (_, k) => {
      const svc = flat[k % flat.length];
      const p = PRECOS[k % PRECOS.length];
      return { valor: brl(p[0]), servico: svc, menor: brl(p[1]), maior: brl(p[2]) };
    });
  });

  readonly pedidos = computed<ReqCard[]>(() => {
    const fb = this.focoBaixo();
    const txts = [
      `Gostaria de saber o valor para ${fb}. Zona oeste, que atenda a região.`,
      `Preciso de um orçamento para ${fb}. Pode ser ainda esta semana?`,
      `Quero contratar ${fb} com qualidade e bom preço. Aguardo propostas.`,
      `Procuro profissional para ${fb}. Atendimento no período da manhã.`,
      `Preciso resolver o quanto antes. Alguém disponível para ${fb}?`,
      `Bom dia! Solicito orçamento de ${fb} para a minha residência.`,
      `Trabalho com ${fb} recorrente, busco parceria de longo prazo.`,
      `Gostaria de agendar ${fb} para o próximo fim de semana.`,
    ];
    return NOMES.slice(0, 8).map((nome, k) => ({ nome, texto: txts[k % txts.length] }));
  });

  readonly faq = computed<FaqItem[]>(() => {
    const foco = this.foco();
    const fb = this.focoBaixo();
    const cat = this.cat();
    const flat = this.flat();
    return [
      { q: `O que é ${fb}?`, a: [`${foco} reúne serviços executados por profissionais capacitados, com foco em qualidade, segurança e bom acabamento.`, 'No Prontto você descreve o que precisa, recebe orçamentos de profissionais avaliados e contrata com tranquilidade.'] },
      { q: `Qual profissional realiza ${fb}?`, a: [`Profissionais especializados na categoria ${cat.label}, com experiência comprovada e avaliações reais de clientes que já contrataram pela plataforma.`] },
      { q: 'Como funciona a contratação no Prontto?', a: ['É simples: faça o seu pedido gratuitamente, receba propostas de profissionais da sua região, compare avaliações e preços e escolha o melhor. Você paga com segurança somente após a conclusão do serviço.'] },
      { q: `Quanto custa ${fb}?`, a: ['O valor varia conforme o tipo de serviço, a complexidade e a sua região. Por isso o ideal é solicitar orçamentos — eles são gratuitos e chegam em até 60 minutos.'] },
      { q: `Que outros serviços de ${cat.label} posso contratar no Prontto?`, a: [`Você encontra profissionais qualificados para ${flat.slice(0, 4).join(', ')} e muitos outros.`] },
    ];
  });

  readonly similares = computed<LinkItem[]>(() =>
    this.flat().map(it => ({ texto: `Especialistas em ${it}`, rota: rotaCategoria(this.cat().key, it) })),
  );

  readonly cidades = computed<string[]>(() => CIDADES.map(c => `${this.foco()} em ${c}`));

  constructor() {
    effect(() => {
      const foco = this.foco();
      this.faqAberto.set(null);
      this.seo.atualizarSeo({
        titulo: `${foco} — Prontto`,
        descricao: `Encontre profissionais de ${foco.toLowerCase()} avaliados. Peça orçamentos grátis e contrate com segurança no Prontto.`,
        url: `https://prontto.org/categoria/${this.cat().key}`,
      });
      if (typeof window !== 'undefined') window.scrollTo(0, 0);
    });
  }

  rota(item?: string): string[] {
    return rotaCategoria(this.cat().key, item);
  }

  alternarFaq(i: number): void {
    this.faqAberto.update(atual => (atual === i ? null : i));
  }
}
