/**
 * Dados da barra de categorias + mega-menu (Home / página de categoria).
 * Portado de js/categorias.js do handoff de redesign.
 */

export interface GrupoMenu {
  titulo: string;
  itens: string[];
}

export interface CategoriaMenu {
  key: string;
  label: string;
  emoji: string;
  icone: string; // classe Remix Icon
  grupos: GrupoMenu[];
}

export const ICONE_CATEGORIA: Record<string, string> = {
  reformas: 'ri-hammer-line',
  pintura: 'ri-brush-line',
  limpeza: 'ri-home-heart-line',
  clima: 'ri-temp-cold-line',
  jardim: 'ri-plant-line',
  montagem: 'ri-tools-line',
  mudanca: 'ri-truck-line',
  assistencia: 'ri-computer-line',
  seguranca: 'ri-shield-check-line',
  serralheria: 'ri-door-lock-line',
  autos: 'ri-car-line',
};

export const CATEGORIAS_MENU: CategoriaMenu[] = [
  {
    key: 'reformas',
    label: 'Reformas e Reparos',
    emoji: '🔨',
    icone: ICONE_CATEGORIA['reformas'],
    grupos: [
      { titulo: 'Elétrica', itens: ['Instalação elétrica', 'Tomadas e interruptores', 'Quadro de distribuição', 'Chuveiro elétrico', 'Iluminação'] },
      { titulo: 'Hidráulica', itens: ['Vazamentos', 'Desentupimento', 'Instalação de torneira', 'Caixa d’água', 'Aquecedor'] },
      { titulo: 'Alvenaria', itens: ['Pedreiro', 'Reboco e massa', 'Assentamento de piso', 'Pequenos reparos', 'Muros'] },
      { titulo: 'Gesso e Drywall', itens: ['Forro de gesso', 'Sancas', 'Paredes de drywall', 'Molduras'] },
    ],
  },
  {
    key: 'pintura',
    label: 'Pintura',
    emoji: '🎨',
    icone: ICONE_CATEGORIA['pintura'],
    grupos: [
      { titulo: 'Residencial', itens: ['Pintura interna', 'Pintura externa', 'Textura', 'Grafiato'] },
      { titulo: 'Acabamentos', itens: ['Verniz e laca', 'Pintura de portões', 'Efeitos decorativos', 'Massa corrida'] },
      { titulo: 'Comercial', itens: ['Lojas e escritórios', 'Fachadas', 'Galpões', 'Sinalização'] },
    ],
  },
  {
    key: 'limpeza',
    label: 'Limpeza',
    emoji: '🧹',
    icone: ICONE_CATEGORIA['limpeza'],
    grupos: [
      { titulo: 'Residencial', itens: ['Diarista', 'Faxina', 'Limpeza pós-obra', 'Passar roupa'] },
      { titulo: 'Especializada', itens: ['Limpeza de vidros', 'Estofados e sofás', 'Carpetes e tapetes', 'Caixa d’água'] },
      { titulo: 'Comercial', itens: ['Escritórios', 'Condomínios', 'Lojas', 'Pós-evento'] },
    ],
  },
  {
    key: 'clima',
    label: 'Climatização',
    emoji: '❄️',
    icone: ICONE_CATEGORIA['clima'],
    grupos: [
      { titulo: 'Ar-condicionado', itens: ['Instalação', 'Higienização', 'Manutenção', 'Recarga de gás'] },
      { titulo: 'Refrigeração', itens: ['Geladeira', 'Freezer', 'Câmara fria', 'Bebedouro'] },
      { titulo: 'Ventilação', itens: ['Exaustores', 'Coifas', 'Cortinas de ar'] },
    ],
  },
  {
    key: 'jardim',
    label: 'Jardinagem',
    emoji: '🌱',
    icone: ICONE_CATEGORIA['jardim'],
    grupos: [
      { titulo: 'Jardim', itens: ['Corte de grama', 'Poda de árvores', 'Paisagismo', 'Plantio'] },
      { titulo: 'Área externa', itens: ['Limpeza de quintal', 'Piscina', 'Dedetização', 'Jardim vertical'] },
    ],
  },
  {
    key: 'montagem',
    label: 'Montagem e Móveis',
    emoji: '🪑',
    icone: ICONE_CATEGORIA['montagem'],
    grupos: [
      { titulo: 'Montagem', itens: ['Móveis em geral', 'Móveis planejados', 'Guarda-roupa', 'Estantes'] },
      { titulo: 'Marcenaria', itens: ['Móveis sob medida', 'Restauração', 'Portas e janelas', 'Pequenos reparos'] },
    ],
  },
  {
    key: 'mudanca',
    label: 'Mudança',
    emoji: '📦',
    icone: ICONE_CATEGORIA['mudanca'],
    grupos: [
      { titulo: 'Mudança', itens: ['Residencial', 'Comercial', 'Içamento', 'Guarda-móveis'] },
      { titulo: 'Transporte', itens: ['Frete', 'Carreto', 'Entregas', 'Motorista'] },
    ],
  },
  {
    key: 'assistencia',
    label: 'Assistência Técnica',
    emoji: '🛠️',
    icone: ICONE_CATEGORIA['assistencia'],
    grupos: [
      { titulo: 'Eletrodomésticos', itens: ['Máquina de lavar', 'Geladeira', 'Microondas', 'Fogão e cooktop'] },
      { titulo: 'Eletrônicos', itens: ['TV', 'Computador e notebook', 'Celular', 'Som e home theater'] },
    ],
  },
  {
    key: 'seguranca',
    label: 'Segurança',
    emoji: '🛡️',
    icone: ICONE_CATEGORIA['seguranca'],
    grupos: [
      { titulo: 'Monitoramento', itens: ['Câmeras / CFTV', 'Alarmes', 'Cerca elétrica', 'Interfone'] },
      { titulo: 'Acesso', itens: ['Portão eletrônico', 'Fechaduras digitais', 'Controle de acesso'] },
    ],
  },
  {
    key: 'serralheria',
    label: 'Serralheria',
    emoji: '⚙️',
    icone: ICONE_CATEGORIA['serralheria'],
    grupos: [
      { titulo: 'Estruturas', itens: ['Portões', 'Grades e corrimãos', 'Estruturas metálicas', 'Solda'] },
      { titulo: 'Esquadrias', itens: ['Janelas de alumínio', 'Portas de ferro', 'Toldos e coberturas'] },
    ],
  },
  {
    key: 'autos',
    label: 'Autos',
    emoji: '🚗',
    icone: ICONE_CATEGORIA['autos'],
    grupos: [
      { titulo: 'Mecânica', itens: ['Mecânico', 'Elétrica automotiva', 'Funilaria e pintura', 'Borracharia'] },
      { titulo: 'Cuidados', itens: ['Lavagem e estética', 'Película / insulfilm', 'Som automotivo', 'Guincho'] },
    ],
  },
];

