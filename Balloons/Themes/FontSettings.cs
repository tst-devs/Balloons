using System;
using System.Windows;
using System.Windows.Media;

using TST.Phoenix.Arm.Data;

namespace TST.Phoenix.Arm.Themes
{
	/// <summary>
	/// Предоставляет настройки шрифтов приложения.
	/// </summary>
	public sealed class FontSettings 
	{
		#region Current FontSettings

		/// <summary>
		/// Текущие настройки шрифтов приложения.
		/// </summary>
		private readonly static Lazy<FontSettings> _Current = new Lazy<FontSettings>(() => new FontSettings());

		/// <summary>
		/// Возвращает текущие настройки шрифтов приложения.
		/// </summary>
		public static FontSettings Current
		{
			get
			{
				return FontSettings._Current.Value;
			}
		}

		#endregion

		/// <summary>
		/// Семейство шрифтов приложения.
		/// </summary>
		private readonly FontFamily _fontFamily = new FontFamily("Tahoma");

		/// <summary>
		/// Размер шрифта приложения.
		/// </summary>
		private readonly double _fontSize = (double)(new LengthConverter().ConvertFrom("10pt"));

		/// <summary>
		/// Возвращает или присваивает семейство шрифтов приложения.
		/// </summary>
		public FontFamily FontFamily
		{
			get
			{
				return this._fontFamily;
			}
		}

		/// <summary>
		/// Возвращает или присваивает размер шрифта приложения.
		/// </summary>
		public double FontSize
		{
			get
			{
				return this._fontSize;
			}
		}

		/// <summary>
		/// Возвращает или присваивает стиль шрифта приложения.
		/// </summary>
		public FontStyle FontStyle
		{
			get
			{
				return FontStyles.Normal;
			}
		}

		/// <summary>
		/// Возвращает или присваивает степень сжатия или расширения шрифта приложения.
		/// </summary>
		public FontStretch FontStretch
		{
			get
			{
				return FontStretches.Normal;
			}
		}

		/// <summary>
		/// Возвращает или присваивает плотность или толщину шрифта приложения.
		/// </summary>
		public FontWeight FontWeight
		{
			get
			{
				return FontWeights.Normal;
			}
		}
	}
}
