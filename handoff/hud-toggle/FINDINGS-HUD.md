# Achados relevantes para o projeto de HUD

Extraidos de `docs/FINDINGS.md` (copia; o original continua intacto).

Pontos que o novo projeto precisa saber:

- O sandbox Lua do CET confina o `io` ao diretorio do mod; `os.getenv` nao existe e `_G` e `nil`.
- O CET v1.37 nao expoe registro de tecla para mods; a tecla global precisa vir de um processo externo.

### 13.1 O painel in-game não pode aplicar — e nunca pôde

O diagnóstico dentro do jogo devolveu, sem ambiguidade:

```
os.getenv e funcao                  -> nil
os = clock date difftime remove rename time      (sem getenv, sem execute)
_G                                    -> nil
io.open caminho com .. (1 nivel)     -> nil (bloqueado)
io.open ABSOLUTO jogo (engine/config) -> nil (bloqueado)
io.open ABSOLUTO temp do usuario     -> nil (bloqueado)
io.open CWD relativo                 -> gravou no CWD (CWD = diretorio do mod)
```

**O sandbox Lua do CET confina o `io` ao diretório do mod.** Três
consequências:

1. **`os.getenv` não existe.** `UserSettingsPath()` chamava
   `os.getenv("LOCALAPPDATA")` → *"attempt to call a nil value"* → o
   callback do botão Apply morria ali, **sem log e sem erro no log do
   CET**. É a causa-raiz da morte silenciosa registrada 3 vezes na §11.
2. **O caminho relativo com `..` também é bloqueado.** A correção de
   "cinco → seis níveis" da §11 é insuficiente: nenhum caminho relativo
   alcança fora do diretório do mod. O painel não tem como escrever o
   `user.ini` de nenhuma forma.
3. **`_G` é `nil`.** O ambiente do mod não expõe a tabela global, e
   `getfenv` também não está disponível — o inventário do ambiente só é
   possível por *sonda* de nomes conhecidos.

**Conclusão de arquitetura (com evidência, não opinião):** o painel in-game
não pode ser o aplicador. Ele pode (e deve) ser **coletor de intenção +
medidor**; a aplicação tem de acontecer fora do jogo. O instalador é o
aplicador; o painel pode gravar a intenção no próprio diretório para que
um aplicador externo a consuma.


### 14.4 Por que o painel in-game saiu do projeto

O painel vivia no sandbox Lua do Cyber Engine Tweaks, que **confina o `io` ao
diretório do mod**. O diagnóstico, rodando dentro do jogo, devolveu:

```
os.getenv                     -> nao existe
os = clock date difftime remove rename time   (sem getenv, sem execute)
_G                            -> nil
io.open caminho com ".."      -> nil
io.open caminho ABSOLUTO      -> nil
io.open no CWD do mod         -> funciona
```

Duas consequências definitivas:

1. **A causa da morte silenciosa do botão Apply.** `UserSettingsPath()` chamava
   `os.getenv("LOCALAPPDATA")` → *"attempt to call a nil value"* → o callback
   morria ali, sem log e sem erro no log do CET. As três falhas registradas
   na §10 são a mesma causa.
2. **Nenhum caminho alcança o jogo.** Nem relativo com `..`, nem absoluto.
   O painel **nunca** conseguiu aplicar o perfil — não era bug de caminho, e
   a correção de "cinco → seis níveis" não resolveria.

Some-se a isso o fato de o painel **poder mentir**: a interface refletia o
valor desejado, não o valor em vigor, e nada mais lia o arquivo de
intenção que ele gravava. Um painel pela metade no menu do jogo é pior do
que não ter: é uma interface que confirma mudanças inexistentes.

O aplicativo assume o papel, e com ele o acesso real aos arquivos, a
validação contra o esquema e contra o executável, e a possibilidade de
mostrar o estado efetivo.

