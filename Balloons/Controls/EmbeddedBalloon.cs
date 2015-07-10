using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TST.Phoenix.Arm.Utility;

namespace TST.Phoenix.Arm.Controls
{
	/// <summary>
	/// Представляет собой контрол с содержимым, отображающим связь с заданной точкой
	/// целевого элемента пользовательского интерфейса.
	/// </summary>
	public class EmbeddedBalloon : ContentControl
	{
		/// <summary>
		/// Инициализирует класс <see cref="EmbeddedBalloon"/>.
		/// </summary>
		static EmbeddedBalloon()
		{
			EmbeddedBalloon.DefaultStyleKeyProperty.OverrideMetadata(
				typeof(EmbeddedBalloon),
				new FrameworkPropertyMetadata(typeof(EmbeddedBalloon)));
		}

		/// <summary>
		/// Расчитывает положение элемента.
		/// </summary>
		private void ComputePosition()
		{
			// Расчитывать положение имеет смысл, если пройдены стадии измерения и компоновки,...
			if (!this.IsMeasureValid || !this.IsArrangeValid)
			{
				this.Dispatcher.BeginInvoke(new Action(() => this.ComputePosition()), System.Windows.Threading.DispatcherPriority.Render);
				return;
			}
			// ... а также установлен целевой элемент.
			var placementTarget = this.PlacementTarget;
			if (placementTarget == null)
			{
				return;
			}

			// Определяем область размещения. 
			// Если PlacementRectangle содержит значение по умолчанию, используем всю область целевого элемента.
			Rect placementRectangle;
			if (Helper.IsBaseValueSourceDefault(this, EmbeddedBalloon.PlacementRectangleProperty))
			{
				placementRectangle = new Rect(placementTarget.RenderSize);
			}
			else
			{
				placementRectangle = this.PlacementRectangle;
			}

			// Получаем варианты размещения. Для этого используем callback всплывающего Balloon-а.
			// Передаем ему нулевой размер в качестве размера целевого элемента, потому что нас
			// интересует положение относительно точки, заданной через Offset-ы, а не относительно
			// реальных placementTarget.
			var placements = Balloon.BalloonPlacementCallback(this.RenderSize, new Size(0, 0), this.FlowAround, 
				this.LeftDockPriority, this.TopDockPriority, this.RightDockPriority, this.BottomDockPriority);

			var dockSides = new[] { 
					new { Priority = this.TopDockPriority, Side = Dock.Top }, 
					new { Priority = this.BottomDockPriority, Side = Dock.Bottom },
					new { Priority = this.LeftDockPriority, Side = Dock.Left }, 
					new { Priority = this.RightDockPriority, Side= Dock.Right }, 
				}
				.Where(side => side.Priority != -1)
				.OrderByDescending(side => side.Priority)
				.ToList();

			// Преобразовываем размещения в прямоугольники, которые может занять balloon.
			var placementBounds = placements.Select(
				placement => new Rect(
					placementRectangle.Left + placement.Point.X,
					placementRectangle.Top + placement.Point.Y,
					this.RenderSize.Width,
					this.RenderSize.Height))
				.ToList();

			// Для каждого потенциального размещения выясняем, вписывается ли его прямоугольник
			// в placementRectangle. Если да, то выбираем его для задания позиции balloon-а.
			// Иначе, определяем площадь пересечения с placementRectangle. В случае, когда ни одно
			// из размещений не впишется полностью, будет выбрано размещение с максимальной площадью
			// пересечения.
			var placementIndex = 0;
			var intersectionSquares = new double[placementBounds.Count];
			while (placementIndex < placementBounds.Count)
			{
				// Выбираем расположение, если оно полностью помещается в 
				// область размещение и не пересекается с областью присоединения элемента.
				var flowRectIntersection = Rect.Intersect(placementBounds[placementIndex], this.FlowAround);
				if (placementRectangle.Contains(placementBounds[placementIndex]) 
					&& (flowRectIntersection.IsEmpty 
						|| flowRectIntersection.Width < double.Epsilon 
						|| flowRectIntersection.Height < double.Epsilon))
				{
					break;
				}
				else
				{
					var intersection = Rect.Intersect(placementRectangle, placementBounds[placementIndex]);
					intersectionSquares[placementIndex] = intersection.IsEmpty ? 0 : intersection.Width * intersection.Height;
				}

				placementIndex++;
			}

			// Если так и не нашли полностью вписывающегося размещения, сравниваем площади.
			if (placementIndex == placementBounds.Count)
			{
				var maxSquare = intersectionSquares[0];
				placementIndex = 0;
				for (int i = 1; i < intersectionSquares.Length; i++)
				{
					if (intersectionSquares[i] > maxSquare)
					{
						maxSquare = intersectionSquares[i];
						placementIndex = i;
					}
				}
			}

			var chosenPlacement = placementBounds[placementIndex];
			var finalPlacement = this.CorrectPlacement(chosenPlacement, placementRectangle, dockSides[placementIndex].Side);

			this.SetValue(EmbeddedBalloon.HorizontalPositionPropertyKey, finalPlacement.Left);
			this.SetValue(EmbeddedBalloon.VerticalPositionPropertyKey, finalPlacement.Top);
			this.SetValue(EmbeddedBalloon.IsConnectorCenteredPropertyKey, finalPlacement == chosenPlacement);
			this.SetValue(EmbeddedBalloon.ConnectorSidePropertyKey, dockSides[placementIndex].Side);
		}

