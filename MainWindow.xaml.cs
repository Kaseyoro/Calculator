using System;
using System.Data;        // DataTable — для вычисления математических выражений
using System.Text;        // StringBuilder — для построения строк
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;   // KeyEventArgs — для обработки клавиатуры
using System.Globalization;   // CultureInfo — для корректного парсинга дробных чисел

namespace Calculator
{
    public partial class MainWindow : Window
    {
        // Текущее математическое выражение, которое вычисляется (например "12+5*3")
        private string currentExpression = "";

        // Флаг: был ли только что показан результат после нажатия "="
        // Нужен чтобы понять — начинать новый пример или продолжать текущий
        private bool resultShown = false;

        // Строка со всеми допустимыми операторами — используется для проверок по всему коду
        private const string Operators = "+-*/";

        // Конструктор окна — вызывается при запуске приложения
        public MainWindow()
        {
            InitializeComponent(); // инициализирует все элементы из XAML
        }

        // ── КЛАВИАТУРНЫЙ ВВОД ────────────────────────────────────────────
        // Срабатывает при нажатии любой клавиши когда окно в фокусе
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Shift зажат — обрабатываем отдельно, чтобы не конфликтовать
            // с теми же клавишами без Shift (например D8 = "8", Shift+D8 = "*")
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
            {
                switch (e.Key)
                {
                    case Key.D8: ProcessInput("*"); break; // Shift+8 = *
                    case Key.OemPlus: ProcessInput("+"); break; // Shift+= = +
                }
                return; // выходим, чтобы не попасть в блок ниже
            }

            // Обработка клавиш без Shift
            switch (e.Key)
            {
                // Цифры на основной клавиатуре (D0-D9) и цифровом блоке (NumPad)
                case Key.D0: case Key.NumPad0: ProcessInput("0"); break;
                case Key.D1: case Key.NumPad1: ProcessInput("1"); break;
                case Key.D2: case Key.NumPad2: ProcessInput("2"); break;
                case Key.D3: case Key.NumPad3: ProcessInput("3"); break;
                case Key.D4: case Key.NumPad4: ProcessInput("4"); break;
                case Key.D5: case Key.NumPad5: ProcessInput("5"); break;
                case Key.D6: case Key.NumPad6: ProcessInput("6"); break;
                case Key.D7: case Key.NumPad7: ProcessInput("7"); break;
                case Key.D8: case Key.NumPad8: ProcessInput("8"); break;
                case Key.D9: case Key.NumPad9: ProcessInput("9"); break;

                // Операторы на цифровом блоке
                case Key.Add: ProcessInput("+"); break;
                case Key.Subtract: ProcessInput("-"); break;
                case Key.Multiply: ProcessInput("*"); break;
                case Key.Divide: ProcessInput("/"); break;

                // Операторы на основной клавиатуре
                case Key.OemMinus: ProcessInput("-"); break; // клавиша минуса
                case Key.OemQuestion: ProcessInput("/"); break; // клавиша /

                // Десятичная точка — обрабатываем и точку и запятую
                case Key.OemPeriod:
                case Key.OemComma:
                case Key.Decimal:
                    ProcessInput("."); break;

                // Enter и = без Shift — вычислить результат
                case Key.Enter:
                case Key.OemPlus:
                    Equals_Click(sender, null!); break;

                case Key.Back: Backspace_Click(sender, null!); break; // удалить символ
                case Key.Escape: Clear_Click(sender, null!); break; // сброс
            }
        }

