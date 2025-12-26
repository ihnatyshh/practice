using Xunit;
using CurrencyConverter; // Підключаємо наш основний проєкт

namespace CurrencyConverter.Tests
{
    public class MyTests
    {
        [Fact]
        public void Test_USDtoUAH()
        {
            // 1. Підготовка
            var service = new CurrencyService();
            double myMoney = 100;  // 100 доларів
            double usdRate = 41.5; // Курс долара
            double uahRate = 1.0;  // Курс гривні

            // 2. Дія 
            double result = service.Calculate(myMoney, usdRate, uahRate);

            // 3. Перевірка
            // 100 * 41.5 / 1.0 = 4150
            Assert.Equal(4150, result);
        }
    }
}