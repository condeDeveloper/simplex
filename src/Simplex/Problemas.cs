namespace Conde.Simplex;

/// <summary>
/// Os problemas que ficaram na historia por quebrarem alguma coisa, e os
/// geradores das buscas.
/// </summary>
public static class Problemas
{
    /// <summary>
    /// O problema de Beale, 1955: a entrada em que a regra de Dantzig CICLA.
    ///
    /// Sao tres restricoes e quatro variaveis. Todos os limites sao zero menos o
    /// ultimo, o que torna a origem um vertice DEGENERADO: varias restricoes se
    /// encontram nele. Num vertice degenerado a razao minima e zero, o pivo nao
    /// anda nada, e o objetivo nao sobe. Sem subir, nada impede o algoritmo de
    /// voltar a uma base ja visitada.
    ///
    /// Com a regra de Dantzig ele volta depois de seis pivos e gira para
    /// sempre. Com a de Bland, termina.
    ///
    /// maximizar  0,75 x1 - 150 x2 + 0,02 x3 - 6 x4
    ///
    /// As fracoes sao exatas de proposito: em ponto flutuante o ciclo se desfaz
    /// por erro de arredondamento, e o fenomeno some junto.
    /// </summary>
    public static Programa Beale()
    {
        var a = new Fracao[3, 4];
        // 0,25 x1 - 60 x2 - 0,04 x3 + 9 x4 <= 0
        a[0, 0] = new Fracao(1, 4); a[0, 1] = -60; a[0, 2] = new Fracao(-1, 25); a[0, 3] = 9;
        // 0,5 x1 - 90 x2 - 0,02 x3 + 3 x4 <= 0
        a[1, 0] = new Fracao(1, 2); a[1, 1] = -90; a[1, 2] = new Fracao(-1, 50); a[1, 3] = 3;
        // x3 <= 1
        a[2, 2] = Fracao.Um;

        var b = new Fracao[] { 0, 0, 1 };
        var c = new Fracao[] { new(3, 4), -150, new(1, 50), -6 };
        return new Programa(a, b, c);
    }

    /// <summary>
    /// O cubo de Klee e Minty, 1972: o problema em que a regra de Dantzig visita
    /// TODOS os 2^n vertices.
    ///
    /// Ele e um hipercubo deformado. Cada vertice e um canto do cubo, e a
    /// deformacao faz a regra do "melhor custo reduzido" percorrer todos eles
    /// numa ordem de codigo de Gray antes de chegar no otimo.
    ///
    /// Este e o resultado que separa "rapido na pratica" de "polinomial". O
    /// simplex e o algoritmo mais usado em otimizacao ha setenta anos e o pior
    /// caso dele e exponencial; foi preciso esperar Khachiyan, em 1979, para ter
    /// um metodo polinomial, e ele e mais lento na pratica.
    ///
    /// maximizar  soma de 2^(n-i) x_i
    /// sujeito a  2 * soma_(j&lt;i) 2^(i-j) x_j + x_i &lt;= 5^i
    /// </summary>
    public static Programa KleeMinty(int n)
    {
        if (n < 1 || n > 12) throw new ArgumentOutOfRangeException(nameof(n), "entre 1 e 12");

        var a = new Fracao[n, n];
        var b = new Fracao[n];
        var c = new Fracao[n];

        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < i; j++) a[i, j] = 2 * Potencia(10, i - j);
            a[i, i] = Fracao.Um;
            b[i] = Potencia(100, i);
            c[i] = Potencia(10, n - 1 - i);
        }
        return new Programa(a, b, c);
    }

    private static Fracao Potencia(long baseDa, int expoente)
    {
        var saida = Fracao.Um;
        for (var i = 0; i < expoente; i++) saida *= baseDa;
        return saida;
    }

    /// <summary>
    /// Um problema de producao, que e o uso original do simplex: quantas
    /// unidades de cada produto fazer, dados os recursos.
    ///
    /// maximizar 3x + 5y sujeito a x &lt;= 4, 2y &lt;= 12, 3x + 2y &lt;= 18.
    /// O otimo e x = 2, y = 6, valor 36, e esta em todo livro.
    /// </summary>
    public static Programa Producao() =>
        Programa.De(
            new long[,] { { 1, 0 }, { 0, 2 }, { 3, 2 } },
            [4, 12, 18],
            [3, 5]);

    /// <summary>
    /// Um problema ILIMITADO: maximizar x + y com x - y &lt;= 1.
    ///
    /// Crescer x e y juntos nunca viola a restricao, e o objetivo sobe sem
    /// parar.
    /// </summary>
    public static Programa Ilimitado() =>
        Programa.De(new long[,] { { 1, -1 } }, [1], [1, 1]);

    /// <summary>
    /// Um problema INVIAVEL: x &gt;= 2 e x &lt;= 1 ao mesmo tempo, escrito como
    /// -x &lt;= -2 e x &lt;= 1.
    /// </summary>
    public static Programa Inviavel() =>
        Programa.De(new long[,] { { -1 }, { 1 } }, [-2, 1], [1]);

    /// <summary>
    /// Um programa sorteado com limites nao negativos, entao a origem sempre e
    /// viavel e o problema nunca e inviavel.
    ///
    /// Os coeficientes podem ser negativos, o que e necessario: com tudo
    /// positivo o poliedro e sempre limitado e metade dos casos interessantes
    /// nunca aparece.
    /// </summary>
    public static Programa Sortear(int linhas, int variaveis, Random sorteio, long maior = 9)
    {
        ArgumentNullException.ThrowIfNull(sorteio);
        var a = new Fracao[linhas, variaveis];
        var b = new Fracao[linhas];
        var c = new Fracao[variaveis];

        for (var i = 0; i < linhas; i++)
        {
            for (var j = 0; j < variaveis; j++) a[i, j] = sorteio.NextInt64(-maior, maior + 1);
            b[i] = sorteio.NextInt64(0, maior + 1);
        }
        for (var j = 0; j < variaveis; j++) c[j] = sorteio.NextInt64(-maior, maior + 1);

        return new Programa(a, b, c);
    }

    /// <summary>
    /// Um programa sorteado DEGENERADO: varios limites valem zero.
    ///
    /// A degenerescencia e a condicao necessaria da ciclagem, entao procurar
    /// ciclo em programas sorteados comuns e procurar no lugar errado: com
    /// limites positivos o objetivo sobe em todo pivo e voltar e impossivel.
    /// </summary>
    public static Programa SortearDegenerado(int linhas, int variaveis, Random sorteio, long maior = 9)
    {
        ArgumentNullException.ThrowIfNull(sorteio);
        var programa = Sortear(linhas, variaveis, sorteio, maior);
        for (var i = 0; i < linhas; i++)
            if (sorteio.NextDouble() < 0.7) programa.Limites[i] = Fracao.Zero;
        return programa;
    }
}
