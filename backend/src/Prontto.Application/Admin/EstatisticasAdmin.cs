namespace Prontto.Application.Admin;

public record EstatisticasAdmin(EstatisticasUsuarios Usuarios, EstatisticasServicos Servicos, EstatisticasReceita Receita);
public record EstatisticasUsuarios(int Total, int Clientes, int Prestadores);
public record EstatisticasServicos(int Total, int Pendentes, int EmAndamento, int Concluidos);
public record EstatisticasReceita(decimal Ganha, decimal Pendente, decimal Gmv);
