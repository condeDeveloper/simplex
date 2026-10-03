namespace Conde.Simplex;

/// <summary>
/// O quadro do simplex: a matriz com as folgas, os limites e a linha do
/// objetivo.
///
/// A ideia do metodo e geometrica e vale ter em mente enquanto se le o codigo.
/// As restricoes definem um poliedro, o maximo de uma funcao linear sobre um
/// poliedro esta sempre num VERTICE, e cada vertice corresponde a uma escolha de
/// quais variaveis valem zero. O simplex caminha de vertice em vértice, sempre
/// subindo, por arestas.
///
/// O quadro e a representacao algebrica de um vertice. A BASE e o conjunto de
/// variaveis que podem ser diferentes de zero; as outras sao zero. Um pivo troca
/// uma variavel da base por outra, o que e andar para um vertice vizinho.
///
/// Toda a aritmetica e exata. O algoritmo decide por comparacao de sinal, e em
/// ponto flutuante um zero que virou 1e-17 troca o sinal, pivota numa linha que
/// nao podia, e produz uma base invalida.
/// </summary>
public sealed class Tabela
{
    private readonly Fracao[,] celulas;

    /// <summary>
    /// Monta o quadro a partir do programa, acrescentando uma folga por
    /// restricao.
    ///
    /// A folga e a diferenca entre o limite e o que foi usado. Com ela, toda
    /// desigualdade vira igualdade, e a base inicial e justamente o conjunto das
    /// folgas: o vertice da origem.
    /// </summary>
    public Tabela(Programa programa)
    {
        ArgumentNullException.ThrowIfNull(programa);
        Programa = programa;
        Linhas = programa.Linhas;
        Variaveis = programa.Variaveis;
        Colunas = Variaveis + Linhas;

        celulas = new Fracao[Linhas + 1, Colunas + 1];
        for (var i = 0; i < Linhas; i++)
        {
            for (var j = 0; j < Variaveis; j++) celulas[i, j] = programa.Restricoes[i, j];
            celulas[i, Variaveis + i] = Fracao.Um;            // a folga da linha i
            celulas[i, Colunas] = programa.Limites[i];
        }

        // A linha do objetivo guarda o NEGATIVO dos custos: assim "melhora"
        // vira "tem entrada negativa", e a condicao de parada fica uma so.
        for (var j = 0; j < Variaveis; j++) celulas[Linhas, j] = -programa.Objetivo[j];

        Base = new int[Linhas];
        for (var i = 0; i < Linhas; i++) Base[i] = Variaveis + i;
    }

    /// <summary>
    /// O quadro da PRIMEIRA FASE, com uma variavel artificial so.
    ///
    /// A construcao e a classica: acrescenta-se x0 com coeficiente -1 em TODA
    /// linha, o que relaxa todas as restricoes de uma vez, e maximiza-se -x0.
    /// Se o maximo for zero, existe ponto viavel; se for negativo, nao existe.
    ///
    /// Uma artificial por linha negativa, que foi a minha primeira tentativa,
    /// nao funciona com desigualdades do tipo menor ou igual: ela muda o SENTIDO
    /// da restricao e a primeira fase passa a dizer que programas viaveis sao
    /// inviaveis. Com uma artificial so o sentido nunca muda.
    /// </summary>
    public static Tabela PrimeiraFase(Programa programa)
    {
        ArgumentNullException.ThrowIfNull(programa);
        var linhas = programa.Linhas;
        var variaveis = programa.Variaveis;
        var artificial = variaveis;                       // a coluna de x0
        var colunas = variaveis + 1 + linhas;

        var celulas = new Fracao[linhas + 1, colunas + 1];
        for (var i = 0; i < linhas; i++)
        {
            for (var j = 0; j < variaveis; j++) celulas[i, j] = programa.Restricoes[i, j];
            celulas[i, artificial] = -Fracao.Um;
            celulas[i, variaveis + 1 + i] = Fracao.Um;
            celulas[i, colunas] = programa.Limites[i];
        }
        // Maximizar -x0: a linha guarda o negativo do custo, entao +1 em x0.
        celulas[linhas, artificial] = Fracao.Um;

        var baseInicial = new int[linhas];
        for (var i = 0; i < linhas; i++) baseInicial[i] = variaveis + 1 + i;

        return new Tabela(programa, celulas, baseInicial, linhas, variaveis, colunas) { Artificial = artificial };
    }

