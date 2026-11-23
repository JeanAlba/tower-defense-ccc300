# Plano de Balanceamento, Diversão e Dificuldade Modular

**Projeto:** Siege of Hollow Keep (`tower-defense-ccc300`)  
**Objetivo deste documento:** Registrar decisões de arquitetura e balanceamento para manter contexto contínuo entre sessões e agentes, alinhado aos pilares de [`README.md`](../README.md).

---

## 1. Decisão Arquitetural: Dados de Inimigos

### Escolha Adotada: `EnemyStatsData` (`ScriptableObject`) — Dados Desacoplados

Em vez de manter propriedades C# fixas (`abstract int MaxHealth { get; }`) em `Enemy.cs` ou serializar os atributos diretamente dentro de cada `.prefab`, adotamos **`ScriptableObject` dedicado (`EnemyStatsData`)** referenciado por `Enemy`.

| Critério | `[SerializeField]` no Prefab (Acoplado) | `EnemyStatsData` `ScriptableObject` (Escolhido) |
|---|---|---|
| **Consistência arquitetural** | Diverge de `LevelData` e `PathIdentifier` | Segue o mesmo padrão *Dados vs. Cena/Prefab* de `Docs/ARQUITETURA_FRAMEWORK_FASES.md` |
| **Conflitos no Git** | Alto risco ao editar prefab (visual + balanceamento juntos) | Baixo risco: balanceamento fica em `Assets/Data/Enemies/*.asset` |
| **Criação de variantes** | Exige subclasse C# ou edição manual por prefab | Basta criar novo `.asset` e trocar o mesh no prefab (sem código novo) |
| **Escala de dificuldade** | Exige expor/copiar campos manualmente | `WaveSpawner` aplica multiplicadores do `DifficultyProfile` sobre os valores base |

### Regra de Implementação para `Enemy.cs`
- Substituir propriedades abstratas (`MaxHealth`, `MoveSpeed`, `GoldReward`, `DamageToCastle`) por referência `[SerializeField] private EnemyStatsData stats;` (com fallback seguro caso nulo).
- Manter valores de runtime modificáveis (`_runtimeMaxHealth`, `_runtimeMoveSpeed`, `_runtimeGoldReward`) inicializados a partir de `stats` e ajustáveis via `ApplyDifficulty(...)` antes de `Enemy.Initialize(...)`.
- Limpar/ignorar os campos obsoletos (`maxHealth`, `goldReward`) que ficaram serializados no YAML de `Assets/Prefabs/Enemies/Esqueleto.prefab`.
- Usar uma classe concreta padrão (ex.: `StandardEnemy : Enemy` ou tornar `Enemy` concreto) para novos monstros que não tenham comportamento especial.

---

## 2. Contexto Atual Medido (`Level_01`)

- **Rota (`Level_01.unity`, `WP_01`..`WP_11`):** `52m` totais; a `3.5 m/s`, o inimigo cruza em `~14.9s`.
- **Slots de construção:** `7` instâncias de `BuildSlot.prefab` em `Level_01.unity`.
- **Vidas iniciais do castelo:** `20` (`GameHUDController.startingLives`).
- **Torres atuais:**
  - `DirectTower.prefab` (Arqueira): custo `50`, alcance `8`, cadência `2/s`, dano `12` (`24 DPS`).
  - `ParabolicTower.prefab` (Canhão): custo `80`, alcance `5`, cadência `0.6/s`, dano `35`, splash `2.5` (`21 DPS` single-target; `ParabolicProjectile.prefab` tem `flightDuration = 0.9s`, tempo em que um inimigo a `3.5 m/s` anda `3.15m` e sai do raio de explosão).
- **Estado atual de `Level_01.asset`:** `3` ondas (`14s + 18s + 22s`), `42` inimigos idênticos (`TestMinionEnemy`), partida total de `~1m20s–1m40s`, sem popup de vitória em `GameHUDController.HandleAllWavesCompleted()` e sem upgrade/venda em `BuildMenuUI`.

---

## 3. Economia e Arquétipos de Inimigos

Alinhado ao escopo de [`README.md`](../README.md) (sem status ocultos como armadura mágica/física; leitura clara; sem chefes com mecânicas complexas).

| Asset sugerido | HP Base | Velocidade | Dano Castelo | Ouro por Abate | Papel |
|---|---:|---:|---:|---:|---|
| `Stats_Esqueleto` | `75` | `3.5` | `1` | **`10`** | Unidade base de horda |
| `Stats_Corredor` | `45` | `5.5` | `1` | **`7`** | Pressão rápida; pune lacunas na defesa |
| `Stats_Cultista` | `160` | `3.0` | `2` | **`18`** | Unidade média/elite |
| `Stats_Brutamontes` | `320` | `2.0` | `3` | **`30`** | Tanque lento; valoriza Canhão e upgrades |
| `Stats_Campeao` | `900` | `1.7` | `8` | **`60`** | Clímax da Onda 5 (apenas mais HP/escala visual, sem mecânicas complexas) |

- **Ouro inicial (Normal):** `150` (permite `3` Arqueiras ou `1` Canhão + `1` Arqueira na abertura).
- **Orçamento total alvo da fase (Normal):** `~900 a 1.100` ouro somado (para preencher os `7` slots + fazer `4 a 6` upgrades, sem sobrar ouro ocioso).

