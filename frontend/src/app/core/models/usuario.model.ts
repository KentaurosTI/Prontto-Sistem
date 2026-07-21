export type TipoContaUsuario = 'cliente' | 'prestador';
export type PapelUsuario = 'usuario' | 'admin';

export interface Usuario {
  id: string;
  nome: string;
  email: string;
  telefone?: string | null;
  tipoConta: TipoContaUsuario;
  papel: PapelUsuario;
  especialidade?: string | null;
  cidadeId?: string | null;
  endereco?: string | null;
  fotoPerfilUrl?: string | null;
  slug?: string | null;
  descricao?: string | null;
  mediaAvaliacoes: number;
  totalAvaliacoes: number;
  criadoEm: string;
}

export interface DadosBancarios {
  id: string;
  usuarioId: string;
  tipoChavePix: 'cpf' | 'cnpj' | 'email' | 'telefone' | 'aleatoria';
  chavePix: string;
  nomeCompleto: string;
  cpfCnpj: string;
  nomeBanco?: string | null;
  agencia?: string | null;
  numeroConta?: string | null;
  tipoConta?: string | null;
  criadoEm: string;
  atualizadoEm: string;
}

export interface Servico {
  id: string;
  titulo: string;
  descricao?: string | null;
  categoriaId: string;
  categoriaNome?: string | null;
  cidadeId?: string | null;
  cidadeNome?: string | null;
  clienteId?: string | null;
  clienteNome?: string | null;
  prestadorId?: string | null;
  prestadorNome?: string | null;
  preco: number;
  taxaAdminPercentual: number;
  status: StatusServico;
  endereco?: string | null;
  agendadoEm?: string | null;
  concluidoEm?: string | null;
  aguardandoConfirmacaoDesde?: string | null;
  criadoEm: string;
  atualizadoEm: string;
  cliente?: { nome: string; email: string } | null;
  prestador?: { nome: string; email: string } | null;
}

export type StatusServico =
  | 'em_negociacao'
  | 'aguardando_pagamento'
  | 'pago'
  | 'em_andamento'
  | 'aguardando_confirmacao_cliente'
  | 'em_disputa'
  | 'concluido'
  | 'cancelado';

export type StatusCobranca =
  | 'pendente'
  | 'pago'
  | 'retido'
  | 'liberado'
  | 'reembolsado'
  | 'cancelado';

export interface Cobranca {
  id: string;
  servicoId: string;
  valorTotal: number;
  taxaAdmin: number;
  valorPrestador: number;
  status: StatusCobranca;
  pagarmeOrderId?: string | null;
  pagarmePagamentoId?: string | null;
  pixQrCode?: string | null;
  pixCopiaCola?: string | null;
  pixExpiracaoEm?: string | null;
  pagoEm?: string | null;
  retidoEm?: string | null;
  liberadoEm?: string | null;
  criadoEm: string;
  atualizadoEm: string;
  servico?: { titulo: string; categoriaId: string; clienteId?: string | null; prestadorId?: string | null } | null;
}

export type PapelRemetente = 'cliente' | 'prestador' | 'admin' | 'sistema';
export type TipoMensagem = 'texto' | 'imagem' | 'proposta' | 'sistema';
export type StatusProposta = 'pendente' | 'aceita' | 'recusada' | 'expirada';

export interface MensagemServico {
  id: string;
  servicoId: string;
  remetenteId?: string | null;
  remetenteNome?: string | null;
  papelRemetente: PapelRemetente;
  tipoMensagem: TipoMensagem;
  conteudo: string;
  valorProposta?: number | null;
  statusProposta?: StatusProposta | null;
  imagemModerada: boolean;
  imagemAprovada?: boolean | null;
  criadoEm: string;
}

export type StatusDisputa = 'aberta' | 'em_analise' | 'resolvida_cliente' | 'resolvida_prestador';

export interface Disputa {
  id: string;
  servicoId: string;
  abertaPorId: string;
  motivo: string;
  descricao?: string | null;
  status: string;
  decisaoAdmin?: string | null;
  criadoEm: string;
  resolvidoEm?: string | null;
}

export interface EstatisticasAdmin {
  usuarios: { total: number; clientes: number; prestadores: number };
  servicos: { total: number; pendentes: number; emAndamento: number; concluidos: number };
  receita: { ganha: number; pendente: number; gmv: number };
}

// ── Perfil Público do Prestador (RF-02) ──────────────────────────────────────

export interface Categoria {
  id: string;
  nome: string;
  slug: string;
}

export interface Cidade {
  id: string;
  nome: string;
  estado: string;
  slug: string;
}

export interface ImagemPortfolio {
  id: string;
  url: string;
  ordem: number;
}

export interface PerfilPublico {
  id: string;
  nome: string;
  fotoPerfilUrl?: string | null;
  slug?: string | null;
  descricao?: string | null;
  especialidade?: string | null;
  mediaAvaliacoes: number;
  totalAvaliacoes: number;
  categorias: Categoria[];
  cidades: Cidade[];
  imagensPortfolio: ImagemPortfolio[];
}

export interface ComandoAtualizarPerfil {
  nome?: string | null;
  descricao?: string | null;
  especialidade?: string | null;
  fotoPerfilUrl?: string | null;
  categoriaIds?: string[] | null;
  cidadeIds?: string[] | null;
}

// ── Busca de Prestadores (RF-03) ─────────────────────────────────────────────

export interface PrestadorBusca {
  id: string;
  nome: string;
  fotoPerfilUrl?: string | null;
  slug: string;
  mediaAvaliacoes: number;
  totalAvaliacoes: number;
  categorias: Categoria[];
  cidades: Cidade[];
}

export interface ResultadoPaginado<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// ── Avaliações (RF-08) ────────────────────────────────────────────────────────

export interface Avaliacao {
  id: string;
  servicoId: string;
  nomeAvaliador: string;
  nota: number;
  comentario?: string;
  criadoEm: string;
}

export interface ResultadoListaAvaliacoes {
  items: Avaliacao[];
  total: number;
  pagina: number;
  totalPaginas: number;
}

// ── Home (RF-01) ─────────────────────────────────────────────────────────

export interface AvaliacaoHome {
  nomeAvaliador: string;
  nota: number;
  comentario: string;
  servicoTitulo: string;
  cidade: string;
}

export interface DadosHome {
  categorias: Categoria[];
  prestadoresDestaque: PrestadorBusca[];
  avaliacoesRecentes: AvaliacaoHome[];
}

// ── Chat com cursor (RF-06) ───────────────────────────────────────────────────

export interface ResultadoMensagens {
  mensagens: MensagemServico[];
  temMais: boolean;
  ultimoId: string | null;
}