    private Tabela(Programa programa, Fracao[,] celulas, int[] baseInicial,
                   int linhas, int variaveis, int colunas)
    {
        Programa = programa;
        this.celulas = celulas;
        Base = baseInicial;
        Linhas = linhas;
        Variaveis = variaveis;
        Colunas = colunas;
    }

    /// <summary>
    /// A coluna da variavel artificial, ou -1 quando nao ha nenhuma.
    ///
    /// Depois da primeira fase ela precisa ficar PROIBIDA de voltar: o custo
    /// dela na segunda fase e zero, entao ela nao entraria por melhora, mas um
    /// empate no desempate de Bland pode traze-la de volta e quebrar a
    /// viabilidade.
    /// </summary>
    public int Artificial { get; private init; } = -1;

    public Programa Programa { get; }

    public int Linhas { get; }

    public int Variaveis { get; }

    /// <summary>Variaveis mais folgas.</summary>
    public int Colunas { get; }

    /// <summary>Qual variavel esta na base em cada linha.</summary>
    public int[] Base { get; }

    public Fracao this[int i, int j] => celulas[i, j];

    /// <summary>O valor do objetivo no vertice atual.</summary>
    public Fracao Valor => celulas[Linhas, Colunas];

    /// <summary>
    /// O PIVO: troca a variavel da base da linha pela coluna que entra.
    ///
    /// Normaliza a linha dividindo pelo elemento pivo e zera a coluna em todas
    /// as outras linhas, inclusive na do objetivo. E eliminacao de Gauss, e e a
    /// unica operacao que o simplex faz.
    /// </summary>
    public void Pivotar(int linha, int coluna)
    {
        if (linha < 0 || linha >= Linhas) throw new ArgumentOutOfRangeException(nameof(linha));
        if (coluna < 0 || coluna >= Colunas) throw new ArgumentOutOfRangeException(nameof(coluna));
        var pivo = celulas[linha, coluna];
        if (pivo.EhZero) throw new InvalidOperationException("pivo zero: essa linha nao podia pivotar");

        for (var j = 0; j <= Colunas; j++) celulas[linha, j] /= pivo;

        for (var i = 0; i <= Linhas; i++)
        {
            if (i == linha) continue;
            var fator = celulas[i, coluna];
            if (fator.EhZero) continue;
            for (var j = 0; j <= Colunas; j++) celulas[i, j] -= fator * celulas[linha, j];
        }

        Base[linha] = coluna;
    }

    /// <summary>
    /// A LINHA QUE SAI, pela razao minima: entre as linhas com coeficiente
    /// positivo na coluna que entra, a de menor limite dividido pelo
    /// coeficiente.
    ///
    /// A razao diz ate onde a variavel que entra pode crescer antes de alguma
    /// outra ficar negativa. Linhas com coeficiente negativo ou zero nao
    /// seguram nada: crescer nelas nunca viola a restricao.
    ///
    /// Devolve -1 quando NENHUMA linha segura, e isso significa ilimitado.
    ///
    /// O desempate por MENOR INDICE e metade da regra de Bland, e nao e
    /// enfeite: sem ele, a outra metade nao evita a ciclagem.
    /// </summary>
    public int LinhaQueSai(int coluna, bool desempatarPeloMenorIndice)
    {
        var escolhida = -1;
        var melhor = Fracao.Zero;

        for (var i = 0; i < Linhas; i++)
        {
            if (celulas[i, coluna].Sinal <= 0) continue;
            var razao = celulas[i, Colunas] / celulas[i, coluna];

            if (escolhida < 0 || razao < melhor)
            {
                escolhida = i;
                melhor = razao;
            }
            else if (razao == melhor && desempatarPeloMenorIndice && Base[i] < Base[escolhida])
            {
                escolhida = i;
            }
        }
        return escolhida;
    }

    /// <summary>Colunas que nao podem mais entrar na base.</summary>
    private readonly HashSet<int> proibidas = [];

    public void Proibir(int coluna) => proibidas.Add(coluna);

