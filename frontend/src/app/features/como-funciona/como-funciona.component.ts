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
  selector: 'app-como-funciona',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './como-funciona.component.html',
  styleUrl: './como-funciona.component.scss',
})
export class ComoFuncionaComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly seoService = inject(SeoService);

  readonly videoBox = viewChild<ElementRef<HTMLElement>>('videoBox');
  readonly videoIframe = viewChild<ElementRef<HTMLIFrameElement>>('videoIframe');
  private ro?: ResizeObserver;

  ngOnInit(): void {
    this.seoService.atualizarSeo({
      titulo: 'Como funciona — Prontto',
      descricao: 'Veja em segundos como funciona a contratação de serviços no Prontto: peça, receba orçamentos e contrate com segurança.',
      url: 'https://prontto.org/como-funciona',
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
