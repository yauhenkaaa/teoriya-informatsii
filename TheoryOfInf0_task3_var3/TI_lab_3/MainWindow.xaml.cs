using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using TI_lab_3.Logic;

namespace OpenKey.BackEnd
{
    public static class Convert
    {
        public static string BigIntegersToString(BigInteger[] bigIntegers)
        {
            if (bigIntegers == null || bigIntegers.Length == 0) return string.Empty;
            return string.Join(" ", bigIntegers);
        }
    }
}

namespace TI_lab_3
{
    public partial class MainWindow : Window
    {
        private BigInteger[] _currentInputNumbers = null;
        private BigInteger[] _currentOutputNumbers = null;
        private string _originalFileName = string.Empty;
        private string _originalFileExtension = string.Empty;
        private bool _isInputPlaintext = true;

        public MainWindow()
        {
            InitializeComponent();
            tbxParamP.TextChanged += KeyParameter_TextChanged;
            tbxParamQ.TextChanged += KeyParameter_TextChanged;
            tbxParamB.TextChanged += KeyParameter_TextChanged;
            UpdateSaveButtonStates();
        }

        private void SetRichTextBoxText(RichTextBox rtb, string text)
        {
            rtb.Document.Blocks.Clear();
            if (!string.IsNullOrEmpty(text))
            {
                rtb.Document.Blocks.Add(new Paragraph(new Run(text)));
            }
        }

