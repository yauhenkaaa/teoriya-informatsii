using System;
using System.Collections.Generic;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace TI_lab_3.Logic
{
    public static class Utils
    {
        private const int MillerRabinIterations = 20;

        public static bool IsPrime(BigInteger n)
        {
            if (n <= 1) return false;
            if (n == 2 || n == 3) return true;
            if (n % 2 == 0) return false;

            BigInteger t = n - 1;
            int s = 0;
            while (t % 2 == 0)
            {
                t /= 2;
                s++;
            }

            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] bytes = new byte[n.ToByteArray().LongLength];
                BigInteger a;

                for (int i = 0; i < MillerRabinIterations; i++)
                {
                    do
                    {
                        rng.GetBytes(bytes);
                        a = new BigInteger(bytes);
                    }
                    while (a < 2 || a >= n - 1);

                    BigInteger x = FastPow(a, t, n);

                    if (x == 1 || x == n - 1)
                        continue;

                    for (int j = 1; j < s; j++)
                    {
                        x = FastPow(x, 2, n);
                        if (x == 1)
                            return false;
                        if (x == n - 1)
                            goto NextIteration;
                    }

                    return false;

                NextIteration:;
                }
            }

            return true;
        }


        public static BigInteger FastPow(BigInteger a, BigInteger z, BigInteger n)
        {
            BigInteger result = 1;
            a %= n;
            while (z > 0)
            {
                if (z % 2 == 1)
                    result = (result * a) % n;

                z >>= 1;
                a = (a * a) % n;
            }
            return result;
        }

        public static (BigInteger x, BigInteger y) ExtendedEuclidean(BigInteger a, BigInteger b)
        {
            if (a == 0) throw new ArgumentException("A не может быть нулевым!", nameof(a));
            if (b == 0) throw new ArgumentException("B не может быть нулевым!", nameof(b));


            BigInteger d0 = a, d1 = b;
            BigInteger x0 = 1, x1 = 0;
            BigInteger y0 = 0, y1 = 1;

            while (d1 > 0)
            {
                BigInteger q = d0 / d1;

                BigInteger d2 = d0 % d1;

                BigInteger x2 = x0 - q * x1;
                BigInteger y2 = y0 - q * y1;

                d0 = d1; d1 = d2;
                x0 = x1; x1 = x2;
                y0 = y1; y1 = y2;
            }

            d0 = a; d1 = b;
            x0 = 1; x1 = 0;
            y0 = 0; y1 = 1;
            while (d1 > 1)
            {
                BigInteger q = d0 / d1;
                BigInteger d2 = d0 % d1;
                BigInteger x2 = x0 - q * x1;
                BigInteger y2 = y0 - q * y1;
                d0 = d1; d1 = d2;
                x0 = x1; x1 = x2;
                y0 = y1; y1 = y2;
            }
            return (x1, y1);
        }

        public static BigInteger ModularSqrt(BigInteger D, BigInteger p)
        {
            if (p % 4 != 3)
            {
                throw new ArgumentException("При делении на 4 p должно давать остаток 3.", nameof(p));
            }
            BigInteger exponent = (p + 1) / 4;
            return FastPow(D, exponent, p);
        }


        public static List<BigInteger> SolveQuadraticCRT(BigInteger c, BigInteger b, BigInteger p, BigInteger q)
        {
            BigInteger n = p * q;
            BigInteger D = (b * b + 4 * c) % n;
            if (D < 0) D += n;

            BigInteger mp = ModularSqrt(D, p);
            BigInteger mq = ModularSqrt(D, q);

            (BigInteger yp_coeff_p, BigInteger yq_coeff_q) = ExtendedEuclidean(p, q);

            BigInteger term1 = yp_coeff_p * p * mq;
            BigInteger term2 = yq_coeff_q * q * mp;

            List<BigInteger> roots = new List<BigInteger>(4);

            BigInteger d1 = (term1 + term2) % n;
            if (d1 < 0) d1 += n;
            roots.Add(d1);

            BigInteger d2 = n - d1;
            if (d2 < 0) d2 += n;
            roots.Add(d2);

            BigInteger d3 = (term1 - term2) % n;
            if (d3 < 0) d3 += n;
            roots.Add(d3);

            BigInteger d4 = n - d3;
            if (d4 < 0) d4 += n;
            roots.Add(d4);

            return roots;
        }


        public static bool IsValidParameters(BigInteger p, BigInteger q, BigInteger b, out string errorMessage)
        {
            StringBuilder errorBuilder = new StringBuilder();
            bool isValid = true;

            if (p <= 1)
            {
                errorBuilder.AppendLine("Число p не натуральное!");
                isValid = false;
            }
            else
            {
                if (!IsPrime(p))
                {
                    errorBuilder.AppendLine("Число p составное!");
                    isValid = false;
                }
                if (p % 4 != 3)
                {
                    errorBuilder.AppendLine("Число p при делении на 4 дает остаток не 3!");
                    isValid = false;
                }
            }

            if (q <= 1)
            {
                errorBuilder.AppendLine("Число q не натуральное!");
                isValid = false;
            }
            else
            {
                if (p == q)
                {
                    errorBuilder.AppendLine("Числа p и q не должны совпадать!");
                    isValid = false;
                }
                if (!IsPrime(q))
                {
                    errorBuilder.AppendLine("Число q составное!");
                    isValid = false;
                }
                if (q % 4 != 3)
                {
                    errorBuilder.AppendLine("Число q при делении на 4 дает остаток не 3!");
                    isValid = false;
                }
            }

            if (b <= 0)
            {
                errorBuilder.AppendLine("Число b не натуральное (должно быть > 0)!");
                isValid = false;
            }

            if (p > 1 && q > 1 && b > 0)
            {
                BigInteger n = p * q;
                if (b >= n)
                {
                    errorBuilder.AppendLine("Число b должно быть меньше p * q!");
                    isValid = false;
                }
            }


            errorMessage = errorBuilder.ToString().TrimEnd();
            return isValid;
        }

        public static string BigIntegersToString(IEnumerable<BigInteger> bigIntegers)
        {
            if (bigIntegers == null) return string.Empty;
            return string.Join(" ", bigIntegers);
        }

        public static bool IsValidPrimeForRabin(BigInteger p)
        {
            if (!IsPrime(p)) return false;
            if (p % 4 != 3) return false;
            return true;
        }
    }
}