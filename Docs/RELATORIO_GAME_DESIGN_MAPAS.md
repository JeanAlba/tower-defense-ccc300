# Relatório de Game Design e Level Design: Base de Mapas para *Siege of Hollow Keep*

**Projeto:** *Siege of Hollow Keep* (`tower-defense-ccc300` · CCC300 2026/2 · Apresentação: `24/11`)  
**Referências base:** [`README.md`](../README.md) (One-Pager), [`ARQUITETURA_FRAMEWORK_FASES.md`](ARQUITETURA_FRAMEWORK_FASES.md), [`GUIA_CRIACAO_DE_FASES.md`](GUIA_CRIACAO_DE_FASES.md), [`PLANO_BALANCEAMENTO_E_DIFICULDADE.md`](PLANO_BALANCEAMENTO_E_DIFICULDADE.md)  
**Última atualização:** 2026-11-22 (alinhado após entrevista `/grill-me`)

---

## 1. Síntese do One-Pager vs. Estado Atual do Projeto

### 1.1 O que o One-Pager define
1. **Identidade e Fantasia:** Tower Defense **2.5D** com câmera fixa (sem rotação pelo jogador), estética **medieval sombria com toque de Halloween**, inspirado em *Kingdom Rush* (slots estratégicos, leitura imediata) e *Bloons TD* (clareza visual). O jogador defende o último castelo contra hordas de monstros.
2. **Pilares de Design:**
   - **Posicionamento e economia acima de microgerenciamento:** o desafio vem de *onde* construir, *quando* evoluir e como combinar torres com alcance/cadência distintos.
   - **Leitura clara dentro da atmosfera sombria:** contraste nítido entre bordas escuras, estrada caminhável, slots vazios, torres e hordas.
   - **Rotas fixas e pressão previsível:** caminhos pré-traçados com `EnemyPath` + `Waypoint` e dispersão lateral orgânica (`spreadRadius`).

### 1.2 Estado Real do Projeto Hoje (Base Verificada)
Muitos sistemas que antes estavam pendentes **já foram implementados** no projeto e mudam o ponto de partida da criação dos mapas:

| Sistema | Estado Atual Verificado | Onde Está |
|---|---|---|
| **Torres e Upgrade/Venda** | `DirectTower` (`50g`, range `8`, `2/s`, dano `12`) e `ParabolicTower` (`80g`, range `5`, `0.6/s`, dano `35`, splash `2.5`); Nível 2 (`+45%` dano, `+20%` cadência, `+0.75` alcance, custo `80%`), reembolso `70%`, `TowerRangeIndicator` | [`Tower.cs`](../Assets/Scripts/Towers/Tower.cs), [`BuildSlot.cs`](../Assets/Scripts/Towers/BuildSlot.cs), [`BuildMenuUI.cs`](../Assets/Scripts/UI/BuildMenuUI.cs) |
| **5 Arquétipos de Inimigos** | `Esqueleto` (`75 HP`, vel `3.5`, `10g`, dano `1`), `Corredor` (`45 HP`, vel `5.5`, `7g`, dano `1`), `Cultista` (`160 HP`, vel `3`, `18g`, dano `2`), `Brutamontes` (`320 HP`, vel `2`, `30g`, dano `3`), `Campeão` (`900 HP`, vel `1.7`, `60g`, dano `8`), todos com `EnemyHealthBar` | [`EnemyStatsData.cs`](../Assets/Scripts/Enemies/EnemyStatsData.cs), `Assets/Data/Enemies/Stats_*.asset`, `Assets/Prefabs/Enemies/` |
| **5 Ondas + Ritmo** | `Level_01.asset` atualizado com **5 ondas** (`22s`, `32s`, `42s`, `50s`, `58s`), `startingGold: 150`, `timeBetweenWaves: 12`, rota `Path_Main` | [`Level_01.asset`](../Assets/Data/Levels/Level_01.asset) |
| **Dificuldade Modular** | `Difficulty_Normal` (`20` vidas, `1x`) e `Difficulty_Hard` (`10` vidas, `0.8x` ouro inicial, `0.85x` recompensa, `1.35x` HP, `1.15x` vel, `1.25x` qtd) integrados via `GameSessionConfig` | [`DifficultyProfile.cs`](../Assets/Scripts/Level/Data/DifficultyProfile.cs), [`GameSessionConfig.cs`](../Assets/Scripts/Level/Data/GameSessionConfig.cs), [`WaveSpawner.cs`](../Assets/Scripts/Level/Spawning/WaveSpawner.cs) |
| **Loop de Partida e HUD** | Velocidade `1x/2x`, bônus de `+1g/s` ao adiantar onda, Derrota (Castelo = `0` HP), Vitória (`VictoryPopup` + `victoryNextLevelButton` + `nextLevelSceneName`) e `CreditsPopup` no `MainMenu` | [`GameHUDController.cs`](../Assets/Scripts/UI/GameHUDController.cs), [`MainMenuController.cs`](../Assets/Scripts/UI/MainMenuController.cs) |
| **VFX e Navegação** | 6 prefabs em `Assets/Prefabs/VFX/` + dispersão lateral de horda com `Waypoint.spreadRadius` (`0.8f` padrão) | [`Waypoint.cs`](../Assets/Scripts/Level/Navigation/Waypoint.cs), [`EnemyPath.cs`](../Assets/Scripts/Level/Navigation/EnemyPath.cs) |
| **Cena / Mapas** | `Level_01.unity` ainda usa grid provisório (`tile_grass`) e escala não padronizada; `Level_02.unity` ainda não existe; `EditorBuildSettings` tem `MainMenu`, `Level_01`, `SampleScene` | [`Level_01.unity`](../Assets/Scenes/Level_01.unity), [`EditorBuildSettings.asset`](../ProjectSettings/EditorBuildSettings.asset) |

