using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SteamToolkitMRM;

internal static class Localization
{
    private static readonly Dictionary<string, string[]> Translations = new(StringComparer.Ordinal)
    {
        ["ESPAÇO DE TRABALHO"] = ["ESPAÇO DE TRABALHO", "WORKSPACE", "ESPACIO DE TRABAJO", "РАБОЧАЯ ОБЛАСТЬ"],
        ["Backups"] = ["Backups", "Backups", "Copias de seguridad", "Резервные копии"],
        ["Definições"] = ["Definições", "Settings", "Configuración", "Настройки"],
        ["A verificar Steam..."] = ["A verificar Steam...", "Checking Steam...", "Comprobando Steam...", "Проверка Steam..."],
        ["A monitorização automática funciona enquanto a aplicação estiver aberta."] = ["A monitorização automática funciona enquanto a aplicação estiver aberta.", "Automatic monitoring works while the app is open.", "La supervisión automática funciona mientras la aplicación está abierta.", "Автоматический мониторинг работает, пока приложение открыто."],
        ["Abrir Ludusavi"] = ["Abrir Ludusavi", "Open Ludusavi", "Abrir Ludusavi", "Открыть Ludusavi"],
        ["Abrir pasta"] = ["Abrir pasta", "Open folder", "Abrir carpeta", "Открыть папку"],
        ["Os teus backups"] = ["Os teus backups", "Your backups", "Tus copias de seguridad", "Ваши резервные копии"],
        ["Guarda e restaura os dados da tua biblioteca Steam e Lua Tools."] = ["Guarda e restaura os dados da tua biblioteca Steam e Lua Tools.", "Back up and restore your Steam library and Lua Tools data.", "Guarda y restaura los datos de tu biblioteca de Steam y Lua Tools.", "Создавайте резервные копии и восстанавливайте данные Steam и Lua Tools."],
        ["Protege o teu progresso"] = ["Protege o teu progresso", "Protect your progress", "Protege tu progreso", "Защитите свой прогресс"],
        ["Inclui userdata, estatísticas, depotcache e st-plug-in."] = ["Inclui userdata, estatísticas, depotcache e st-plug-in.", "Includes userdata, stats, depotcache, and st-plug-in.", "Incluye userdata, estadísticas, depotcache y st-plug-in.", "Включает userdata, статистику, depotcache и st-plug-in."],
        ["Criar backup"] = ["Criar backup", "Create backup", "Crear copia de seguridad", "Создать резервную копию"],
        ["Histórico de backups"] = ["Histórico de backups", "Backup history", "Historial de copias", "История резервных копий"],
        ["Atualizar"] = ["Atualizar", "Refresh", "Actualizar", "Обновить"],
        ["Renomear"] = ["Renomear", "Rename", "Renombrar", "Переименовать"],
        ["Restaurar"] = ["Restaurar", "Restore", "Restaurar", "Восстановить"],
        ["Apagar"] = ["Apagar", "Delete", "Eliminar", "Удалить"],
        ["Fixar"] = ["Fixar", "Pin", "Fijar", "Закрепить"],
        ["Desafixar"] = ["Desafixar", "Unpin", "Desfijar", "Открепить"],
        ["Ainda não há backups"] = ["Ainda não há backups", "No backups yet", "Aún no hay copias", "Резервных копий пока нет"],
        ["Cria o primeiro backup ou inicia um jogo com a monitorização ativa."] = ["Cria o primeiro backup ou inicia um jogo com a monitorização ativa.", "Create your first backup or start a game while monitoring is active.", "Crea tu primera copia o inicia un juego con la supervisión activa.", "Создайте первую копию или запустите игру при включенном мониторинге."],
        ["Configura o comportamento e o local de armazenamento."] = ["Configura o comportamento e o local de armazenamento.", "Configure behavior and storage location.", "Configura el comportamiento y la ubicación de almacenamiento.", "Настройте поведение и место хранения."],
        ["Automação"] = ["Automação", "Automation", "Automatización", "Автоматизация"],
        ["Criar backup automaticamente quando um jogo iniciar"] = ["Criar backup automaticamente quando um jogo iniciar", "Automatically back up when a game starts", "Crear una copia automáticamente al iniciar un juego", "Создавать копию при запуске игры"],
        ["Criar backup automaticamente quando um jogo fechar"] = ["Criar backup automaticamente quando um jogo fechar", "Automatically back up when a game closes", "Crear una copia automáticamente al cerrar un juego", "Создавать копию при закрытии игры"],
        ["A monitorização usa o estado de execução da Steam e só funciona enquanto esta aplicação estiver aberta."] = ["A monitorização usa o estado de execução da Steam e só funciona enquanto esta aplicação estiver aberta.", "Monitoring uses Steam's running state and only works while this app is open.", "La supervisión utiliza el estado de Steam y solo funciona mientras esta aplicación está abierta.", "Мониторинг проверяет состояние Steam и работает, только пока приложение открыто."],
        ["Arranque do Windows"] = ["Arranque do Windows", "Windows startup", "Inicio de Windows", "Запуск Windows"],
        ["Executar com o Windows"] = ["Executar com o Windows", "Run at Windows startup", "Iniciar con Windows", "Запускать вместе с Windows"],
        ["Iniciar minimizado na bandeja do sistema"] = ["Iniciar minimizado na bandeja do sistema", "Start minimized to the system tray", "Iniciar minimizado en la bandeja del sistema", "Запускать свернутым в системный трей"],
        ["Idioma"] = ["Idioma", "Language", "Idioma", "Язык"],
        ["Idioma da aplicação"] = ["Idioma da aplicação", "Application language", "Idioma de la aplicación", "Язык приложения"],
        ["Aparência"] = ["Aparência", "Appearance", "Apariencia", "Внешний вид"],
        ["Escolhe uma cor na grelha ou escreve o seu código hexadecimal."] = ["Escolhe uma cor na grelha ou escreve o seu código hexadecimal.", "Choose a color from the grid or enter its hexadecimal code.", "Elige un color de la cuadrícula o escribe su código hexadecimal.", "Выберите цвет в палитре или введите его шестнадцатеричный код."],
        ["Clica para abrir a grelha de cores"] = ["Clica para abrir a grelha de cores", "Click to open the color grid", "Haz clic para abrir la paleta de colores", "Нажмите, чтобы открыть палитру цветов"],
        ["Armazenamento"] = ["Armazenamento", "Storage", "Almacenamiento", "Хранилище"],
        ["Pasta dos backups (deixa vazio para usar a pasta backups junto da aplicação)"] = ["Pasta dos backups (deixa vazio para usar a pasta backups junto da aplicação)", "Backup folder (leave blank to use the app's backups folder)", "Carpeta de copias (déjala vacía para usar la carpeta de la aplicación)", "Папка резервных копий (оставьте пустой, чтобы использовать папку приложения)"],
        ["Caminho completo da pasta onde os backups novos serão guardados"] = ["Caminho completo da pasta onde os backups novos serão guardados", "Full path to the folder where new backups will be stored", "Ruta completa de la carpeta donde se guardarán las copias nuevas", "Полный путь к папке для новых резервных копий"],
        ["Escolher..."] = ["Escolher...", "Browse...", "Elegir...", "Обзор..."],
        ["Padrão"] = ["Padrão", "Default", "Predeterminada", "По умолчанию"],
        ["O caminho será usado a partir do próximo backup."] = ["O caminho será usado a partir do próximo backup.", "This location will be used starting with the next backup.", "Esta ubicación se usará a partir de la próxima copia.", "Это расположение будет использоваться со следующей копии."],
        ["Histórico"] = ["Histórico", "History", "Historial", "История"],
        ["Limite de backups automáticos e manuais não fixados"] = ["Limite de backups automáticos e manuais não fixados", "Limit for unpinned automatic and manual backups", "Límite de copias automáticas y manuales sin fijar", "Лимит незакрепленных автоматических и ручных копий"],
        ["Backups fixados são preservados mesmo quando o limite é atingido."] = ["Backups fixados são preservados mesmo quando o limite é atingido.", "Pinned backups are kept even when the limit is reached.", "Las copias fijadas se conservan aunque se alcance el límite.", "Закрепленные копии сохраняются даже при достижении лимита."],
        ["Guardar definições"] = ["Guardar definições", "Save settings", "Guardar configuración", "Сохранить настройки"],
        ["A monitorizar Steam"] = ["A monitorizar Steam", "Monitoring Steam", "Supervisando Steam", "Мониторинг Steam"],
        ["Jogo em execução"] = ["Jogo em execução", "Game running", "Juego en ejecución", "Игра запущена"],
        ["Steam não detetada"] = ["Steam não detetada", "Steam not detected", "Steam no detectado", "Steam не обнаружена"],
        ["Monitorização indisponível"] = ["Monitorização indisponível", "Monitoring unavailable", "Supervisión no disponible", "Мониторинг недоступен"],
        ["A criar backup..."] = ["A criar backup...", "Creating backup...", "Creando copia...", "Создание резервной копии..."],
        ["Backup criado com sucesso."] = ["Backup criado com sucesso.", "Backup created successfully.", "Copia creada correctamente.", "Резервная копия успешно создана."],
        ["A guardar definições..."] = ["A guardar definições...", "Saving settings...", "Guardando configuración...", "Сохранение настроек..."],
        ["Definições guardadas."] = ["Definições guardadas.", "Settings saved.", "Configuración guardada.", "Настройки сохранены."],
        ["As definições foram guardadas."] = ["As definições foram guardadas.", "Settings have been saved.", "La configuración se ha guardado.", "Параметры успешно сохранены."],
        ["A atualizar backup..."] = ["A atualizar backup...", "Updating backup...", "Actualizando copia...", "Обновление копии..."],
        ["A atualizar nome..."] = ["A atualizar nome...", "Updating name...", "Actualizando nombre...", "Обновление названия..."],
        ["A restaurar backup. A Steam poderá ser reiniciada..."] = ["A restaurar backup. A Steam poderá ser reiniciada...", "Restoring backup. Steam may restart...", "Restaurando copia. Steam podría reiniciarse...", "Восстановление копии. Steam может перезапуститься..."],
        ["Backup restaurado. A Steam foi iniciada."] = ["Backup restaurado. A Steam foi iniciada.", "Backup restored. Steam has been started.", "Copia restaurada. Steam se ha iniciado.", "Копия восстановлена. Steam запущена."],
        ["O backup foi restaurado com sucesso."] = ["O backup foi restaurado com sucesso.", "Backup restored successfully.", "La copia se restauró correctamente.", "Резервная копия успешно восстановлена."],
        ["Restauro concluído"] = ["Restauro concluído", "Restore complete", "Restauración completada", "Восстановление завершено"],
        ["A apagar backup..."] = ["A apagar backup...", "Deleting backup...", "Eliminando copia...", "Удаление резервной копии..."],
        ["Backup apagado."] = ["Backup apagado.", "Backup deleted.", "Copia eliminada.", "Резервная копия удалена."],
        ["Jogo iniciado. A criar backup automático..."] = ["Jogo iniciado. A criar backup automático...", "Game started. Creating automatic backup...", "Juego iniciado. Creando copia automática...", "Игра запущена. Создание резервной копии..."],
        ["Backup automático criado ao iniciar o jogo."] = ["Backup automático criado ao iniciar o jogo.", "Automatic backup created when the game started.", "Copia automática creada al iniciar el juego.", "Создана копия при запуске игры."],
        ["Jogo fechado. A criar backup automático..."] = ["Jogo fechado. A criar backup automático...", "Game closed. Creating automatic backup...", "Juego cerrado. Creando copia automática...", "Игра закрыта. Создание резервной копии..."],
        ["Backup automático criado após o fecho do jogo."] = ["Backup automático criado após o fecho do jogo.", "Automatic backup created after the game closed.", "Copia automática creada al cerrar el juego.", "Создана копия после закрытия игры."],
        ["Local: {0}"] = ["Local: {0}", "Location: {0}", "Ubicación: {0}", "Расположение: {0}"],
        ["Local ativo: {0}"] = ["Local ativo: {0}", "Current location: {0}", "Ubicación actual: {0}", "Текущее расположение: {0}"],
        ["Os backups novos serão guardados em: {0}"] = ["Os backups novos serão guardados em: {0}", "New backups will be saved to: {0}", "Las copias nuevas se guardarán en: {0}", "Новые копии будут сохранены в: {0}"],
        ["O limite de backups precisa ser um número igual ou superior a 1."] = ["O limite de backups precisa ser um número igual ou superior a 1.", "The backup limit must be a number greater than or equal to 1.", "El límite debe ser un número igual o mayor que 1.", "Лимит должен быть числом не меньше 1."],
        ["Valor inválido"] = ["Valor inválido", "Invalid value", "Valor no válido", "Недопустимое значение"],
        ["Indica uma cor hexadecimal válida no formato #RRGGBB."] = ["Indica uma cor hexadecimal válida no formato #RRGGBB.", "Enter a valid hexadecimal color in #RRGGBB format.", "Introduce un color hexadecimal válido con el formato #RRGGBB.", "Введите допустимый шестнадцатеричный цвет в формате #RRGGBB."],
        ["Cor inválida"] = ["Cor inválida", "Invalid color", "Color no válido", "Недопустимый цвет"],
        ["O caminho indicado não é válido.\n\n{0}"] = ["O caminho indicado não é válido.\n\n{0}", "The specified path is invalid.\n\n{0}", "La ruta indicada no es válida.\n\n{0}", "Указан недопустимый путь.\n\n{0}"],
        ["Caminho inválido"] = ["Caminho inválido", "Invalid path", "Ruta no válida", "Недопустимый путь"],
        ["Não foi possível verificar os backups existentes no local atual."] = ["Não foi possível verificar os backups existentes no local atual.", "Could not check for existing backups at the current location.", "No se pudieron comprobar las copias existentes en la ubicación actual.", "Не удалось проверить существующие копии в текущем расположении."],
        ["O novo caminho será usado para backups futuros. Queres também mover para lá os backups existentes?\n\nSim: move os backups e passa a mostrá-los no novo caminho.\nNão: mantém os backups antigos no local atual."] = ["O novo caminho será usado para backups futuros. Queres também mover para lá os backups existentes?\n\nSim: move os backups e passa a mostrá-los no novo caminho.\nNão: mantém os backups antigos no local atual.", "The new location will be used for future backups. Would you also like to move existing backups there?\n\nYes: move the backups and show them at the new location.\nNo: keep existing backups at the current location.", "La nueva ubicación se usará para las próximas copias. ¿También quieres mover allí las copias existentes?\n\nSí: mueve las copias y las muestra en la nueva ubicación.\nNo: conserva las copias existentes en la ubicación actual.", "Новое расположение будет использоваться для будущих копий. Переместить туда существующие копии?\n\nДа: переместить копии и показывать их в новом расположении.\nНет: оставить существующие копии на прежнем месте."],
        ["Alterar local dos backups"] = ["Alterar local dos backups", "Change backup location", "Cambiar ubicación de las copias", "Изменить место хранения копий"],
        ["A Steam será fechada para restaurar este backup:\n\n{0}\n\nOs ficheiros existentes serão atualizados pelos ficheiros do backup. Continuar?"] = ["A Steam será fechada para restaurar este backup:\n\n{0}\n\nOs ficheiros existentes serão atualizados pelos ficheiros do backup. Continuar?", "Steam will be closed to restore this backup:\n\n{0}\n\nExisting files will be overwritten by files from the backup. Continue?", "Steam se cerrará para restaurar esta copia:\n\n{0}\n\nLos archivos existentes se reemplazarán con los de la copia. ¿Continuar?", "Steam будет закрыта для восстановления этой копии:\n\n{0}\n\nСуществующие файлы будут заменены файлами из копии. Продолжить?"],
        ["Confirmar restauro"] = ["Confirmar restauro", "Confirm restore", "Confirmar restauración", "Подтвердить восстановление"],
        ["Queres apagar permanentemente este backup?\n\n{0}"] = ["Queres apagar permanentemente este backup?\n\n{0}", "Permanently delete this backup?\n\n{0}", "¿Eliminar esta copia permanentemente?\n\n{0}", "Удалить эту резервную копию без возможности восстановления?\n\n{0}"],
        ["Apagar backup"] = ["Apagar backup", "Delete backup", "Eliminar copia", "Удалить копию"],
        ["Não foi possível carregar os backups."] = ["Não foi possível carregar os backups.", "Could not load backups.", "No se pudieron cargar las copias.", "Не удалось загрузить резервные копии."],
        ["A operação não foi concluída."] = ["A operação não foi concluída.", "The operation could not be completed.", "No se pudo completar la operación.", "Не удалось выполнить операцию."],
        ["Não foi possível abrir a pasta de backups."] = ["Não foi possível abrir a pasta de backups.", "Could not open the backup folder.", "No se pudo abrir la carpeta de copias.", "Не удалось открыть папку резервных копий."],
        ["Não foi possível abrir o Ludusavi."] = ["Não foi possível abrir o Ludusavi.", "Could not open Ludusavi.", "No se pudo abrir Ludusavi.", "Не удалось открыть Ludusavi."],
        ["Não foi possível atualizar o arranque automático do Windows."] = ["Não foi possível atualizar o arranque automático do Windows.", "Could not update Windows startup settings.", "No se pudo actualizar el inicio automático de Windows.", "Не удалось обновить настройки автозапуска Windows."],
        ["A aplicação continua em execução na bandeja do sistema."] = ["A aplicação continua em execução na bandeja do sistema.", "The app is still running in the system tray.", "La aplicación sigue ejecutándose en la bandeja del sistema.", "Приложение продолжает работать в системном трее."],
        ["Abrir Steam Toolkit MRM"] = ["Abrir Steam Toolkit MRM", "Open Steam Toolkit MRM", "Abrir Steam Toolkit MRM", "Открыть Steam Toolkit MRM"],
        ["Sair"] = ["Sair", "Exit", "Salir", "Выход"],
        ["Localização dos backups"] = ["Localização dos backups", "Backup location", "Ubicación de las copias", "Расположение копий"],
        ["Escolher pasta para backups"] = ["Escolher pasta para backups", "Choose backup folder", "Elegir carpeta de copias", "Выберите папку для копий"],
        ["Escolher cor"] = ["Escolher cor", "Choose color", "Elegir color", "Выбрать цвет"],
        ["Escolhe uma cor"] = ["Escolhe uma cor", "Choose a color", "Elige un color", "Выберите цвет"],
        ["Cancelar"] = ["Cancelar", "Cancel", "Cancelar", "Отмена"],
        ["Escolher"] = ["Escolher", "Choose", "Elegir", "Выбрать"],
        ["Renomear backup"] = ["Renomear backup", "Rename backup", "Renombrar copia", "Переименовать копию"],
        ["Nome do backup"] = ["Nome do backup", "Backup name", "Nombre de la copia", "Название копии"],
        ["Guardar"] = ["Guardar", "Save", "Guardar", "Сохранить"],
        ["O nome não pode ficar vazio."] = ["O nome não pode ficar vazio.", "The name cannot be empty.", "El nombre no puede estar vacío.", "Название не может быть пустым."],
        ["Nome inválido"] = ["Nome inválido", "Invalid name", "Nombre no válido", "Недопустимое название"],
        ["Não foi possível detetar a instalação da Steam."] = ["Não foi possível detetar a instalação da Steam.", "Could not detect the Steam installation.", "No se pudo detectar la instalación de Steam.", "Не удалось обнаружить установку Steam."],
        ["Não foi possível reiniciar a Steam após a restauração."] = ["Não foi possível reiniciar a Steam após a restauração.", "Could not restart Steam after restoring.", "No se pudo reiniciar Steam después de la restauración.", "Не удалось перезапустить Steam после восстановления."],
        ["A pasta da Steam não foi encontrada: {0}"] = ["A pasta da Steam não foi encontrada: {0}", "The Steam folder was not found: {0}", "No se encontró la carpeta de Steam: {0}", "Папка Steam не найдена: {0}"],
        ["Não foram encontradas pastas Steam para incluir no backup."] = ["Não foram encontradas pastas Steam para incluir no backup.", "No Steam folders were found to include in the backup.", "No se encontraron carpetas de Steam para incluir en la copia.", "Не найдены папки Steam для включения в резервную копию."],
        ["O backup selecionado já não existe."] = ["O backup selecionado já não existe.", "The selected backup no longer exists.", "La copia seleccionada ya no existe.", "Выбранная резервная копия больше не существует."],
        ["Este backup não contém nenhuma das pastas Steam esperadas."] = ["Este backup não contém nenhuma das pastas Steam esperadas.", "This backup does not contain any of the expected Steam folders.", "Esta copia no contiene ninguna de las carpetas de Steam esperadas.", "Эта копия не содержит ожидаемых папок Steam."],
        ["Este backup está fixado e não pode ser apagado."] = ["Este backup está fixado e não pode ser apagado.", "This backup is pinned and cannot be deleted.", "Esta copia está fijada y no se puede eliminar.", "Эта копия закреплена и не может быть удалена."],
        ["O limite precisa ser pelo menos 1."] = ["O limite precisa ser pelo menos 1.", "The limit must be at least 1.", "El límite debe ser al menos 1.", "Лимит должен быть не меньше 1."],
        ["A pasta dos backups não pode ficar dentro de um backup existente."] = ["A pasta dos backups não pode ficar dentro de um backup existente.", "The backup folder cannot be inside an existing backup.", "La carpeta de copias no puede estar dentro de una copia existente.", "Папка резервных копий не может находиться внутри существующей копии."],
        ["Ludusavi não foi encontrado junto dos dados da aplicação."] = ["Ludusavi não foi encontrado junto dos dados da aplicação.", "Ludusavi was not found alongside the application data.", "No se encontró Ludusavi junto a los datos de la aplicación.", "Ludusavi не найден рядом с данными приложения."],
        ["Não foi possível consultar o estado do jogo na Steam."] = ["Não foi possível consultar o estado do jogo na Steam.", "Could not check the game's status in Steam.", "No se pudo consultar el estado del juego en Steam.", "Не удалось проверить состояние игры в Steam."],
        ["O ficheiro de configurações está vazio ou inválido."] = ["O ficheiro de configurações está vazio ou inválido.", "The settings file is empty or invalid.", "El archivo de configuración está vacío o no es válido.", "Файл настроек пуст или недействителен."],
        ["Nome de backup inválido."] = ["Nome de backup inválido.", "Invalid backup name.", "Nombre de copia no válido.", "Недопустимое имя резервной копии."],
        ["Não foi possível ler as configurações em {0}."] = ["Não foi possível ler as configurações em {0}.", "Could not read settings at {0}.", "No se pudo leer la configuración en {0}.", "Не удалось прочитать настройки в {0}."],
        ["Metadados inválidos no backup {0}."] = ["Metadados inválidos no backup {0}.", "Invalid metadata in backup {0}.", "Metadatos no válidos en la copia {0}.", "Недопустимые метаданные в копии {0}."],
        ["A nova pasta não pode ficar dentro de um backup existente."] = ["A nova pasta não pode ficar dentro de um backup existente.", "The new folder cannot be inside an existing backup.", "La nueva carpeta no puede estar dentro de una copia existente.", "Новая папка не может находиться внутри существующей копии."],
        ["Já existe um backup com o mesmo nome no destino: {0}"] = ["Já existe um backup com o mesmo nome no destino: {0}", "A backup with the same name already exists at the destination: {0}", "Ya existe una copia con el mismo nombre en el destino: {0}", "В месте назначения уже есть копия с таким именем: {0}"],
        ["Steam não encontrada. Confirma a instalação e a chave SteamPath no registo."] = ["Steam não encontrada. Confirma a instalação e a chave SteamPath no registo.", "Steam was not found. Check the installation and the SteamPath registry key.", "No se encontró Steam. Comprueba la instalación y la clave SteamPath del registro.", "Steam не найдена. Проверьте установку и параметр SteamPath в реестре."],
        ["Não foi possível aceder às opções de arranque do utilizador atual."] = ["Não foi possível aceder às opções de arranque do utilizador atual.", "Could not access startup settings for the current user.", "No se pudo acceder a las opciones de inicio del usuario actual.", "Не удалось получить доступ к параметрам автозапуска текущего пользователя."],
        ["Não foi possível detetar o caminho do executável."] = ["Não foi possível detetar o caminho do executável.", "Could not detect the executable path.", "No se pudo detectar la ruta del ejecutable.", "Не удалось определить путь к исполняемому файлу."],
    };

