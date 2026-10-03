using Conde.Simplex;

namespace Conde.Simplex.Medidor;

/// <summary>
/// As medidas. O que se mede e CONTAGEM DE PIVOS, que e propriedade do
/// algoritmo e da o mesmo numero em qualquer maquina.
/// </summary>
public static class Programa
{
    public static int Main(string[] argumentos)
    {
        var qual = argumentos.Length > 0 ? argumentos[0] : "tudo";
        switch (qual)
        {
            case "cubo": Cubo(); break;
            case "ciclo": Ciclo(); break;
            case "regras": Regras(); break;
            case "juiz": OJuiz(); break;
            case "tudo": Cubo(); Ciclo(); Regras(); OJuiz(); break;
            default:
                Console.Error.WriteLine("medidas: cubo, ciclo, regras, juiz, tudo");
                return 1;
        }
        return 0;
    }

    /// <summary>
    /// O cubo de Klee e Minty, com as tres regras.
    ///
    /// E a tabela mais surpreendente deste repositorio, e ela corrigiu duas
    /// coisas que eu ia escrever.
    /// </summary>
    private static void Cubo()
    {
        Console.WriteLine("== o cubo de Klee e Minty, 1972: pivos por regra ==");
        Console.WriteLine($"{"n",4}{"vertices",12}{"Dantzig",12}{"Bland",12}{"maior aumento",16}");

        for (var n = 1; n <= 12; n++)
        {
            var dantzig = Metodo.Resolver(Problemas.KleeMinty(n), Regra.Dantzig, teto: 100_000);
            var bland = Metodo.Resolver(Problemas.KleeMinty(n), Regra.Bland, teto: 100_000);
            var maior = Metodo.Resolver(Problemas.KleeMinty(n), Regra.MaiorAumento, teto: 100_000);

            Console.WriteLine($"{n,4}{1L << n,12:N0}{dantzig.Pivos,12:N0}{bland.Pivos,12:N0}{maior.Pivos,16:N0}");
        }

        Console.WriteLine();
        Console.WriteLine("a coluna da Dantzig e exatamente 2^n - 1: ela visita TODOS os vertices.");
        Console.WriteLine();
        Console.WriteLine("a de Bland tambem e exponencial, e eu ia escrever que ela era linear. A");
        Console.WriteLine("sequencia obedece a a(n) = a(n-1) + a(n-2) + 1, que e Fibonacci com um termo");
        Console.WriteLine("constante, e cresce na RAZAO AUREA. Ela nao escapa do cubo: cai mais devagar.");
        Console.WriteLine();
        Console.WriteLine("e a do maior aumento resolve em UM pivo, sempre. Klee e Minty construiram a");
        Console.WriteLine("entrada contra a regra de Dantzig, que olha so o custo reduzido; a deformacao");
        Console.WriteLine("faz a variavel de maior custo reduzido ser justo a que mal pode crescer. Quem");
        Console.WriteLine("multiplica pelo quanto ela anda descobre isso e vai direto na que resolve.");
        Console.WriteLine();
        Console.WriteLine("a licao e sobre entrada adversaria: ela e feita contra UMA regra, e nao");
        Console.WriteLine("contra o metodo. Existem familias construidas contra cada regra conhecida, e");
        Console.WriteLine("se existe uma regra polinomial continua aberto desde 1947.");
        Console.WriteLine();
    }

