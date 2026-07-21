import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '../../core/seo/seo.service';
import {
  CATEGORIAS_MENU,
  imagemSub,
  itensPlanos,
  rotaCategoria,
} from '../../core/data/menu-categorias';

const DESC: ((s: string) => string)[] = [
  s => `Profissionais verificados para ${low(s)} perto de você. Orçamento grátis e sem compromisso.`,
  s => `Precisa de ${low(s)}? Receba até 4 orçamentos de especialistas avaliados da sua região.`,
  s => `Contrate ${low(s)} com qualidade e segurança. Você paga somente após a conclusão.`,
  s => `Especialistas em ${low(s)} prontos para atender. Compare avaliações e preços num só lugar.`,
];
function low(s: string): string {
  return s.charAt(0).toLowerCase() + s.slice(1);
}

interface ItemCatalogo { nome: string; img: string | null; desc: string; rota: string[]; }
interface GrupoCatalogo { titulo: string; itens: ItemCatalogo[]; }
interface CatCatalogo {
  key: string; label: string; icone: string;
  grupoPrincipal: GrupoCatalogo; grupos: GrupoCatalogo[];
  total: number; restoCount: number;
}

@Component({
  selector: 'app-servicos',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './servicos.component.html',
  styleUrl: './servicos.component.scss',
})
export class ServicosComponent implements OnInit {
  private readonly seo = inject(SeoService);

  readonly catalogo: CatCatalogo[] = CATEGORIAS_MENU.map(cat => {
    let idx = 0;
    const grupos: GrupoCatalogo[] = cat.grupos.map(g => ({
      titulo: g.titulo,
      itens: g.itens.map(it => ({
        nome: it,
        img: imagemSub(it),
        desc: DESC[idx++ % DESC.length](it),
        rota: rotaCategoria(cat.key, it),
      })),
    }));
    const total = itensPlanos(cat).length;
    return {
      key: cat.key,
      label: cat.label,
      icone: cat.icone,
      grupoPrincipal: grupos[0],
      grupos: grupos.slice(1),
      total,
      restoCount: total - grupos[0].itens.length,
    };
  });

  /** Categorias com a listagem completa expandida. */
  readonly expandidos = signal<Set<string>>(new Set());

  ngOnInit(): void {
    this.seo.atualizarSeo({
      titulo: 'Nossos Serviços — Prontto',
      descricao: 'Catálogo completo de serviços do Prontto. Encontre o profissional ideal para cada necessidade e contrate com segurança.',
      url: 'https://prontto.org/servicos',
    });
  }

  estaExpandido(key: string): boolean {
    return this.expandidos().has(key);
  }

  alternar(key: string): void {
    this.expandidos.update(s => {
      const n = new Set(s);
      n.has(key) ? n.delete(key) : n.add(key);
      return n;
    });
  }

  irPara(key: string): void {
    const el = document.getElementById('cat-' + key);
    if (el) {
      window.scrollTo({ top: el.getBoundingClientRect().top + window.scrollY - 90, behavior: 'smooth' });
    }
  }
}
