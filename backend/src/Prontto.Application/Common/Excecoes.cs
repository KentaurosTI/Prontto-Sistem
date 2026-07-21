namespace Prontto.Application.Common;

public class ExcecaoNaoEncontrado(string mensagem) : Exception(mensagem);
public class ExcecaoConflito(string mensagem) : Exception(mensagem);
public class ExcecaoNaoAutorizado(string mensagem) : Exception(mensagem);
public class ExcecaoProibido(string mensagem) : Exception(mensagem);
public class ExcecaoValidacao(string mensagem) : Exception(mensagem);
public class ExcecaoTransicaoInvalida(string mensagem) : Exception(mensagem);