    /// <summary>
    /// A ciclagem no problema de Beale, pivo a pivo.
    /// </summary>
    private static void Ciclo()
    {
        Console.WriteLine("== a ciclagem no problema de Beale, 1955 ==");
        Console.WriteLine($"{"pivo",6}{"entra",8}{"sai",8}{"base",22}{"valor",10}{"ja vista?",12}");

        var tabela = new Tabela(Problemas.Beale());
        var vistas = new Dictionary<string, int> { [tabela.Chave()] = 0 };

        for (var pivo = 1; pivo <= 10; pivo++)
        {
            var entra = tabela.ColunaQueEntra(Regra.Dantzig);
            if (entra < 0) { Console.WriteLine("  otimo"); break; }
            var sai = tabela.LinhaQueSai(entra, desempatarPeloMenorIndice: false);
            if (sai < 0) { Console.WriteLine("  ilimitado"); break; }

            var queSai = tabela.Base[sai];
            tabela.Pivotar(sai, entra);
            var chave = tabela.Chave();
            var repetida = vistas.TryGetValue(chave, out var quando);
            if (!repetida) vistas[chave] = pivo;

            Console.WriteLine($"{pivo,6}{"x" + entra,8}{"x" + queSai,8}{chave,22}{tabela.Valor,10}" +
                              $"{(repetida ? $"sim, do {quando}" : ""),12}");
            if (repetida) break;
        }

        Console.WriteLine();
        Console.WriteLine("o valor NAO sobe em nenhum pivo, e e isso que permite voltar: todos os");
        Console.WriteLine("vertices visitados sao degenerados, com razao minima zero, e o pivo nao anda.");
        Console.WriteLine();

        foreach (var regra in Enum.GetValues<Regra>())
        {
            var resultado = Metodo.Resolver(Problemas.Beale(), regra, teto: 500);
            Console.WriteLine($"  {regra,-14} {resultado}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// As tres regras em programas sorteados, onde o pior caso nao aparece.
    ///
    /// A distancia entre o pior caso e o caso tipico e o que explica setenta
    /// anos de uso do simplex apesar do limite exponencial.
    /// </summary>
    private static void Regras()
    {
        Console.WriteLine("== as tres regras em programas sorteados ==");
        Console.WriteLine($"{"tamanho",12}{"Dantzig",12}{"Bland",12}{"maior aumento",16}{"m + n",10}");

        foreach (var (linhas, variaveis) in new[] { (5, 5), (10, 10), (20, 20), (40, 40) })
        {
            var sorteio = new Random(linhas * 31);
            long dantzig = 0, bland = 0, maior = 0;
            var casos = 0;

            for (var tentativa = 0; tentativa < 200; tentativa++)
            {
                var programa = Problemas.Sortear(linhas, variaveis, sorteio);
                var a = Metodo.Resolver(programa, Regra.Dantzig);
                if (a.Fim != Fim.Otimo) continue;
                var b = Metodo.Resolver(programa, Regra.Bland);
                var c = Metodo.Resolver(programa, Regra.MaiorAumento);
                if (b.Fim != Fim.Otimo || c.Fim != Fim.Otimo) continue;

                dantzig += a.Pivos; bland += b.Pivos; maior += c.Pivos;
                casos++;
            }

            if (casos == 0) continue;
            Console.WriteLine($"{linhas + "x" + variaveis,12}{dantzig / (double)casos,12:F1}" +
                              $"{bland / (double)casos,12:F1}{maior / (double)casos,16:F1}{linhas + variaveis,10}");
        }

        Console.WriteLine();
        Console.WriteLine("o numero de pivos fica na ordem de m + n, e nao de 2^n. E essa distancia");
        Console.WriteLine("entre o pior caso e o tipico que explica setenta anos de uso do simplex");
        Console.WriteLine("apesar do limite exponencial");
        Console.WriteLine();
    }

    /// <summary>
    /// O juiz: quantos sistemas a enumeracao de vertices precisa resolver.
    ///
    /// E a conta que mostra por que o simplex existe.
    /// </summary>
    private static void OJuiz()
    {
        Console.WriteLine("== o preco de enumerar vertices em vez de caminhar ==");
        Console.WriteLine($"{"m",6}{"n",6}{"C(m+n, n)",16}{"vertices viaveis",20}{"pivos do simplex",18}");

        var sorteio = new Random(7);
        foreach (var (linhas, variaveis) in new[] { (2, 2), (3, 3), (4, 4), (5, 5), (6, 6) })
        {
            var programa = Problemas.Sortear(linhas, variaveis, sorteio);
            var combinacoes = Forca.Combinacoes(linhas + variaveis, variaveis);

            var pelaForca = Forca.Resolver(programa);
            var peloSimplex = Metodo.Resolver(programa, Regra.Bland);

            Console.WriteLine($"{linhas,6}{variaveis,6}{combinacoes,16:N0}" +
                              $"{pelaForca.BasesVisitadas,20:N0}{peloSimplex.Pivos,18:N0}");

            if (pelaForca.Fim == Fim.Otimo && peloSimplex.Fim == Fim.Otimo && pelaForca.Valor != peloSimplex.Valor)
                throw new InvalidOperationException("o juiz e o simplex discordaram");
        }

        Console.WriteLine();
        Console.WriteLine("a coluna do meio e quantos vertices existem de verdade; a da esquerda e");
        Console.WriteLine("quantos sistemas e preciso resolver para acha-los. O simplex nao enumera: ele");
        Console.WriteLine("caminha, e visita uma fracao minuscula dos vertices.");
        Console.WriteLine();
        Console.WriteLine("com m = n = 20, C(40, 20) passa de 137 bilhoes de sistemas; o simplex resolve");
        Console.WriteLine("em algumas dezenas de pivos.");
        Console.WriteLine();
    }
}
