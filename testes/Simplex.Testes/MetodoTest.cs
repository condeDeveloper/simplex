using Xunit;

namespace Conde.Simplex.Testes;

/// <summary>
/// O simplex contra a ENUMERACAO DE VERTICES.
///
/// O juiz nao usa pivo, nao usa base, nao usa razao minima: ele escolhe quais
/// restricoes valem com igualdade, resolve o sistema por eliminacao de Gauss
/// exata, confere se o ponto e viavel, e fica com o melhor. Sao C(m+n, n)
/// sistemas, o que explode rapido e e exatamente por isso que o simplex existe.
/// </summary>
public class MetodoTest
{
    /// <summary>
    /// O problema de producao que esta em todo livro: o otimo e x = 2, y = 6,
    /// valor 36.
    /// </summary>
    [Fact]
    public void OProblemaDeProducaoDaOValorConhecido()
    {
        foreach (var regra in Enum.GetValues<Regra>())
        {
            var resultado = Metodo.Resolver(Problemas.Producao(), regra);
            Assert.Equal(Fim.Otimo, resultado.Fim);
            Assert.Equal(new Fracao(36), resultado.Valor);
            Assert.Equal(new Fracao(2), resultado.Solucao![0]);
            Assert.Equal(new Fracao(6), resultado.Solucao[1]);
        }
    }

    /// <summary>
    /// As tres regras dao o MESMO valor otimo, em mil programas sorteados,
    /// conferido contra a enumeracao.
    ///
    /// A escolha da coluna que entra nao muda a resposta: muda quantos pivos
    /// sao precisos, e, no caso de Dantzig, pode fazer o algoritmo nao terminar.
    /// </summary>
    [Fact]
    public void AsTresRegrasConcordamComAEnumeracaoDeVertices()
    {
        var sorteio = new Random(3);
        var otimos = 0;

        for (var tentativa = 0; tentativa < 1000; tentativa++)
        {
            var linhas = sorteio.Next(1, 4);
            var variaveis = sorteio.Next(1, 4);
            var programa = Problemas.Sortear(linhas, variaveis, sorteio);

            var pelaForca = Forca.Resolver(programa);

            foreach (var regra in Enum.GetValues<Regra>())
            {
                var resultado = Metodo.Resolver(programa, regra);
                Assert.Equal(pelaForca.Fim, resultado.Fim);
                if (resultado.Fim != Fim.Otimo) continue;

                otimos++;
                Assert.Equal(pelaForca.Valor, resultado.Valor);
                Assert.True(programa.Viavel(resultado.Solucao!), "a solucao nao e viavel");
                Assert.Equal(resultado.Valor, programa.Valor(resultado.Solucao!));
            }
        }
        Assert.True(otimos > 1000, $"so {otimos} casos otimos; o sorteio ficou degenerado demais");
    }

    /// <summary>
    /// A solucao devolvida e sempre VIAVEL e o valor dela bate com o objetivo.
    ///
    /// Comparar so o valor otimo deixaria passar um algoritmo que acerta o
    /// numero e devolve um ponto que nao satisfaz as restricoes.
    /// </summary>
    [Fact]
    public void ASolucaoDevolvidaEhViavelEOValorBate()
    {
        var sorteio = new Random(5);
        for (var tentativa = 0; tentativa < 2000; tentativa++)
        {
            var programa = Problemas.Sortear(sorteio.Next(1, 5), sorteio.Next(1, 5), sorteio);
            var resultado = Metodo.Resolver(programa);
            if (resultado.Fim != Fim.Otimo) continue;

            Assert.True(programa.Viavel(resultado.Solucao!));
            Assert.Equal(resultado.Valor, programa.Valor(resultado.Solucao!));
        }
    }

    /// <summary>
    /// O caso ILIMITADO: crescer x e y juntos nunca viola x - y &lt;= 1.
    /// </summary>
    [Fact]
    public void OProblemaIlimitadoEhReconhecido()
    {
        foreach (var regra in Enum.GetValues<Regra>())
            Assert.Equal(Fim.Ilimitado, Metodo.Resolver(Problemas.Ilimitado(), regra).Fim);

        Assert.Equal(Fim.Ilimitado, Forca.Resolver(Problemas.Ilimitado()).Fim);
    }

    /// <summary>
    /// O caso INVIAVEL, que precisa da primeira fase: com limite negativo, a
    /// origem nao e viavel e o simplex nao tem por onde comecar.
    /// </summary>
    [Fact]
    public void OProblemaInviavelEhReconhecidoPelaPrimeiraFase()
    {
        var inviavel = Problemas.Inviavel();
        // Sem a primeira fase, o metodo recusa em vez de devolver lixo.
        Assert.Throws<ArgumentException>(() => Metodo.Resolver(inviavel));
        Assert.Equal(Fim.Inviavel, Metodo.ResolverComDuasFases(inviavel).Fim);
        Assert.Equal(Fim.Inviavel, Forca.Resolver(inviavel).Fim);
    }

