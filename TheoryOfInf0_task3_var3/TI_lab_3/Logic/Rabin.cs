using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace TI_lab_3.Logic
{
    public static class Rabin
    {
        public static BigInteger EncryptBlock(BigInteger m, BigInteger p, BigInteger q, BigInteger b)
        {
            BigInteger n = p * q;
            if (n <= 256 && m >= n)
                throw new ArgumentException($"Message block m={m} must be less than n={n} for small n.", nameof(m));

            BigInteger c = m * (m + b);
            c %= n;
            if (c < 0) c += n;

            return c;
        }

        public static BigInteger DecryptBlock(BigInteger c, BigInteger p, BigInteger q, BigInteger b)
        {
            BigInteger n = p * q;
            if (c < 0 || c >= n)
                throw new ArgumentException("Ciphertext c must be in the range [0, n-1]", nameof(c));

            List<BigInteger> d_roots = Utils.SolveQuadraticCRT(c, b, p, q);

            foreach (BigInteger d in d_roots)
            {
                BigInteger term = d - b;

                if (term % 2 != 0)
                    term += n;

                if (term % 2 != 0)
                {
                    Console.WriteLine($"Warning: Term {term} remained odd after adding n={n}. d={d}, b={b}");
                    continue;
                }

                BigInteger m = term / 2;
                m %= n;
                if (m < 0) m += n;

                if (m >= 0 && m < 256)
                    return m;
            }

            return -1;
        }

        public static BigInteger[] Encrypt(BigInteger[] plaintextBytes, BigInteger p, BigInteger q, BigInteger b)
        {
            return plaintextBytes.Select(m => EncryptBlock(m, p, q, b)).ToArray();
        }

        public static BigInteger[] Decrypt(BigInteger[] ciphertextNumbers, BigInteger p, BigInteger q, BigInteger b)
        {
            List<BigInteger> decryptedBytes = new List<BigInteger>(ciphertextNumbers.Length);
            int blockNum = 0;
            foreach (BigInteger c in ciphertextNumbers)
            {
                blockNum++;
                BigInteger m = DecryptBlock(c, p, q, b);
                if (m == -1)
                {
                    throw new InvalidOperationException($"Decryption failed for block {blockNum} (ciphertext {c}). Check parameters or file integrity.");
                }
                decryptedBytes.Add(m);
            }
            return decryptedBytes.ToArray();
        }
    }
}