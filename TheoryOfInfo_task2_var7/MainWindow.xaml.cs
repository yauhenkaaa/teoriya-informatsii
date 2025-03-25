using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using TheoryOfInfo_task2_var7.Logic; // для LSFR.ValidateRegisterState

namespace TheoryOfInfo_task2_var7
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Сохраняем путь к выбранному файлу
        private string selectedFilePath = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Обработчик изменения текста в tbxRegister – обновляет длину в tbxLength
        private void tbxRegister_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (tbxRegister != null && tbxLength != null)
            {
                tbxLength.Text = $"{tbxRegister.Text.Length}";
            }
        }

        // Обработчик нажатия на кнопку "Открыть файл"
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            if (dlg.ShowDialog() == true)
            {
                selectedFilePath = dlg.FileName;
            }
        }

        // Обработчик нажатия на кнопку "Зашифровать"
        private void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessFile(encrypt: true);
        }

        // Обработчик нажатия на кнопку "Дешифровать"
        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessFile(encrypt: false);
        }

        /// <summary>
        /// Основной метод обработки файла – выполняется шифрование или дешифрование в памяти.
        /// Результат сохраняется туда, куда укажет пользователь через диалог сохранения.
        /// После сохранения в RichTextBox'ах отображаются битовые представления исходного файла, сгенерированного ключа и результата.
        /// Если данных много (файл > 30 байт), выводятся только первые и последние 15 байт.
        /// </summary>
        /// <param name="encrypt">Если true – шифрование, иначе дешифрование.</param>
        private void ProcessFile(bool encrypt)
        {
            // Получаем состояние регистра из текстового поля (например, tbxRegister)
            string registerState = tbxRegister.Text.Trim();
            if (string.IsNullOrEmpty(registerState))
            {
                MessageBox.Show("Введите начальное состояние регистра.");
                return;
            }
            if (!LSFR.ValidateRegisterState(registerState))
            {
                MessageBox.Show("Состояние регистра должно содержать ровно 29 символов, только 0 и 1.");
                return;
            }
            if (string.IsNullOrEmpty(selectedFilePath) || !File.Exists(selectedFilePath))
            {
                MessageBox.Show("Сначала выберите файл для обработки.");
                return;
            }

            try
            {
                // Формируем имя файла по умолчанию для сохранения результата
                string directory = System.IO.Path.GetDirectoryName(selectedFilePath);
                string filenameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(selectedFilePath);
                string extension = System.IO.Path.GetExtension(selectedFilePath);
                string defaultFileName = filenameWithoutExt + (encrypt ? "_encrypted" : "_decrypted") + extension;

                // Диалоговое окно для сохранения файла (пользователь может изменить имя/путь)
                SaveFileDialog saveDlg = new SaveFileDialog
                {
                    Title = encrypt ? "Сохранить зашифрованный файл" : "Сохранить дешифрованный файл",
                    FileName = defaultFileName,
                    InitialDirectory = directory,
                    Filter = "Все файлы (*.*)|*.*"
                };

                if (saveDlg.ShowDialog() == true)
                {
                    // Вызываем LSFR.ProcessFile для обработки файла (шифрование или дешифрование идентичны по логике XOR)
                    LSFR.ProcessFile(selectedFilePath, saveDlg.FileName, registerState);

                    // Считываем исходный файл и обработанный файл для отображения в RichTextBox'ах
                    byte[] originalBytes = File.ReadAllBytes(selectedFilePath);
                    byte[] resultBytes = File.ReadAllBytes(saveDlg.FileName);

                    // Отображаем двоичное представление в RichTextBox'ах (например, rtbOpenText, rtbKeyText, rtbCipherText)
                    rtbOpenText.Document.Blocks.Clear();
                    rtbOpenText.AppendText(GetPartialBitString(new BitArray(originalBytes)));

                    rtbKeyText.Document.Blocks.Clear();
                    // Используем сгенерированный LSFR.KeyStream для вывода ключевого потока
                    rtbKeyText.AppendText(GetPartialBitString(LSFR.KeyStream));

                    rtbCipherText.Document.Blocks.Clear();
                    rtbCipherText.AppendText(GetPartialBitString(new BitArray(resultBytes)));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при обработке файла: " + ex.Message);
            }
        }

        /// <summary>
        /// Преобразует BitArray в строку. Если размер данных превышает 30 байт,
        /// выводятся только первые и последние 15 байт (по 8 бит на байт).
        /// </summary>
        /// <param name="bits">Битовый массив</param>
        /// <returns>Строковое представление битов</returns>
        private string GetPartialBitString(BitArray bits)
        {
            const int bytesToDisplay = 15;
            int totalBytes = bits.Length / 8;
            StringBuilder sb = new StringBuilder();

            if (totalBytes <= bytesToDisplay * 2)
            {
                for (int i = 0; i < bits.Length; i++)
                    sb.Append(bits[i] ? "1" : "0");
            }
            else
            {
                sb.AppendLine("Первые 15 байт:");
                for (int i = 0; i < bytesToDisplay * 8; i++)
                    sb.Append(bits[i] ? "1" : "0");
                sb.AppendLine();
                sb.AppendLine("Последние 15 байт:");
                int start = bits.Length - bytesToDisplay * 8;
                for (int i = start; i < bits.Length; i++)
                    sb.Append(bits[i] ? "1" : "0");
            }
            return sb.ToString();
        }
    }
}
