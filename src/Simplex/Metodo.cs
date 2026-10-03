namespace Conde.Simplex;

/// <summary>
/// O metodo simplex, de George Dantzig, 1947.
///
/// A ideia cabe em tres frases. O maximo de uma funcao linear sobre um poliedro
/// esta sempre num vertice. Cada vertice corresponde a uma escolha de quais
/// variaveis valem zero. Entao basta caminhar de vertice em vertice, sempre
/// subindo, ate que nenhum vizinho suba.
///
/// O que o metodo NAO tem e garantia de tempo polinomial. Klee e Minty
/// mostraram em 1972 um cubo deformado em que a regra de Dantzig visita TODOS os
/// 2^n vertices. Na pratica ele e rapidissimo, e essa distancia entre o pior
/// caso e o caso tipico e um dos assuntos mais interessantes de otimizacao.
///
/// Aqui ha duas fases, e a primeira existe por um motivo especifico: com algum
/// limite negativo, a origem nao e viavel e o simplex nao tem por onde comecar.
/// A primeira fase resolve um problema auxiliar cuja resposta e um vertice
/// viavel do problema de verdade.
/// </summary>
public static class Metodo
{
    /// <summary>
    /// Resolve, assumindo que a origem e viavel (todos os limites nao
    /// negativos).
    ///
    /// O teto de pivos existe para a ciclagem poder ser OBSERVADA em vez de
    /// travar o processo. Com a regra de Bland ele nunca e alcancado.
    /// </summary>
    public static Resultado Resolver(Programa programa, Regra regra = Regra.Bland, int teto = 10_000)
    {
        ArgumentNullException.ThrowIfNull(programa);
        foreach (var limite in programa.Limites)
            if (limite.Sinal < 0)
                throw new ArgumentException("ha limite negativo: use ResolverComDuasFases", nameof(programa));

        var tabela = new Tabela(programa);
        return Girar(tabela, regra, teto);
    }

    private static Resultado Girar(Tabela tabela, Regra regra, int teto)
    {
        var vistas = new HashSet<string> { tabela.Chave() };
        var pivos = 0;

        while (pivos < teto)
        {
            var entra = tabela.ColunaQueEntra(regra);
            if (entra < 0)
                return new Resultado(Fim.Otimo, tabela.Solucao(), tabela.Valor, pivos, vistas.Count);

            // A regra de Bland tambem desempata a SAIDA pelo menor indice. As
            // duas metades sao necessarias: so a entrada nao evita a ciclagem.
            var sai = tabela.LinhaQueSai(entra, desempatarPeloMenorIndice: regra == Regra.Bland);
            if (sai < 0)
                return new Resultado(Fim.Ilimitado, null, Fracao.Zero, pivos, vistas.Count);

            tabela.Pivotar(sai, entra);
            pivos++;
            vistas.Add(tabela.Chave());
        }

        return new Resultado(Fim.Ciclou, tabela.Solucao(), tabela.Valor, pivos, vistas.Count);
    }

    /// <summary>
    /// As DUAS FASES: acha um vertice viavel e so entao otimiza.
    ///
    /// A primeira fase acrescenta UMA variavel artificial, com coeficiente -1
    /// em todas as linhas, e maximiza o negativo dela. Fazendo x0 grande o
    /// bastante, qualquer ponto vira viavel; entao o problema auxiliar sempre
    /// tem solucao, e o maximo e zero exatamente quando o problema original tem
    /// ponto viavel.
    ///
    /// Eu tinha escrito outra coisa antes: uma artificial POR LINHA de limite
    /// negativo, com a linha multiplicada por -1. Isso vem do caso de
    /// igualdades e nao funciona com desigualdades do tipo menor ou igual,
    /// porque inverter a linha inverte o SENTIDO da restricao. O resultado era
    /// uma primeira fase que declarava inviaveis programas perfeitamente
    /// viaveis, e so a comparacao com a enumeracao de vertices pegou isso.
    /// </summary>
    public static Resultado ResolverComDuasFases(Programa programa, Regra regra = Regra.Bland, int teto = 10_000)
    {
        ArgumentNullException.ThrowIfNull(programa);

        var temNegativo = false;
        foreach (var limite in programa.Limites) if (limite.Sinal < 0) temNegativo = true;
        if (!temNegativo) return Resolver(programa, regra, teto);

        var tabela = Tabela.PrimeiraFase(programa);

        // O primeiro pivo e forcado: x0 entra na linha de limite mais negativo.
        // Depois dele todos os limites ficam nao negativos, e dai em diante o
        // giro e o giro normal.
        var pior = tabela.LinhaMaisNegativa();
        tabela.Pivotar(pior, tabela.Artificial);

        var primeira = Girar(tabela, regra, teto);
        if (primeira.Fim == Fim.Ciclou) return primeira;
        if (primeira.Fim != Fim.Otimo || primeira.Valor.Sinal < 0)
            return new Resultado(Fim.Inviavel, null, Fracao.Zero, primeira.Pivos, primeira.BasesVisitadas);

        // Viavel. Tira x0 da base se ele ficou nela valendo zero.
        var linhaDoX0 = tabela.LinhaDaBase(tabela.Artificial);
        if (linhaDoX0 >= 0)
            for (var j = 0; j < tabela.Colunas; j++)
            {
                if (j == tabela.Artificial || tabela[linhaDoX0, j].EhZero) continue;
                tabela.Pivotar(linhaDoX0, j);
                break;
            }
        tabela.Proibir(tabela.Artificial);

        // Segunda fase: o objetivo de verdade, zero nas colunas que nao sao
        // variaveis originais.
        var custos = new Fracao[tabela.Colunas];
        for (var j = 0; j < programa.Variaveis; j++) custos[j] = programa.Objetivo[j];
        tabela.TrocarObjetivo(custos);

        var segunda = Girar(tabela, regra, teto);
        return new Resultado(segunda.Fim, segunda.Solucao, segunda.Valor,
                             primeira.Pivos + segunda.Pivos + 1,
                             primeira.BasesVisitadas + segunda.BasesVisitadas);
    }
}