---

## 2. Decisões de Design Alinhadas (`/grill-me`)

Todas as decisões estruturais abaixo foram fechadas antes da execução dos mapas:

| # | Decisão | Escolha Fechada | Impacto Direto |
|---|---|---|---|
| **D1** | **Escopo de entrega (`24/11`)** | **Mínimo 2 mapas jogáveis** (`Level_01` e `Level_02`); 3º mapa apenas se sobrar tempo | Foco total em reconstruir `Level_01` com qualidade e entregar `Level_02` completo e balanceado |
| **D2** | **Técnica visual sem grid** | **`Unity Terrain` com estrada pintada + props Kenney ao redor** | Remove `tile_grass`; permite curvas suaves, desníveis naturais nas bordas e largura de estrada sob medida para `spreadRadius` |
| **D3** | **Câmera e Escala** | **Câmera 2.5D fixa** enquadrando 100% da área jogável + **normalizar mundo/prefabs para escala `1:1`** | `BuildSlot.prefab` passa a operar em `(1,1,1)`; obriga passe de recalibração em `range`, `splashRadius`, `moveSpeed`, `spreadRadius` e distância da câmera |
| **D4** | **Topologia de rotas** | **`Level_01`: 1 rota longa (S/U)** \| **`Level_02`: 2 rotas convergentes** antes do gargalo do castelo | Fase 1 ensina posicionamento e economia; Fase 2 testa cobertura dupla vs concentração no ponto de encontro |
| **D5** | **Seleção de Fase e Dificuldade** | **Será feita depois por outro agente** em um menu próprio de Fases + Seletor de Dificuldade | As cenas dos mapas devem depender apenas de `GameSessionConfig` (com fallback local `defaultDifficulty`) e configurar `nextLevelSceneName` |
| **D6** | **Condição de Derrota** | **Remover derrota por "tempo máximo da partida"** (derrota apenas por Castelo = `0` HP); alinhar o `README.md` | Evita frustração artificial e combina com bônus por adiantar onda e controle de velocidade `1x/2x` |
| **D7** | **Ambientação dos Mapas** | **Fase 1: Floresta & Cemitério Externo** \| **Fase 2: Muralhas & Pátio do Castelo** | Aproveita `kenney_mini-forest_1.0`, `kenney_graveyard-kit_5.0`, `kenney_castle-kit`, `kenney_mini-dungeon` e materiais foscos já criados |
| **D8** | **Arquitetura Reutilizável** | **Extrair Prefab `LevelCore`** (HUD + BuildMenu + Spawner + Paths + EventSystem) + hierarquia padrão | Qualquer melhoria na UI/HUD propaga para todos os mapas automaticamente e reduz conflito de cena entre agentes |

---

## 3. Diretrizes de Escala `1:1` e Recalibração de Gameplay

Normalizar o projeto para **escala `1:1`** elimina ajustes artificiais de escala nos `BuildSlot`s (hoje `BuildSlot.prefab` tem `BoxCollider {1.1, 0.2, 1.1}` e escala `(1,1,1)`, mas [`BuildSlot.cs`](../Assets/Scripts/Towers/BuildSlot.cs) instancia a torre como filha herdando a escala do slot).

