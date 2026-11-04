# Siege of Hollow Keep (título provisório) — One-Pager

**CCC300 · Computação Gráfica e Realidade Virtual · Trabalho Prático 2026/2**
Dupla: Jean L. Alba e Ricardo Zancan · Engine: Unity 6000.3.25f1 · Plataforma: PC / Linux

| Campo | Descrição |
|---|---|
| **Conceito** | Tower Defense 2.5D (cenário 3D com câmera fixa) em que o jogador constrói torres em pontos pré-definidos para impedir que ondas de monstros, que seguem um caminho sinuoso fixo, destruam o castelo. |
| **Fantasia do jogador** | Comandar a última defesa de um castelo assombrado, decidindo onde cada moeda vale mais antes que a próxima horda chegue. |
| **Referências** | *Kingdom Rush* (slots fixos de construção, leitura clara do campo) e *Bloons TD* (clareza visual). Tom: fantasia medieval sombria com toque de Halloween. |
| **Estilo de arte** | **Provisório:** greybox com primitivas da Unity enquanto as regras ainda mudam. **Final:** modelos 3D simples (low-poly) do pacote Kenney (CC0), paleta escura, iluminação noturna e neblina leve. Câmera fixa, sem rotação pelo jogador. |
| **Mecânicas centrais** | 1. **Inimigos** seguem uma lista de *waypoints* da entrada até o castelo, têm vida, recebem dano e são removidos ao chegar a 0; ao alcançar o castelo, tiram vida dele e somem.<br>2. **Torres** ocupam slots fixos, detectam inimigos por raio de alcance, escolhem um alvo (o mais avançado no caminho), disparam um projétil e aguardam um intervalo até o próximo ataque.<br>3. **Projéteis** se deslocam até o alvo, detectam colisão, causam dano e são destruídos ao acertar ou sair da área.<br>4. **Dois tipos de torre**, com comportamentos distintos (ex.: arqueiros, de alcance longo, dano baixo e disparo rápido; canhão, de alcance curto, dano alto em área e disparo lento).<br>5. **Economia:** matar inimigos rende moedas, usadas para construir torres.<br>6. **Ondas espaçadas**, com intervalo entre elas para o jogador planejar e construir. |
| **Controles** | Mouse. Clique esquerdo em um slot vazio abre o menu de construção; clique na opção compra a torre. Menu principal com Jogar, Sobre o jogo (informações e controles) e Sair. |
| **Vitória e derrota** | **Vitória:** sobreviver a todas as ondas do nível (previsto: 5) com o castelo de pé.<br>**Derrota:** a vida do castelo chega a 0, ou o **tempo máximo da partida** se esgota sem a missão cumprida.<br>Ambos levam a uma tela de resultado com reinício. |
| **Público e sessão** | Fãs do gênero e jogadores casuais, no PC. Partida de poucos minutos, com tempo máximo definido no balanceamento (previsão inicial: 5 a 8 min). |
| **Entrega** | Repositório no GitHub (compartilhado com `diegopatr`, commits de ambos), README com links do executável e do vídeo de demonstração. Apresentação em 24/11, com o jogo rodando por 10 minutos. |
| **Extras opcionais (se sobrar tempo)** | 2º nível de dificuldade; ranking simples no menu. |
| **Fora do escopo (Won't)** | Pathfinding dinâmico (labirinto), construção livre em grid, controle direto de herói, múltiplos mapas, save system, câmera livre, suporte a VR/AR, chefes com mecânicas complexas, árvore de talentos. |

## Pilares de design

1. **Posicionamento acima de microgerenciamento:** o desafio é *onde* e *quando* construir. **Recusa:** habilidades ativas com cooldown, controle de herói ou mira manual.
2. **Leitura clara dentro da atmosfera sombria:** o clima vem de iluminação, paleta e escolha de assets, e nunca esconde torres, slots ou inimigos. **Recusa:** modelagem própria, shaders complexos, cena escura a ponto de prejudicar a leitura e dezenas de status ocultos (armadura mágica vs. física, sangramento, crítico).
3. **Caminho previsível, pressão constante:** a rota é fixa para focar em economia e posicionamento. **Recusa:** construção livre em grid que exija recalcular rota (NavMesh/A*) em tempo de execução.
