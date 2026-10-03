using System.Numerics;

namespace Conde.Simplex;

/// <summary>
/// O JUIZ: enumera TODOS os vertices do poliedro e pega o melhor.
///
/// A base teorica e o que faz o simplex existir: o maximo de uma funcao linear
/// sobre um poliedro, quando existe, esta num VERTICE, e cada vertice e a
/// solucao de um sistema quadrado obtido escolhendo quais restricoes valem com
/// igualdade. Entao basta escolher n das m+n restricoes, resolver o sistema,
/// conferir se o ponto e viavel, e ficar com o de maior valor.
///
/// Sao C(m+n, n) sistemas a resolver, o que explode rapido e e exatamente por
/// isso que o simplex existe. Como juiz, porem, ele e perfeito: nao usa pivo,
/// nao usa base, nao usa razao minima, e nao pode errar pelos mesmos motivos
/// que o simplex erraria.
///
/// A eliminacao de Gauss aqui e exata, em fracoes.
/// </summary>
public static class Forca
{
    /// <summary>
    /// Resolve por enumeracao de vertices. O campo Pivos do resultado conta
    /// quantos SISTEMAS foram resolvidos.
    /// </summary>
    public static Resultado Resolver(Programa programa, int tetoDeSistemas = 2_000_000)
    {
        ArgumentNullException.ThrowIfNull(programa);
        var n = programa.Variaveis;
        var m = programa.Linhas;
        var total = m + n;                 // m restricoes mais n limites x >= 0

        if (Combinacoes(total, n) > tetoDeSistemas)
            throw new ArgumentOutOfRangeException(nameof(programa), "a enumeracao de vertices ficaria grande demais");

        Fracao[]? melhor = null;
        var melhorValor = Fracao.Zero;
        var sistemas = 0;
        var viaveis = 0;

        foreach (var escolha in Escolher(total, n))
        {
            sistemas++;
            var ponto = Resolver(programa, escolha);
            if (ponto is null) continue;
            if (!programa.Viavel(ponto)) continue;

            viaveis++;
            var valor = programa.Valor(ponto);
            if (melhor is null || valor > melhorValor) { melhor = ponto; melhorValor = valor; }
        }

        if (melhor is null) return new Resultado(Fim.Inviavel, null, Fracao.Zero, sistemas, 0);

        // Ilimitado nao e detectavel so pelos vertices: se o poliedro tiver uma
        // direcao de melhora infinita, o melhor vertice existe e nao e o maximo.
        if (Ilimitado(programa)) return new Resultado(Fim.Ilimitado, null, Fracao.Zero, sistemas, viaveis);

        return new Resultado(Fim.Otimo, melhor, melhorValor, sistemas, viaveis);
    }

    /// <summary>
    /// Resolve o sistema quadrado dado pela escolha de restricoes ativas, por
    /// eliminacao de Gauss exata. Devolve nulo quando o sistema e singular.
    /// </summary>
    private static Fracao[]? Resolver(Programa programa, int[] escolha)
    {
        var n = programa.Variaveis;
        var m = programa.Linhas;
        var a = new Fracao[n, n + 1];

        for (var linha = 0; linha < n; linha++)
        {
            var qual = escolha[linha];
            if (qual < m)
            {
                for (var j = 0; j < n; j++) a[linha, j] = programa.Restricoes[qual, j];
                a[linha, n] = programa.Limites[qual];
            }
            else
            {
                // A restricao x[k] >= 0, ativa quer dizer x[k] = 0.
                a[linha, qual - m] = Fracao.Um;
                a[linha, n] = Fracao.Zero;
            }
        }

        for (var coluna = 0; coluna < n; coluna++)
        {
            var pivo = -1;
            for (var linha = coluna; linha < n; linha++)
                if (!a[linha, coluna].EhZero) { pivo = linha; break; }
            if (pivo < 0) return null;               // singular

            if (pivo != coluna)
                for (var j = 0; j <= n; j++) (a[coluna, j], a[pivo, j]) = (a[pivo, j], a[coluna, j]);

            var divisor = a[coluna, coluna];
            for (var j = 0; j <= n; j++) a[coluna, j] /= divisor;

            for (var linha = 0; linha < n; linha++)
            {
                if (linha == coluna) continue;
                var fator = a[linha, coluna];
                if (fator.EhZero) continue;
                for (var j = 0; j <= n; j++) a[linha, j] -= fator * a[coluna, j];
            }
        }

        var x = new Fracao[n];
        for (var j = 0; j < n; j++) x[j] = a[j, n];
        return x;
    }

