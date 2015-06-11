using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TST.Phoenix.Arm.Data
{
	/// <summary>
	/// Виды арифметических операций, поддерживаемых <see cref="ArithmeticConverter"/>.
	/// </summary>
	public enum ArithmeticConverterOperation
	{
		/// <summary>
		/// Операция сложения.
		/// </summary>
		Addition,
		
		/// <summary>
		/// Операция вычитания.
		/// </summary>
		Subtraction,

		/// <summary>
		/// Операция умножения.
		/// </summary>
		Multiplication,

		/// <summary>
		/// Операция деления.
		/// </summary>
		Division
	}
}
