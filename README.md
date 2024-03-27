## README - Projeto API de Gerenciamento de Aluguel de Motos e Entregas

Este README detalha a API desenvolvida em C# com .NET, destinada a gerenciar o aluguel de motos e entregas, baseada em requisitos específicos de um desafio de backend.

### Tecnologias Utilizadas

- **C# (Linguagem de Programação)**
- **RabbitMQ (Mensageria)**
- **PostgreSQL (Banco de Dados)**

### Configuração

**Pré-requisitos:**
- RabbitMQ instalado e executando localmente ou acessível via rede.
- Banco de dados PostgreSQL configurado e acessível.
- .NET SDK compatível instalado em sua máquina.

**Passos para a configuração:**
1. Clone o repositório para a sua máquina local.
2. Abra o projeto em sua IDE de escolha compatível com projetos .NET.
3. No arquivo de configuração, ajuste as strings de conexão para o PostgreSQL e as configurações de conexão para o RabbitMQ.
4. Construa e execute o projeto para iniciar a API.

### Casos de Uso e Exemplos de Rotas

A aplicação permite realizar diversas operações relacionadas ao gerenciamento de motos e entregadores, conforme os casos de uso abaixo. Um exemplo de rota é fornecido para ilustrar como as operações são acessadas via API.

#### Account

- **Registro de Admin:**
  - `POST /api/account/register-admin`
    - Registra um usuário com função de Admin e retorna um token de acesso.

- **Registro de Entregador:**
  - `POST /api/account/register-deliverer`
    - Registra um usuário entregador com função de Deliverer e retorna um token de acesso.
    - O campo "driverLicenseType" pode ter os valores: 0 - A, 1 - B, 2 - A+B.

- **Login:**
  - `POST /api/account/login`
    - Permite fazer login após o cadastro e retorna um token de acesso.

#### Deliverer

- **Envio de Foto da CNH:**
  - `POST /api/deliverer/upload-driver-license`
    - Permite o envio da foto da CNH para atualização do cadastro do entregador.
    - Os tipos de arquivo aceitos são PNG e BMP.

#### Moto

- **Consulta de Motos:**
  - `GET /api/moto`
    - Obtém todas as motos cadastradas.

- **Consulta de Moto por Placa:**
  - `GET /api/moto/{plate}`
    - Obtém detalhes de uma moto específica pela placa.

- **Cadastro de Moto:**
  - `POST /api/moto`
    - Cadastra uma nova moto. Não é possível cadastrar uma moto com a mesma placa.
    - O formato da placa deve seguir o padrão `AAA9X999`, onde:
      - `A` representa uma letra.
      - `9` representa um número.
      - `X` representa qualquer caractere alfanumérico (letra ou número)

- **Modificação de Placa de Moto:**
  - `PUT /api/moto/{motoId}?plate={plate}`
    - Permite modificar a placa de uma moto específica pelo seu ID. Use este método para corrigir o número da placa se cadastrado incorretamente.

- **Remoção de Moto:**
  - `DELETE /api/moto/delete/{id}`
    - Permite remover uma moto pelo seu ID, desde que não tenha locações associadas.

#### Rental

- **Aluguel de Moto:**
  - `POST /api/rental/rent/{rentalPlan}`
    - Permite que os entregadores aluguem uma moto. 
	- Os planos disponíveis são para 7, 15 e 30 dias. 
	- Ao realizar o aluguel, o serviço retorna o ID do aluguel, a placa e o ID da moto, juntamente com outras informações relevantes.

- **Devolução de Moto:**
  - `POST /api/rental/return/{rentId}?simulate={bool}`
    - Esta API permite que os entregadores informem a data de devolução da moto e consultem o valor total da locação.
	- O parâmetro opcional `simulate` se definido como true, apenas a simulação do retorno será realizada, mostrando as informações de custo sem salvar as alterações.


#### Order

- **Consulta de Ordens:**
  - `GET /api/order`
    - Retorna todas os pedidos cadastrados na plataforma.

- **Cadastro de Pedido:**
  - `POST /api/order`
    - Permite ao admin cadastrar um pedido na plataforma e disponibilizá-lo para os entregadores aptos efetuarem a entrega. 
    - Quando cadastrada, a situação do pedido é definido como "disponível".
	- Notificações são publicadas por mensageria e podem ser consumidas pelo método `POST/api/notification`.

- **Aceitar Pedido:**
  - `POST /api/order/take-order/{orderId}`
    - Permite que os entregadores notificados aceitem um pedido. 
	- Ao aceitar o pedido, o entregador se compromete com a entrega do mesmo, e a situação do pedido é atualizada para "aceito".

- **Marcar Pedido como Entregue:**
  - `POST /api/order/deliver-order/{orderId}`
    - Permite que os entregadores marquem um pedido como entregue, atualizando a situação do pedido para "entregue". Isso indica a conclusão da entrega.
	
#### Notification

- **Consulta de Notificações:**
  - `GET /api/notification`
    - Esta API permite visualizar todas as notificações enviadas para os entregadores.

- **Consumir Mensagens da Fila:**
  - `POST /api/notification`
    - Esta API consome as mensagens na fila da mensageria do RabbitMQ e cadastra uma notificação de cada pedido para cada entregador notificado. 

### Projeto de Testes e Coleção do Postman

Junto à API, é fornecido um projeto base de testes automatizados para facilitar a validação das funcionalidades implementadas. Além disso, um arquivo com a coleção do Postman contendo todos os métodos disponibilizados pela API é incluído para auxiliar no teste e na documentação das rotas disponíveis.
