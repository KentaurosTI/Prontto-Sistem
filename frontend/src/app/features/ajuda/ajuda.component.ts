import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '../../core/seo/seo.service';

interface FaqAjuda { pergunta: string; resposta: string; }

const EMAIL_SUPORTE = 'prontto.org@gmail.com';

@Component({
  selector: 'app-ajuda',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './ajuda.component.html',
  styleUrl: './ajuda.component.scss',
})
export class AjudaComponent implements OnInit {
  private readonly seo = inject(SeoService);

  readonly nome = signal('');
  readonly telefone = signal('');
  readonly descricao = signal('');
  readonly enviado = signal(false);
  readonly faqAberto = signal<number | null>(null);

  readonly faq: FaqAjuda[] = [
    { pergunta: 'Como faço para solicitar um serviço?', resposta: 'Na página inicial, descreva o que você precisa na busca ou escolha uma categoria em "Serviços". Depois é só solicitar o orçamento — você recebe propostas de profissionais avaliados da sua região.' },
    { pergunta: 'Preciso pagar para pedir um orçamento?', resposta: 'Não. Solicitar orçamentos é gratuito e sem compromisso. Você só paga o serviço que decidir contratar.' },
    { pergunta: 'Como o pagamento é feito e é seguro?', resposta: 'O pagamento pode ser feito pela plataforma, com proteção: o valor é liberado ao profissional somente após a conclusão do serviço. Prefira sempre combinar e pagar pelo Prontto.' },
    { pergunta: 'Como me torno um profissional Prontto?', resposta: 'Clique em "Seja um profissional" ou "Trabalhe Conosco" e faça seu cadastro. Após a verificação, você começa a receber pedidos de clientes da sua região.' },
    { pergunta: 'Esqueci minha senha. E agora?', resposta: 'Na tela de login, use a opção de recuperação de acesso ou entre em contato pelo formulário acima que ajudamos você a recuperar sua conta.' },
    { pergunta: 'Como funciona a avaliação dos profissionais?', resposta: 'Após a conclusão de um serviço, o contratante pode avaliar o profissional com nota e comentário. Essas avaliações ajudam outros clientes a escolherem com mais segurança.' },
    { pergunta: 'O Prontto é responsável pelo serviço executado?', resposta: 'O Prontto é uma plataforma de intermediação: conectamos contratantes e profissionais, mas o serviço é prestado diretamente pelo profissional. Oferecemos verificação, avaliações e suporte para aumentar a segurança de todos.' },
    { pergunta: 'Quais regiões o Prontto atende?', resposta: 'No momento atendemos São Paulo e a Grande São Paulo. Estamos crescendo e em breve chegaremos a mais cidades.' },
    { pergunta: 'Como altero meus dados cadastrais?', resposta: 'Acesse "Minha área" após entrar na sua conta. Lá você pode atualizar seus dados de perfil e, no caso de profissionais, dados bancários e portfólio.' },
    { pergunta: 'Como faço para excluir minha conta ou meus dados?', resposta: 'Você pode solicitar a exclusão dos seus dados a qualquer momento, conforme a LGPD. Basta enviar o pedido pelo formulário acima ou para ' + EMAIL_SUPORTE + '.' },
  ];

  ngOnInit(): void {
    this.seo.atualizarSeo({
      titulo: 'Central de Ajuda — Prontto',
      descricao: 'Precisa de ajuda? Envie seu problema ou sugestão e confira as perguntas mais frequentes do Prontto.',
      url: 'https://prontto.org/ajuda',
    });
    if (typeof window !== 'undefined') window.scrollTo(0, 0);
  }

  get formValido(): boolean {
    return this.nome().trim().length >= 2 && this.telefone().trim().length >= 8 && this.descricao().trim().length >= 10;
  }

  enviar(): void {
    if (!this.formValido) return;
    const assunto = `Ajuda / Contato — ${this.nome().trim()}`;
    const corpo =
      `Nome: ${this.nome().trim()}\n` +
      `Telefone: ${this.telefone().trim()}\n\n` +
      `Descrição do problema / sugestão:\n${this.descricao().trim()}`;
    const url = `mailto:${EMAIL_SUPORTE}?subject=${encodeURIComponent(assunto)}&body=${encodeURIComponent(corpo)}`;
    window.location.href = url;
    this.enviado.set(true);
  }

  alternarFaq(i: number): void {
    this.faqAberto.update(a => (a === i ? null : i));
  }
}
