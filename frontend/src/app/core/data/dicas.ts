/** Dicas (blog) exibidas na Home e em telas dedicadas /dicas/:slug */
export interface Dica {
  slug: string;
  titulo: string;
  img: string;
  resumo: string;
  categoria: string;
  conteudo: string[];
}

export const DICAS: Dica[] = [
  {
    slug: 'seguranca-na-contratacao',
    titulo: 'Como garantir a segurança na hora da contratação?',
    img: '/img/seguranca.jpg',
    categoria: 'Segurança',
    resumo: 'Saiba como proteger seus dados no dia a dia e manter sua segurança para evitar cair em golpes.',
    conteudo: [
      'Contratar um serviço pela internet ficou muito mais prático, mas é importante manter alguns cuidados para não cair em golpes. A primeira regra é simples: desconfie de valores muito abaixo do mercado e de profissionais que pressionam por pagamento adiantado.',
      'Sempre confira as avaliações de outros clientes antes de fechar negócio. Perfis com histórico, fotos de trabalhos anteriores e comentários reais dão muito mais segurança do que contatos vindos de fontes desconhecidas.',
      'Prefira combinar todos os detalhes pela própria plataforma — valor, prazo e escopo do serviço. Assim fica tudo registrado, e você conta com o suporte do Prontto caso algo saia do combinado.',
      'Na hora do pagamento, priorize Pix ou cartão por aproximação e confira sempre se os dados da conta pertencem ao profissional contratado. Nunca compartilhe senhas, códigos de verificação ou dados do cartão por telefone ou mensagem.',
    ],
  },
  {
    slug: 'botao-ligar-celular',
    titulo: 'O que fazer quando o botão de ligar do celular não funciona mais?',
    img: '/img/dica-reparo-celular.jpg',
    categoria: 'Assistência Técnica',
    resumo: 'Quando o botão físico do celular deixa de funcionar, pode dar muita dor de cabeça — saiba como substituí-lo de forma prática.',
    conteudo: [
      'O botão de ligar é um dos componentes mais usados do celular e, com o tempo, pode apresentar desgaste. Antes de pensar em troca, vale limpar a região com um pano seco e verificar se não há sujeira ou capa pressionando o botão.',
      'Em muitos aparelhos, é possível contornar o problema temporariamente com recursos de acessibilidade — como toque na tela para ativar, ou gestos que dispensam o botão físico. Isso ajuda enquanto você organiza o reparo.',
      'Quando o botão realmente falha, a substituição envolve abrir o aparelho e trocar o flex (a peça flexível que conecta o botão à placa). É um serviço delicado, que exige ferramentas específicas e experiência para não danificar outros componentes.',
      'O ideal é procurar um profissional de assistência técnica avaliado. No Prontto você encontra especialistas na sua região, compara preços e contrata com segurança, pagando apenas após a conclusão do serviço.',
    ],
  },
  {
    slug: 'diarista-faxineira-domestica',
    titulo: 'Qual a diferença entre diarista, faxineira e doméstica?',
    img: '/img/limpeza.jpg',
    categoria: 'Serviços Domésticos',
    resumo: 'Na hora de contratar, muitas pessoas ficam na dúvida sobre a diferença entre diarista, faxineira e doméstica. Saiba mais sobre esses profissionais!',
    conteudo: [
      'Apesar de trabalharem com limpeza, esses profissionais têm funções e vínculos diferentes. Entender essa distinção ajuda a contratar da forma certa e a evitar problemas trabalhistas.',
      'A diarista trabalha de forma autônoma, geralmente até dois dias por semana na mesma casa, e é paga por diária. Não há vínculo empregatício, o que torna a contratação mais flexível — ideal para limpezas pontuais ou periódicas.',
      'A doméstica é uma funcionária com vínculo empregatício (carteira assinada), que trabalha três ou mais dias por semana na mesma residência. Já a faxineira costuma ser contratada para limpezas mais pesadas e específicas, como pós-obra ou faxina geral.',
      'Se você precisa de uma limpeza esporádica, a diarista costuma ser a melhor opção. No Prontto, você descreve o que precisa e recebe orçamentos de profissionais avaliados, escolhendo o que melhor atende à sua rotina.',
    ],
  },
  {
    slug: 'quando-chamar-eletricista',
    titulo: 'Quando chamar um eletricista em vez de tentar resolver sozinho?',
    img: '/img/eletrica.jpg',
    categoria: 'Reformas e Reparos',
    resumo: 'Pequenos reparos podem virar um problema sério. Veja os sinais de que é hora de chamar um profissional verificado.',
    conteudo: [
      'Mexer com eletricidade sem conhecimento é um risco real — de choques a incêndios. Alguns sinais indicam que é hora de chamar um profissional em vez de tentar resolver por conta própria.',
      'Fique atento se as luzes piscam com frequência, se disjuntores desarmam sem motivo, se há cheiro de queimado perto de tomadas ou se você sente pequenos choques ao tocar em aparelhos. Esses são alertas de que algo não está certo na instalação.',
      'Trocar uma lâmpada ou resetar um disjuntor são tarefas simples. Mas instalações novas, troca de fiação, quadros de distribuição e chuveiros elétricos exigem um eletricista qualificado, que conhece as normas e garante a segurança da sua casa.',
      'No Prontto você encontra eletricistas avaliados por outros clientes, compara orçamentos e contrata com tranquilidade. Não arrisque: um bom profissional resolve com segurança e evita prejuízos maiores.',
    ],
  },
  {
    slug: 'quanta-tinta-comprar',
    titulo: 'Quanta tinta comprar para pintar um cômodo?',
    img: '/img/pintura.jpg',
    categoria: 'Pintura',
    resumo: 'Calcular a quantidade certa evita desperdício e retrabalho. Aprenda a estimar antes de começar a obra.',
    conteudo: [
      'Comprar tinta demais gera desperdício; de menos, você corre o risco de ficar sem no meio do trabalho e não achar o mesmo tom. Um cálculo simples resolve isso antes de começar.',
      'Primeiro, meça a área a ser pintada: multiplique a largura pela altura de cada parede e some tudo, descontando portas e janelas. Uma lata de 18 litros costuma render cerca de 300 m² por demão, e a de 3,6 litros, cerca de 60 m².',
      'Considere sempre pelo menos duas demãos para um acabamento uniforme, especialmente em cores fortes ou paredes novas. Superfícies porosas ou não seladas absorvem mais tinta, então vale acrescentar uma margem de segurança.',
      'Na dúvida, um pintor profissional faz esse cálculo com precisão e ainda garante o acabamento. No Prontto, você encontra profissionais de pintura avaliados, pede orçamento grátis e contrata quem oferece o melhor custo-benefício.',
    ],
  },
];

export function findDica(slug: string): Dica | undefined {
  return DICAS.find(d => d.slug === slug);
}
