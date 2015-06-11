using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace TST.Phoenix.Arm.Data
{
	/// <summary>
	/// Конвертер, выполняющий арифметические операции с заданным аргументом над преобразуемым
	/// в double значением привязки данных.
	/// </summary>
	[ValueConversion(typeof(double), typeof(double))]
	public sealed class ArithmeticConverter : IValueConverter
	{
		/// <summary>
		/// Инициализирует новый объект класса <see cref="ArithmeticConverter"/>.
		/// </summary>
		public ArithmeticConverter()
			: this(ArithmeticConverterOperation.Addition, Double.NaN)
		{
		}

		/// <summary>
		/// Инициализирует новый объект класса <see cref="ArithmeticConverter"/>.
		/// </summary>
		/// <param name="operation">Операция, которую будет выполнять <see cref="ArithmeticConverter"/>
		/// при преобразовании.</param>
		/// <param name="argument">Аргумент, изменяющий значение привязки данных при преобразовании.</param>
		public ArithmeticConverter(ArithmeticConverterOperation operation, double argument)
		{
			this.Operation = operation;
			this.Argument = argument;
		}

		/// <summary>
		/// Возвращает или присваивает операцию, выполняемую конвертером при преобразовании.
		/// </summary>
		public ArithmeticConverterOperation Operation
		{
			get;
			set;
		}

		/// <summary>
		/// Возвращает или присваивает аргумент, изменяющий значение привязки данных при
		/// преобразовании.
		/// </summary>
		public double Argument
		{
			get;
			set;
		}
		
		/// <summary>
		/// Преобразует исходное значение привязки данных.
		/// </summary>
		/// <param name="value">Исходное значение привязки данных, которое требуется преобразовать.</param>
		/// <param name="targetType">Тип целевого значения привязки данных.</param>
		/// <param name="parameter">Параметр преобразования.</param>
		/// <param name="culture">Региональные стандарты, используемые при преобразовании.</param>
		/// <returns>Значение, полученное путем преобразования.</returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var convertible = value as IConvertible;
			if (convertible == null)
			{
				return DependencyProperty.UnsetValue;
			}

			return ArithmeticConverter.ExecuteOperation(this.Operation, convertible.ToDouble(culture), this.Argument);
		}

		/// <summary>
		/// Преобразует целевое значение привязки данных в исходное.
		/// </summary>
		/// <param name="value">Целевое значение привязки данных, которое требуется преобразовать.</param>
		/// <param name="targetType">Тип исходного значения привязки данных.</param>
		/// <param name="parameter">Параметр преобразования.</param>
		/// <param name="culture">Региональные стандарты, используемые при преобразовании.</param>
		/// <returns>Значение, полученное путем преобразования.</returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var convertible = value as IConvertible;
			if (convertible == null)
			{
				return DependencyProperty.UnsetValue;
			}

			return ArithmeticConverter.ExecuteOperation(this.ReversedOperation, convertible.ToDouble(culture), this.Argument);
		}

		/// <summary>
		/// Возвращает операцию, выполняемую конвертером при обратном преобразовании.
		/// </summary>
		private ArithmeticConverterOperation ReversedOperation
		{
			get
			{
				switch (this.Operation)
				{
					case ArithmeticConverterOperation.Addition:
						{
							return ArithmeticConverterOperation.Subtraction;
						}
					case ArithmeticConverterOperation.Subtraction:
						{
							return ArithmeticConverterOperation.Addition;
						}
					case ArithmeticConverterOperation.Multiplication:
						{
							return ArithmeticConverterOperation.Division;
						}
					case ArithmeticConverterOperation.Division:
						{
							return ArithmeticConverterOperation.Multiplication;
						}
				}

				throw new ArgumentOutOfRangeException("Operation");
			}
		}

		/// <summary>
		/// Выполняет заданную арифметическую операцию с заданным аргументом над заданным аккумулятором.
		/// </summary>
		/// <param name="operation">Арифметическая операция, которую требуется выполнить.</param>
		/// <param name="acc">Аккумулятор, над которым нужно выполнить операцию.</param>
		/// <param name="arg">Аргумент, изменяющий аккумулятор.</param>
		/// <returns>Объект, содержащий результат операции.</returns>
		private static object ExecuteOperation(ArithmeticConverterOperation operation, double acc, double arg)
		{
			switch (operation)
			{
				case ArithmeticConverterOperation.Addition:
					{
						return acc + arg;
					}
				case ArithmeticConverterOperation.Subtraction:
					{
						return acc - arg;
					}
				case ArithmeticConverterOperation.Multiplication:
					{
						return acc * arg;
					}
				case ArithmeticConverterOperation.Division:
					{
						if (arg == 0)
						{
							break;
						}

						return acc / arg;
					}
			}

			return DependencyProperty.UnsetValue;
		}
	}
}