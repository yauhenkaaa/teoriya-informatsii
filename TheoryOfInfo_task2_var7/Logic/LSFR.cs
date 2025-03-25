﻿using System;
using System.Collections;
using System.IO;

namespace TheoryOfInfo_task2_var7.Logic
{
    public static class LSFR
    {
        private const int RegisterLength = 29;
        private const int BitsToProcess = 240; // обрабатываются только первые и последние 15 байт для больших файлов

        // Поле для хранения сгенерированного потока ключа, который можно потом вывести в интерфейсе
        public static BitArray KeyStream { get; private set; }

        /// <summary>
        /// Проверяет, что состояние регистра содержит ровно 29 символа, состоящих только из '0' и '1'.
        /// </summary>
        public static bool ValidateRegisterState(string state)
        {
            if (state.Length != RegisterLength)
                return false;
            foreach (char c in state)
            {
                if (c != '0' && c != '1')
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Генерирует поток ключа длины bitLength по алгоритму LSFR (полином x^29 + x^2 + 1).
        /// Сгенерированный поток сохраняется в свойство KeyStream.
        /// </summary>
        public static BitArray GenerateKeyStream(string registerState, int bitLength)
        {
            bool[] register = new bool[RegisterLength];
            for (int i = 0; i < RegisterLength; i++)
            {
                register[i] = registerState[i] == '1';
            }
            BitArray keyStream = new BitArray(bitLength);
            for (int i = 0; i < bitLength; i++)
            {
                bool keyBit = register[0];
                keyStream[i] = keyBit;
                // Новый бит вычисляется как XOR первого бита и бита с индексом 29
                bool newBit = register[0] ^ register[27];
                for (int j = 0; j < RegisterLength - 1; j++)
                    register[j] = register[j + 1];
                register[RegisterLength - 1] = newBit;
            }
            KeyStream = keyStream;
            return keyStream;
        }

        /// <summary>
        /// Выполняет шифрование или дешифрование файла по пути inputFilePath,
        /// используя заданное состояние регистра registerState.
        /// Если общее число бит больше 2 * BitsToProcess, обрабатываются только первые и последние BitsToProcess бит.
        /// Сгенерированный результат записывается в outputFilePath.
        /// При этом ключевая последовательность генерируется и сохраняется в KeyStream.
        /// </summary>
        public static void ProcessFile(string inputFilePath, string outputFilePath, string registerState)
        {
            // Считываем исходные байты файла
            byte[] fileBytes = File.ReadAllBytes(inputFilePath);
            BitArray fileBits = new BitArray(fileBytes);
            int totalBits = fileBits.Length;
            bool partialProcessing = totalBits > 8192;

            // Инициализация LSFR-регистра из строки registerState
            bool[] register = new bool[RegisterLength];
            for (int i = 0; i < RegisterLength; i++)
            {
                register[i] = registerState[i] == '1';
            }

            // Обработка каждого бита: если файл большой – только первые и последние BitsToProcess бит обрабатываются,
            // для остальных битов операция XOR не применяется.
            for (int i = 0; i < totalBits; i++)
            {
                bool keyBit = register[0];
                bool processThisBit = true;
                if (partialProcessing && i >= BitsToProcess && i < totalBits - BitsToProcess)
                {
                    processThisBit = false;
                }
                if (processThisBit)
                {
                    fileBits[i] = fileBits[i] ^ keyBit;
                }

                // Обновление LSFR-регистра: новый бит = XOR второго и бита с индексом 29
                bool newBit = register[1] ^ register[28];
                for (int j = 0; j < RegisterLength - 1; j++)
                {
                    register[j] = register[j + 1];
                }
                register[RegisterLength - 1] = newBit;
            }

            // Сохраняем ключевую последовательность – она будет иметь ту же длину, что и файл (в битах)
            GenerateKeyStream(registerState, totalBits);

            // Преобразуем обработанный BitArray обратно в байты и сохраняем в файл
            byte[] resultBytes = new byte[fileBytes.Length];
            fileBits.CopyTo(resultBytes, 0);
            File.WriteAllBytes(outputFilePath, resultBytes);
        }
    }
}
