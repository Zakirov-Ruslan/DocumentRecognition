using DocumentRecognition.Application.Services;
using DocumentRecognition.Core.Models.Results;
using System.Text.Json.Serialization;

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
                Filter = "pdf|*.pdf;*.pdf;*.pdf",
                Title = "Выберите фотографию"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                button1.Enabled = false;

                var currentImagePath = openFileDialog.FileName;

                try
                {
                    using var fs = new FileStream(currentImagePath, FileMode.Open, FileAccess.Read);

                    var result = await _recognitionService.ProcessAsync(fs);

                    if (result is not DrawingRecognitionResult drawingRecResult)
                        throw new Exception("");

                    using (var ms = new MemoryStream(drawingRecResult.DebugImage))
                    {
                        pictureBox1.Image = Image.FromStream(ms);
                    }

                    richTextBox1.Text = $"Обозначение: {drawingRecResult.TitleBlock.Designation}" +
                        $"\nНаименование: {drawingRecResult.TitleBlock.Name}" +
                        $"\nМасса: {drawingRecResult.TitleBlock.Mass}" +
                        $"\nМасштаб: {drawingRecResult.TitleBlock.Scale}" +
                        $"\nМатериал: {drawingRecResult.TitleBlock.Material}" +
                        $"\nЛист: {drawingRecResult.TitleBlock.SheetNumber}" +
                        $"\nЛистов: {drawingRecResult.TitleBlock.TotalSheets}" +
                        $"\n\nПодписи:" +
                        $"\n{string.Join("\n", drawingRecResult.TitleBlock.Signatures.Select(s => $"{s.SignatoryText} | {s.Name} | {s.Date?.ToShortDateString()}"))}" +
                        $"\n\nТехнические требования:\n" + string.Join("\n", drawingRecResult.TechnicalRequirements );
                }
                catch (Exception ex)
                {
                    richTextBox1.Text = ex.Message + $"\n{ex.InnerException}";
                }
                finally
                {
                    button1.Enabled = true;
                }
            }
        }
    }
}
