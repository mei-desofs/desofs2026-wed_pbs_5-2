# Melhores Práticas de Desenvolvimento

## Objetivo

Este documento resume as convenções de codificação C# da Microsoft e como elas se aplicam a esta solução. O foco é manter o código legível, consistente e fácil de manter.

## Convenções Principais

### Linguagem e estilo

- Deve-se utilizar recursos modernos da linguagem C# quando eles melhorarem a clareza.
- Devem ser preferidos `string`, `int` e outros tipos da linguagem em vez de nomes de tipos de runtime.
- Deve-se usar `var` apenas quando o tipo for óbvio no lado direito da atribuição.
- Devem ser preferidos namespaces com escopo de arquivo quando houver apenas um namespace no arquivo.
- Devem ser colocadas as diretivas `using` fora da declaração de namespace.
- Devem ser usadas chaves no estilo Allman.
- Deve-se escrever uma instrução por linha e uma declaração por linha.

### Construção de código

- Devem ser usados inicializadores de objeto e a forma concisa de criação de objetos quando isso facilitar a leitura.
- Deve-se usar `using` ou declarações `using` para objetos descartáveis.
- Devem ser usados `&&` e `||` em vez de `&` e `|` em condições lógicas.
- Deve-se usar interpolação de strings para composições curtas de texto.
- Devem ser usados nomes significativos para variáveis, especialmente em consultas LINQ e laços.
- Devem ser usados tipos explícitos em `foreach` quando o tipo do elemento não for imediato.

### Comentários e documentação

- Devem ser usados comentários de linha única para explicações curtas.
- O comentário deve ser colocado em sua própria linha, começando com letra maiúscula e terminando com ponto final.

## Diretrizes de Layout

- Deve-se usar indentação consistente com quatro espaços.
- Deve-se manter as linhas em tamanho razoável e quebrar expressões longas quando isso melhorar a leitura.
- Devem ser separados blocos lógicos e declarações de membros com linhas em branco.
- Devem ser usados parênteses quando eles deixarem a precedência dos operadores mais clara.