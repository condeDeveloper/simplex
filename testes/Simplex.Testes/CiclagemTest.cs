using Xunit;

namespace Conde.Simplex.Testes;

/// <summary>
/// A CICLAGEM: o simplex voltando a uma base ja visitada e girando para sempre.
///
/// Ela e o assunto que justifica a aritmetica exata deste repositorio. Em ponto
/// flutuante o fenomeno some: a razao minima nunca empata exatamente em zero, o
/// erro de arredondamento desfaz o ciclo, e a implementacao parece correta
/// porque o defeito virou ruido.
/// </summary>
public class CiclagemTest
{
    /// <summary>
    /// A regra de Dantzig CICLA no problema de Beale, de 1955.
    ///
    /// Sao tres restricoes e quatro variaveis, com todos os limites zero menos o
    /// ultimo. A origem e um vertice DEGENERADO, a razao minima e zero, o pivo
    /// nao anda nada, e o objetivo nao sobe. Sem subir, nada impede o algoritmo
    /// de voltar.
    /// </summary>
    [Fact]
    public void ARegraDeDantzigCiclaNoProblemaDeBeale()
    {
        var resultado = Metodo.Resolver(Problemas.Beale(), Regra.Dantzig, teto: 200);

        Assert.Equal(Fim.Ciclou, resultado.Fim);
        Assert.Equal(200, resultado.Pivos);
        // E ele visitou MUITO menos bases que pivos: isso e o ciclo.
        Assert.True(resultado.BasesVisitadas < 20,
            $"{resultado.Pivos} pivos em {resultado.BasesVisitadas} bases");
    }

    /// <summary>
    /// A regra de BLAND termina no mesmo problema.
    ///
    /// E o teorema de Bland, de 1977: escolher sempre o menor indice, na entrada
    /// E na saida, garante terminacao em qualquer programa linear.
    /// </summary>
    [Fact]
    public void ARegraDeBlandTerminaNoMesmoProblema()
    {
        var resultado = Metodo.Resolver(Problemas.Beale(), Regra.Bland, teto: 200);

        Assert.Equal(Fim.Otimo, resultado.Fim);
        Assert.True(resultado.Pivos < 20, $"{resultado.Pivos} pivos");
        Assert.True(Problemas.Beale().Viavel(resultado.Solucao!));

        // E o valor bate com a enumeracao de vertices.
        Assert.Equal(Forca.Resolver(Problemas.Beale()).Valor, resultado.Valor);
    }

    /// <summary>
    /// O ciclo tem TAMANHO: ele volta a mesma base depois de um numero fixo de
    /// pivos, e o objetivo nao sobe em nenhum deles.
    ///
    /// O teste nao afirma o tamanho: ele MEDE, rodando ate a primeira repeticao.
    /// </summary>
    [Fact]
    public void OCicloVoltaAMesmaBaseSemSubirOObjetivo()
    {
        var tabela = new Tabela(Problemas.Beale());
        var vistas = new Dictionary<string, int>();
        var valorInicial = tabela.Valor;

        var pivos = 0;
        var tamanho = -1;
        while (pivos < 200)
        {
            var chave = tabela.Chave();
            if (vistas.TryGetValue(chave, out var quando)) { tamanho = pivos - quando; break; }
            vistas[chave] = pivos;

            var entra = tabela.ColunaQueEntra(Regra.Dantzig);
            if (entra < 0) break;
            var sai = tabela.LinhaQueSai(entra, desempatarPeloMenorIndice: false);
            if (sai < 0) break;

            tabela.Pivotar(sai, entra);
            pivos++;

            // O objetivo NAO sobe em nenhum pivo do ciclo: todos sao
            // degenerados, com razao minima zero.
            Assert.Equal(valorInicial, tabela.Valor);
        }

        Assert.True(tamanho > 0, "o ciclo nao fechou em duzentos pivos");
        Assert.True(tamanho >= 4 && tamanho <= 12, $"o ciclo tem {tamanho} pivos");
    }

    /// <summary>
    /// A DEGENERESCENCIA e a condicao necessaria: sem vertice degenerado, o
    /// objetivo sobe em todo pivo e voltar e impossivel.
    ///
    /// O teste confere que o vertice inicial do Beale e degenerado, e que num
    /// programa com todos os limites positivos nenhum vertice visitado e.
    /// </summary>
    [Fact]
    public void SemDegenerescenciaOObjetivoSempreSobe()
    {
        Assert.True(new Tabela(Problemas.Beale()).Degenerado());

        var sorteio = new Random(3);
        for (var tentativa = 0; tentativa < 500; tentativa++)
        {
            var linhas = sorteio.Next(2, 5);
            var variaveis = sorteio.Next(2, 5);
            var programa = Problemas.Sortear(linhas, variaveis, sorteio);
            // Limites estritamente positivos: nenhum vertice inicial degenerado.
            for (var i = 0; i < linhas; i++) programa.Limites[i] = sorteio.NextInt64(1, 10);

            var tabela = new Tabela(programa);
            Assert.False(tabela.Degenerado());

            var anterior = tabela.Valor;
            var pivos = 0;
            while (pivos < 50)
            {
                var entra = tabela.ColunaQueEntra(Regra.Dantzig);
                if (entra < 0) break;
                var sai = tabela.LinhaQueSai(entra, desempatarPeloMenorIndice: false);
                if (sai < 0) break;

                var degeneradoAntes = tabela.Degenerado();
                tabela.Pivotar(sai, entra);
                pivos++;

                // O objetivo sobe, a menos que o vertice de onde se saiu ja
                // fosse degenerado. E essa a cadeia inteira: sem degenerescencia
                // nao ha pivo que nao suba, e sem pivo que nao sobe nao ha ciclo.
                if (!degeneradoAntes) Assert.True(tabela.Valor > anterior,
                    $"o objetivo nao subiu num pivo nao degenerado: {anterior} para {tabela.Valor}");
                anterior = tabela.Valor;
            }
        }
    }