### 3.1 Regras Canônicas de Escala `1:1`
1. **Unidade base (`1.0u = 1 metro de jogo`):**
   - `BuildSlot` sempre em escala `(1, 1, 1)` sobre o terreno nivelado ao redor do slot.
   - Qualquer ajuste visual de altura/tamanho de modelo em torre ou inimigo deve ficar **no objeto filho visual (`Visual`/`Model`)**, nunca na raiz do `BuildSlot` nem do `Enemy`.
2. **Enquadramento da Câmera Fixa (16:9):**
   - Toda a zona jogável (origem das rotas, curvas, todos os `BuildSlot`s e portão do castelo) deve caber dentro de uma área útil central aproximada de **`26m x 16m`** (ou proporcional à altura/FOV escolhida na câmera fixa), reservando margens para a HUD:
     - Topo esquerdo: painel de Vida / Ouro / Onda.
     - Topo direito: botões de Velocidade `1x/2x` e Pausa.
     - Canto inferior direito: botão de avançar onda.
   - As bordas externas além da zona jogável ficam preenchidas por terreno elevado, árvores e muros para evitar sensação de "ilha flutuante".

### 3.2 Relação Estrada ↔ `Waypoint.spreadRadius` ↔ `ParabolicTower`
- **Fórmula da largura visual da estrada:**
  `Largura Visual da Estrada >= 2 * spreadRadius + Largura do Inimigo`
- **Calibração inicial recomendada em `1:1`:**
  - `spreadRadius` padrão entre **`0.55` e `0.75`** nas retas/curvas amplas (estrada pintada com largura visual ~`1.8m–2.2m`) e **`0.25–0.35`** nos gargalos (*chokepoints*) para agrupar hordas.
- **Calibração pós-normalização (passe obrigatório após montar o `Terrain` de `Level_01`):**
  - Na cena anterior em grid (`~52m` de rota), `DirectTower` tinha `range = 8` e `ParabolicTower` tinha `range = 5` / `splashRadius = 2.5`.
  - Ao reconstruir em `1:1`, **manter o tempo de travessia da rota principal perto de `15s–18s`** para o `Esqueleto` (`moveSpeed = 3.5` → rota total alvo entre **`50m` e `60m`** em zigue-zague S/U), ou ajustar `range` (`DirectTower` / `ParabolicTower`), `splashRadius` e `moveSpeed` proporcionalmente se a área enquadrada tiver comprimento menor (ex.: rota de `32m–36m` exige multiplicar velocidades e alcances por `~0.65`).

> **Regra de Ouro:** O parâmetro mais seguro para manter todo o balanceamento de [`PLANO_BALANCEAMENTO_E_DIFICULDADE.md`](PLANO_BALANCEAMENTO_E_DIFICULDADE.md) intacto sem precisar refazer todas as 5 waves é **desenhar o traçado em S/U de `Level_01` com `~48m–55m` dentro do quadro da câmera 1:1** e só então ajustar `range` / `moveSpeed` caso o círculo do `TowerRangeIndicator` cubra mapa demais ou de menos.

---

## 4. Projeto dos 2 Mapas (Template Reproduzível + Referências de Caminho)

> As imagens de inspiração foram salvas localmente fora do git em `Docs/Referencias/` (`Referencias/level_01_caminho_ref.webp` e `Referencias/level_02_caminho_ref.webp`). **Escopo da referência:** aproveitamos **apenas o desenho dos caminhos e posições de `BuildSlot`**; estética, iluminação, `Unity Terrain` `1:1`, inimigos e HUD seguem 100% as decisões anteriores deste documento.

### 4.1 Mapa 1 — `Level_01`: *Bosque Sombrio & Cemitério Externo*
- **Referência de traçado:** `Docs/Referencias/level_01_caminho_ref.webp` (arquivo local não versionado).
- **Papel de Game Design:** mapa de introdução clara. Ensina leitura da estrada, posicionamento de `DirectTower` em retas/curvas abertas vs `ParabolicTower` nas curvas fechadas, além de upgrade nível 2 e uso do botão de adiantar onda.
- **Geometria do Caminho (`Path_Main` — 1 rota longa em S/U):**
  - **Origem:** **lado direito** (borda direita superior, com marco visual de cemitério/cripta usando `kenney_graveyard-kit_5.0` e `kenney_mini-forest_1.0`).
  - **Traçado:** entra pelo lado direito → sobe formando o **U invertido amplo no topo central** e desce pelas **curvas centrais** exatamente como em `level_01_caminho_ref.webp` → **apenas no trecho final, dobra para o canto inferior esquerdo** em vez de terminar no centro inferior.
  - **Destino (Castelo/Portão Assombrado):** posicionado no **lado esquerdo inferior** em ângulo 2.5D voltado para a arena (sem ocluir a estrada atrás dele e fora da HUD, que ocupa topo esquerdo, topo direito e canto inferior direito).
  - **Comprimento alvo em `1:1`:** `~48m–55m` para manter o ritmo das 5 ondas de [`Level_01.asset`](../Assets/Data/Levels/Level_01.asset).
