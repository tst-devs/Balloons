using System;
using System.Diagnostics.Contracts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TST.Phoenix.Arm.Utility
{
	/// <summary>
	/// Вспомогательный класс для различных утилитных функций.
	/// </summary>
	internal static class Helper
	{
		/// <summary>
		/// Упакованное нулевое значение типа <see cref="double"/>.
		/// </summary>
		public static readonly object DoubleZero = (double)0;

		/// <summary>
		/// Упакованное значение <see cref="Thickness"/>, содержащее нуль во всех компонентах.
		/// </summary>
		public static readonly object ThicknessZero = new Thickness(0);

		/// <summary>
		/// Возвращает старшее слово из заданного значения.
		/// </summary>
		/// <param name="value">Значение, из которого запрашивается старшее слово.</param>
		/// <returns>Старшее слово из заданного значения.</returns>
		public static int HiWord(int value)
		{
			return (short)(value >> 16) & 0xFFFF;
		}

		/// <summary>
		/// Возвращает старшее слово из заданного значения.
		/// </summary>
		/// <param name="value">Значение, из которого запрашивается старшее слово.</param>
		/// <returns>Старшее слово из заданного значения.</returns>
		public static int HiWord(IntPtr value)
		{
			return Helper.HiWord(value.ToInt32());
		}

		/// <summary>
		/// Возвращает младшее слово из заданного значения.
		/// </summary>
		/// <param name="value">Значение, из которого запрашивается младшее слово.</param>
		/// <returns>Младшее слово из заданного значения.</returns>
		public static int LoWord(int value)
		{
			return (short)(value & 0xFFFF);
		}

		/// <summary>
		/// Возвращает младшее слово из заданного значения.
		/// </summary>
		/// <param name="value">Значение, из которого запрашивается младшее слово.</param>
		/// <returns>Младшее слово из заданного значения.</returns>
		public static int LoWord(IntPtr value)
		{
			return Helper.LoWord(value.ToInt32());
		}

		/// <summary>
		/// Генерирует события с заданными аргументами, сначала со стратегией <see cref="RoutingStrategy.Tunnel"/>,
		/// затем со стратегией <see cref="RoutingStrategy.Bubble"/>, если первое не пометило событие как обработанное.
		/// </summary>
		/// <param name="raiseOn">Элемент, от которого генерируется событие.</param>
		/// <param name="args">Аргументы маршрутизируемого события.</param>
		/// <param name="tunnelEvent">Идентификатор со стратегией <see cref="RoutingStrategy.Tunnel"/>.</param>
		/// <param name="bubbleEvent">Идентификатор со стратегией <see cref="RoutingStrategy.Bubble"/>.</param>
		public static void RaiseTunnelThenBubble(
			UIElement raiseOn,
			RoutedEventArgs args,
			RoutedEvent tunnelEvent,
			RoutedEvent bubbleEvent)
		{
			Contract.Requires<ArgumentNullException>(tunnelEvent != null);
			Contract.Requires<ArgumentException>(tunnelEvent.RoutingStrategy == RoutingStrategy.Tunnel);

			Contract.Requires<ArgumentNullException>(bubbleEvent != null);
			Contract.Requires<ArgumentException>(bubbleEvent.RoutingStrategy == RoutingStrategy.Bubble);

			args.RoutedEvent = tunnelEvent;
			raiseOn.RaiseEvent(args);

			if (args.Handled)
			{
				return;
			}
			
			args.RoutedEvent = bubbleEvent;
			raiseOn.RaiseEvent(args);
		}

		/// <summary>
		/// Присоединяет обработчик события <paramref name="routedEvent"/> к заданному объекту <paramref name="source"/>.
		/// </summary>
		/// <param name="routedEvent">Идентификатор присоединяемого маршрутизируемого события.</param>
		/// <param name="source">Объект, к которому присоединить обработчик.</param>
		/// <param name="handler">Обработчик, который требуется присоединить.</param>
		public static void AddRoutedEventHandler(RoutedEvent routedEvent, DependencyObject source, Delegate handler)
		{
			UIElement uiElement = source as UIElement;
			if (uiElement != null)
			{
				uiElement.AddHandler(routedEvent, handler);
			}
			else
			{
				ContentElement contentElement = source as ContentElement;
				if (contentElement != null)
				{
					contentElement.AddHandler(routedEvent, handler);
				}
				else
				{
					UIElement3D uiElement3D = source as UIElement3D;
					if (uiElement3D == null)
					{
						throw new ArgumentOutOfRangeException("source");
					}
					uiElement3D.AddHandler(routedEvent, handler);
				}
			}
		}

		/// <summary>
		/// Отсоединяет обработчик события <paramref name="routedEvent"/> от заданного объекта <paramref name="source"/>.
		/// </summary>
		/// <param name="routedEvent">Идентификатор присоединяемого маршрутизируемого события.</param>
		/// <param name="source">Объект, от которого отсоединить обработчик.</param>
		/// <param name="handler">Обработчик, который требуется отсоединить.</param>
		public static void RemoveRoutedEventHandler(RoutedEvent routedEvent, DependencyObject source, Delegate handler)
		{
			UIElement uiElement = source as UIElement;
			if (uiElement != null)
			{
				uiElement.RemoveHandler(routedEvent, handler);
			}
			else
			{
				ContentElement contentElement = source as ContentElement;
				if (contentElement != null)
				{
					contentElement.RemoveHandler(routedEvent, handler);
				}
				else
				{
					UIElement3D uiElement3D = source as UIElement3D;
					if (uiElement3D == null)
					{
						throw new ArgumentOutOfRangeException("source");
					}
					uiElement3D.RemoveHandler(routedEvent, handler);
				}
			}
		}

		/// <summary>
		/// Поднимается вверх по визуальному и логическому дереву, до корневого элемента.
		/// </summary>
		/// <param name="from">Элемент, с которого начинается обход дерева.</param>
		/// <param name="nodePredicate">Функция, вызываемая для каждого встреченного узла,
		/// возвращающая <see langword="true"/>, если обход следует завершить.</param>
		/// <returns><see langword="true"/>, если обход деревьев был завершен методом
		/// <paramref name="nodePredicate"/>; иначе <see langword="false"/>.</returns>
		public static bool WalkUpwardsToRoot(DependencyObject from, Func<DependencyObject, bool> nodePredicate)
		{
			while (from != null)
			{
				// Вызываем функцию обработки очередного узла дерева.
				if (nodePredicate(from))
				{
					return true;
				}

				// Если объект Visual, то поднимаемся по визуальному дереву, иначе по логическому.
				if (from is Visual)
				{
					from = VisualTreeHelper.GetParent(from);
				}
				else
				{
					from = LogicalTreeHelper.GetParent(from);
				}
			}

			return false;
		}

		/// <summary>
		/// Представляет собой <see cref="HitTestFilterCallback"/>, который отфильтровывает элементы,
		/// невидимые на экране и для проверки попадания.
		/// </summary>
		/// <param name="potentialHitTestTarget">Объект для проверки возможности попадания.</param>
		/// <returns><see cref="HitTestFilterBehavior"/>, определяющий результат действия фильтра.</returns>
		/// <remarks>
		/// Необходимость в данном методе продиктована тем, что текущая версия WPF по непонятным причинам не
		/// учитывает значение свойств IsVisible и IsHitTestVisible, когда проверка попадания осуществляется
		/// вызовом метода VisualTreeHelper.HitTest(Visual/Visual3D, Point).
		/// </remarks>
		public static HitTestFilterBehavior HitTestFilterInvisible(DependencyObject potentialHitTestTarget)
		{
			bool isVisible = false;
			bool isHitTestVisible = false;

			// Определяем видимость.
			var uiElement = potentialHitTestTarget as UIElement;
			if (uiElement != null)
			{
				isVisible = uiElement.IsVisible;
				if (isVisible)
				{
					isHitTestVisible = uiElement.IsHitTestVisible;
				}
			}
			else
			{
				UIElement3D uiElement3D = potentialHitTestTarget as UIElement3D;
				if (uiElement3D != null)
				{
					isVisible = uiElement3D.IsVisible;
					if (isVisible)
					{
						isHitTestVisible = uiElement3D.IsHitTestVisible;
					}
				}
			}

			if (isVisible)
			{
				// Если элемент видим и на экране и для проверки попадания, засчитываем его.
				// Если же, он невидим для hit-test, направляем просмотр вглубь.
				return isHitTestVisible ? HitTestFilterBehavior.Continue : HitTestFilterBehavior.ContinueSkipSelf;
			}

			// Если элемент невидим вообще, пропускаем всё его дерево.
			return HitTestFilterBehavior.ContinueSkipSelfAndChildren;
		}

		/// <summary>
		/// Возвращает <see langword="true"/>, если текущей ОС является Windows Vista или последовавшие за ней.
		/// </summary>
		/// <returns><see langword="true"/>, если текущей ОС является Windows Vista или последовавшие за ней;
		/// иначе <see langword="false"/>.</returns>
		public static bool IsVistaOrNewer()
		{
			return Environment.OSVersion.Version >= new Version(6, 0);
		}

		/// <summary>
		/// Возвращает <langword>true</langword>, если положение, заданное типом <see cref="Dock"/>,
		/// ориентировано в горизонтальной плоскости.
		/// </summary>
		/// <returns><langword>true</langword>, если положение ориентировано в горизонтальной плоскости;
		/// иначе <langword>false</langword>.</returns>
		public static bool IsHorizontalPlacement(Dock placement)
		{
			return placement == Dock.Top || placement == Dock.Bottom;
		}

		/// <summary>
		/// Определяет, используется ли для заданного свойства зависимостей заданного объекта значение по умолчанию.
		/// </summary>
		/// <param name="dependencyObject">Объект, для которого проверяется источник значения свойства.</param>
		/// <param name="dependencyProperty">Свойство, для которого проверяется источник значения.</param>
		/// <returns><langword>true</langword>, если используется значение по умолчанию; иначе <langword>false</langword>.</returns>
		public static bool IsBaseValueSourceDefault(DependencyObject dependencyObject, DependencyProperty dependencyProperty)
		{
			var valueSource = DependencyPropertyHelper.GetValueSource(dependencyObject, dependencyProperty);
			return valueSource.BaseValueSource == BaseValueSource.Default;
		}

		/// <summary>
		/// Возвращает прямоугольник, уменьшенный на соответствующие значения из <see cref="Thickness"/>.
		/// </summary>
		/// <param name="rect">Прямоугольник, для которого расчитывается уменьшенный.</param>
		/// <param name="thickness">Значения, на которые следует уменьшить прямоугольник с соответствующих сторон.</param>
		/// <returns>Прямоугольник, измененный на значения из <see cref="Thickness"/>.</returns>
		public static Rect DeflateRect(Rect rect, Thickness thickness)
		{
			return new Rect(
				rect.Left + thickness.Left,
				rect.Top + thickness.Top,
				Math.Max(0, rect.Width - thickness.Left - thickness.Right),
				Math.Max(0, rect.Height - thickness.Top - thickness.Bottom));
		}

		/// <summary>
		/// Возвращает прямоугольник, увеличенный на соответствующие значения из <see cref="Thickness"/>.
		/// </summary>
		/// <param name="rect">Прямоугольник, для которого расчитывается увеличенный.</param>
		/// <param name="thickness">Значения, на которые следует увеличить прямоугольник с соответствующих сторон.</param>
		/// <returns>Прямоугольник, измененный на значения из <see cref="Thickness"/>.</returns>
		public static Rect InflateRect(Rect rect, Thickness thickness)
		{
			return new Rect(
				rect.Left - thickness.Left,
				rect.Top - thickness.Top,
				Math.Max(0, rect.Width + thickness.Left + thickness.Right),
				Math.Max(0, rect.Height + thickness.Top + thickness.Bottom));
		}

		/// <summary>
		/// Возвращает прямоугольник, полученный путем размещения прямоугольника
		/// <paramref name="original"/> внутри прямоугольника <paramref name="area"/>.
		/// </summary>
		/// <param name="original">Прямоугольник, который необходимо разместить внутри <paramref name="area"/>.</param>
		/// <param name="area">Прямоугольник, внутри которого необходимо разместить <paramref name="original"/>.</param>
		/// <returns><see cref="Rect"/>, полученный в результате размещения <paramref name="original"/>
		/// внутри <paramref name="area"/>.</returns>
		public static Rect PutRectInside(Rect original, Rect area)
		{
			// По горизонтали.
			if (original.Right > area.Right)
			{
				original.Offset(area.Right - original.Right, 0);
			}
			if (original.Left < area.Left)
			{
				original.Offset(area.Left - original.Left, 0);
			}

			// По вертикали.
			if (original.Bottom > area.Bottom)
			{
				original.Offset(0, area.Bottom - original.Bottom);
			}
			if (original.Top < area.Top)
			{
				original.Offset(0, area.Top - original.Top);
			}

			return original;
		}
	}
}
