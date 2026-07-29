export const environment = {
  production: true,
  // Origem pública da API na VPS (os serviços já anexam "/api/..." ao final).
  // Ajuste se usar outro domínio/subdomínio para o backend.
  apiUrl: 'https://api.prontto.org',
  // Chave publicável do Stripe (pública por design). Trocar por pk_live_... na produção real.
  stripePublishableKey: 'pk_test_51TrfueQ8qrGEcAtI58b34fTroq9WRCSOWjU4RlcTRMHq4Brv2nDC0NPdJaFOT3fARCQRR24kW4JPBRu9uKbkchNO00ySRxo2IO',
};
