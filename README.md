<div align="center">

<h1>Shrek2ModManager</h1>

<p>Gerenciador de mods para o Shrek 2 Remastered (versão Steam).<br/>
Marque os mods que quiser, clique em Aplicar e pronto.</p>

<p>
  <a href="https://github.com/gabrielfernandesdelara/Shrek2ModManager/issues/new">Reportar um bug</a>
  ·
  <a href="https://github.com/gabrielfernandesdelara/Shrek2ModManager/issues/new">Sugerir uma funcionalidade</a>
</p>

<p>
  <img src="https://img.shields.io/badge/status-em%20desenvolvimento-yellow?style=for-the-badge" alt="Status" />
  <img src="https://img.shields.io/github/last-commit/gabrielfernandesdelara/Shrek2ModManager?style=for-the-badge&label=%C3%BAltimo%20commit" alt="Último commit" />
</p>

</div>

---

<details>
  <summary><strong>Sumário</strong></summary>

- [Sobre o projeto](#sobre-o-projeto)
- [Funcionalidades](#funcionalidades)
- [Mods inclusos](#mods-inclusos)
- [Tecnologias](#tecnologias)
- [Como compilar e rodar](#como-compilar-e-rodar)
- [Formato de um mod](#formato-de-um-mod)
- [Estrutura de pastas](#estrutura-de-pastas)
- [Roadmap](#roadmap)
- [Aviso legal](#aviso-legal)
- [Autor](#autor)

</details>

## Sobre o projeto

<div align="center">
  <img src="assets/images/readme/screenshot.png" alt="Janela do Shrek2ModManager" width="80%" />
  <p><sub><em>Janela principal. O layout pode sofrer alterações.</em></sub></p>
</div>

Os mods do Shrek 2 Remastered deste projeto funcionam trocando alguns bytes do `Shrek.exe`. Fazer isso com scripts `.bat` e `.ps1` é chato para quem instala e costuma ser barrado por filtros automáticos de sites de mods. O Shrek2ModManager resolve isso com uma janela simples: ele encontra o jogo pela Steam, mostra a lista de mods com caixinhas e instala ou remove cada um com um clique.

Cada mod é um arquivo `.json` na pasta `mods`, contendo o endereço de cada trecho, os bytes originais e os bytes novos. O programa não baixa nada, não acessa a internet e não inclui nenhum arquivo do jogo: ele só altera o `Shrek.exe` que já está instalado no computador, depois de salvar uma cópia do original.

## Funcionalidades

- Localiza o `Shrek.exe` automaticamente pelas bibliotecas da Steam, com opção de selecionar manualmente
- Instala e remove mods de forma independente: tirar um mod não desfaz os outros
- Salva o executável original como `Shrek.exe.original` antes da primeira alteração
- Confere os bytes antes de gravar: se o jogo for de outra versão, o mod aparece como incompatível e nada é alterado
- Avisa quando dois mods mexem no mesmo trecho do jogo e não deixa marcar os dois juntos
- Não aplica mudanças com o jogo aberto
- Lembra os mods instalados e oferece reinstalar quando uma atualização da Steam ou a verificação de arquivos desfaz os patches
- Interface em **português e inglês**, escolhida pelo idioma do Windows
- Executável de cerca de **32 KB**, sem instalador e sem dependências além do .NET Framework 4.8 que já vem no Windows 10 e 11

## Mods inclusos

O projeto vem com **3 mods**, feitos para a versão Steam do jogo (build CL 583774):

| Mod | O que faz |
|---|---|
| Teclado + Controle no co-op local | O teclado deixa de dividir o Jogador 1 com o primeiro controle e pode entrar como outro jogador |
| Cutscenes sem barras pretas | Remove as barras laterais e a de cima nas cutscenes; a faixa de baixo só aparece quando há legenda |
| Ultrawide sem barras pretas | Em telas mais largas que 16:9, a cena 3D ocupa a largura toda, sem esticar a imagem |

## Tecnologias

<p>
  <img src="https://img.shields.io/badge/C%23-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt="C#" />
  <img src="https://img.shields.io/badge/.NET%20Framework%204.8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET Framework 4.8" />
  <img src="https://img.shields.io/badge/Windows%20Forms-0078D4?style=for-the-badge&logoColor=white" alt="Windows Forms" />
  <img src="https://img.shields.io/badge/JSON-000000?style=for-the-badge&logo=json&logoColor=white" alt="JSON" />
</p>

| Tecnologia | Uso |
|---|---|
| C# 5 | Todo o código do programa |
| .NET Framework 4.8 | Plataforma de execução, já presente no Windows 10 e 11 |
| Windows Forms | Janela, lista de mods e botões |
| JSON | Formato dos arquivos de mod e das preferências do usuário |

## Como compilar e rodar

Não é preciso instalar Visual Studio nem SDK: o `build.bat` usa o compilador C# (`csc.exe`) que já vem com o Windows.

```bash
# 1. Clone o repositório
git clone https://github.com/gabrielfernandesdelara/Shrek2ModManager.git

# 2. Entre na pasta
cd Shrek2ModManager

# 3. Compile (gera dist\Shrek2ModManager.exe e copia os mods para dist\mods)
build.bat

# 4. Abra o programa
dist\Shrek2ModManager.exe
```

Depois de aplicar os mods, abra o jogo pela Steam ou pelo botão "Iniciar jogo". O jogo não abre quando o `Shrek.exe` é executado diretamente.

O arquivo `tests\EngineTests.cs` testa o motor de patches sem a janela. Ele recebe a pasta `mods` e o caminho de uma **cópia** do `Shrek.exe` original, e trabalha somente nessa cópia.

## Formato de um mod

Para criar um mod, coloque um arquivo `.json` na pasta `mods` e clique em "Atualizar":

```json
{
  "id": "ultrawide",
  "name": { "en": "Ultrawide without black bars", "pt": "Ultrawide sem barras pretas" },
  "description": { "en": "...", "pt": "..." },
  "version": "1.0",
  "gameBuild": "CL 583774",
  "patches": [
    { "rva": "0x244984", "note": "o que este trecho faz", "original": "8bf8", "patched": "33ff" }
  ]
}
```

- `rva` é o endereço do trecho dentro do executável.
- `original` e `patched` são os bytes em hexadecimal e precisam ter o mesmo tamanho.
- O mod só é instalado se todos os trechos estiverem com os bytes de `original`.

## Estrutura de pastas

```
Shrek2ModManager/
├── build.bat              # Compila o programa com o csc.exe do Windows
├── mods/                  # Um arquivo .json por mod
│   ├── cutscene-sem-barras.json
│   ├── teclado-controle.json
│   └── ultrawide.json
├── src/
│   ├── Program.cs         # Ponto de entrada
│   ├── MainForm.cs        # Janela: lista, descrição e botões
│   ├── PatchEngine.cs     # Leitura e gravação dos bytes do Shrek.exe
│   ├── ModLoader.cs       # Leitura dos arquivos .json
│   ├── SteamLocator.cs    # Localiza o jogo pelas bibliotecas da Steam
│   ├── Settings.cs        # Preferências salvas em %AppData%
│   ├── Texts.cs           # Textos em português e inglês
│   └── app.manifest       # Execução sem administrador e suporte a telas com escala
├── tests/
│   └── EngineTests.cs     # Testes do motor de patches
└── assets/images/readme/  # Imagens do README
```

## Roadmap

- [x] Instalar e remover mods por caixinha
- [x] Localizar o jogo automaticamente pela Steam
- [x] Backup do executável original
- [x] Aviso de conflito e de versão incompatível
- [x] Reinstalar mods desfeitos por atualização da Steam
- [x] Interface em português e inglês
- [ ] Ícone próprio para o programa
- [ ] Suporte a mods para outras versões do jogo
- [ ] Botão para restaurar o backup original

## Aviso legal

Este é um projeto pessoal e **não possui afiliação** com a DreamWorks Animation nem com as empresas responsáveis pelo Shrek 2 Remastered. Todos os direitos sobre o jogo, seus nomes e imagens pertencem aos seus respectivos donos. Nenhum arquivo do jogo é distribuído neste repositório.

## Autor

<p>
  Desenvolvido por <strong>Gabriel Fernandes de Lara</strong>
</p>

<p>
  <a href="https://www.linkedin.com/in/gabrielfernandesdelara/"><img src="https://img.shields.io/badge/LinkedIn-0A66C2?style=for-the-badge&logo=linkedin&logoColor=white" alt="LinkedIn" /></a>
  <a href="https://github.com/gabrielfernandesdelara"><img src="https://img.shields.io/badge/GitHub-181717?style=for-the-badge&logo=github&logoColor=white" alt="GitHub" /></a>
</p>
