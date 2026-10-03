# simplex

O método simplex do zero em C# e .NET 8, em aritmética exata: duas fases, três
regras de pivoteamento, o problema de Beale que faz a regra de Dantzig ciclar e
o cubo de Klee e Minty que a faz visitar todos os 2^n vértices. O juiz enumera
os vértices do poliedro.

```
$ dotnet medidor.dll ciclo

  pivo   entra     sai                  base     valor   ja vista?
     1      x0      x4                 0,5,6         0
     2      x1      x5                 0,1,6         0
     3      x2      x0                 1,2,6         0
     4      x3      x1                 2,3,6         0
     5      x4      x2                 3,4,6         0
     6      x5      x3                 4,5,6         0   sim, do 0
```

Seis pivôs e o algoritmo está de volta exatamente onde começou. O valor não
subiu em nenhum deles.

## A aritmética exata não é preciosismo

O simplex decide por **comparação de sinal**: entra na base quem tem custo
reduzido negativo, sai quem tem a menor razão. Em ponto flutuante, um valor que
deveria ser zero vira 1e-17, o sinal sai errado, e o resultado não é uma
resposta um pouco torta — é um pivô numa linha que não podia pivotar, uma base
inválida, e um laço que não termina.

E há um motivo mais forte neste repositório: o assunto central é a **ciclagem**,
que acontece quando a razão mínima empata em zero. Com ponto flutuante esses
empates não existem, o erro de arredondamento desfaz o ciclo, e o fenômeno
desaparece junto com a possibilidade de estudá-lo.

Os coeficientes do problema de Beale são 1/25 e 1/50, que não têm representação
binária. Um teste mostra a diferença: somar 0,04 vinte e cinco vezes em `double`
dá 1,0000000000000002; em fração dá um.

## O juiz enumera vértices

O teorema que faz o simplex existir: o máximo de uma função linear sobre um
poliedro está sempre num **vértice**, e cada vértice é a solução de um sistema
quadrado obtido escolhendo quais restrições valem com igualdade.

Então o juiz escolhe n das m+n restrições, resolve o sistema por eliminação de
Gauss exata, confere se o ponto é viável, e fica com o de maior valor. Não usa
pivô, não usa base, não usa razão mínima, e não pode errar pelos mesmos motivos
que o simplex erraria.

```
$ dotnet medidor.dll juiz

     m     n       C(m+n, n)    vertices viaveis  pivos do simplex
     2     2               6                   3                 1
     3     3              20                   7                 2
     4     4              70                  14                 0
     5     5             252                  10                 1
     6     6             924                  49                11
```

Com m = n = 20, C(40, 20) passa de **137 bilhões** de sistemas. O simplex não
enumera: ele caminha.

## O que as medidas me corrigiram

**A regra de Bland também é exponencial no cubo de Klee e Minty.** Eu ia
escrever que ela resolve em n pivôs — afinal, ela é a regra que garante
terminação. Não resolve:

```
$ dotnet medidor.dll cubo

   n    vertices     Dantzig       Bland   maior aumento
   1           2           1           1               1
   4          16          15           9               1
   7         128         127          41               1
  10       1,024       1,023         177               1
  12       4,096       4,095         465               1
```

A sequência de Bland é 1, 3, 5, 9, 15, 25, 41, 67, 109, 177, e obedece a
`a(n) = a(n-1) + a(n-2) + 1` — Fibonacci com um termo constante, crescendo na
**razão áurea**. Ela não escapa do cubo: cai mais devagar.

**E a regra do maior aumento resolve o cubo em UM pivô, em qualquer tamanho.**
Isso foi a maior surpresa. Klee e Minty construíram a entrada contra a regra de
**Dantzig**, que olha só o custo reduzido, ou seja, quanto cada unidade da
variável melhora. A deformação do cubo faz a variável de maior custo reduzido
ser justamente a que mal pode crescer. A regra do maior aumento multiplica o
custo reduzido pela razão mínima, descobre que aquela variável não anda nada, e
vai direto na que resolve.

