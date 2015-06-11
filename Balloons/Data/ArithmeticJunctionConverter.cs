using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TST.Phoenix.Arm.Data
{
	/// <summary>
	/// Конвертер, выполняющий арифметические операции над преобразуемыми в double
	/// значениями привязки данных.
	/// </summary>
	[ValueConversion(typeof(double[]), typeof(double))]
	public sealed class ArithmeticJunctionConverter : IMultiValueConverter
	{
		/// <summary>
		/// Инициализирует новый объект класса <see cref="ArithmeticJunctionConverter"/>.
		/// </summary>
		public ArithmeticJunctionConverter()
			: this(ArithmeticConverterOperation.Addition)
		{
		}

		/// <summary>
		/// Инициализирует новый объект класса <see cref="ArithmeticJunctionConverter"/>.
		/// </summary>
		/// <param name="operation">Операция, которую будет выполнять <see cref="ArithmeticJunctionConverter"/>
		/// при преобразовании.</param>
		public ArithmeticJunctionConverter(ArithmeticConverterOperation operation)
		{
			this.Operation = operation;
		}

		/// <summary>
		/// Возвращает или присваивает операцию, выполняемую данным конвертером при преобразовании.
		/// </summary>
		public ArithmeticConverterOperation Operation
		{
			get;
			set;
		}

		/// <summary>
		/// Преобразует исходные значения привязки данных.
		/// </summary>
		/// <param name="values">Исходные значения привязки данных, которые требуется преобразовать.</param>
		/// <param name="targetType">Тип целевого значения привязки данных.</param>
		/// <param name="parameter">Параметр преобразования.</param>
		/// <param name="culture">Региональные стандарты, используемые при преобразовании.</param>
		/// <returns>Значение, полученное путем преобразования.</returns>
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var acc = Double.NaN;
			 
			for (int i = 0; i < values.Length; i++)
			{
				var convertible = values[i] as IConvertible;
				if (convertible == null)
				{
					return DependencyProperty.UnsetValue;
				}

				var arg = convertible.ToDouble(culture);

				// Первый операнд помещаем в аккумулятор.
				if (i == 0)
				{
					acc = arg;
					continue;
				}

				switch (this.Operation)
				{
					case ArithmeticConverterOperation.Addition:
						{
							acc += arg;
							break;
						}
					case ArithmeticConverterOperation.Subtraction:
						{
							acc -= arg;
							break;
						}
					case ArithmeticConverterOperation.Multiplication:
						{
							acc *= arg;
							break;
						}
					case ArithmeticConverterOperation.Division:
						{
							if (arg == 0)
							{
								return DependencyProperty.UnsetValue;
							}

							acc /= arg;
							break;
						}
				}
			}

			return acc;
		}

		/// <summary>
		/// Преобразует целевое значение привязки данных в исходные.
		/// </summary>
		/// <param name="value">Целевое значение привязки данных, которое требуется преобразовать.</param>
		/// <param name="targetTypes">Типы исходных значений привязки данных.</param>
		/// <param name="parameter">Параметр преобразования.</param>
		/// <param name="culture">Региональные стандарты, используемые при преобразовании.</param>
		/// <returns>Значения, полученное путем преобразования.</returns>
		object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}