		/// <summary>
		/// Пытается исправить размещение таким образом, чтобы оно помещалось в заданную область.
		/// </summary>
		/// <param name="sourcePlacement">Размещение, которое нужно разместить в заданной области.</param>
		/// <param name="placementArea">Область, в которой можно разместить заданный прямоугольник.</param>
		/// <param name="dockSide">Сторона прикрепления коннектора.</param>
		/// <returns>Исправленное размещение, если его удалось поместить в заданную область; иначе исходное размещение.</returns>
		private Rect CorrectPlacement(Rect sourcePlacement, Rect placementArea, Dock dockSide)
		{
			var deflatedSourcePlacement = Helper.DeflateRect(
				sourcePlacement, 
				new Thickness(
					dockSide == Dock.Left ? this.ConnectorSize : 0d,
					dockSide == Dock.Top ? this.ConnectorSize : 0d,
					dockSide == Dock.Right ? this.ConnectorSize : 0d,
					dockSide == Dock.Bottom ? this.ConnectorSize : 0d));

			var union = Rect.Union(deflatedSourcePlacement, placementArea);
			var intersect = Rect.Intersect(this.FlowAround, placementArea);
			if (union != placementArea)
			{
				if (dockSide == Dock.Bottom || dockSide == Dock.Top)
				{
					if (union.Left < placementArea.Left)
					{
						var offset = this.FlowAround.Left < placementArea.Left
							? this.FlowAround.Left - union.Left
							: placementArea.Left - union.Left;
						sourcePlacement.Offset(offset, 0);
					}
					if (union.Right > placementArea.Right)
					{
						var offset = this.FlowAround.Right > placementArea.Right
							? this.FlowAround.Right - union.Right
							: placementArea.Right - union.Right;
						sourcePlacement.Offset(offset, 0);
					}
				}
				if (dockSide == Dock.Left || dockSide == Dock.Right)
				{
					if (union.Top < placementArea.Top)
					{
						var offset = this.FlowAround.Top < placementArea.Top
							? this.FlowAround.Top - union.Top
							: placementArea.Top - union.Top;
						sourcePlacement.Offset(0, Math.Round(offset));
					}
					if (union.Bottom > placementArea.Bottom)
					{
						var offset = this.FlowAround.Bottom > placementArea.Bottom
							? this.FlowAround.Bottom - union.Bottom
							: placementArea.Bottom - union.Bottom;
						sourcePlacement.Offset(0, offset);
					}
				}
			}

			return sourcePlacement;
		}

		/// <summary>
		/// Вызывает событие <see cref="FrameworkElement.SizeChanged"/> с заданными параметрами.
		/// </summary>
		/// <param name="sizeInfo">Описывает специфику изменений размеров.</param>
		protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
		{
			base.OnRenderSizeChanged(sizeInfo);
			this.ComputePosition();
		}

		/// <summary>
		/// Обрабатывает изменение значения свойства <see cref="FlowAround"/>.
		/// </summary>
		/// <param name="dependencyObject"><see cref="DependencyObject"/>, значение свойства которого изменилось.</param>
		/// <param name="e">Объект, содержащий аргументы события.</param>
		private static void OnFlowAroundPropertyChanged(DependencyObject dependencyObject,
			DependencyPropertyChangedEventArgs e)
		{
			((EmbeddedBalloon)dependencyObject).ComputePosition();
		}