A lição é sobre entrada adversária: ela é feita contra **uma regra**, e não
contra o método. Existem famílias construídas contra cada regra conhecida, e se
existe uma regra polinomial continua aberto desde 1947.

**Meu método de duas fases estava errado, e o juiz pegou.** Eu tinha escrito uma
artificial por linha de limite negativo, com a linha multiplicada por -1. Isso
vem do caso de igualdades e não funciona com desigualdades do tipo menor ou
igual, porque inverter a linha inverte o **sentido** da restrição. O resultado
era uma primeira fase que declarava inviáveis programas perfeitamente viáveis. A
versão certa usa **uma** artificial com coeficiente -1 em todas as linhas, o que
relaxa tudo de uma vez sem mexer em sentido nenhum.

**A detecção de ilimitado por direções canônicas estava errada.** Eu olhava uma
variável de cada vez: existe x que cresce sem limite melhorando? Um poliedro
pode ser ilimitado numa direção que é **combinação** de variáveis e limitado em
cada eixo. A versão certa enumera os raios extremos, escolhendo n-1 restrições
homogêneas e resolvendo o núcleo.

**Em programas sorteados, Bland é a pior das três.** É o preço da garantia:

```
$ dotnet medidor.dll regras

     tamanho     Dantzig       Bland   maior aumento     m + n
         5x5         2.3         2.6             2.0        10
       10x10         6.1         8.6             5.4        20
       20x20        17.5        31.0            14.1        40
       40x40        55.3       133.3            36.8        80
```

O número de pivôs fica na ordem de **m + n**, e não de 2^n. É essa distância
entre o pior caso e o típico que explica setenta anos de uso do simplex apesar
do limite exponencial.

## A ciclagem precisa de degenerescência

Um vértice é **degenerado** quando alguma variável da base vale zero. Ali a razão
mínima é zero, o pivô não anda nada, e o objetivo não sobe. Sem subir, nada
impede o algoritmo de voltar a uma base já visitada.

A cadeia inteira está num teste: em programas com limites estritamente
positivos, nenhum vértice inicial é degenerado, e o teste confere que **o
objetivo sobe em todo pivô não degenerado**. Sem pivô que não sobe, não há
ciclo.

É por isso que procurar ciclagem em programas sorteados comuns é procurar no
lugar errado, e por isso que Beale teve de construir um exemplo à mão em 1955 em
vez de achar um por acaso.

A regra de Bland, 1977, resolve: entre as colunas que melhoram, entra a de
**menor índice**; e, no empate da razão mínima, sai a de menor índice também. As
duas metades são necessárias, e aplicar só uma delas é um erro fácil de cometer
porque o algoritmo continua funcionando na maioria dos problemas.

## As peças

| arquivo | o que faz |
| --- | --- |
| `Fracao.cs` | aritmética exata em inteiro grande, com forma canônica |
| `Programa.cs` | o programa linear na forma padrão, e os tipos de fim |
| `Tabela.cs` | o quadro, o pivô, a razão mínima e as três regras |
| `Regra.cs` | Dantzig, Bland e maior aumento |
| `Metodo.cs` | o giro e as duas fases |
| `Forca.cs` | o juiz: enumeração de vértices e de raios extremos |
| `Problemas.cs` | Beale, Klee-Minty, e os geradores |

## Como rodar

```
dotnet test testes/Simplex.Testes/Simplex.Testes.csproj -c Release
dotnet run --project ferramentas/Medidor/Medidor.csproj -c Release -- tudo
```

As medidas aceitam `cubo`, `ciclo`, `regras`, `juiz` e `tudo`.

## Aviso

Isto é um estudo. Um resolvedor de verdade usa a forma revisada, fatoração LU
com atualização, pricing parcial e pré-processamento, e trabalha em ponto
flutuante com tolerâncias cuidadosamente escolhidas. A aritmética exata aqui
existe para que a ciclagem e a degenerescência sejam observáveis, e ela não
escala.

## Licença

MIT.
