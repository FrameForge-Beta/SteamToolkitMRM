# Steam Toolkit MRM para Windows

Aplicação desktop nativa em WPF para gerir backups de instalações Steam usadas com Lua Tools. A aplicação reconhece backups `BackupSteamMRM-*` e mantém a estrutura e os metadados `meta.json`.

## Utilização

Instala a aplicação com o instalador publicado em GitHub Releases. O instalador é autónomo e inclui o Ludusavi oficial. As definições ficam em `%LOCALAPPDATA%\SteamToolkitMRM` e os backups, por predefinição, na subpasta `backups`.

Escolhe **Criar backup** para uma cópia imediata ou deixa a aplicação aberta para criar backups automáticos ao iniciar e/ou fechar jogos. Cada momento pode ser ativado separadamente nas definições. **Executar com o Windows** e **Iniciar minimizado na bandeja do sistema** vêm ativados; o menu da bandeja permite reabrir a aplicação ou sair. Na lista podes fixar, renomear, restaurar ou apagar backups. Backups fixados não são removidos automaticamente pelo limite nem apagados até serem desafixados. A restauração fecha a Steam, copia os dados do backup para os locais correspondentes e volta a iniciar a Steam.

Em **Definições → Armazenamento**, podes escolher uma pasta e, se quiseres, mover os backups existentes. O idioma pode ser definido em **Definições → Idioma**: português, inglês, espanhol ou russo. A cor principal padrão é roxa; podes alterá-la em **Definições → Aparência**.

## Executar, testar e compilar

Requer o .NET 10 SDK para compilar, Inno Setup 6 para gerar o instalador e acesso à internet para descarregar o Ludusavi oficial v0.31.0. O script valida os SHA-256 dos arquivos baixados antes de os incluir:

```powershell
dotnet run --project .\native\SteamToolkitMRM.csproj
dotnet run --project .\native.tests\SteamToolkitMRM.Tests.csproj
.\build-distributions.ps1
```

O script cria o instalador principal em `WindowsApp\Instalador\SteamToolkitMRM-Setup.exe` e uma edição portátil para testes em `WindowsApp\Portatil\SteamToolkitMRM.exe`. Os artefactos compilados e definições locais são excluídos do controlo de versões; os instaladores públicos são distribuídos através de GitHub Releases.

## Testes dos backups

Os testes criam uma instalação Steam de exemplo isolada com as mesmas três pastas usadas pela versão anterior (`userdata`, `appcache\stats` e `depotcache`), validam a estrutura e os metadados gerados e confirmam a cópia e o restauro. Não alteram a instalação Steam real.

O código do aplicativo é licenciado sob MIT. Créditos e licenças de Ludusavi e das respetivas dependências estão em [`../THIRD-PARTY-NOTICES.md`](../THIRD-PARTY-NOTICES.md).