		#region PlacementRectangle Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="PlacementRectangle"/>.
		/// </summary>
		public static readonly DependencyProperty PlacementRectangleProperty = Balloon.PlacementRectangleProperty.AddOwner(
			typeof(EmbeddedBalloon), new FrameworkPropertyMetadata(EmbeddedBalloon.OnFlowAroundPropertyChanged, null));

		/// <summary>
		/// Возвращает или присваивает прямоугольник, относительно которого элемент управления
		/// <see cref="EmbeddedBalloon"/> размещается при показе.
		/// </summary>
		public Rect PlacementRectangle
		{
			get
			{
				return (Rect)this.GetValue(EmbeddedBalloon.PlacementRectangleProperty);
			}
			set
			{
				this.SetValue(EmbeddedBalloon.PlacementRectangleProperty, value);
			}
		}

		#endregion

		#region PlacementTarget Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="PlacementTarget"/>.
		/// </summary>
		public static readonly DependencyProperty PlacementTargetProperty = Balloon.PlacementTargetProperty.AddOwner(
			typeof(EmbeddedBalloon), new FrameworkPropertyMetadata(EmbeddedBalloon.OnFlowAroundPropertyChanged, null));

		/// <summary>
		/// Возвращает или присваивает элемент, относительно которого элемент управления <see cref="EmbeddedBalloon"/>
		/// размещается при показе.
		/// </summary>
		public UIElement PlacementTarget
		{
			get
			{
				return (UIElement)this.GetValue(EmbeddedBalloon.PlacementTargetProperty);
			}
			set
			{
				this.SetValue(EmbeddedBalloon.PlacementTargetProperty, value);
			}
		}

		#endregion

		#region IsConnectorVisible Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="IsConnectorVisible"/>.
		/// </summary>
		public static readonly DependencyProperty IsConnectorVisibleProperty = Balloon.IsConnectorVisibleProperty.AddOwner(
			typeof(EmbeddedBalloon),
			new FrameworkPropertyMetadata(
				Balloon.IsConnectorVisibleProperty.DefaultMetadata.DefaultValue,
				FrameworkPropertyMetadataOptions.AffectsMeasure
				| FrameworkPropertyMetadataOptions.AffectsArrange
				| FrameworkPropertyMetadataOptions.AffectsRender));

		/// <summary>
		/// Возвращает или присваивает значение, указывающее, показывает ли <see cref="EmbeddedBalloon"/>
		/// связь с областью, определяемой <see cref="FlowAround"/>.
		/// </summary>
		public bool IsConnectorVisible
		{
			get
			{
				return (bool)this.GetValue(EmbeddedBalloon.IsConnectorVisibleProperty);
			}
			set
			{
				this.SetValue(EmbeddedBalloon.IsConnectorVisibleProperty, value);
			}
		}

		#endregion

		#region HasDropShadow Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="HasDropShadow"/>.
		/// </summary>
		public static readonly DependencyProperty HasDropShadowProperty =
			Balloon.HasDropShadowProperty.AddOwner(typeof(EmbeddedBalloon));

		/// <summary>
		/// Возвращает или присваивает значение, указывающее, отображается ли <see cref="EmbeddedBalloon"/> с эффектом отбрасывания тени.
		/// </summary>
		public bool HasDropShadow
		{
			get
			{
				return (bool)this.GetValue(EmbeddedBalloon.HasDropShadowProperty);
			}
			set
			{
				this.SetValue(EmbeddedBalloon.HasDropShadowProperty, value);
			}
		}

		#endregion

		#region HorizontalPosition Dependency Property

		/// <summary>
		/// Идентификатор ключа свойства зависимостей <see cref="HorizontalPosition"/>.
		/// </summary>
		private static readonly DependencyPropertyKey HorizontalPositionPropertyKey = DependencyProperty.RegisterReadOnly(
			"HorizontalPosition", typeof(double), typeof(EmbeddedBalloon),
			new FrameworkPropertyMetadata(Helper.DoubleZero));

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="HorizontalPosition"/>.
		/// </summary>
		public static readonly DependencyProperty HorizontalPositionProperty = EmbeddedBalloon.HorizontalPositionPropertyKey.DependencyProperty;

		/// <summary>
		/// Возвращает позицию <see cref="EmbeddedBalloon"/> по горизонтали относительно начала координат целевого объекта.
		/// </summary>
		public double HorizontalPosition
		{
			get
			{
				return (double)this.GetValue(EmbeddedBalloon.HorizontalPositionProperty);
			}
		}

		#endregion

		#region VerticalPosition Dependency Property

