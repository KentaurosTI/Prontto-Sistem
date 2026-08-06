export const environment = {
  production: true,
  // Origem pública da API na VPS (os serviços já anexam "/api/..." ao final).
  // Ajuste se usar outro domínio/subdomínio para o backend.
  apiUrl: 'https://api.prontto.org',
  // Chave publicável do Stripe (pública por design). Trocar por pk_live_... na produção real.
  stripePublishableKey: 'pk_live_51U1AtxCKOOz0tzucFcDTebyIa6mmnne2P7KfDImEI4keIMENbqVfXCdd3rabBxfyAlVsyZh4WKJBHnIFdmSWYy8700FZywyuod',
};
