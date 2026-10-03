namespace Conde.Simplex;

/// <summary>
/// Qual coluna entra na base quando mais de uma melhora o objetivo.
///
/// Essa escolha nao muda a resposta, quando o algoritmo termina. Ela muda
/// quantos pivos sao precisos e, no caso de Dantzig, PODE FAZER O ALGORITMO NAO
/// TERMINAR.
/// </summary>
public enum Regra
{
    /// <summary>
    /// A de Dantzig, 1947: entra a coluna de custo reduzido mais negativo, ou
    /// seja, a que melhora mais por unidade.
    ///
    /// E a regra natural, e a que todo mundo escreve primeiro, e a que quase
    /// todo material apresenta. Ela e rapida na pratica e pode CICLAR: existe um
    /// problema de Beale, de 1955, com tres restricoes, em que ela volta a uma
    /// base ja visitada e gira para sempre.
    /// </summary>
    Dantzig,

    /// <summary>
    /// A de Bland, 1977: entre as colunas que melhoram, entra a de MENOR INDICE;
    /// e, no empate da razao minima, sai a de menor indice tambem.
    ///
    /// Ela parece arbitraria e e o oposto disso: Bland provou que ela garante
    /// terminacao, qualquer que seja o problema. O preco e que ela ignora o
    /// quanto cada coluna melhora, e costuma dar mais pivos.
    ///
    /// As DUAS metades sao necessarias. Aplicar o menor indice so na entrada,
    /// ou so na saida, nao evita a ciclagem, e e um erro facil de cometer
    /// porque o algoritmo continua funcionando na maioria dos problemas.
    /// </summary>
    Bland,

    /// <summary>
    /// A do MAIOR AUMENTO: entra a coluna que de fato aumenta mais o objetivo,
    /// levando em conta a razao minima dela.
    ///
    /// E a mais cara por pivo, porque exige calcular a razao de todas as
    /// colunas candidatas em vez de uma. Em troca, costuma dar menos pivos. O
    /// medidor mostra se a troca vale.
    /// </summary>
    MaiorAumento,
}