    /// <summary>
    /// Verdadeiro quando existe uma direcao de melhora ILIMITADA: um vetor d
    /// com d &gt;= 0, A·d &lt;= 0 e c·d &gt; 0.
    ///
    /// Andar nessa direcao a partir de qualquer ponto viavel melhora para
    /// sempre sem violar nada, entao nao existe maximo.
    ///
    /// A primeira versao disto olhava so as direcoes CANONICAS, uma variavel de
    /// cada vez, e estava errada: um poliedro pode ser ilimitado numa direcao
    /// que e combinacao de variaveis e limitado em cada eixo. A comparacao com
    /// o simplex, que detecta o caso de forma exata, pegou isso.
    ///
    /// A versao certa enumera os RAIOS EXTREMOS: escolhem-se n-1 das m+n
    /// restricoes homogeneas para valerem com igualdade, resolve-se o sistema
    /// para achar a direcao, e testa-se ela. Todo raio extremo aparece nessa
    /// enumeracao, e se nenhum serve, nenhum raio serve.
    /// </summary>
    public static bool Ilimitado(Programa programa)
    {
        ArgumentNullException.ThrowIfNull(programa);
        var n = programa.Variaveis;
        var m = programa.Linhas;

        foreach (var escolha in Escolher(m + n, n - 1))
        {
            var direcao = Nucleo(programa, escolha);
            if (direcao is null) continue;
            if (Serve(programa, direcao)) return true;

            var oposta = new Fracao[n];
            for (var j = 0; j < n; j++) oposta[j] = -direcao[j];
            if (Serve(programa, oposta)) return true;
        }
        return false;
    }

    private static bool Serve(Programa programa, Fracao[] d)
    {
        var n = programa.Variaveis;
        foreach (var valor in d) if (valor.Sinal < 0) return false;

        var ganho = Fracao.Zero;
        for (var j = 0; j < n; j++) ganho += programa.Objetivo[j] * d[j];
        if (ganho.Sinal <= 0) return false;

        for (var i = 0; i < programa.Linhas; i++)
        {
            var soma = Fracao.Zero;
            for (var j = 0; j < n; j++) soma += programa.Restricoes[i, j] * d[j];
            if (soma.Sinal > 0) return false;
        }
        return true;
    }

    /// <summary>
    /// Uma direcao nao nula que anula as restricoes escolhidas, ou nulo quando
    /// o nucleo tem dimensao diferente de um.
    /// </summary>
    private static Fracao[]? Nucleo(Programa programa, int[] escolha)
    {
        var n = programa.Variaveis;
        var m = programa.Linhas;
        var linhas = escolha.Length;
        var a = new Fracao[Math.Max(linhas, 1), n];

        for (var linha = 0; linha < linhas; linha++)
        {
            var qual = escolha[linha];
            if (qual < m)
                for (var j = 0; j < n; j++) a[linha, j] = programa.Restricoes[qual, j];
            else
                a[linha, qual - m] = Fracao.Um;
        }

        // Eliminacao de Gauss, guardando qual coluna e pivo de cada linha.
        var pivoDa = new int[Math.Max(linhas, 1)];
        Array.Fill(pivoDa, -1);
        var usadas = new bool[n];
        var posto = 0;

        for (var coluna = 0; coluna < n && posto < linhas; coluna++)
        {
            var pivo = -1;
            for (var linha = posto; linha < linhas; linha++)
                if (!a[linha, coluna].EhZero) { pivo = linha; break; }
            if (pivo < 0) continue;

            if (pivo != posto)
                for (var j = 0; j < n; j++) (a[posto, j], a[pivo, j]) = (a[pivo, j], a[posto, j]);

            var divisor = a[posto, coluna];
            for (var j = 0; j < n; j++) a[posto, j] /= divisor;

            for (var linha = 0; linha < linhas; linha++)
            {
                if (linha == posto) continue;
                var fator = a[linha, coluna];
                if (fator.EhZero) continue;
                for (var j = 0; j < n; j++) a[linha, j] -= fator * a[posto, j];
            }

            pivoDa[posto] = coluna;
            usadas[coluna] = true;
            posto++;
        }

        // O nucleo so tem dimensao um quando sobra exatamente uma coluna livre.
        var livres = 0;
        var livre = -1;
        for (var j = 0; j < n; j++) if (!usadas[j]) { livres++; livre = j; }
        if (livres != 1) return null;

        var d = new Fracao[n];
        d[livre] = Fracao.Um;
        for (var linha = 0; linha < posto; linha++) d[pivoDa[linha]] = -a[linha, livre];
        return d;
    }

    /// <summary>Todas as escolhas de <paramref name="quantas"/> entre n.</summary>
    public static IEnumerable<int[]> Escolher(int total, int quantas)
    {
        if (quantas < 0 || quantas > total) yield break;
        var escolha = new int[quantas];
        for (var i = 0; i < quantas; i++) escolha[i] = i;

        while (true)
        {
            yield return (int[])escolha.Clone();

            var i = quantas - 1;
            while (i >= 0 && escolha[i] == total - quantas + i) i--;
            if (i < 0) yield break;
            escolha[i]++;
            for (var j = i + 1; j < quantas; j++) escolha[j] = escolha[j - 1] + 1;
        }
    }

    public static BigInteger Combinacoes(int total, int quantas)
    {
        if (quantas < 0 || quantas > total) return 0;
        BigInteger saida = 1;
        for (var i = 0; i < quantas; i++) saida = saida * (total - i) / (i + 1);
        return saida;
    }
}
