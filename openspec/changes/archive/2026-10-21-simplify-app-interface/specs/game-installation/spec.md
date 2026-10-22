# Spec Delta

## MODIFIED Requirements

### Requirement: A operação é recusada com o jogo em execução

O aplicativo SHALL recusar aplicar o perfil enquanto o processo do jogo estiver em execução, e SHALL explicar o motivo. Quando o processo não encerrar por conta própria, a mensagem SHALL orientar como encerrá-lo.

#### Scenario: Jogo em execução no momento de aplicar

- **WHEN** o usuário tenta aplicar o perfil e o processo do jogo está em execução
- **THEN** o aplicativo recusa a operação
- **AND** informa que o jogo sobrescreve as opções de menu ao encerrar, anulando a aplicação
- **AND** nenhuma configuração é alterada

#### Scenario: Jogo encerrado entre a checagem e a aplicação

- **WHEN** o jogo está em execução na checagem e termina antes da aplicação
- **THEN** a operação prossegue normalmente
- **AND** o estado do jogo é verificado de novo imediatamente antes de escrever

#### Scenario: Processo que não encerra

- **WHEN** o jogo já foi fechado mas o processo ainda consta como em execução
- **THEN** a mensagem de recusa orienta a encerrar o processo pelo Gerenciador de Tarefas
- **AND** a operação continua recusada