    public static string Language { get; private set; } = "pt";

    public static string NormalizeLanguage(string? language)
    {
        return language?.ToLowerInvariant() switch
        {
            "en" or "es" or "ru" => language.ToLowerInvariant(),
            _ => "pt"
        };
    }

    public static void SetLanguage(string? language) => Language = NormalizeLanguage(language);

    public static int GetLanguageIndex(string? language) => LanguageIndex(NormalizeLanguage(language));

    public static string GetLanguageCode(int index) => index switch
    {
        1 => "en",
        2 => "es",
        3 => "ru",
        _ => "pt"
    };

    public static string Get(string text)
    {
        if (!Translations.TryGetValue(text, out string[]? values))
        {
            foreach ((string source, string[] translations) in Translations)
            {
                if (translations.Contains(text, StringComparer.Ordinal))
                {
                    values = translations;
                    break;
                }
            }
        }

        return values is null ? text : values[LanguageIndex(Language)];
    }

    public static string Format(string template, params object?[] values)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(template), values);
    }

    public static CultureInfo Culture => Language switch
    {
        "en" => CultureInfo.GetCultureInfo("en-US"),
        "es" => CultureInfo.GetCultureInfo("es-ES"),
        "ru" => CultureInfo.GetCultureInfo("ru-RU"),
        _ => CultureInfo.GetCultureInfo("pt-PT")
    };

    public static void Apply(DependencyObject root)
    {
        var visited = new HashSet<DependencyObject>();
        TranslateTree(root, visited);
    }

    private static void TranslateTree(DependencyObject node, HashSet<DependencyObject> visited)
    {
        if (!visited.Add(node))
            return;
        if (node is ComboBox { Name: "LanguageComboBox" })
            return;

        if (node is TextBlock textBlock && !BindingOperations.IsDataBound(textBlock, TextBlock.TextProperty))
            textBlock.Text = Get(textBlock.Text);

        if (node is ContentControl contentControl &&
            !BindingOperations.IsDataBound(contentControl, ContentControl.ContentProperty) &&
            contentControl.Content is string content)
            contentControl.Content = Get(content);

        if (node is FrameworkElement element && element.ToolTip is string toolTip)
            element.ToolTip = Get(toolTip);

        if (node is Window window)
            window.Title = Get(window.Title);

        foreach (object child in LogicalTreeHelper.GetChildren(node))
            if (child is DependencyObject dependencyObject)
                TranslateTree(dependencyObject, visited);

        if (node is Visual or System.Windows.Media.Media3D.Visual3D)
        {
            int count = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < count; i++)
                TranslateTree(VisualTreeHelper.GetChild(node, i), visited);
        }
    }

    private static int LanguageIndex(string language) => language switch
    {
        "en" => 1,
        "es" => 2,
        "ru" => 3,
        _ => 0
    };
}