		/// <summary>
		/// Идентификатор ключа свойства зависимостей <see cref="VerticalPosition"/>.
		/// </summary>
		private static readonly DependencyPropertyKey VerticalPositionPropertyKey = DependencyProperty.RegisterReadOnly(
			"VerticalPosition", typeof(double), typeof(EmbeddedBalloon),
			new FrameworkPropertyMetadata(Helper.DoubleZero));

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="VerticalPosition"/>.
		/// </summary>
		public static readonly DependencyProperty VerticalPositionProperty = EmbeddedBalloon.VerticalPositionPropertyKey.DependencyProperty;

		/// <summary>
		/// Возвращает позицию <see cref="EmbeddedBalloon"/> по вертикали относительно начала координат целевого объекта.
		/// </summary>
		public double VerticalPosition
		{
			get
			{
				return (double)this.GetValue(EmbeddedBalloon.VerticalPositionProperty);
			}
		}

		#endregion

		#region LeftDockPriority Dependency Property

		/// <summary>
		/// Возвращает или присваивает приоритет варианта размещения, 
		/// в котором точка присоединения находится левее balloon'а.
		/// Отрицательное значение исключает этот вариант размещения. 
		/// Это свойство зависимостей.
		/// </summary>
		public int LeftDockPriority
		{
			get
			{
				return (int)this.GetValue(EmbeddedBalloon.LeftDockPriorityProperty);
			}
			set
			{
				this.SetValue(EmbeddedBalloon.LeftDockPriorityProperty, value);
			}
		}

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="LeftDockPriority"/>.
		/// </summary>
		public static readonly DependencyProperty LeftDockPriorityProperty =
			DependencyProperty.Register("LeftDockPriority",
				typeof(int),
				typeof(EmbeddedBalloon),
				new FrameworkPropertyMetadata(1, EmbeddedBalloon.DockPriorityChanged));

		#endregion

		#region TopDockPriority Dependency Property

		/// <summary>
		/// Возвращает или присваивает приоритет варианта размещения, 
		/// в котором точка присоединения находится выше balloon'а.
		/// Отрицательное значение исключает этот вариант размещения. 
		/// Это свойство зависимостей.
		/// </summary>
		public int TopDockPriority
		{
			get
			{
				return (int)this.GetValue(EmbeddedBalloon.TopDockPriorityProperty);
			}
			set
			{
				this.SetValue(EmbeddedBalloon.TopDockPriorityProperty, value);
			}
		}

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="TopDockPriority"/>.
		/// </summary>
		public static readonly DependencyProperty TopDockPriorityProperty =
			DependencyProperty.Register("TopDockPriority",
				typeof(int),
				typeof(EmbeddedBalloon),
				new FrameworkPropertyMetadata(3, EmbeddedBalloon.DockPriorityChanged));

		#endregion

		#region RightDockPriority Dependency Property

		/// <summary>
		/// Возвращает или присваивает приоритет варианта размещения, 
		/// в котором точка присоединения находится правее balloon'а.
		/// Отрицательное значение исключает этот вариант размещения. 
		/// Это свойство зависимостей.
		/// </summary>
		public int RightDockPriority
		{
			get
			{
				return (int)this.GetValue(EmbeddedBalloon.RightDockPriorityProperty);
			}
			set
			{
				this.SetValue(EmbeddedBalloon.RightDockPriorityProperty, value);
			}
		}

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="RightDockPriority"/>.
		/// </summary>
		public static readonly DependencyProperty RightDockPriorityProperty =
			DependencyProperty.Register("RightDockPriority",
				typeof(int),
				typeof(EmbeddedBalloon),
				new FrameworkPropertyMetadata(0, EmbeddedBalloon.DockPriorityChanged));

		#endregion

		#region BottomDockPriority Dependency Property

		/// <summary>
		/// Возвращает или присваивает приоритет варианта размещения, 
		/// в котором точка присоединения находится ниже balloon'а.
		/// Отрицательное значение исключает этот вариант размещения. 
		/// Это свойство зависимостей.
		/// </summary>
		public int BottomDockPriority
		{
			get
			{
				return (int)this.GetValue(EmbeddedBalloon.BottomDockPriorityProperty);
			}
			set
			{
				this.SetValue(EmbeddedBalloon.BottomDockPriorityProperty, value);
			}
		}

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="BottomDockPriority"/>.
		/// </summary>
		public static readonly DependencyProperty BottomDockPriorityProperty =
			DependencyProperty.Register("BottomDockPriority",
				typeof(int),
				typeof(EmbeddedBalloon),
				new FrameworkPropertyMetadata(2, EmbeddedBalloon.DockPriorityChanged));