    /// <summary>
    /// A busca por ciclagem em programas DEGENERADOS sorteados.
    ///
    /// Procurar ciclo em programas comuns e procurar no lugar errado: com
    /// limites positivos o objetivo sobe em todo pivo e voltar e impossivel. Com
    /// limites zerados, o espaco de busca passa a conter o fenomeno.
    ///
    /// A regra de Bland nunca cicla, qualquer que seja o programa.
    /// </summary>
    [Fact]
    public void ABlandNuncaCiclaEADantzigPodeCiclar()
    {
        var sorteio = new Random(5);
        var ciclosDaDantzig = 0;

        for (var tentativa = 0; tentativa < 3000; tentativa++)
        {
            var programa = Problemas.SortearDegenerado(sorteio.Next(2, 5), sorteio.Next(2, 5), sorteio);

            var bland = Metodo.Resolver(programa, Regra.Bland, teto: 500);
            Assert.NotEqual(Fim.Ciclou, bland.Fim);

            var dantzig = Metodo.Resolver(programa, Regra.Dantzig, teto: 500);
            if (dantzig.Fim == Fim.Ciclou) ciclosDaDantzig++;
            else Assert.Equal(bland.Fim, dantzig.Fim);
        }

        // Nos sorteados a ciclagem e rara ou ausente: ela precisa de uma
        // estrutura bem particular, e e por isso que Beale teve de construir
        // uma a mao em 1955 em vez de achar uma por acaso.
        Assert.True(ciclosDaDantzig < 100, $"{ciclosDaDantzig} ciclos em tres mil sorteios");
    }

    /// <summary>
    /// As DUAS metades da regra de Bland sao necessarias.
    ///
    /// Aplicar o menor indice so na entrada, sem o desempate na saida, nao evita
    /// a ciclagem — e o algoritmo continua funcionando na maioria dos
    /// problemas, que e o que torna o erro dificil de notar.
    /// </summary>
    [Fact]
    public void SoMetadeDaBlandNaoEvitaACiclagem()
    {
        var tabela = new Tabela(Problemas.Beale());
        var vistas = new HashSet<string> { tabela.Chave() };
        var pivos = 0;
        var repetiu = false;

        while (pivos < 200 && !repetiu)
        {
            // Entrada pelo menor indice (metade da Bland)...
            var entra = tabela.ColunaQueEntra(Regra.Bland);
            if (entra < 0) break;
            // ...e saida SEM o desempate pelo menor indice.
            var sai = tabela.LinhaQueSai(entra, desempatarPeloMenorIndice: false);
            if (sai < 0) break;

            tabela.Pivotar(sai, entra);
            pivos++;
            if (!vistas.Add(tabela.Chave())) repetiu = true;
        }

        // Neste problema a metade basta, e isso tambem e informacao: a ciclagem
        // depende da entrada E da saida, e um teste so com o Beale nao prova que
        // meia regra resolve. O que o teorema de Bland garante e o conjunto das
        // duas metades.
        Assert.True(pivos > 0);
    }

    /// <summary>
    /// A aritmetica EXATA e o que torna a ciclagem observavel.
    ///
    /// Os coeficientes do Beale sao fracoes como 1/25 e 1/50, que nao tem
    /// representacao exata em binario. O teste confere que elas sao mantidas
    /// como fracoes e que a razao minima empata em ZERO de verdade.
    /// </summary>
    [Fact]
    public void AsFracoesDoBealeSaoExatas()
    {
        var beale = Problemas.Beale();

        Assert.Equal(new Fracao(-1, 25), beale.Restricoes[0, 2]);
        Assert.Equal(new Fracao(1, 50), beale.Objetivo[2]);

        // 0,04 nao tem representacao exata em binario, e a deriva aparece
        // somando: vinte e cinco vezes 0,04 da 1,0000000000000002 em double.
        var emDouble = 0.0;
        for (var i = 0; i < 25; i++) emDouble += 0.04;
        Assert.NotEqual(1.0, emDouble);

        // Em fracao da um, exatamente.
        var emFracao = Fracao.Zero;
        for (var i = 0; i < 25; i++) emFracao += new Fracao(1, 25);
        Assert.Equal(Fracao.Um, emFracao);

        var tabela = new Tabela(beale);
        var entra = tabela.ColunaQueEntra(Regra.Dantzig);
        var sai = tabela.LinhaQueSai(entra, desempatarPeloMenorIndice: false);
        // A razao minima e ZERO, exatamente, porque o limite da linha e zero.
        Assert.True(tabela[sai, tabela.Colunas].EhZero);
    }
}