    /// <summary>
    /// A COLUNA QUE ENTRA, pela regra pedida. Devolve -1 quando nenhuma melhora,
    /// o que significa que o vertice atual e otimo.
    /// </summary>
    public int ColunaQueEntra(Regra regra)
    {
        var escolhida = -1;

        switch (regra)
        {
            case Regra.Bland:
                // O MENOR INDICE entre as que melhoram. Nada de olhar o quanto.
                for (var j = 0; j < Colunas; j++)
                    if (!proibidas.Contains(j) && celulas[Linhas, j].Sinal < 0) return j;
                return -1;

            case Regra.Dantzig:
                var maisNegativo = Fracao.Zero;
                for (var j = 0; j < Colunas; j++)
                    if (!proibidas.Contains(j) && celulas[Linhas, j] < maisNegativo)
                    { maisNegativo = celulas[Linhas, j]; escolhida = j; }
                return escolhida;

            case Regra.MaiorAumento:
                var melhorGanho = Fracao.Zero;
                for (var j = 0; j < Colunas; j++)
                {
                    if (proibidas.Contains(j) || celulas[Linhas, j].Sinal >= 0) continue;
                    var linha = LinhaQueSai(j, desempatarPeloMenorIndice: true);
                    if (linha < 0) return j;                  // ilimitado por esta coluna
                    var ganho = -celulas[Linhas, j] * (celulas[linha, Colunas] / celulas[linha, j]);
                    if (escolhida < 0 || ganho > melhorGanho) { melhorGanho = ganho; escolhida = j; }
                }
                return escolhida;

            default:
                throw new NotSupportedException(regra.ToString());
        }
    }

    /// <summary>
    /// Troca a linha do objetivo, mantendo a base.
    ///
    /// Depois da troca, as colunas da base precisam voltar a ter zero na linha
    /// do objetivo, senao o custo reduzido de todas as outras sai errado. E por
    /// isso que a eliminacao no fim nao e opcional.
    /// </summary>
    public void TrocarObjetivo(Fracao[] custos)
    {
        ArgumentNullException.ThrowIfNull(custos);
        if (custos.Length != Colunas) throw new ArgumentException("um custo por coluna", nameof(custos));

        for (var j = 0; j < Colunas; j++) celulas[Linhas, j] = -custos[j];
        celulas[Linhas, Colunas] = Fracao.Zero;

        for (var i = 0; i < Linhas; i++)
        {
            var fator = celulas[Linhas, Base[i]];
            if (fator.EhZero) continue;
            for (var j = 0; j <= Colunas; j++) celulas[Linhas, j] -= fator * celulas[i, j];
        }
    }

    /// <summary>A linha em que a coluna esta na base, ou -1.</summary>
    public int LinhaDaBase(int coluna)
    {
        for (var i = 0; i < Linhas; i++) if (Base[i] == coluna) return i;
        return -1;
    }

    /// <summary>A linha de limite mais negativo, ou -1 quando todos sao nao negativos.</summary>
    public int LinhaMaisNegativa()
    {
        var escolhida = -1;
        for (var i = 0; i < Linhas; i++)
            if (celulas[i, Colunas].Sinal < 0 && (escolhida < 0 || celulas[i, Colunas] < celulas[escolhida, Colunas]))
                escolhida = i;
        return escolhida;
    }

    /// <summary>
    /// A solucao do vertice atual: as variaveis da base valem o limite da linha
    /// delas, as outras valem zero.
    /// </summary>
    public Fracao[] Solucao()
    {
        var x = new Fracao[Variaveis];
        for (var i = 0; i < Linhas; i++)
            if (Base[i] < Variaveis) x[Base[i]] = celulas[i, Colunas];
        return x;
    }

    /// <summary>
    /// A base atual como uma chave, para detectar repeticao.
    ///
    /// A ciclagem e exatamente isto: voltar a uma base ja visitada com o mesmo
    /// valor de objetivo. Sem guardar as bases, o unico sintoma e o laco nao
    /// terminar, e nao da para distinguir ciclagem de problema grande.
    /// </summary>
    public string Chave()
    {
        var copia = (int[])Base.Clone();
        Array.Sort(copia);
        return string.Join(",", copia);
    }

    /// <summary>
    /// Verdadeiro quando o vertice atual e DEGENERADO: alguma variavel da base
    /// vale zero.
    ///
    /// A degenerescencia e a condicao necessaria da ciclagem. Num vertice
    /// degenerado, a razao minima e zero, o pivo nao anda nada, e o objetivo nao
    /// sobe. Sem isso o simplex sempre melhora e nunca pode voltar.
    /// </summary>
    public bool Degenerado()
    {
        for (var i = 0; i < Linhas; i++) if (celulas[i, Colunas].EhZero) return true;
        return false;
    }
}
