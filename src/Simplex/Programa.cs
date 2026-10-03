namespace Conde.Simplex;

/// <summary>
/// Um programa linear na FORMA PADRAO: maximizar c·x sujeito a Ax &lt;= b e
/// x &gt;= 0.
///
/// A forma padrao nao perde generalidade: minimizar e maximizar o negativo,
/// uma igualdade sao duas desigualdades, e uma variavel livre e a diferenca de
/// duas nao negativas. O que ela compra e um algoritmo so em vez de quatro.
///
/// A restricao b &gt;= 0 NAO e exigida aqui, e e por isso que existe a primeira
/// fase: com algum b negativo, a origem nao e uma solucao viavel e o simplex
/// nao tem por onde comecar.
/// </summary>
public sealed class Programa
{
    public Programa(Fracao[,] restricoes, Fracao[] limites, Fracao[] objetivo)
    {
        ArgumentNullException.ThrowIfNull(restricoes);
        ArgumentNullException.ThrowIfNull(limites);
        ArgumentNullException.ThrowIfNull(objetivo);

        Restricoes = restricoes;
        Limites = limites;
        Objetivo = objetivo;

        if (restricoes.GetLength(0) != limites.Length)
            throw new ArgumentException("a quantidade de linhas nao bate com a de limites", nameof(limites));
        if (restricoes.GetLength(1) != objetivo.Length)
            throw new ArgumentException("a quantidade de colunas nao bate com a do objetivo", nameof(objetivo));
    }

    /// <summary>A matriz A.</summary>
    public Fracao[,] Restricoes { get; }

    /// <summary>O vetor b.</summary>
    public Fracao[] Limites { get; }

    /// <summary>O vetor c.</summary>
    public Fracao[] Objetivo { get; }

    public int Linhas => Limites.Length;

    public int Variaveis => Objetivo.Length;

    /// <summary>Verdadeiro quando x satisfaz todas as restricoes e e nao negativo.</summary>
    public bool Viavel(Fracao[] x)
    {
        ArgumentNullException.ThrowIfNull(x);
        if (x.Length != Variaveis) return false;
        foreach (var valor in x) if (valor.Sinal < 0) return false;

        for (var i = 0; i < Linhas; i++)
        {
            var soma = Fracao.Zero;
            for (var j = 0; j < Variaveis; j++) soma += Restricoes[i, j] * x[j];
            if (soma > Limites[i]) return false;
        }
        return true;
    }

    /// <summary>O valor de c·x.</summary>
    public Fracao Valor(Fracao[] x)
    {
        ArgumentNullException.ThrowIfNull(x);
        var soma = Fracao.Zero;
        for (var j = 0; j < Variaveis; j++) soma += Objetivo[j] * x[j];
        return soma;
    }

    /// <summary>Um programa montado a partir de numeros inteiros, para os testes.</summary>
    public static Programa De(long[,] restricoes, long[] limites, long[] objetivo)
    {
        ArgumentNullException.ThrowIfNull(restricoes);
        ArgumentNullException.ThrowIfNull(limites);
        ArgumentNullException.ThrowIfNull(objetivo);

        var a = new Fracao[restricoes.GetLength(0), restricoes.GetLength(1)];
        for (var i = 0; i < restricoes.GetLength(0); i++)
            for (var j = 0; j < restricoes.GetLength(1); j++) a[i, j] = restricoes[i, j];

        return new Programa(a, [.. limites.Select(x => (Fracao)x)], [.. objetivo.Select(x => (Fracao)x)]);
    }
}

/// <summary>Como o simplex terminou.</summary>
public enum Fim
{
    /// <summary>Achou o maximo.</summary>
    Otimo,

    /// <summary>
    /// O objetivo cresce sem limite: existe direcao de melhora em que nenhuma
    /// restricao segura.
    /// </summary>
    Ilimitado,

    /// <summary>Nao existe nenhum x que satisfaca as restricoes.</summary>
    Inviavel,

    /// <summary>
    /// O algoritmo deu mais pivos que o teto e parou.
    ///
    /// Com a regra de Bland isso nao acontece, e e exatamente por isso que o
    /// resultado existe: ele e o que a regra de Dantzig devolve quando CICLA.
    /// </summary>
    Ciclou,
}

/// <summary>O que o simplex devolve.</summary>
public sealed class Resultado
{
    public Resultado(Fim fim, Fracao[]? solucao, Fracao valor, int pivos, int visitados)
    {
        Fim = fim;
        Solucao = solucao;
        Valor = valor;
        Pivos = pivos;
        BasesVisitadas = visitados;
    }

    public Fim Fim { get; }

    public Fracao[]? Solucao { get; }

    public Fracao Valor { get; }

    /// <summary>Quantas trocas de base o algoritmo fez. E a medida do custo.</summary>
    public int Pivos { get; }

    /// <summary>Quantas bases DISTINTAS ele visitou.</summary>
    public int BasesVisitadas { get; }

    public override string ToString() => Fim switch
    {
        Fim.Otimo => $"otimo {Valor} em {Pivos} pivos",
        Fim.Ilimitado => $"ilimitado, {Pivos} pivos",
        Fim.Inviavel => $"inviavel, {Pivos} pivos",
        _ => $"ciclou depois de {Pivos} pivos em {BasesVisitadas} bases",
    };
}
