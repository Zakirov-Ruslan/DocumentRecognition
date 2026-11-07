using DocumentRecognition.Application.Services;
using DocumentRecognition.Core.Models.Results;
using System.Text;

namespace DocumentRecognition.WindowsForms
{
    public partial class Form1 : Form
    {
        private readonly DocumentRecognitionService _recognitionService;

        public Form1(DocumentRecognitionService recognitionService)
        {
            InitializeComponent();

            _recognitionService = recognitionService;
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

                var result = await _recognitionService.ProcessAsync(fs);

                if (result is not DrawingRecognitionResult drawingRecResult)
                {
                    MessageBox.Show(this, "Получен неизвестный тип результата распознавания.",
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ShowDebugImage(drawingRecResult.DebugImage);
                ShowTitleBlock(drawingRecResult.TitleBlock);
                ShowTechnicalRequirements(drawingRecResult.TechnicalRequirements);
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

        private void ShowDebugImage(byte[]? debugImage)
        {
            var previousImage = pictureBox1.Image;
            pictureBox1.Image = null;
            previousImage?.Dispose();

            if (debugImage is null || debugImage.Length == 0)
                return;

            using var stream = new MemoryStream(debugImage);
            using var image = Image.FromStream(stream);
            pictureBox1.Image = new Bitmap(image);
        }

        private void ShowTitleBlock(TitleBlock? titleBlock)
        {
            dgvTitleBlock.Rows.Clear();
            dgvSignatures.Rows.Clear();

            if (titleBlock is null)
                return;

            AddTitleBlockRow("Обозначение", titleBlock.Designation);
            AddTitleBlockRow("Наименование", titleBlock.Name);
            AddTitleBlockRow("Масса", titleBlock.Mass);
            AddTitleBlockRow("Масштаб", titleBlock.Scale);
            AddTitleBlockRow("Материал", titleBlock.Material);
            AddTitleBlockRow("Лист", titleBlock.SheetNumber);
            AddTitleBlockRow("Листов", titleBlock.TotalSheets);

            if (titleBlock.Signatures is null)
                return;

            foreach (var signature in titleBlock.Signatures)
            {
                dgvSignatures.Rows.Add(
                    signature.SignatoryText,
                    signature.Name,
                    signature.Date?.ToShortDateString() ?? string.Empty);
            }
        }

        private void AddTitleBlockRow(string fieldName, string? value)
        {
            dgvTitleBlock.Rows.Add(fieldName, string.IsNullOrWhiteSpace(value) ? "—" : value);
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