		#endregion

		/// <summary>
		/// Called when one of <see cref="LeftDockPriorityProperty"/>, <see cref="TopDockPriorityProperty"/>, 
		/// <see cref="RightDockPriorityProperty"/>, <see cref="BottomDockPriorityProperty"/> changes.
		/// </summary>
		/// <param name="dependencyObject">Dependency object, whose property has been changed.</param>
		/// <param name="e">Event arguments.</param>
		private static void DockPriorityChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
		{
			((EmbeddedBalloon)dependencyObject).ComputePosition();
		}

		#region FlowAround Property

		/// <summary>
		/// Возвращает или присваивает прямоугольную область, 
		/// вокруг которой позиционируется <see cref="EmbeddedBalloon"/>.
		/// Это свойство зависимостей.
		/// </summary>
		public Rect FlowAround
		{
			get 
			{
				return (Rect)this.GetValue(EmbeddedBalloon.FlowAroundProperty); 
			}
			set 
			{ 
				this.SetValue(EmbeddedBalloon.FlowAroundProperty, value); 
			}
		}

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="FlowAround"/>.
		/// </summary>
		public static readonly DependencyProperty FlowAroundProperty =
			DependencyProperty.Register(
				"FlowAround", 
				typeof(Rect), 
				typeof(EmbeddedBalloon),
				new FrameworkPropertyMetadata(EmbeddedBalloon.OnFlowAroundPropertyChanged));

		#endregion

		#region IsConnectorCentered Readonly Dependency Property

		/// <summary>
		/// Идентификатор ключа свойства зависимостей <see cref="IsConnectorCentered"/>.
		/// </summary>
		public static readonly DependencyPropertyKey IsConnectorCenteredPropertyKey =
			DependencyProperty.RegisterReadOnly("IsConnectorCentered",
				typeof(bool),
				typeof(EmbeddedBalloon),
				new FrameworkPropertyMetadata(false));

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="IsConnectorCentered"/>.
		/// </summary>
		public static readonly DependencyProperty IsConnectorCenteredProperty = EmbeddedBalloon.IsConnectorCenteredPropertyKey.DependencyProperty;

		/// <summary>
		/// Возвращает признак того, что коннектор находится по середине стыкуемой стороны.
		/// Это свойство зависимостей.
		/// </summary>
		public bool IsConnectorCentered
		{
			get 
			{
				return (bool)this.GetValue(EmbeddedBalloon.IsConnectorCenteredProperty); 
			}
		}

		#endregion

		#region ConnectorSize Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="ConnectorSize"/>.
		/// </summary>
		public static readonly DependencyProperty ConnectorSizeProperty = 
			Balloon.ConnectorSizeProperty.AddOwner(typeof(EmbeddedBalloon));

		/// <summary>
		/// Возвращает или присваивает значение, определяющее размер, выделяемый для рендеринга связи.
		/// Это свойство зависимостей.
		/// </summary>
		public double ConnectorSize
		{
			get
			{
				return (double)this.GetValue(EmbeddedBalloon.ConnectorSizeProperty);
			}
			set
			{
				this.SetValue(EmbeddedBalloon.ConnectorSizeProperty, value);
			}
		}

		#endregion

		#region ConnectorSide Dependency Property

		/// <summary>
		/// Ключ идентификатора свойства зависимостей <see cref="ConnectorSide"/>.
		/// </summary>
		private static readonly DependencyPropertyKey ConnectorSidePropertyKey = DependencyProperty.RegisterReadOnly(
			"ConnectorSide",
			typeof(Dock),
			typeof(EmbeddedBalloon),
			new FrameworkPropertyMetadata(default(Dock), FrameworkPropertyMetadataOptions.AffectsRender));

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="ConnectorSide"/>.
		/// </summary>
		public static readonly DependencyProperty ConnectorSideProperty = EmbeddedBalloon.ConnectorSidePropertyKey.DependencyProperty;

		/// <summary>
		/// Возвращает или присваивает сторону, с которой отображается коннектор.
		/// Это свойство зависимостей.
		/// </summary>
		public Dock ConnectorSide
		{
			get
			{
				return (Dock)this.GetValue(EmbeddedBalloon.ConnectorSideProperty);
			}
		}

		#endregion
	}
}
