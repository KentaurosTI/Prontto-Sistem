import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '../../core/seo/seo.service';

interface FaqItem {
  pergunta: string;
  resposta: string;
  aberta: boolean;
}

@Component({
  selector: 'app-seguranca',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './seguranca.component.html',
  styleUrl: './seguranca.component.scss',
})
export class SegurancaComponent implements OnInit {
  private readonly seoService = inject(SeoService);

  readonly faqItems = signal<FaqItem[]>([
    {
      pergunta: 'Caí em um golpe, e agora?',
      resposta: 'Bloqueie seu cartão: entre em contato com a operadora para impedir novas transações indevidas. Fale com seu banco: informe o ocorrido, peça orientações sobre estorno e anote o número do protocolo. Registre um Boletim de Ocorrência (B.O.): esse documento é essencial para colaborar com as autoridades. Em seguida, fale com a gente pelo chat ou e-mail.',
      aberta: false,
    },
    {
      pergunta: 'Tive um problema de segurança com o profissional. O que faço?',
      resposta: 'Temos um canal onde clientes e não clientes podem denunciar tentativas de golpes, fraudes, perfis e sites falsos. Entre em contato com a gente imediatamente para que possamos te auxiliar.',
      aberta: false,
    },
    {
      pergunta: 'Como confiro o endereço correto do site?',
      resposta: 'Ao acessar pelo computador, verifique sempre se a URL é a correta e se ela é criptografada — ou seja, se começa com https:// (com o "S" depois do http).',
      aberta: false,
    },
    {
      pergunta: 'Devo preencher formulários para atualizações cadastrais?',
      resposta: 'Não enviamos formulários para você preencher com informações pessoais. A atualização do cadastro é feita diretamente na plataforma. Também não solicitamos atualização por e-mail, SMS ou ligação.',
      aberta: false,
    },
  ]);

  ngOnInit(): void {
    this.seoService.atualizarSeo({
      titulo: 'Segurança — Contrate com tranquilidade',
      descricao: 'Saiba como proteger seus dados, fazer pagamentos seguros e evitar golpes ao contratar profissionais na Prontto.',
      url: 'https://prontto.org/seguranca',
    });
  }

  alternarFaq(index: number): void {
    this.faqItems.update(items =>
      items.map((item, i) => ({ ...item, aberta: i === index ? !item.aberta : false }))
    );
  }
}