export function slugificar(str: string): string {
  return str
    .toLowerCase()
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

/** Rota da página de categoria (equivalente a categoria.html?cat=&sub=). */
export function rotaCategoria(key: string, item?: string): string[] {
  return item ? ['/servicos', key, slugificar(item)] : ['/servicos', key];
}

/** Encontra a categoria pelo key. */
export function findCat(key: string): CategoriaMenu | undefined {
  return CATEGORIAS_MENU.find(c => c.key === key);
}

/** Encontra o item de subcategoria (label) pelo slug, dentro de uma categoria. */
export function findSub(cat: CategoriaMenu, subSlug: string | null): string | null {
  if (!subSlug) return null;
  for (const g of cat.grupos) {
    for (const it of g.itens) {
      if (slugificar(it) === subSlug) return it;
    }
  }
  return null;
}

/** Lista plana de todos os itens de serviço de uma categoria. */
export function itensPlanos(cat: CategoriaMenu): string[] {
  return cat.grupos.flatMap(g => g.itens);
}

/** Todas as sugestões de busca (categorias + subcategorias), para autocomplete. */
export function sugestoesBusca(): string[] {
  const set = new Set<string>();
  for (const c of CATEGORIAS_MENU) {
    set.add(c.label);
    for (const it of itensPlanos(c)) set.add(it);
  }
  return [...set].sort((a, b) => a.localeCompare(b, 'pt-BR'));
}

/**
 * Procura uma categoria/subcategoria a partir de um termo livre.
 * Retorna a rota (/servicos/:cat[/:sub]) ou null se não encontrar.
 */
export function buscarServico(termo: string): string[] | null {
  const s = slugificar(termo);
  if (!s) return null;
  const casa = (alvo: string) => {
    const a = slugificar(alvo);
    return a === s || (s.length >= 3 && (a.includes(s) || s.includes(a)));
  };
  // 1) subcategoria
  for (const cat of CATEGORIAS_MENU) {
    for (const it of itensPlanos(cat)) {
      if (casa(it)) return rotaCategoria(cat.key, it);
    }
  }
  // 2) categoria (label ou key)
  for (const cat of CATEGORIAS_MENU) {
    if (casa(cat.label) || cat.key === s) return rotaCategoria(cat.key);
  }
  return null;
}

/** Slugs de subcategoria que possuem foto em /img/sub/{slug}.jpg */
const SUBIMG_SLUGS = new Set([
  'alarmes', 'aquecedor', 'assentamento-de-piso', 'bebedouro', 'borracharia', 'caixa-d-agua',
  'camara-fria', 'cameras-cftv', 'carpetes-e-tapetes', 'carreto', 'celular', 'cerca-eletrica',
  'chuveiro-eletrico', 'coifas', 'comercial', 'computador-e-notebook', 'condominios',
  'controle-de-acesso', 'corte-de-grama', 'cortinas-de-ar', 'dedetizacao', 'diarista',
  'efeitos-decorativos', 'eletrica-automotiva', 'entregas', 'escritorios', 'estantes',
  'estofados-e-sofas', 'estruturas-metalicas', 'exaustores', 'fachadas', 'faxina',
  'fechaduras-digitais', 'fogao-e-cooktop', 'forro-de-gesso', 'freezer', 'frete', 'funilaria-e-pintura',
  'galpoes', 'geladeira', 'grades-e-corrimaos', 'grafiato', 'guarda-moveis', 'guarda-roupa', 'guincho',
  'higienizacao', 'icamento', 'iluminacao', 'instalacao', 'instalacao-de-torneira', 'instalacao-eletrica',
  'interfone', 'janelas-de-aluminio', 'jardim-vertical', 'lavagem-e-estetica', 'limpeza-de-quintal',
  'limpeza-de-vidros', 'limpeza-pos-obra', 'lojas', 'lojas-e-escritorios', 'manutencao',
  'maquina-de-lavar', 'massa-corrida', 'mecanico', 'microondas', 'molduras', 'motorista',
  'moveis-em-geral', 'moveis-planejados', 'moveis-sob-medida', 'muros', 'paisagismo',
  'paredes-de-drywall', 'passar-roupa', 'pedreiro', 'pelicula-insulfilm', 'pequenos-reparos',
  'pintura-de-portoes', 'pintura-externa', 'pintura-interna', 'piscina', 'plantio', 'poda-de-arvores',
  'portao-eletronico', 'portas-de-ferro', 'portas-e-janelas', 'portoes', 'pos-evento',
  'quadro-de-distribuicao', 'reboco-e-massa', 'recarga-de-gas', 'residencial', 'restauracao', 'sancas',
  'sinalizacao', 'solda', 'som-automotivo', 'som-e-home-theater', 'textura', 'toldos-e-coberturas',
  'tomadas-e-interruptores', 'tv', 'vazamentos', 'verniz-e-laca',
]);

/** Categorias que possuem capa em /img/cat/{key}.jpg */
const CATIMG_KEYS = new Set(['assistencia', 'autos', 'limpeza', 'serralheria', 'reformas']);

export function imagemSub(item: string): string | null {
  const s = slugificar(item);
  return SUBIMG_SLUGS.has(s) ? `/img/sub/${s}.jpg` : null;
}

export function imagemCat(key: string): string | null {
  return CATIMG_KEYS.has(key) ? `/img/cat/${key}.jpg` : null;
}

/** Imagem de hero da página de categoria (sub → categoria → 1ª subcategoria com foto). */
export function imagemHero(cat: CategoriaMenu, sub: string | null): string | null {
  if (sub) {
    const i = imagemSub(sub);
    if (i) return i;
  }
  const c = imagemCat(cat.key);
  if (c) return c;
  for (const it of itensPlanos(cat)) {
    const i = imagemSub(it);
    if (i) return i;
  }
  return null;
}
