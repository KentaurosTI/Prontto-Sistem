import {
  Component,
  OnInit,
  AfterViewInit,
  OnDestroy,
  ElementRef,
  viewChild,
  inject,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { SeoService } from '../../core/seo/seo.service';

@Component({
  selector: 'app-para-prestadores',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './para-prestadores.component.html',
  styleUrl: './para-prestadores.component.scss',
})
export class ParaPrestadoresComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly seoService = inject(SeoService);

  readonly videoBox = viewChild<ElementRef<HTMLElement>>('videoBox');
  readonly videoIframe = viewChild<ElementRef<HTMLIFrameElement>>('videoIframe');
  private ro?: ResizeObserver;

  ngOnInit(): void {
    this.seoService.atualizarSeo({
      titulo: 'Seja um profissional — Prontto',
      descricao: 'Cadastre-se como profissional na Prontto e receba pedidos de clientes na sua região.',
      url: 'https://prontto.org/para-prestadores',
    });
  }

  ngAfterViewInit(): void {
    const box = this.videoBox()?.nativeElement;
    const frame = this.videoIframe()?.nativeElement;
    if (!box || !frame) return;
    const fit = () => (frame.style.transform = `scale(${box.clientWidth / 1280})`);
    fit();
    frame.addEventListener('load', fit);
    if (typeof ResizeObserver !== 'undefined') {
      this.ro = new ResizeObserver(fit);
      this.ro.observe(box);
    }
  }

  ngOnDestroy(): void {
    this.ro?.disconnect();
  }
}
