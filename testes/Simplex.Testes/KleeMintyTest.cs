using Xunit;

namespace Conde.Simplex.Testes;

/// <summary>
/// O cubo de Klee e Minty, 1972: o problema em que a regra de Dantzig visita
/// TODOS os 2^n vertices.
///
/// E o resultado que separa "rapido na pratica" de "polinomial". O simplex e o
/// algoritmo mais usado em otimizacao ha setenta anos e o pior caso dele e
/// exponencial.
/// </summary>
public class KleeMintyTest
{
    /// <summary>
    /// A regra de Dantzig gasta EXATAMENTE 2^n - 1 pivos no cubo de ordem n.
    ///
    /// Nao e "da ordem de": e o numero exato, e o teste confere um por um. Se
    /// fosse so uma ordem de grandeza, a construcao de Klee e Minty nao seria
    /// um resultado.
    /// </summary>
    [Fact]
    public void ADantzigVisitaTodosOsDoisElevadoANVertices()
    {
        for (var n = 1; n <= 9; n++)
        {
            var resultado = Metodo.Resolver(Problemas.KleeMinty(n), Regra.Dantzig);
            Assert.Equal(Fim.Otimo, resultado.Fim);
            Assert.Equal((1 << n) - 1, resultado.Pivos);
        }
    }

    /// <summary>
    /// A regra de BLAND tambem e exponencial no cubo, com base MENOR.
    ///
    /// Eu ia escrever que ela resolve em n pivos. Nao resolve. A sequencia
    /// medida e 1, 3, 5, 9, 15, 25, 41, 67, 109, 177, e ela obedece a
    /// recorrencia a(n) = a(n-1) + a(n-2) + 1, que e a de Fibonacci com um
    /// termo constante. O crescimento e geometrico de razao igual a RAZAO
    /// AUREA, perto de 1,618.
    ///
    /// Entao Bland nao escapa do cubo: ela so cai mais devagar. Com dez
    /// variaveis, 177 pivos contra 1.023 da regra de Dantzig.
    /// </summary>
    [Fact]
    public void ABlandTambemEhExponencialNoCuboMasComBaseMenor()
    {
        var medidos = new List<int>();
        for (var n = 1; n <= 10; n++)
        {
            var bland = Metodo.Resolver(Problemas.KleeMinty(n), Regra.Bland);
            Assert.Equal(Fim.Otimo, bland.Fim);
            medidos.Add(bland.Pivos);
        }

        Assert.Equal([1, 3, 5, 9, 15, 25, 41, 67, 109, 177], medidos);

        // A recorrencia, conferida termo a termo.
        for (var i = 2; i < medidos.Count; i++)
            Assert.Equal(medidos[i - 1] + medidos[i - 2] + 1, medidos[i]);

        // E a razao tende a razao aurea, nao a um.
        var razao = medidos[^1] / (double)medidos[^2];
        Assert.InRange(razao, 1.55, 1.68);

        // Ainda assim ela ganha muito da Dantzig, que dobra.
        var dantzig = Metodo.Resolver(Problemas.KleeMinty(10), Regra.Dantzig).Pivos;
        Assert.Equal(1023, dantzig);
        Assert.True(medidos[^1] * 5 < dantzig);
    }

    /// <summary>
    /// O valor otimo do cubo e 100^(n-1), que e o ultimo limite.
    ///
    /// O otimo esta num vertice so, e todas as regras chegam nele; o que muda e
    /// por onde elas passam.
    /// </summary>
    [Fact]
    public void OOtimoDoCuboEhOUltimoLimite()
    {
        for (var n = 1; n <= 7; n++)
        {
            var programa = Problemas.KleeMinty(n);
            var esperado = Fracao.Um;
            for (var i = 0; i < n - 1; i++) esperado *= 100;

            foreach (var regra in Enum.GetValues<Regra>())
            {
                var resultado = Metodo.Resolver(programa, regra);
                Assert.Equal(Fim.Otimo, resultado.Fim);
                Assert.Equal(esperado, resultado.Valor);
                Assert.True(programa.Viavel(resultado.Solucao!));
            }
        }
    }