        // ── ОБРАБОТЧИК КНОПОК (цифры, точка, операторы) ─────────────────
        // Единый обработчик для всех кнопок цифр и операторов
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            // Читаем текст кнопки, убираем пробелы на случай если они есть в Content
            string value = btn.Content?.ToString()?.Trim() ?? "";
            ProcessInput(value); // передаём в общий метод обработки ввода
        }

        // ── ОСНОВНАЯ ЛОГИКА ВВОДА ────────────────────────────────────────
        // Единая точка обработки любого ввода — и с кнопок и с клавиатуры
        private void ProcessInput(string value)
        {
            if (value == "") return;

            // Проверяем — является ли введённый символ оператором
            bool isOperator = Operators.Contains(value);

            // Если только что был показан результат "="
            if (resultShown)
            {
                resultShown = false;
                if (isOperator)
                    // Оператор после результата — продолжаем считать с этого числа
                    // Например: результат 10, нажали "+" → "10+"
                    currentExpression += value;
                else
                    // Цифра после результата — начинаем новый пример с нуля
                    currentExpression = value;
                UpdateDisplay();
                return;
            }

            // Нельзя начинать ввод с оператора — игнорируем
            if (isOperator && currentExpression == "") return;

            // Нельзя вводить два оператора подряд — заменяем последний
            // Например: "5+" → нажали "*" → становится "5*"
            if (isOperator && Operators.Contains(currentExpression[^1]))
            {
                // [^1] — последний символ строки (синтаксис C# 8+)
                // [..^1] — строка без последнего символа
                currentExpression = currentExpression[..^1] + value;
                UpdateDisplay();
                return;
            }

            // Обработка десятичной точки
            if (value == ".")
            {
                // Находим где начинается текущее число (после последнего оператора)
                int lastOp = LastOperatorIndex();
                string currentNumber = currentExpression[(lastOp + 1)..];

                // Если в текущем числе уже есть точка — не добавляем вторую
                if (currentNumber.Contains('.')) return;

                // Если точка нажата первой (пустое число) — добавляем "0" перед ней
                // Чтобы получилось "0." а не просто "."
                if (currentNumber == "") currentExpression += "0";
            }

            // Добавляем символ к выражению и обновляем экран
            currentExpression += value;
            UpdateDisplay();
        }

        // ── КНОПКА "=" ───────────────────────────────────────────────────
        private void Equals_Click(object sender, RoutedEventArgs e)
        {
            // Нечего считать — выходим
            if (currentExpression == "") return;

            // Нельзя считать если выражение заканчивается на оператор ("5+")
            if (Operators.Contains(currentExpression[^1])) return;

            try
            {
                // DataTable.Compute — встроенный вычислитель математических выражений
                // Умеет считать строки вида "2+3*4" с учётом приоритета операций
                var result = new DataTable().Compute(currentExpression, null);

                // Форматируем результат: G10 — до 10 значащих цифр без лишних нулей
                // InvariantCulture — всегда точка как разделитель (не запятая)
                string resultStr = Convert.ToDouble(result)
                    .ToString("G10", CultureInfo.InvariantCulture);

                // Показываем выражение в строке истории, результат на главном дисплее
                HistoryLine.Text = BuildDisplay(currentExpression) + " =";
                Display.Text = resultStr;
                currentExpression = resultStr; // результат становится новым числом
                resultShown = true;            // помечаем что результат показан
            }
            catch
            {
                // Если DataTable не смог вычислить — показываем ошибку
                Display.Text = "Ошибка";
                HistoryLine.Text = "";
                currentExpression = "";
                resultShown = false;
            }
        }

        // ── КНОПКА "C" — полный сброс ────────────────────────────────────
        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            currentExpression = "";
            Display.Text = "";
            HistoryLine.Text = "";
            resultShown = false;
        }

        // ── КНОПКА "⌫" — удалить последний символ ────────────────────────
        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            // После результата бэкспейс сбрасывает всё (как в Windows-калькуляторе)
            if (resultShown)
            {
                Clear_Click(sender, e);
                return;
            }

            if (currentExpression == "") return;

            // Удаляем последний символ из выражения
            currentExpression = currentExpression[..^1];
            UpdateDisplay();
        }

        // ── КНОПКА "√" — квадратный корень ──────────────────────────────
        private void Sqrt_Click(object sender, RoutedEventArgs e)
        {
            if (currentExpression == "") return;

            // TryParse — безопасный парсинг: не бросает исключение если строка не число
            // InvariantCulture — ожидаем точку как разделитель дробной части
            if (!double.TryParse(currentExpression, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double num))
            {
                Display.Text = "Ошибка";
                currentExpression = "";
                return;
            }

            // Корень из отрицательного числа — математически невозможен (в вещественных числах)
            if (num < 0)
            {
                Display.Text = "Ошибка: √ от отриц.";
                currentExpression = "";
                return;
            }

            string resultStr = Math.Sqrt(num)
                .ToString("G10", CultureInfo.InvariantCulture);

            // Показываем в истории что именно мы посчитали
            HistoryLine.Text = $"√({currentExpression}) =";
            currentExpression = resultStr;
            Display.Text = resultStr;
            resultShown = false;
        }

        // ── КНОПКА "x²" — возведение в квадрат ──────────────────────────
        private void Square_Click(object sender, RoutedEventArgs e)
        {
            if (currentExpression == "") return;

            int lastOp = LastOperatorIndex();

            // Если lastOp указывает на минус в самом начале строки —
            // это унарный минус, он часть числа, а не оператор
            if (lastOp == 0 && currentExpression[0] == '-')
                lastOp = -1;

            string prefix = currentExpression[..(lastOp + 1)];
            string numberPart = currentExpression[(lastOp + 1)..];

            if (numberPart == "") return;
            if (!double.TryParse(numberPart, NumberStyles.Any,
                    CultureInfo.InvariantCulture, out double num)) return;

            string resultStr = (num * num)
                .ToString("G10", CultureInfo.InvariantCulture);

            HistoryLine.Text = $"({numberPart})² =";
            currentExpression = prefix + resultStr;
            UpdateDisplay();
            resultShown = false;
        }

        // ── ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ────────────────────────────────────────

        // Обновляет главный дисплей и сбрасывает строку истории при новом вводе
        private void UpdateDisplay()
        {
            Display.Text = BuildDisplay(currentExpression);
            if (!resultShown) HistoryLine.Text = "";
        }

        // Возвращает индекс последнего оператора в выражении
        // Нужен чтобы выделить "текущее число" — то что идёт после последнего оператора
        // Возвращает -1 если операторов нет (выражение — просто одно число)
        private int LastOperatorIndex()
        {
            for (int i = currentExpression.Length - 1; i >= 0; i--)
                if (Operators.Contains(currentExpression[i])) return i;
            return -1;
        }

        // Преобразует внутреннее выражение "5+3*2" в читаемый вид "5 + 3 * 2"
        // Добавляет пробелы вокруг каждого оператора для отображения на дисплее
        private static string BuildDisplay(string expr)
        {
            if (expr == "") return "";
            var sb = new StringBuilder();
            foreach (char c in expr)
                // Если символ — оператор, добавляем пробелы вокруг него
                sb.Append(Operators.Contains(c) ? $" {c} " : c.ToString());
            return sb.ToString();
        }
    }
}