    /// <summary>
    /// As duas fases contra a enumeracao, em programas com limites negativos.
    /// </summary>
    [Fact]
    public void AsDuasFasesConcordamComAEnumeracao()
    {
        var sorteio = new Random(7);
        int viaveis = 0, inviaveis = 0;

        for (var tentativa = 0; tentativa < 500; tentativa++)
        {
            var linhas = sorteio.Next(2, 4);
            var variaveis = sorteio.Next(1, 4);
            var programa = Problemas.Sortear(linhas, variaveis, sorteio);
            // Torna algum limite negativo: a origem deixa de ser viavel.
            programa.Limites[sorteio.Next(linhas)] = -sorteio.NextInt64(1, 5);

            var pelaForca = Forca.Resolver(programa);
            var pelasFases = Metodo.ResolverComDuasFases(programa);

            Assert.Equal(pelaForca.Fim, pelasFases.Fim);
            if (pelaForca.Fim == Fim.Otimo)
            {
                viaveis++;
                Assert.Equal(pelaForca.Valor, pelasFases.Valor);
                Assert.True(programa.Viavel(pelasFases.Solucao!));
            }
            else if (pelaForca.Fim == Fim.Inviavel) inviaveis++;
        }

        Assert.True(viaveis > 50, $"so {viaveis} casos viaveis");
        Assert.True(inviaveis > 50, $"so {inviaveis} casos inviaveis");
    }

    /// <summary>
    /// A solucao otima esta sempre num VERTICE, que e o teorema que faz o
    /// simplex existir.
    ///
    /// O teste confere isso de forma direta: a solucao devolvida tem pelo menos
    /// n restricoes ativas, onde n e a quantidade de variaveis.
    /// </summary>
    [Fact]
    public void OOtimoEstaSempreNumVertice()
    {
        var sorteio = new Random(11);
        for (var tentativa = 0; tentativa < 500; tentativa++)
        {
            var linhas = sorteio.Next(2, 5);
            var variaveis = sorteio.Next(1, 4);
            var programa = Problemas.Sortear(linhas, variaveis, sorteio);
            var resultado = Metodo.Resolver(programa);
            if (resultado.Fim != Fim.Otimo) continue;

            var x = resultado.Solucao!;
            var ativas = 0;
            for (var j = 0; j < variaveis; j++) if (x[j].EhZero) ativas++;
            for (var i = 0; i < linhas; i++)
            {
                var soma = Fracao.Zero;
                for (var j = 0; j < variaveis; j++) soma += programa.Restricoes[i, j] * x[j];
                if (soma == programa.Limites[i]) ativas++;
            }

            Assert.True(ativas >= variaveis,
                $"so {ativas} restricoes ativas com {variaveis} variaveis: nao e vertice");
        }
    }

    /// <summary>
    /// O pivo e eliminacao de Gauss: depois dele, a coluna que entrou tem 1 na
    /// linha pivotada e zero em todas as outras.
    /// </summary>
    [Fact]
    public void OPivoDeixaAColunaCanonica()
    {
        var sorteio = new Random(13);
        for (var tentativa = 0; tentativa < 200; tentativa++)
        {
            var programa = Problemas.Sortear(sorteio.Next(2, 5), sorteio.Next(2, 5), sorteio);
            var tabela = new Tabela(programa);

            var entra = tabela.ColunaQueEntra(Regra.Bland);
            if (entra < 0) continue;
            var sai = tabela.LinhaQueSai(entra, desempatarPeloMenorIndice: true);
            if (sai < 0) continue;

            tabela.Pivotar(sai, entra);

            Assert.Equal(Fracao.Um, tabela[sai, entra]);
            for (var i = 0; i <= tabela.Linhas; i++)
                if (i != sai) Assert.True(tabela[i, entra].EhZero, $"linha {i} nao zerou");
            Assert.Equal(entra, tabela.Base[sai]);
        }
    }

    [Fact]
    public void ParametrosInvalidosSaoRecusados()
    {
        Assert.Throws<ArgumentException>(() => new Programa(new Fracao[2, 2], new Fracao[3], new Fracao[2]));
        Assert.Throws<ArgumentException>(() => new Programa(new Fracao[2, 2], new Fracao[2], new Fracao[3]));
        Assert.Throws<DivideByZeroException>(() => new Fracao(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Problemas.KleeMinty(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Problemas.KleeMinty(13));

        var tabela = new Tabela(Problemas.Producao());
        Assert.Throws<ArgumentOutOfRangeException>(() => tabela.Pivotar(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => tabela.Pivotar(0, 99));
    }
}