- **`BuildSlot`s (`7` da referência, expansível para `8–10`):**
  - **2 slots empilhados dentro do U invertido superior:** cobrem entrada, retorno e descida ao mesmo tempo (posições de maior valor).
  - **1 slot no bolsão interno da curva média-esquerda** + **2 slots flanqueando a curva central/inferior**.
  - **1–2 slots acompanhando a curva final rumo ao lado esquerdo inferior** (última linha antes do castelo, garantindo margem da borda da tela para o anel radial `BuildMenuCanvas`).
- **Estética e Kits (`Assets/KenneyPack/`):** `Unity Terrain` com grama sombria/fria e estrada de terra batida/pedra em alto contraste (sem fazendas/moinhos alegres da referência; usar floresta fechada, lápides e ruínas de borda).

### 4.2 Mapa 2 — `Level_02`: *Muralhas & Pátio de Hollow Keep*
- **Referência de traçado:** `Docs/Referencias/level_02_caminho_ref.webp` (arquivo local não versionado) — **caminho mantido com o mesmo design da imagem** (já entra pela direita e termina à esquerda).
- **Papel de Game Design:** mapa de escalada tática com curvas fechadas e corredor comprido, forçando escolha entre cobrir os laços iniciais ou concentrar poder na reta final.
- **Geometria do Caminho (adaptando a imagem para a decisão D4 — 2 rotas convergentes sem sair da tela):**
  - **Observação sobre a imagem:** o desenho mostra um grande laço em grampo na parte superior (vindo da **direita superior**, indo até o topo-esquerdo e voltando para a direita em torno de uma península com `3` slots) e um trecho inferior que volta pela **direita central** contornando curvas até sair no **centro-esquerdo** (onde fica o portão do castelo).
  - **Adaptação exata para `Level_02` (`Path_North` + `Path_South` convergentes):**
    1. **`Path_North`:** entra pelo **canto superior direito**, percorre todo o corredor superior até o grampo à esquerda, faz a volta em torno dos **3 slots da península** e **desce na direita interna para convergir** com `Path_South` (evitando saída fora do quadro da câmera fixa).
    2. **`Path_South`:** entra pela **borda centro-direita** (segundo foco de spawn), encontra `Path_North` na zona de junção/bolsão direito e segue unido pelo **grande arco inferior até o portão no centro-esquerda**.
    *(Alternativa opcional: caso prefira 1 rota contínua como serpentina única, basta conectar a curva direita dentro da área visível — mas manter os 2 spawns preserva o desafio tático alinhado em D4.)*
- **`BuildSlot`s (`9` a `12` slots em `1:1`, baseados na imagem):**
  - **2 slots acima do corredor superior** + **3 slots alinhados na península central do grampo superior** (área chave que atinge ida e volta de `Path_North`).
  - **1 slot no bolsão da curva centro-direita** (ponto de convergência ideal para `ParabolicTower`, com `spreadRadius` reduzido para `~0.35`).
  - **3–4 slots acompanhando a margem superior do arco inferior** + **2–3 slots abaixo da estrada antes do portão** na esquerda.
- **Estética e Kits (`Assets/KenneyPack/`):** `Unity Terrain` em pátio de fortaleza/pedra gasta (`kenney_castle-kit`, `kenney_graveyard-kit_5.0` e `kenney_mini-dungeon`) substituindo lago/tendas/ovelhas da imagem original por muralhas quebradas, fosso escuro ou ruínas na borda inferior.
- **Ondas (`Level_02.asset`):** 5 ondas alternando e combinando `Path_North` e `Path_South` (ex.: pressão rápida de `Corredor` por um flanco enquanto `Brutamontes`/`Cultista` marcham pelo outro até a Onda 5 com `Campeão`).

---

## 5. Arquitetura Reutilizável de Cena (`LevelCore` + Hierarquia Padrão)

Para que o time e outros agentes possam trabalhar em UI, Menu de Fases e novos mapas sem conflito de cena e sem perda de referências:

