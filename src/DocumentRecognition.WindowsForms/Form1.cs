using DocumentRecognition.Application.Services;
using DocumentRecognition.Core.Models.Options;
using DocumentRecognition.Core.Models.Results;
using DocumentRecognition.Infrastructure.Extensions;
using DocumentRecognition.OnnxRecognizer;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace DocumentRecognition.WindowsForms
{
    public partial class Form1 : Form
    {
        private const string TechnicalRequirementsKeyPrefix = "technical_requirements:";
        private static readonly Color HoverRowColor = Color.FromArgb(255, 249, 196);

        private readonly DocumentRecognitionService _recognitionService;
        private readonly IRuntimeOcrSettings _runtimeSettings;
        private readonly Dictionary<string, DataGridViewRow> _titleBlockRowsByKey = new();
        private readonly Dictionary<string, DataGridViewRow> _signatureRowsByKey = new();

        /// <summary>
        /// Настройки распознавания, задаваемые на вкладке «Настройки».
        /// </summary>
        private readonly DrawingRecognitionOptions _recognitionOptions = new();

        /// <summary>
        /// Защита от рекурсии при синхронизации выбора между изображением и таблицами.
        /// </summary>
        private bool _syncingSelection;

        public Form1(DocumentRecognitionService recognitionService, IRuntimeOcrSettings runtimeSettings)
        {
            InitializeComponent();

            _recognitionService = recognitionService;
            _runtimeSettings = runtimeSettings;

            PopulateEngineSettings();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Filter = "PDF (*.pdf)|*.pdf|Все файлы (*.*)|*.*",
                Title = "Выберите документ"
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return;

            button1.Enabled = false;

            try
            {
                using var fs = new FileStream(openFileDialog.FileName, FileMode.Open, FileAccess.Read);

                var result = await _recognitionService.ProcessAsync(fs, CreateRecognitionOptions());

                ShowRecognitionImage(result);
                ShowTitleBlock(result.TitleBlock);
                ShowTechnicalRequirements(result.TechnicalRequirements);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка распознавания", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                button1.Enabled = true;
            }
        }

        private void ShowRecognitionImage(DrawingRecognitionResult result)
        {
            // Контрол сам декодирует изображение, владеет им и отрисовывает области результата.
            imageViewer1.LoadImage(result.DebugImage, result.Regions, result.Dpi);
        }

        /// <summary>
        /// Собирает настройки распознавания из элементов вкладки «Настройки».
        /// </summary>
        private DrawingRecognitionOptions CreateRecognitionOptions()
        {
            _recognitionOptions.Dpi = (int)numDpi.Value;
            _recognitionOptions.CleanImage = chkCleanImage.Checked;
            _recognitionOptions.Normalize();

            return _recognitionOptions;
        }

        private void btnResetSettings_Click(object? sender, EventArgs e)
        {
            numDpi.Value = DrawingRecognitionOptions.DefaultDpi;
            chkCleanImage.Checked = true;

            ApplyEngineSettings(new OcrSettings());
        }

        private static readonly InferenceExecutionProvider[] ProviderOptions =
        {
            InferenceExecutionProvider.Auto,
            InferenceExecutionProvider.Cpu,
            InferenceExecutionProvider.DirectMl
        };

        private void PopulateEngineSettings()
        {
            var settings = GetCurrentSettingsOrFallback(out var settingsError);

            cmbProvider.BeginUpdate();
            cmbProvider.Items.Clear();

            foreach (var provider in ProviderOptions)
                cmbProvider.Items.Add(GetProviderTitle(provider));

            cmbProvider.EndUpdate();

            var index = Array.IndexOf(ProviderOptions, settings.Provider);
            cmbProvider.SelectedIndex = index >= 0 ? index : 0;

            UpdateModelsPathLabel(settings);

            if (settingsError != null)
                SetSettingsStatus(settingsError, isError: true);
        }

        /// <summary>
        /// Возвращает текущие настройки или безопасный дефолт, если конфигурация
        /// некорректна (например, пропала папка с моделями).
        /// </summary>
        private OcrSettings GetCurrentSettingsOrFallback(out string? error)
        {
            try
            {
                error = null;
                return _runtimeSettings.Current;
            }
            catch (OptionsValidationException ex)
            {
                error = "Настройки моделей некорректны: " + ex.Message;
                return new OcrSettings();
            }
        }

        private void UpdateModelsPathLabel(OcrSettings settings)
        {
            lblModelsPath.Text = $"Модели: {settings.ResolveModelsDirectory()}";
        }

        private void SetSettingsStatus(string? text, bool isError = false)
        {
            lblEngineSettingsStatus.Text = text ?? string.Empty;
            lblEngineSettingsStatus.ForeColor = isError ? Color.Firebrick : SystemColors.GrayText;
        }

        private static string GetProviderTitle(InferenceExecutionProvider provider)
        {
            switch (provider)
            {
                case InferenceExecutionProvider.Cpu:
                    return "CPU";
                case InferenceExecutionProvider.DirectMl:
                    return "DirectML (GPU)";
                default:
                    return "Auto (DirectML → CPU)";
            }
        }

        private void btnChangeModelsDir_Click(object? sender, EventArgs e)
        {
            var current = GetCurrentSettingsOrFallback(out _);

            using var dialog = new FolderBrowserDialog
            {
                Description = "Выберите каталог с ONNX-моделями",
                UseDescriptionForTitle = true
            };

            var modelsDirectory = current.ResolveModelsDirectory();

            if (Directory.Exists(modelsDirectory))
                dialog.SelectedPath = modelsDirectory;

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var settings = current.Copy();

            // Абсолютный путь: не зависит от расположения приложения.
            settings.ModelsDir = dialog.SelectedPath;

            ApplyEngineSettings(settings);
        }

        private void btnResetModelsDir_Click(object? sender, EventArgs e)
        {
            var settings = GetCurrentSettingsOrFallback(out _).Copy();

            // Дефолт — папка OnnxModels рядом с приложением.
            settings.ModelsDir = new OcrSettings().ModelsDir;

            ApplyEngineSettings(settings);
        }

        private void btnApplyEngineSettings_Click(object? sender, EventArgs e)
        {
            var settings = GetCurrentSettingsOrFallback(out _).Copy();
            settings.Provider = cmbProvider.SelectedIndex >= 0
                ? ProviderOptions[cmbProvider.SelectedIndex]
                : InferenceExecutionProvider.Auto;

            ApplyEngineSettings(settings);
        }

        /// <summary>
        /// Проверяет наличие моделей и, если всё на месте, применяет настройки
        /// и сохраняет их в пользовательский файл.
        /// </summary>
        private void ApplyEngineSettings(OcrSettings settings)
        {
            var errors = settings.Validate();

            if (errors.Count > 0)
            {
                MessageBox.Show(
                    this,
                    string.Join(Environment.NewLine, errors),
                    "Модели не найдены",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                SetSettingsStatus($"Не применено: проблем с моделями — {errors.Count}.", isError: true);
                return;
            }

            try
            {
                _runtimeSettings.Apply(settings);
                SaveEngineSettings(settings);
                PopulateEngineSettings();

                SetSettingsStatus($"{DateTime.Now:HH:mm:ss} — применено; изменённые модели перезагрузятся при следующем распознавании.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Ошибка применения настроек", MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetSettingsStatus("Не удалось применить настройки.", isError: true);
            }
        }

        /// <summary>
        /// Сохраняет настройки движков в пользовательский файл, чтобы они применялись
        /// и после перезапуска приложения.
        /// </summary>
        private static void SaveEngineSettings(OcrSettings settings)
        {
            try
            {
                var directory = Path.GetDirectoryName(Program.SettingsFilePath);

                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                var payload = new Dictionary<string, object?>
                {
                    [OcrSettings.SectionName] = new Dictionary<string, object?>
                    {
                        [nameof(OcrSettings.ModelsDir)] = settings.ModelsDir,
                        [nameof(OcrSettings.DetModel)] = settings.DetModel,
                        [nameof(OcrSettings.ClsModel)] = settings.ClsModel,
                        [nameof(OcrSettings.RecModel)] = settings.RecModel,
                        [nameof(OcrSettings.DictFile)] = settings.DictFile,
                        [nameof(OcrSettings.DrawingLayoutDetModel)] = settings.DrawingLayoutDetModel,
                        [nameof(OcrSettings.DocOrientationClsModel)] = settings.DocOrientationClsModel,
                        [nameof(OcrSettings.WiredTableCellDetModel)] = settings.WiredTableCellDetModel,
                        [nameof(OcrSettings.Provider)] = settings.Provider.ToString(),
                        [nameof(OcrSettings.Threads)] = settings.Threads
                    }
                };

                File.WriteAllText(
                    Program.SettingsFilePath,
                    JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Не удалось сохранить настройки: {ex.Message}");
            }
        }

        private void imageViewer1_HoveredRegionChanged(object? sender, RecognitionRegionEventArgs e)
        {
            HighlightRegionTarget(e.Region);
        }

        private void imageViewer1_RegionSelected(object? sender, RecognitionRegionEventArgs e)
        {
            if (_syncingSelection)
                return;

            _syncingSelection = true;

            try
            {
                SelectRegionTarget(e.Region);
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        private void dgvTitleBlock_SelectionChanged(object? sender, EventArgs e)
        {
            TrySelectRegionFromGrid(dgvTitleBlock);
        }

        private void dgvSignatures_SelectionChanged(object? sender, EventArgs e)
        {
            TrySelectRegionFromGrid(dgvSignatures);
        }

        private void TrySelectRegionFromGrid(DataGridView grid)
        {
            if (_syncingSelection || grid.SelectedRows.Count == 0)
                return;

            if (grid.SelectedRows[0].Tag is not string key || string.IsNullOrEmpty(key))
                return;

            _syncingSelection = true;

            try
            {
                imageViewer1.SelectRegionByKey(key);
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        /// <summary>
        /// Подсвечивает строку таблицы, соответствующую области под курсором.
        /// </summary>
        private void HighlightRegionTarget(RecognitionRegion? region)
        {
            ClearRowHighlight(dgvTitleBlock);
            ClearRowHighlight(dgvSignatures);

            if (region?.Key is null)
            {
                txtTechnicalRequirements.SelectionLength = 0;
                return;
            }

            if (_titleBlockRowsByKey.TryGetValue(region.Key, out var titleRow))
            {
                titleRow.DefaultCellStyle.BackColor = HoverRowColor;
                return;
            }

            if (_signatureRowsByKey.TryGetValue(region.Key, out var signatureRow))
            {
                signatureRow.DefaultCellStyle.BackColor = HoverRowColor;
                return;
            }

            if (TryGetTechnicalRequirementIndex(region.Key, out var requirementIndex))
            {
                SelectTechnicalRequirement(region, requirementIndex, focus: false);
                return;
            }

            txtTechnicalRequirements.SelectionLength = 0;
        }

        /// <summary>
        /// Выбирает в UI строку/пункт, соответствующий клику по области изображения.
        /// </summary>
        private void SelectRegionTarget(RecognitionRegion? region)
        {
            if (region?.Key is null)
                return;

            if (_titleBlockRowsByKey.TryGetValue(region.Key, out var titleRow))
            {
                tabControl1.SelectedTab = tabPageTitleBlock;
                SelectRow(dgvTitleBlock, titleRow);
                return;
            }

            if (_signatureRowsByKey.TryGetValue(region.Key, out var signatureRow))
            {
                tabControl1.SelectedTab = tabPageTitleBlock;
                SelectRow(dgvSignatures, signatureRow);
                return;
            }

            if (TryGetTechnicalRequirementIndex(region.Key, out var requirementIndex))
            {
                tabControl1.SelectedTab = tabPageRequirements;
                SelectTechnicalRequirement(region, requirementIndex, focus: true);
            }
        }

        private static void ClearRowHighlight(DataGridView grid)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (!row.DefaultCellStyle.BackColor.IsEmpty)
                    row.DefaultCellStyle.BackColor = Color.Empty;
            }
        }

        private static void SelectRow(DataGridView grid, DataGridViewRow row)
        {
            if (row.Index < 0)
                return;

            grid.ClearSelection();
            row.Selected = true;

            if (row.Cells.Count > 0)
                grid.CurrentCell = row.Cells[0];

            // Если вкладка ещё не отображена, прокрутка невыполнима — это не ошибка.
            try
            {
                grid.FirstDisplayedScrollingRowIndex = row.Index;
            }
            catch (InvalidOperationException)
            {
            }
        }

        private static bool TryGetTechnicalRequirementIndex(string key, out int index)
        {
            index = -1;

            if (!key.StartsWith(TechnicalRequirementsKeyPrefix, StringComparison.Ordinal))
                return false;

            return int.TryParse(key.Substring(TechnicalRequirementsKeyPrefix.Length), out index);
        }

        private void SelectTechnicalRequirement(RecognitionRegion region, int index, bool focus)
        {
            var text = txtTechnicalRequirements.Text;
            var start = -1;
            var length = 0;

            if (!string.IsNullOrWhiteSpace(region.Text))
            {
                start = text.IndexOf(region.Text, StringComparison.Ordinal);

                if (start >= 0)
                    length = region.Text.Length;
            }

            if (start < 0)
            {
                // Запасной вариант: по номеру пункта списка.
                start = txtTechnicalRequirements.GetFirstCharIndexFromLine(index);

                if (start < 0)
                    return;

                var line = txtTechnicalRequirements.Lines.ElementAtOrDefault(index) ?? string.Empty;
                length = line.Length;
            }

            txtTechnicalRequirements.SelectionStart = start;
            txtTechnicalRequirements.SelectionLength = length;
            txtTechnicalRequirements.ScrollToCaret();

            if (focus)
                txtTechnicalRequirements.Focus();
        }

        private void ShowTitleBlock(TitleBlock? titleBlock)
        {
            dgvTitleBlock.Rows.Clear();
            dgvSignatures.Rows.Clear();
            _titleBlockRowsByKey.Clear();
            _signatureRowsByKey.Clear();

            if (titleBlock is null)
                return;

            AddTitleBlockRow("Обозначение", titleBlock.Designation, "designation");
            AddTitleBlockRow("Наименование", titleBlock.Name, "name");
            AddTitleBlockRow("Масса", titleBlock.Mass, "mass");
            AddTitleBlockRow("Масштаб", titleBlock.Scale, "scale");
            AddTitleBlockRow("Материал", titleBlock.Material, "material");
            AddTitleBlockRow("Лист", titleBlock.SheetNumber, "sheet_number");
            AddTitleBlockRow("Листов", titleBlock.TotalSheets, "total_sheets");

            if (titleBlock.Signatures is null)
                return;

            for (var index = 0; index < titleBlock.Signatures.Count; index++)
            {
                var signature = titleBlock.Signatures[index];

                var rowIndex = dgvSignatures.Rows.Add(
                    signature.SignatoryText,
                    signature.Name,
                    signature.Date?.ToShortDateString() ?? string.Empty);

                var row = dgvSignatures.Rows[rowIndex];

                // Ячейки подписи в шаблоне основной надписи: left_r5..left_r10, столбцы 0..3.
                // Наведение на любую из них подсвечивает соответствующую строку таблицы подписей.
                for (var column = 0; column < 4; column++)
                    _signatureRowsByKey[$"left_r{index + 5}_c{column}"] = row;

                row.Tag = $"left_r{index + 5}_c{0}";
            }
        }

        private void AddTitleBlockRow(string fieldName, string? value, string key)
        {
            var rowIndex = dgvTitleBlock.Rows.Add(fieldName, string.IsNullOrWhiteSpace(value) ? "—" : value);
            var row = dgvTitleBlock.Rows[rowIndex];

            row.Tag = key;
            _titleBlockRowsByKey[key] = row;
        }

        private void ShowTechnicalRequirements(string[]? technicalRequirements)
        {
            txtTechnicalRequirements.Text = technicalRequirements is null || technicalRequirements.Length == 0
                ? "Технические требования не найдены."
                : BuildNumberedList(technicalRequirements);

            txtTechnicalRequirements.SelectionStart = 0;
            txtTechnicalRequirements.SelectionLength = 0;
        }

        private static string BuildNumberedList(string[] items)
        {
            var builder = new StringBuilder();

            for (var i = 0; i < items.Length; i++)
            {
                builder.AppendLine($"{i + 1}. {items[i]}");
            }

            return builder.ToString().TrimEnd();
        }
    }
}
