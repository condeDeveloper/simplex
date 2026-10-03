using System.Numerics;

namespace Conde.Simplex;

/// <summary>
/// Uma fracao exata, em inteiro grande.
///
/// O simplex PRECISA de aritmetica exata, e isso nao e preciosismo. O algoritmo
/// decide por COMPARACAO DE SINAL: entra na base quem tem custo reduzido
/// negativo, sai quem tem a menor razao. Em ponto flutuante, um valor que
/// deveria ser zero vira 1e-17, o sinal sai errado, e o resultado nao e uma
/// resposta um pouco torta: e um pivo numa linha que nao podia pivotar, uma base
/// invalida, e um laco que nao termina.
///
/// E ha um motivo mais forte neste repositorio em particular: o assunto central
/// e a CICLAGEM, que acontece quando a razao minima empata em zero. Com ponto
/// flutuante esses empates nao existem, e o fenomeno desaparece junto com a
/// possibilidade de estuda-lo.
///
/// A forma canonica e mantida em toda operacao: denominador positivo e maximo
/// divisor comum um. Sem isso os numeros crescem sem controle numa dezena de
/// pivos.
/// </summary>
public readonly struct Fracao : IEquatable<Fracao>, IComparable<Fracao>
{
    private readonly BigInteger baixo;

    public BigInteger Cima { get; }

    /// <summary>
    /// O denominador, com uma guarda que nao e detalhe: o campo de verdade vale
    /// ZERO num <c>default(Fracao)</c>, e todo vetor de fracoes nasce assim.
    ///
    /// Sem esta linha, montar o quadro do simplex estoura com divisao por zero
    /// na primeira celula que ninguem preencheu, porque em C# um vetor de
    /// struct e zerado e nao construido. Tratar o zero do campo como um faz o
    /// default valer zero, que e o que uma celula vazia deve valer.
    /// </summary>
    public BigInteger Baixo => baixo.IsZero ? BigInteger.One : baixo;

    public Fracao(BigInteger cima, BigInteger baixo)
    {
        if (baixo.IsZero) throw new DivideByZeroException("fracao com denominador zero");
        if (baixo.Sign < 0) { cima = -cima; baixo = -baixo; }
        var divisor = BigInteger.GreatestCommonDivisor(BigInteger.Abs(cima), baixo);
        if (divisor > BigInteger.One) { cima /= divisor; baixo /= divisor; }
        Cima = cima;
        this.baixo = baixo;
    }

    public Fracao(BigInteger inteiro) : this(inteiro, BigInteger.One) { }

    public static readonly Fracao Zero = new(0);
    public static readonly Fracao Um = new(1);

    public static implicit operator Fracao(int x) => new(x);
    public static implicit operator Fracao(long x) => new(x);

    public int Sinal => Cima.Sign;
    public bool EhZero => Cima.IsZero;

    public static Fracao operator +(Fracao a, Fracao b) => new(a.Cima * b.Baixo + b.Cima * a.Baixo, a.Baixo * b.Baixo);
    public static Fracao operator -(Fracao a, Fracao b) => new(a.Cima * b.Baixo - b.Cima * a.Baixo, a.Baixo * b.Baixo);
    public static Fracao operator *(Fracao a, Fracao b) => new(a.Cima * b.Cima, a.Baixo * b.Baixo);
    public static Fracao operator /(Fracao a, Fracao b) => new(a.Cima * b.Baixo, a.Baixo * b.Cima);
    public static Fracao operator -(Fracao a) => new(-a.Cima, a.Baixo);

    public static bool operator <(Fracao a, Fracao b) => a.CompareTo(b) < 0;
    public static bool operator >(Fracao a, Fracao b) => a.CompareTo(b) > 0;
    public static bool operator <=(Fracao a, Fracao b) => a.CompareTo(b) <= 0;
    public static bool operator >=(Fracao a, Fracao b) => a.CompareTo(b) >= 0;
    public static bool operator ==(Fracao a, Fracao b) => a.Equals(b);
    public static bool operator !=(Fracao a, Fracao b) => !a.Equals(b);

    public int CompareTo(Fracao outra) => (Cima * outra.Baixo).CompareTo(outra.Cima * Baixo);

    public bool Equals(Fracao outra) => Cima == outra.Cima && Baixo == outra.Baixo;

    public override bool Equals(object? o) => o is Fracao f && Equals(f);

    public override int GetHashCode() => HashCode.Combine(Cima, Baixo);

    /// <summary>
    /// O valor aproximado, so para relatorio. Nenhuma decisao do algoritmo passa
    /// por aqui.
    /// </summary>
    public double ComoDouble() => Baixo.IsOne ? (double)Cima : (double)Cima / (double)Baixo;

    public override string ToString() => Baixo.IsOne ? Cima.ToString() : $"{Cima}/{Baixo}";
}