### 5.1 Separação Obrigatória da Cena
1. **Prefab compartilhado `LevelCore.prefab` (em `Assets/Prefabs/`):**
   - `WaveSpawner` (campo `levelData` sobrescrito por cena)
   - `PathRegistry` (alimentado pelos `EnemyPath` locais da cena)
   - `GameHUDCanvas` (já com `VictoryPopup`, `GameOverPopup`, `PausePopup`, botões `1x/2x`, `nextLevelSceneName` configurado por fase)
   - `BuildMenuCanvas` (`BuildMenuUI`)
   - `EventSystem`
   - *Regra multiagente:* alterações de HUD, menus radiais ou popups vão no `LevelCore.prefab` (e não editando cada cena `.unity` separadamente).
2. **Estrutura padrão da raiz de toda cena de fase (`Level_01.unity`, `Level_02.unity`, etc.):**
   - `01_Systems`: instância de `LevelCore` (`WaveSpawner.levelData` e `GameHUDController.nextLevelSceneName` configurados por fase) + `defaultDifficulty` apontando para [`Difficulty_Normal.asset`](../Assets/Data/Difficulty/Difficulty_Normal.asset).
   - `02_CameraAndLighting`: `Main Camera` fixa (`Orthographic: false`, enquadramento 100% da arena), `Directional Light` noturna, `Global Volume`, luzes locais de spawn e castelo.
   - `03_Terrain`: objeto `Unity Terrain` (`TerrainData` exclusivo da fase em `Assets/Data/Terrains/` ou `Assets/Levels/<Level>/`).
   - `04_Environment`: props Kenney (`Trees`, `Rocks`, `Graveyard`, `CastleGate`) fora da área de clique de slots.
   - `05_Paths`: objetos `EnemyPath` (`Path_Main` ou `Path_North`/`Path_South`) com `Waypoint`s alinhados à superfície do `Terrain` (`y` no nível da estrada).
   - `06_BuildSlots`: apenas instâncias de [`BuildSlot.prefab`](../Assets/Prefabs/Towers/BuildSlot.prefab) em escala `(1,1,1)`.

### 5.2 Contrato com o Futuro Menu de Fases e Dificuldade (Outro Agente)
- O outro agente conectará os botões de fase e dificuldade chamando `GameSessionConfig.SetDifficulty(selectedProfile)` e carregando a cena da fase pelo nome.
- **Garantias que nossas cenas precisam cumprir:**
  1. `WaveSpawner.defaultDifficulty` e `GameHUDController.defaultDifficulty` preenchidos com `Difficulty_Normal.asset` para funcionar quando rodar a fase isolada no Editor.
  2. `GameHUDController.nextLevelSceneName` em `Level_01.unity` preenchido como `"Level_02"` (e em `Level_02.unity` vazio ou voltando ao menu quando não houver `Level_03`).
  3. [`EditorBuildSettings.asset`](../ProjectSettings/EditorBuildSettings.asset) atualizado com `Level_02.unity` habilitada.
  4. [`README.md`](../README.md) atualizado removendo "tempo máximo da partida" das condições de derrota.

---

## 6. Ordem Recomendada de Execução

1. **Alinhar documentação:** remover a menção a "tempo máximo da partida" no One-Pager ([`README.md`](../README.md)).
2. **Criar `LevelCore.prefab` + validar escala `1:1`:** padronizar escala `(1,1,1)` de [`BuildSlot.prefab`](../Assets/Prefabs/Towers/BuildSlot.prefab), torres e inimigos e extrair o núcleo reutilizável de `Level_01.unity`.
3. **Reconstruir `Level_01.unity` com `Unity Terrain`:** remover o grid de `tile_grass`, pintar estrada larga em S/U (`~48m–55m`), posicionar `EnemyPath` (`Path_Main`), `8–10` `BuildSlot`s e cenário Floresta/Cemitério + Castelo.
4. **Calibrar gameplay em `1:1`:** testar as 5 ondas de `Level_01.asset` em `Normal` e `Difícil`, ajustando `range`, `splashRadius`, `moveSpeed` ou `spreadRadius` se necessário.
5. **Criar `Level_02.unity` + `Level_02.asset`:** montar `Terrain` das Muralhas/Pátio do Castelo com 2 rotas convergentes (`Path_North`, `Path_South`), `9–12` `BuildSlot`s, 5 ondas assimétricas, ligar `nextLevelSceneName = "Level_02"` no `Level_01` e adicionar `Level_02.unity` no `EditorBuildSettings`.
