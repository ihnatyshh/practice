using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace CurrencyConverter
{
    public partial class MainWindow : Window
    {
        
        private CurrencyService _currencyService = new CurrencyService();

        private Dictionary<string, double> exchangeRates = new Dictionary<string, double>();
        private DateTime lastUpdate;

        public MainWindow()
        {
            InitializeComponent();
            LoadExchangeRatesAsync();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Анімація зникнення заставки
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.4));
            fadeOut.Completed += (s, args) =>
            {
                SplashScreen.Visibility = Visibility.Collapsed;
                MainBorder.Visibility = Visibility.Visible;

                // Анімація появи головного контенту
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
                MainBorder.BeginAnimation(OpacityProperty, fadeIn);

                var scaleAnim = new DoubleAnimation(0.8, 1, TimeSpan.FromSeconds(0.5))
                {
                    EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.3 }
                };
                MainScaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleAnim);
                MainScaleTransform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleAnim);
            };

            SplashScreen.BeginAnimation(OpacityProperty, fadeOut);
        }

        // Валідація введення - дозволяємо тільки цифри, крапку та кому
        private void InputAmount_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Дозволяємо тільки цифри, крапку та кому
            Regex regex = new Regex(@"^[0-9.,]+$");
            e.Handled = !regex.IsMatch(e.Text);
        }

        // Завантаження курсів валют з API НБУ
        private async Task LoadExchangeRatesAsync()
        {
            try
            {
                StatusText.Text = "⏳ Завантаження курсів НБУ...";

                using (HttpClient client = new HttpClient())
                {
                    // API НБУ повертає курси валют до гривні
                    string url = "https://bank.gov.ua/NBUStatService/v1/statdirectory/exchange?json";

                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();

                    string jsonResponse = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var rates = JsonSerializer.Deserialize<List<NbuRate>>(jsonResponse, options);

                    // Очищуємо словник
                    exchangeRates.Clear();

                    // UAH до UAH = 1
                    exchangeRates["UAH"] = 1.0;

                    // Додаємо курси з API
                    foreach (var rate in rates)
                    {
                        exchangeRates[rate.Cc] = rate.Rate;
                    }

                    lastUpdate = DateTime.Now;
                    StatusText.Text = $"✅ Курси оновлено: {lastUpdate:dd.MM.yyyy HH:mm}";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = "❌ Помилка завантаження курсів";
                MessageBox.Show($"Не вдалося завантажити курси валют:\n{ex.Message}",
                    "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);

                // Встановлюємо курси за замовчуванням для тестування
                SetDefaultRates();
            }
        }

        // Резервні курси на випадок помилки API
        private void SetDefaultRates()
        {
            exchangeRates.Clear();
            exchangeRates["UAH"] = 1.0;
            exchangeRates["USD"] = 41.50;
            exchangeRates["EUR"] = 44.80;
            exchangeRates["GBP"] = 52.30;
            exchangeRates["PLN"] = 10.20;

            StatusText.Text = "⚠️ Використовуються резервні курси";
        }

        // Конвертація валют
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            // Перевіряємо чи завантажені курси
            if (exchangeRates.Count == 0)
            {
                MessageBox.Show("Курси валют ще не завантажені. Зачекайте...",
                    "Увага", MessageBoxButton.OK, MessageBoxImage.Warning);
                await LoadExchangeRatesAsync();
                return;
            }

            // Отримуємо код валюти (перші 3 символи)
            string fromCurrency = GetCurrencyCode(FromCurrencySelector.SelectedIndex);
            string toCurrency = GetCurrencyCode(ToCurrencySelector.SelectedIndex);

            // Перевіряємо чи не вибрана однакова валюта
            if (fromCurrency == toCurrency)
            {
                ResultText.Text = "⚠️ Оберіть різні валюти!";
                return;
            }

            // Парсимо суму (підтримуємо і крапку, і кому)
            string amountText = InputAmount.Text.Replace(',', '.');

            if (double.TryParse(amountText, NumberStyles.Any, CultureInfo.InvariantCulture, out double amount))
            {
                if (amount <= 0)
                {
                    ResultText.Text = "⚠️ Сума має бути більше 0";
                    return;
                }

                try
                {
                    // Отримуємо курси
                    double fromRate = exchangeRates.ContainsKey(fromCurrency)
                        ? exchangeRates[fromCurrency]
                        : 1.0;

                    double toRate = exchangeRates.ContainsKey(toCurrency)
                        ? exchangeRates[toCurrency]
                        : 1.0;

                    // 2. ЗМІНЕНО: Викликаємо метод Calculate з нашого сервісу
                    double result = _currencyService.Calculate(amount, fromRate, toRate);

                    // Форматуємо результат
                    ResultText.Text = $"💰 Результат:\n{amount:F2} {fromCurrency} = {result:F2} {toCurrency}";
                }
                catch (Exception ex)
                {
                    ResultText.Text = "❌ Помилка конвертації";
                    MessageBox.Show($"Помилка: {ex.Message}", "Помилка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                ResultText.Text = "❌ Невірний формат суми!";
            }
        }

        // Отримання коду валюти за індексом
        private string GetCurrencyCode(int index)
        {
            return index switch
            {
                0 => "UAH",
                1 => "USD",
                2 => "EUR",
                3 => "GBP",
                4 => "PLN",
                _ => "UAH"
            };
        }
    }

    // Клас для десеріалізації відповіді API НБУ
    public class NbuRate
    {
        public int R030 { get; set; }          // Цифровий код
        public string Txt { get; set; }        // Назва валюти
        public double Rate { get; set; }       // Курс
        public string Cc { get; set; }         // Літерний код (USD, EUR, etc.)
        public string Exchangedate { get; set; } // Дата
    }
}