        private bool ValidateInputs(out BigInteger p, out BigInteger q, out BigInteger b, out BigInteger n, bool checkInputData = true)
        {
            p = q = b = n = 0;
            string errorText = "";

            if (!BigInteger.TryParse(tbxParamP.Text.Trim(), out p) || p <= 0)
                errorText += "P must be a positive integer.\n";
            if (!BigInteger.TryParse(tbxParamQ.Text.Trim(), out q) || q <= 0)
                errorText += "Q must be a positive integer.\n";
            if (!BigInteger.TryParse(tbxParamB.Text.Trim(), out b) || b < 0)
                errorText += "B must be a non-negative integer.\n";

            if (!string.IsNullOrEmpty(errorText))
            {
                MessageBox.Show(errorText, "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!Utils.IsValidPrimeForRabin(p))
                errorText += "P is not prime or does not satisfy P % 4 == 3.\n";
            if (!Utils.IsValidPrimeForRabin(q))
                errorText += "Q is not prime or does not satisfy Q % 4 == 3.\n";
            if (p == q && p > 0)
                errorText += "P and Q cannot be the same.\n";


            if (p > 0 && q > 0)
            {
                n = p * q;
                if (b >= n)
                    errorText += $"B must be less than P * Q (B < {n}).\n";
            }
            else
            {
                n = 0;
            }

            if (checkInputData && _currentInputNumbers == null)
            {
                errorText += "No input data loaded or entered for operation.\n";
            }

            if (!string.IsNullOrEmpty(errorText))
            {
                MessageBox.Show(errorText, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        private void btnOpenPlainFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Open Plaintext File",
                Filter = "All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _originalFileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    _originalFileExtension = Path.GetExtension(openFileDialog.FileName);
                    byte[] fileBytes = File.ReadAllBytes(openFileDialog.FileName);

                    _currentInputNumbers = fileBytes.Select(b => new BigInteger(b)).ToArray();
                    _isInputPlaintext = true;

                    SetRichTextBoxText(rtbSource, OpenKey.BackEnd.Convert.BigIntegersToString(_currentInputNumbers));
                    SetRichTextBoxText(rtbResult, string.Empty);
                    _currentOutputNumbers = null;
                    UpdateSaveButtonStates();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening or reading file: {ex.Message}", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _currentInputNumbers = null;
                    SetRichTextBoxText(rtbSource, string.Empty);
                    UpdateSaveButtonStates();
                }
            }
        }

        private void btnOpenEncrFile_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs(out BigInteger p, out BigInteger q, out BigInteger b, out BigInteger n, checkInputData: false))
            {
                return;
            }
            if (n <= 0)
            {
                MessageBox.Show("Cannot open encrypted file without valid P and Q.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Open Encrypted File",
                Filter = "All Files (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _originalFileName = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
                    _originalFileExtension = Path.GetExtension(openFileDialog.FileName);

                    byte[] fileBytes = File.ReadAllBytes(openFileDialog.FileName);

                    BigInteger maxSizeValue = n - 1;
                    int byteSize = maxSizeValue.ToByteArray().Length;
                    if (byteSize == 0) byteSize = 1;


                    if (fileBytes.Length == 0)
                    {
                        _currentInputNumbers = Array.Empty<BigInteger>();
                    }
                    else if (fileBytes.Length % byteSize != 0)
                    {
                        MessageBox.Show($"Encrypted file size ({fileBytes.Length} bytes) is not a multiple of the expected block size ({byteSize} bytes based on P*Q). The file may be corrupt or opened with incorrect P/Q values.",
                                        "File Size Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        _currentInputNumbers = null;
                        SetRichTextBoxText(rtbSource, string.Empty);
                        UpdateSaveButtonStates();
                        return;
                    }
                    else
                    {
                        int numberCount = fileBytes.Length / byteSize;
                        List<BigInteger> ciphertextNumbers = new List<BigInteger>(numberCount);

                        for (int i = 0; i < numberCount; i++)
                        {
                            byte[] numberBytes = new byte[byteSize];
                            Array.Copy(fileBytes, i * byteSize, numberBytes, 0, byteSize);
                            if ((numberBytes[numberBytes.Length - 1] & 0x80) > 0)
                            {
                                byte[] positiveBytes = new byte[numberBytes.Length + 1];
                                Array.Copy(numberBytes, positiveBytes, numberBytes.Length);
                                ciphertextNumbers.Add(new BigInteger(positiveBytes));
                            }
                            else
                            {
                                ciphertextNumbers.Add(new BigInteger(numberBytes));
                            }
                        }
                        _currentInputNumbers = ciphertextNumbers.ToArray();
                    }


                    _isInputPlaintext = false;
                    SetRichTextBoxText(rtbSource, OpenKey.BackEnd.Convert.BigIntegersToString(_currentInputNumbers));
                    SetRichTextBoxText(rtbResult, string.Empty);
                    _currentOutputNumbers = null;
                    UpdateSaveButtonStates();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening or reading encrypted file: {ex.Message}", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _currentInputNumbers = null;
                    SetRichTextBoxText(rtbSource, string.Empty);
                    UpdateSaveButtonStates();
                }
            }
        }

        private void btnSaveEncrFile_Click(object sender, RoutedEventArgs e)
        {
            bool isOutputEncrypted = _currentOutputNumbers != null && (_currentInputNumbers != null && _isInputPlaintext);
            if (!isOutputEncrypted)
            {
                MessageBox.Show("No encrypted data available to save. Please encrypt some data first.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!ValidateInputs(out BigInteger p, out BigInteger q, out BigInteger b, out BigInteger n, checkInputData: false))
            {
                return;
            }
            if (n <= 0)
            {
                MessageBox.Show("Cannot save encrypted file without valid P and Q.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Save Encrypted Data",
                Filter = !string.IsNullOrEmpty(_originalFileExtension) ? $"Encrypted File (*{_originalFileExtension})|*{_originalFileExtension}|All Files (*.*)|*.*" : "All Files (*.*)|*.*",
                FileName = !string.IsNullOrEmpty(_originalFileName) ? $"{_originalFileName}_encrypted{_originalFileExtension}" : "encrypted_output" + _originalFileExtension
            };


            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    BigInteger maxSizeValue = n - 1;
                    int byteSize = maxSizeValue.ToByteArray().Length;
                    if (byteSize == 0) byteSize = 1;

                    List<byte> allBytes = new List<byte>();
                    foreach (BigInteger cipherNum in _currentOutputNumbers)
                    {
                        byte[] numBytes = cipherNum.ToByteArray();
                        byte[] fixedSizeBytes = new byte[byteSize];
                        int bytesToCopy = Math.Min(numBytes.Length, byteSize);
                        Array.Copy(numBytes, 0, fixedSizeBytes, 0, bytesToCopy);
                        if (numBytes.Length > byteSize)
                        {
                            Console.WriteLine($"Note: BigInteger bytes (length {numBytes.Length}) longer than calculated size ({byteSize}). Assuming truncation of sign byte.");
                        }
                        allBytes.AddRange(fixedSizeBytes);
                    }

                    File.WriteAllBytes(saveFileDialog.FileName, allBytes.ToArray());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving encrypted file: {ex.Message}", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void btnSaveDecrFile_Click(object sender, RoutedEventArgs e)
        {
            bool isOutputDecrypted = _currentOutputNumbers != null && (_currentInputNumbers != null && !_isInputPlaintext);
            if (!isOutputDecrypted)
            {
                MessageBox.Show("No decrypted data available to save. Please decrypt some data first.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Save Decrypted Data",
                Filter = !string.IsNullOrEmpty(_originalFileExtension) ? $"Decrypted File (*{_originalFileExtension})|*{_originalFileExtension}|All Files (*.*)|*.*" : "All Files (*.*)|*.*",
                FileName = !string.IsNullOrEmpty(_originalFileName) ? $"{_originalFileName}_decrypted{_originalFileExtension}" : "decrypted_output" + _originalFileExtension
            };


            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    List<byte> fileBytes = new List<byte>();
                    foreach (BigInteger decryptedNum in _currentOutputNumbers)
                    {
                        if (decryptedNum < 0 || decryptedNum > 255)
                            MessageBox.Show($"Warning: Decrypted number {decryptedNum} is outside the valid byte range (0-255). It will be skipped during saving.", "Save Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        else
                            fileBytes.Add((byte)decryptedNum);

                    }
                    File.WriteAllBytes(saveFileDialog.FileName, fileBytes.ToArray());
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving decrypted file: {ex.Message}", "File Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void btnEncrypt_Click(object sender, RoutedEventArgs e)
        {
            if (_currentInputNumbers == null)
            {
                MessageBox.Show("Please load or enter plaintext data first.", "Input Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!_isInputPlaintext)
            {
                MessageBox.Show("Loaded data appears to be ciphertext. Please load plaintext to encrypt.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!ValidateInputs(out BigInteger p, out BigInteger q, out BigInteger b, out BigInteger n, checkInputData: true)) return;

            try
            {
                _currentOutputNumbers = Rabin.Encrypt(_currentInputNumbers, p, q, b);
                SetRichTextBoxText(rtbResult, OpenKey.BackEnd.Convert.BigIntegersToString(_currentOutputNumbers));
                UpdateSaveButtonStates(canSaveEncrypted: true, canSaveDecrypted: false);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Encryption failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _currentOutputNumbers = null;
                SetRichTextBoxText(rtbResult, string.Empty);
                UpdateSaveButtonStates();
            }
        }

        private void btnDecrypt_Click(object sender, RoutedEventArgs e)
        {
            if (_currentInputNumbers == null)
            {
                MessageBox.Show("Please load or enter ciphertext data first.", "Input Missing", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (_isInputPlaintext)
            {
                MessageBox.Show("Loaded data appears to be plaintext. Please load ciphertext to decrypt.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!ValidateInputs(out BigInteger p, out BigInteger q, out BigInteger b, out BigInteger n, checkInputData: true)) return;

            try
            {
                _currentOutputNumbers = Rabin.Decrypt(_currentInputNumbers, p, q, b);
                SetRichTextBoxText(rtbResult, OpenKey.BackEnd.Convert.BigIntegersToString(_currentOutputNumbers));
                UpdateSaveButtonStates(canSaveEncrypted: false, canSaveDecrypted: true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Decryption failed: {ex.Message}\n(Ensure P, Q, B match the values used for encryption and the file is not corrupt)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                _currentOutputNumbers = null;
                SetRichTextBoxText(rtbResult, string.Empty);
                UpdateSaveButtonStates();
            }
        }

        private void UpdateSaveButtonStates(bool? canSaveEncrypted = null, bool? canSaveDecrypted = null)
        {
            bool outputExists = _currentOutputNumbers != null && _currentOutputNumbers.Length > 0;
            bool outputIsEncrypted = outputExists && (_currentInputNumbers != null && _isInputPlaintext);
            bool outputIsDecrypted = outputExists && (_currentInputNumbers != null && !_isInputPlaintext);

            btnSaveEncrFile.IsEnabled = canSaveEncrypted ?? outputIsEncrypted;
            btnSaveDecrFile.IsEnabled = canSaveDecrypted ?? outputIsDecrypted;
        }

        private void KeyParameter_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetRichTextBoxText(rtbResult, string.Empty);
            _currentOutputNumbers = null;
            UpdateSaveButtonStates(false, false);
        }
    }
}