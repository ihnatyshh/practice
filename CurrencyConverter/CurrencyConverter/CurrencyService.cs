using System;
using System.Collections.Generic;
using System.Text;

namespace CurrencyConverter
{
    public class CurrencyService
    {
        public double Calculate(double amount, double fromRate, double toRate)
        {
            // Якщо курс 0, повертаємо 0, щоб не було помилки
            if (toRate == 0) return 0;

            // Формула: переводимо в гривню -> потім у нову валюту
            return (amount * fromRate) / toRate;
        }
    }
}
