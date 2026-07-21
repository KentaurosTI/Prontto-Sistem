import { Injectable, inject } from '@angular/core';
import { Title, Meta } from '@angular/platform-browser';

export interface DadosSeo {
  titulo: string;
  descricao: string;
  imagem?: string;
  url?: string;
}

@Injectable({ providedIn: 'root' })
export class SeoService {
  private readonly title = inject(Title);
  private readonly meta = inject(Meta);

  atualizarSeo(dados: DadosSeo): void {
    const tituloCompleto = `${dados.titulo} | Prontto`;

    this.title.setTitle(tituloCompleto);

    // Meta básico
    this.meta.updateTag({ name: 'description', content: dados.descricao });

    // Open Graph
    this.meta.updateTag({ property: 'og:title', content: tituloCompleto });
    this.meta.updateTag({ property: 'og:description', content: dados.descricao });
    this.meta.updateTag({ property: 'og:type', content: 'website' });
    this.meta.updateTag({ property: 'og:site_name', content: 'Prontto' });
    if (dados.url) this.meta.updateTag({ property: 'og:url', content: dados.url });
    if (dados.imagem) this.meta.updateTag({ property: 'og:image', content: dados.imagem });

    // Twitter Card
    this.meta.updateTag({ name: 'twitter:card', content: 'summary_large_image' });
    this.meta.updateTag({ name: 'twitter:title', content: tituloCompleto });
    this.meta.updateTag({ name: 'twitter:description', content: dados.descricao });
    if (dados.imagem) this.meta.updateTag({ name: 'twitter:image', content: dados.imagem });
  }
}