---

## 4. Ritmo e Duração da Partida (~5 minutos, 5 Ondas)

Alinhado à previsão de **5 ondas** e **5 a 8 min** do [`README.md`](../README.md).

- **Intervalo entre ondas (`timeBetweenWaves`):** `12s` (1ª onda mantém `3s` iniciais no `WaveSpawner`).
- **Bônus de adiantamento:** chamar `SkipIntermission()` concede **`+1` ouro por segundo restante**.
- **Estrutura de ondas no `Level_01.asset`:**
  1. **Onda 1 (~40s):** `10` Esqueletos em 2 subondas (`0%`, `50%`).
  2. **Onda 2 (~50s):** `12` Esqueletos + `6` Corredores (`0%`, `45%`, `75%`).
  3. **Onda 3 (~60s):** `10` Esqueletos + `8` Corredores + `3` Brutamontes (`0%`, `40%`, `75%`).
  4. **Onda 4 (~70s):** `12` Corredores + `8` Cultistas + `4` Brutamontes (`0%`, `35%`, `70%`).
  5. **Onda 5 (~80s + combate):** `14` Esqueletos + `10` Corredores + `6` Cultistas + `4` Brutamontes + `1` Campeão aos `70%`.

---

## 5. Sistema Modular de Dificuldade (`Normal` e `Difícil`)

### Componentes
1. **`DifficultyProfile` (`ScriptableObject`):** armazenado em `Assets/Data/Difficulty/` (`Difficulty_Normal.asset` e `Difficulty_Hard.asset`).
2. **`GameSessionConfig` (estático / persistente entre cenas):** guarda o `DifficultyProfile` escolhido no `MainMenuController` (com fallback automático para `Difficulty_Normal` ao iniciar `Level_01` direto no Editor).
3. **Pontos de aplicação:**
   - `GameHUDController`: lê `startingLives` e `startingGoldMultiplier`; aplica `goldRewardMultiplier` em `HandleEnemyDied`.
   - `WaveSpawner`: aplica `timeBetweenWavesMultiplier`, `spawnQuantityMultiplier`, `spawnIntervalMultiplier` e injeta `enemyHealthMultiplier`, `enemySpeedMultiplier`, `goldRewardMultiplier` no `Enemy` instanciado.

### Valores Iniciais dos Perfis

| Campo | `Normal` | `Difícil` |
|---|---:|---:|
| `startingLives` | `20` | `10` |
| `startingGoldMultiplier` | `1.0` (`150`) | `0.8` (`120`) |
| `goldRewardMultiplier` | `1.0` | `0.85` |
| `enemyHealthMultiplier` | `1.0` | `1.35` |
| `enemySpeedMultiplier` | `1.0` | `1.15` |
| `spawnQuantityMultiplier` | `1.0` | `1.25` |
| `spawnIntervalMultiplier` | `1.0` | `0.85` |
| `timeBetweenWavesMultiplier` | `1.0` (`12s`) | `0.75` (`9s`) |

---

## 6. Backlog Priorizado (Checklist de Execução)

Marcar os itens conforme forem concluídos:

- [x] **Etapa 1 — `EnemyStatsData` + `DifficultyProfile`:** criar ambos os `ScriptableObject`s (`EnemyStatsData`, `DifficultyProfile`), `GameSessionConfig`, refatorar `Enemy.cs` / `TestMinionEnemy.cs`, conectar em `WaveSpawner` e `GameHUDController`, e criar assets `Difficulty_Normal`, `Difficulty_Hard`, `Stats_Esqueleto`, `Stats_TestMinion`.
- [x] **Etapa 2 — Correção do Canhão + Feedback de Combate:** predição de rota em `Enemy.PredictPositionAfter` + `ParabolicTower` + voo `0.6s` em `ParabolicProjectile`, `EnemyHealthBar` em World Space nos inimigos e `TowerRangeIndicator` ao passar o mouse nas opções ou clicar em torres construídas.
- [x] **Etapa 3 — Loop Completo (Vitória + Velocidade 1x/2x + Bônus de Onda):** popup de Vitória com botão único "PRÓXIMA FASE" conectado em `GameHUDController.HandleAllWavesCompleted()`, botão `1X/2X` ao lado do Pause com flag `showSpeedButton`, e bônus de ouro ao adiantar onda escalado pela `DifficultyProfile`.
- [x] **Etapa 4 — Upgrade (Nível 2) e Demolição de Torres:** evolução de 1 nível (`MaxLevel = 2`, `+45%` dano, `+20%` cadência, `+0.75m` alcance) em `Tower.cs` + `BuildSlot.cs`, e menu radial de gerenciamento (Evoluir / Demolir com `70%` de reembolso) ao clicar em slots ocupados em `BuildMenuUI.cs`.
- [x] **Etapa 5 — Conteúdo, Ondas e Seletor de Dificuldade Pré-Fase:** criados `Stats_Corredor`, `Stats_Cultista`, `Stats_Brutamontes`, `Stats_Campeao` e os 4 prefabs correspondentes (`Corredor`, `Cultista`, `Brutamontes`, `Campeao`); `Level_01.asset` atualizado para 5 ondas (~5 min); e seletor `Normal / Difícil` integrado ao `StageBriefingPopup` em `LevelCore.prefab` via `GameSessionConfig.SetDifficulty(...)`.
