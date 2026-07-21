import { environment } from '../../../environments/environment';

/**
 * Resolve a URL de uma imagem para exibição.
 * Uploads locais são salvos como caminho relativo (ex.: "/uploads/abc.jpg"),
 * mas os arquivos são servidos pela API (api.prontto.org), não pelo site.
 * Por isso, prefixamos o host da API quando a URL é relativa.
 * URLs absolutas (http/https, ex.: Cloudinary) são retornadas sem alteração.
 */
export function resolverUrlImagem(url: string | null | undefined): string {
  if (!url) return '';
  const u = url.trim();
  if (u.startsWith('http://') || u.startsWith('https://') || u.startsWith('data:')) {
    return u;
  }
  const base = environment.apiUrl.replace(/\/$/, '');
  return u.startsWith('/') ? `${base}${u}` : `${base}/${u}`;
}
