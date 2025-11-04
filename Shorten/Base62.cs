using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace Shorten;
public static class Base62
{
    const string ALPHABET = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    const int BASE = 62;

    public static string Encode(long number)
    {
        if (number is 0)
            return ALPHABET[0].ToString();

        var result = new StringBuilder();
        while (number > 0)
        {
            result.Insert(0, ALPHABET[(int)number % BASE]);
            
            number /= BASE;
            Console.WriteLine(result.ToString());
        }

        return result.ToString();
    }

    public static long Decode(string encoded)
    {
        long result = 0;
        for (int i = 0; i < encoded.Length; i++)
        {
            result = result * BASE + ALPHABET.IndexOf(encoded[i]);
        }
        return result;
    }
}