    /// <summary>
    /// E a enumeracao de vertices confirma o otimo, nos tamanhos em que ela
    /// cabe.
    /// </summary>
    [Fact]
    public void AEnumeracaoConfirmaOOtimoNosTamanhosPequenos()
    {
        for (var n = 1; n <= 4; n++)
        {
            var programa = Problemas.KleeMinty(n);
            var pelaForca = Forca.Resolver(programa);
            var peloSimplex = Metodo.Resolver(programa, Regra.Bland);

            Assert.Equal(Fim.Otimo, pelaForca.Fim);
            Assert.Equal(pelaForca.Valor, peloSimplex.Valor);
            // O cubo tem 2^n vertices, e todos sao viaveis.
            Assert.Equal(1 << n, pelaForca.BasesVisitadas);
        }
    }

    /// <summary>
    /// A regra do MAIOR AUMENTO resolve o cubo em UM pivo, em qualquer tamanho.
    ///
    /// Eu ia escrever que ela tambem cai, porque escolher pelo ganho parece a
    /// mesma coisa que escolher pelo custo reduzido. Nao e, e a diferenca e o
    /// cubo inteiro.
    ///
    /// Klee e Minty construiram a entrada contra a regra de DANTZIG, que olha
    /// so o custo reduzido, ou seja, o quanto cada unidade da variavel melhora.
    /// A deformacao do cubo faz a variavel de maior custo reduzido ser
    /// justamente a que mal pode crescer. A regra do maior aumento multiplica o
    /// custo reduzido pela razao minima, descobre que aquela variavel nao anda
    /// nada, e vai direto na que resolve.
    ///
    /// Isso nao torna o maior aumento imune: existem familias construidas
    /// contra cada regra conhecida, e a pergunta de se existe UMA regra
    /// polinomial continua aberta desde 1947. O que o cubo mostra e que a
    /// entrada adversaria e feita contra UMA regra, e nao contra o metodo.
    /// </summary>
    [Fact]
    public void OMaiorAumentoResolveOCuboEmUmPivo()
    {
        for (var n = 1; n <= 10; n++)
        {
            var maior = Metodo.Resolver(Problemas.KleeMinty(n), Regra.MaiorAumento);
            var bland = Metodo.Resolver(Problemas.KleeMinty(n), Regra.Bland);

            Assert.Equal(Fim.Otimo, maior.Fim);
            Assert.Equal(bland.Valor, maior.Valor);
            Assert.Equal(1, maior.Pivos);
        }
    }

    /// <summary>
    /// A estrutura do cubo: cada linha i tem limite 100^i, e os coeficientes
    /// crescem em potencias de dez. E essa deformacao que faz o caminho ser
    /// longo.
    /// </summary>
    [Fact]
    public void OCuboTemAEstruturaQueDizTer()
    {
        var cubo = Problemas.KleeMinty(4);
        Assert.Equal(4, cubo.Linhas);
        Assert.Equal(4, cubo.Variaveis);

        // Diagonal de uns.
        for (var i = 0; i < 4; i++) Assert.Equal(Fracao.Um, cubo.Restricoes[i, i]);
        // Nada acima da diagonal.
        for (var i = 0; i < 4; i++)
            for (var j = i + 1; j < 4; j++) Assert.True(cubo.Restricoes[i, j].EhZero);
        // E abaixo dela, 2 vezes 10^(i-j).
        Assert.Equal(new Fracao(20), cubo.Restricoes[1, 0]);
        Assert.Equal(new Fracao(200), cubo.Restricoes[2, 0]);
        Assert.Equal(new Fracao(20), cubo.Restricoes[2, 1]);

        Assert.Equal(new Fracao(1), cubo.Limites[0]);
        Assert.Equal(new Fracao(100), cubo.Limites[1]);
        Assert.Equal(new Fracao(10_000), cubo.Limites[2]);
        Assert.Equal(new Fracao(1_000_000), cubo.Limites[3]);
    }
}
