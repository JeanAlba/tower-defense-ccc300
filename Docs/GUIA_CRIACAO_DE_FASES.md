# 🏰 Guia do Designer de Fases: Framework de Ondas e Rotas
**Projeto:** Siege of Hollow Keep (CCC300 · 2026/2)  
**Público-alvo:** Documentação de uso para a equipe de desenvolvimento (Jean e Ricardo).

---

## 📌 Sumário
1. [Visão Geral da Arquitetura](#-visão-geral-da-arquitetura)
2. [Conceitos Fundamentais](#-conceitos-fundamentais)
3. [Passo a Passo 1: Criando uma Rota e Waypoints na Cena](#-passo-a-passo-1-criando-uma-rota-e-waypoints-na-cena)
4. [Passo a Passo 2: Criando Inimigos](#-passo-a-passo-2-criando-inimigos)
5. [Passo a Passo 3: Configurando as Ondas no LevelData](#-passo-a-passo-3-configurando-as-ondas-no-leveldata)
6. [Passo a Passo 4: Conectando Tudo e Testando](#-passo-a-passo-4-conectando-tudo-e-testando)
7. [Dicas, Atalhos e Solução de Problemas](#-dicas-atalhos-e-solução-de-problemas)

---

## 🧠 Visão Geral da Arquitetura

O sistema é dividido estritamente em **duas camadas desacopladas**:

```
        CAMADA DE ASSETS (No Project)                  CAMADA ESPACIAL (Na Cena)
   ┌─────────────────────────────────────┐         ┌────────────────────────────────┐
   │ PathIdentifier (ScriptableObject)   │◀────────│ EnemyPath (MonoBehaviour)      │
   │ Ex: Path_Main.asset (Cor, Info)     │         │ Lista de Waypoint 1, 2, 3...   │
   └──────────────────┬──────────────────┘         └────────────────┬───────────────┘
                      │ Referência                                  │ Registrado em
                      ▼                                             ▼
   ┌─────────────────────────────────────┐         ┌────────────────────────────────┐
   │ LevelData (ScriptableObject)        │────────▶│ WaveSpawner + PathRegistry     │
   │  └── Waves [Duração]                │ Dispara │ Orquestra a cena e instancia   │
   │       └── Subwaves [% da Wave]      │         │ os monstros no caminho correto │
   │            └── SpawnGroup [Enemy]   │         └────────────────────────────────┘
   └─────────────────────────────────────┘
```

* **Vantagem:** Você pode equilibrar vida dos monstros, quantidade de inimigos, tempos de spawn e ritmo das ondas **sem precisar abrir ou modificar a cena** (evitando conflitos no Git).
* **Tipagem Estrita:** Nada de `Transform` ou `GameObject` genéricos. O Inspector só aceita o tipo certo (não há como colocar um objeto errado por engano).

---

## 🏷️ Conceitos Fundamentais

| Elemento | O que é? | Onde fica? |
|---|---|---|
| **`PathIdentifier`** | Asset "etiqueta" que dá identidade a um caminho (ex: Rota Principal, Flanco Esquerdo) e define a cor dos seus Gizmos. | Pasta de Assets (`.asset`) |
| **`Waypoint`** | Objeto vazio na cena que marca uma coordenada 3D exata por onde o monstro deve passar. | Na Cena (GameObject) |
| **`EnemyPath`** | Componente que agrupa uma sequência ordenada de `Waypoint`s e se associa a um `PathIdentifier`. | Na Cena (GameObject) |
| **`PathRegistry`** | Componente mestre na cena que cataloga todos os `EnemyPath` para o spawner encontrar rapidamente. | Na Cena (`[LEVEL]`) |
| **`LevelData`** | Asset que guarda a configuração completa das 5 ondas, economia inicial e intervalos. | Pasta de Assets (`.asset`) |
| **`WaveData`** | Uma onda com tempo de duração próprio (ex: 45s). | Dentro do `LevelData` |
| **`SubWaveData`** | Momento de disparo dentro da onda definido por **porcentagem** (`0%` = início, `50%` = metade, `80%` = clímax). | Dentro do `WaveData` |
| **`SpawnGroup`** | O lote de monstros da subonda: Prefab tipado (`Enemy`), quantidade, intervalo e qual rota seguir (`PathIdentifier`). | Dentro da `SubWaveData` |

---

## 🛣️ Passo a Passo 1: Criando uma Rota e Waypoints na Cena

### 1.1 Criar o Identificador da Rota (Project)
1. Na janela **Project**, vá em `Assets/Settings/` (ou crie uma pasta `Assets/Data/Paths/`).
2. Clique com botão direito -> **Create -> HollowKeep -> Level -> Path Identifier**.
3. Nomeie como `Path_Main` (ou `Path_Flank`).
4. No Inspector, defina a **Gizmo Color** (ex: Amarelo para Principal, Vermelho para Flanco).

### 1.2 Posicionar os Waypoints na Cena
1. Crie um GameObject vazio na cena e chame de `WP_01`.
2. Adicione o componente **`Waypoint`** nele.
3. Duplique (`Ctrl + D`) e posicione os próximos waypoints ao longo da estrada (`WP_02`, `WP_03`, ..., até o Castelo).
   > *Dica:* Na Scene View, você verá esferas azuis translúcidas indicando a posição de cada waypoint.

### 1.3 Agrupar a Rota (`EnemyPath`)
1. Crie um GameObject vazio chamado `Path_Principal`.
2. Adicione o componente **`EnemyPath`**.
3. No campo **Path Id**, arraste o asset `Path_Main`.
4. No campo **Waypoints**, defina o tamanho e arraste os waypoints na **ordem exata** do início ao castelo.
5. **Teste visual:** Ao clicar no GameObject `Path_Principal` na hierarquia, **o caminho inteiro acenderá na Scene View**, desenhando linhas e cubos conectando todos os waypoints na cor definida no `PathIdentifier`!

---

## 👾 Passo a Passo 2: Criando Inimigos

Qualquer inimigo no jogo herda da classe base `Enemy`:

1. Crie um modelo 3D simples (ex: Cubo ou Esfera para Greybox, ou modelo Kenney).
2. Adicione um `Collider` (para futuros disparos de torres) e o script do monstro (ex: `TestMinionEnemy` ou outro derivado de `Enemy`).
3. No Inspector, configure:
   * **Max Health:** Vida total (ex: `100`).
   * **Move Speed:** Velocidade de caminhada (ex: `3.5`).
   * **Gold Reward:** Ouro concedido ao morrer (ex: `15`).
   * **Damage To Castle:** Dano causado se chegar ao castelo (ex: `1`).
4. Arraste o objeto para a pasta `Assets/Prefabs/Enemies/` para transformá-lo em um **Prefab**.

---

## 🌊 Passo a Passo 3: Configurando as Ondas no `LevelData`

### 3.1 Criar o Asset do Nível
1. No Project, clique com botão direito -> **Create -> HollowKeep -> Level -> Level Data**.
2. Nomeie como `Level_01`.
3. Configure os parâmetros gerais:
   * **Starting Gold:** Ouro inicial do jogador (ex: `150`).
   * **Time Between Waves:** Segundos de intervalo/planejamento entre uma onda e outra (ex: `15`).

### 3.2 Montar as Ondas (`Waves`)
Expanda a lista **Waves** e adicione os elementos (previsão do projeto: 5 ondas). Para cada onda:
* **Duration:** Tempo total da janela de spawns da onda (ex: `60` segundos).
* **Subwaves:**
  * **Subwave 0 (Início da onda):**
    * `Wave Percentage:` `0` (dispara aos 0 segundos).
    * `Spawn Groups:`
      * `Enemy Prefab:` Arraste o prefab do inimigo.
      * `Quantity:` `8`
      * `Interval:` `1.0` (um a cada 1 segundo).
      * `Target Path:` Arraste o asset `Path_Main`.
  * **Subwave 1 (Metade da onda):**
    * `Wave Percentage:` `50` (dispara aos 30s de uma onda de 60s).
    * `Spawn Groups:`
      * `Enemy Prefab:` Arraste um inimigo mais forte ou rápido.
      * `Quantity:` `5`
      * `Interval:` `1.5`
      * `Target Path:` Arraste o asset `Path_Main` (ou `Path_Flank`).

> **Poder do sistema percentual:** Se durante o balanceamento você achar que a onda ficou muito rápida e mudar a `Duration` de `60s` para `90s`, as subondas das porcentagens `0%` e `50%` vão recalcular automaticamente seus disparos (0s e 45s) sem você ter que refazer contas!

---

## 🎮 Passo a Passo 4: Conectando Tudo e Testando

1. Na cena, crie um GameObject mestre chamado `[LEVEL]`.
2. Adicione os componentes:
   * **`PathRegistry`**: ele buscará automaticamente os `EnemyPath` existentes na cena.
   * **`WaveSpawner`**:
     * Arraste o asset `Level_01` no campo **Level Data**.
     * Deixe marcado **Auto Start On Play**.
3. Aperte o **Play** no Unity!
   * Após o timer inicial de 3 segundos, os inimigos começarão a nascer no primeiro waypoint da rota e caminharão até o último ponto.
   * No console aparecerão os logs:
     `[WaveSpawner] Iniciando Onda 1/5 (Duração: 60s)`
     `[WaveSpawner] Onda 1/5 finalizada!`

---

## 💡 Dicas e Solução de Problemas

* **O inimigo não está andando:**
  * Verifique se o `EnemyPath` contém waypoints no array.
  * Verifique se o monstro tem velocidade (`MoveSpeed > 0`).
* **Erro: "Caminho não encontrado no registro":**
  * Certifique-se de que o `Target Path` selecionado no `SpawnGroup` do `LevelData` é o **mesmo asset** `PathIdentifier` atribuído ao `EnemyPath` na cena.
* **Ordem dos Waypoints:**
  * O índice `0` deve ser o ponto de entrada (spawn) e o último índice deve ser o ponto de entrada no castelo.
* **Gizmos não aparecem:**
  * Certifique-se de que o botão **Gizmos** no canto superior direito da Scene View está ativado no Unity.
