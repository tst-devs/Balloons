using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace TST.Phoenix.Arm.Controls
{
	/// <summary>
	/// Представляет собой всплывающий контрол, отображающий связь с заданным
	/// элементом пользовательского интерфейса.
	/// </summary>
	[Obsolete("Useless and incompleted control. Use EmbeddedBalloon instead.")]
	public class Balloon : ContentControl
	{
		/// <summary>
		/// <see cref="Popup"/>, используемый для хостинга <see cref="Balloon"/> в <see cref="PresentationSource"/>.
		/// </summary>
		private Popup _rootPopup;

		/// <summary>
		/// Инициализирует класс <see cref="Balloon"/>.
		/// </summary>
		static Balloon()
		{
			Balloon.DefaultStyleKeyProperty.OverrideMetadata(
				typeof(Balloon),
				new FrameworkPropertyMetadata(typeof(Balloon)));
		}

		/// <summary>
		/// Обрабатывает изменение значения свойства <see cref="IsOpen"/>.
		/// </summary>
		/// <param name="e">Объект, содержащий аргументы события изменения значения свойства.</param>
		private void OnIsOpenChanged(DependencyPropertyChangedEventArgs e)
		{
			var isOpen = (bool)e.NewValue;

			// Если хост еще не создан, создаем его.
			if (isOpen && this._rootPopup == null)
			{
				this._rootPopup = new Popup { AllowsTransparency = true };
				Popup.CreateRootPopup(this._rootPopup, this);

				// CreateRootPopup устанавливает однонаправленную привязку IsOpen.
				// Это не самый подходящий вариант для Balloon, поэтому переопределяем ее, используя режим TwoWay.
				var isOpenBinding = new Binding(Popup.IsOpenProperty.Name)
				{
					Mode = BindingMode.TwoWay,
					Source = this
				};
				BindingOperations.SetBinding(this._rootPopup, Popup.IsOpenProperty, isOpenBinding);
			}

			// Вызываем событие, соответствующее новому значению свойства.
			var correspondingEvent = isOpen ? Balloon.OpenedEvent : Balloon.ClosedEvent;
			this.RaiseEvent(new RoutedEventArgs(correspondingEvent, this));
		}

		/// <summary>
		/// Задает набор размещений всплывающего элемента <see cref="Balloon"/> по умолчанию.
		/// </summary>
		/// <param name="popupSize">Размер всплывающего элемента.</param>
		/// <param name="targetSize">Размер элемента, относительно которого позиционируется всплывающий элемент.</param>
		/// <param name="flowAround">Прямоугольная область, которую всплывающий элемент должен обтекать.</param>
		/// <param name="leftDockPriority">Приоритет варианта размещения, 
		/// в котором точка присоединения находится левее balloon'а.</param>
		/// <param name="topDockPriority">Приоритет варианта размещения, 
		/// в котором точка присоединения находится выше balloon'а.</param>
		/// <param name="rightDockPriority">Приоритет варианта размещения, 
		/// в котором точка присоединения находится правее balloon'а.</param>
		/// <param name="bottomDockPriority">Приоритет варианта размещения, 
		/// в котором точка присоединения находится ниже balloon'а.</param>
		/// <returns>Набор размещений всплывающего элемента.</returns>
		internal static CustomPopupPlacement[] BalloonPlacementCallback(Size popupSize, Size targetSize, Rect flowAround,
			int leftDockPriority, int topDockPriority, int rightDockPriority, int bottomDockPriority)
		{
			/*
			var half = new Size(targetSize.Width / 2, targetSize.Height / 2);
			var center = new Point(half.Width - popupSize.Width / 2, half.Height - popupSize.Height / 2);

			var floatRectCenter = new Point(flowAround.X + flowAround.Width / 2, flowAround.Top + flowAround.Height / 2);

			// Задаем четыре возможных положения всплывающего элемента,
			// позиционируя его по центру в направляющей плоскости.
			var placements = new[] 
			{
				new CustomPopupPlacement(new Point(floatRectCenter.X - popupSize.Width / 2, flowAround.Bottom), PopupPrimaryAxis.Horizontal), 
				new CustomPopupPlacement(new Point(floatRectCenter.X - popupSize.Width / 2, flowAround.Top - popupSize.Height), PopupPrimaryAxis.Horizontal),
				new CustomPopupPlacement(new Point(flowAround.Right, floatRectCenter.Y - popupSize.Height / 2), PopupPrimaryAxis.Vertical),
				new CustomPopupPlacement(new Point(flowAround.Left - popupSize.Width, floatRectCenter.Y - popupSize.Height / 2), PopupPrimaryAxis.Vertical)
			};

			// Порядок четырех возможных положений, описанных выше.
			var priorities = new[]
			{
				topDockPriority, 
				bottomDockPriority, 
				leftDockPriority, 
				rightDockPriority
			};

			// Сортируем возможные положения согласно заданным приоритетам.
			return placements
				.Zip(priorities, (pl, pr) => new { Placement = pl, Priority = pr })
				.Where(a => a.Priority >= 0)
				.OrderByDescending(anonymous => anonymous.Priority)
				.Select(anonymous => anonymous.Placement)
				.ToArray();
			 */
			var half = new Size(targetSize.Width / 2, targetSize.Height / 2);
			var center = new Point(half.Width - popupSize.Width / 2, half.Height - popupSize.Height / 2);

			var floatRectCenter = new Point(flowAround.X + flowAround.Width / 2, flowAround.Top + flowAround.Height / 2);

			// Задаем четыре возможных положения всплывающего элемента,
			// позиционируя его по центру в направляющей плоскости.
			var placements = new[] 
			{
				new CustomPopupPlacement(new Point(center.X + floatRectCenter.X, half.Height + flowAround.Bottom), PopupPrimaryAxis.Horizontal), 
				new CustomPopupPlacement(new Point(center.X + floatRectCenter.X, half.Height - popupSize.Height + flowAround.Top), PopupPrimaryAxis.Horizontal),
				new CustomPopupPlacement(new Point(half.Width + flowAround.Right, center.Y + floatRectCenter.Y), PopupPrimaryAxis.Vertical),
				new CustomPopupPlacement(new Point(half.Width - popupSize.Width + flowAround.Left, center.Y + floatRectCenter.Y), PopupPrimaryAxis.Vertical)
			};

			// Порядок четырех возможных положений, описанных выше.
			var priorities = new[]
			{
				topDockPriority, 
				bottomDockPriority, 
				leftDockPriority, 
				rightDockPriority
			};

			// Сортируем возможные положения согласно заданным приоритетам.
			return placements
				.Zip(priorities, (pl, pr) => new { Placement = pl, Priority = pr })
				.Where(a => a.Priority >= 0)
				.OrderByDescending(anonymous => anonymous.Priority)
				.Select(anonymous => anonymous.Placement)
				.ToArray(); 
		}

		/// <summary>
		/// Корректирует значение свойства, которое принимает значение по умолчанию,
		/// когда <see cref="IsConnectorVisible"/> содержит <langword>true</langword>.
		/// </summary>
		/// <param name="dependencyProperty">Идентификатор свойства, которое корректируется.</param>
		/// <param name="baseValue">Значение свойства до корректировок.</param>
		/// <returns>Скорректированное значение свойства.</returns>
		private object CoerceToDefaultWhenConnected(DependencyProperty dependencyProperty, object baseValue)
		{
			if (this.IsConnectorVisible)
			{
				return dependencyProperty.GetMetadata(typeof(Balloon)).DefaultValue;
			}

			return baseValue;
		}

		#region Placement Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="Placement"/>.
		/// </summary>
		public static readonly DependencyProperty PlacementProperty = Popup.PlacementProperty.AddOwner(
			typeof(Balloon),
			new FrameworkPropertyMetadata(
				PlacementMode.Custom,
				null,
				(d, v) => ((Balloon)d).CoerceToDefaultWhenConnected(Balloon.PlacementProperty, v)));

		/// <summary>
		/// Возвращает или присваивает ориентацию элемента управления <see cref="Balloon"/> при его открытии
		/// и определяет поведение элемента управления <see cref="Balloon"/>, когда он пересекает границы экрана.
		/// </summary>
		public PlacementMode Placement
		{
			get
			{
				return (PlacementMode)this.GetValue(Balloon.PlacementProperty);
			}
			set
			{
				this.SetValue(Balloon.PlacementProperty, value);
			}
		}

		#endregion

		#region PlacementRectangle Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="PlacementRectangle"/>.
		/// </summary>
		public static readonly DependencyProperty PlacementRectangleProperty = Popup.PlacementRectangleProperty.AddOwner(
			typeof(Balloon),
			new FrameworkPropertyMetadata(
				null,
				(d, v) => ((Balloon)d).CoerceToDefaultWhenConnected(Balloon.PlacementRectangleProperty, v)));

		/// <summary>
		/// Возвращает или присваивает прямоугольник, относительно которого элемент управления <see cref="Balloon"/> размещается при открытии.
		/// </summary>
		public Rect PlacementRectangle
		{
			get
			{
				return (Rect)this.GetValue(Balloon.PlacementRectangleProperty);
			}
			set
			{
				this.SetValue(Balloon.PlacementRectangleProperty, value);
			}
		}

		#endregion

		#region PlacementTarget Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="PlacementTarget"/>.
		/// </summary>
		public static readonly DependencyProperty PlacementTargetProperty =
			Popup.PlacementTargetProperty.AddOwner(typeof(Balloon));

		/// <summary>
		/// Возвращает или присваивает элемент, относительно которого элемент управления <see cref="Balloon"/> размещается при открытии.
		/// </summary>
		public UIElement PlacementTarget
		{
			get
			{
				return (UIElement)this.GetValue(Balloon.PlacementTargetProperty);
			}
			set
			{
				this.SetValue(Balloon.PlacementTargetProperty, value);
			}
		}

		#endregion

		#region HorizontalOffset Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="HorizontalOffset"/>.
		/// </summary>
		public static readonly DependencyProperty HorizontalOffsetProperty =
			Popup.HorizontalOffsetProperty.AddOwner(typeof(Balloon));

		/// <summary>
		/// Возвращает или присваивает расстояние по горизонтали между началом координат целевого объекта
		/// и точкой выравнивания всплывающего элемента.
		/// </summary>
		public double HorizontalOffset
		{
			get
			{
				return (double)this.GetValue(Balloon.HorizontalOffsetProperty);
			}
			set
			{
				this.SetValue(Balloon.HorizontalOffsetProperty, value);
			}
		}

		#endregion

		#region VerticalOffset Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="VerticalOffset"/>.
		/// </summary>
		public static readonly DependencyProperty VerticalOffsetProperty =
			Popup.VerticalOffsetProperty.AddOwner(typeof(Balloon));

		/// <summary>
		/// Возвращает или присваивает расстояние по вертикали между началом координат целевого объекта
		/// и точкой выравнивания всплывающего элемента.
		/// </summary>
		public double VerticalOffset
		{
			get
			{
				return (double)this.GetValue(Balloon.VerticalOffsetProperty);
			}
			set
			{
				this.SetValue(Balloon.VerticalOffsetProperty, value);
			}
		}

		#endregion

		#region CustomPopupPlacementCallback Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="CustomPopupPlacementCallback"/>.
		/// </summary>
		public static readonly DependencyProperty CustomPopupPlacementCallbackProperty = Popup.CustomPopupPlacementCallbackProperty.AddOwner(
			typeof(Balloon),
			new FrameworkPropertyMetadata(
				new CustomPopupPlacementCallback((popupSize, targetSize, offset) =>
					Balloon.BalloonPlacementCallback(popupSize, targetSize, new Rect(offset, offset), 3, 1, 4, 2)),
				(d, v) => ((Balloon)d).CoerceToDefaultWhenConnected(Balloon.CustomPopupPlacementCallbackProperty, v)));

		/// <summary>
		/// Возвращает или присваивает делегат обработчика, размещающего элемент управления <see cref="Balloon"/>.
		/// </summary>
		public CustomPopupPlacementCallback CustomPopupPlacementCallback
		{
			get
			{
				return (CustomPopupPlacementCallback)this.GetValue(Balloon.CustomPopupPlacementCallbackProperty);
			}
			set
			{
				this.SetValue(Balloon.CustomPopupPlacementCallbackProperty, value);
			}
		}

		#endregion

		#region StaysOpen Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="StaysOpen"/>.
		/// </summary>
		public static readonly DependencyProperty StaysOpenProperty = Popup.StaysOpenProperty.AddOwner(
			typeof(Balloon), new FrameworkPropertyMetadata(false));

		/// <summary>
		/// Возвращает или присваивает значение, указывающее, закрывается ли элемент управления <see cref="Balloon"/>, когда теряет фокус.
		/// </summary>
		public bool StaysOpen
		{
			get
			{
				return (bool)this.GetValue(Balloon.StaysOpenProperty);
			}
			set
			{
				this.SetValue(Balloon.StaysOpenProperty, value);
			}
		}

		#endregion

		#region IsOpen Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="IsOpen"/>.
		/// </summary>
		public static readonly DependencyProperty IsOpenProperty = Popup.IsOpenProperty.AddOwner(
			typeof(Balloon),
			new FrameworkPropertyMetadata((d, e) => ((Balloon)d).OnIsOpenChanged(e)));

		/// <summary>
		/// Возвращает или присваивает значение, указывающее, является ли <see cref="Balloon"/> видимым.
		/// </summary>
		public bool IsOpen
		{
			get
			{
				return (bool)this.GetValue(Balloon.IsOpenProperty);
			}
			set
			{
				this.SetValue(Balloon.IsOpenProperty, value);
			}
		}

		#endregion

		#region IsConnectorVisible Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="IsConnectorVisible"/>.
		/// </summary>
		public static readonly DependencyProperty IsConnectorVisibleProperty = DependencyProperty.Register(
			"IsConnectorVisible", typeof(bool), typeof(Balloon),
			new FrameworkPropertyMetadata(
				true,
				(d, e) =>
				{
					d.CoerceValue(Balloon.PlacementProperty);
					d.CoerceValue(Balloon.PlacementRectangleProperty);
					d.CoerceValue(Balloon.CustomPopupPlacementCallbackProperty);
				}));

		/// <summary>
		/// Возвращает или присваивает значение, указывающее, показывает ли <see cref="Balloon"/>
		/// связь с элементом, заданным в <see cref="PlacementTarget"/>.
		/// </summary>
		public bool IsConnectorVisible
		{
			get
			{
				return (bool)this.GetValue(Balloon.IsConnectorVisibleProperty);
			}
			set
			{
				this.SetValue(Balloon.IsConnectorVisibleProperty, value);
			}
		}

		#endregion

		#region ConnectorSize Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="ConnectorSize"/>.
		/// </summary>
		public static readonly DependencyProperty ConnectorSizeProperty = DependencyProperty.Register(
			"ConnectorSize", typeof(double), typeof(Balloon),
			new FrameworkPropertyMetadata(
				12d,
				FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender,
				(d, e) => { },
				(d, v) => ((Balloon)d).IsConnectorVisible ? v : 0d));

		/// <summary>
		/// Возвращает или присваивает значение, определяющее размер, выделяемый для рендеринга связи.
		/// </summary>
		public double ConnectorSize
		{
			get
			{
				return (double)this.GetValue(Balloon.ConnectorSizeProperty);
			}
			set
			{
				this.SetValue(Balloon.ConnectorSizeProperty, value);
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
			typeof(Balloon),
			new FrameworkPropertyMetadata(default(Dock), FrameworkPropertyMetadataOptions.AffectsRender));

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="ConnectorSide"/>.
		/// </summary>
		public static readonly DependencyProperty ConnectorSideProperty =
			Balloon.ConnectorSidePropertyKey.DependencyProperty;

		/// <summary>
		/// Возвращает или присваивает сторону, с которой отображается коннектор.
		/// Это свойство зависимостей.
		/// </summary>
		public Dock ConnectorSide
		{
			get
			{
				return (Dock)this.GetValue(Balloon.ConnectorSideProperty);
			}
		}

		#endregion

		#region HasDropShadow Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="HasDropShadow"/>.
		/// </summary>
		public static readonly DependencyProperty HasDropShadowProperty = System.Windows.Controls.ToolTip.HasDropShadowProperty.AddOwner(typeof(Balloon));

		/// <summary>
		/// Возвращает или присваивает значение, указывающее, отображается ли <see cref="Balloon"/> с эффектом отбрасывания тени.
		/// </summary>
		public bool HasDropShadow
		{
			get
			{
				return (bool)this.GetValue(Balloon.HasDropShadowProperty);
			}
			set
			{
				this.SetValue(Balloon.HasDropShadowProperty, value);
			}
		}

		#endregion

		#region Opened Routed Event

		/// <summary>
		/// Идентификатор маршрутизируемого события <see cref="Opened"/>.
		/// </summary>
		public static readonly RoutedEvent OpenedEvent = ContextMenu.OpenedEvent.AddOwner(typeof(Balloon));

		/// <summary>
		/// Возникает при показе <see cref="Balloon"/> на экране.
		/// </summary>
		public event RoutedEventHandler Opened
		{
			add
			{
				this.AddHandler(Balloon.OpenedEvent, value);
			}
			remove
			{
				this.RemoveHandler(Balloon.OpenedEvent, value);
			}
		}

		#endregion

		#region Closed Routed Event

		/// <summary>
		/// Идентификатор маршрутизируемого события <see cref="Closed"/>.
		/// </summary>
		public static readonly RoutedEvent ClosedEvent = ContextMenu.ClosedEvent.AddOwner(typeof(Balloon));

		/// <summary>
		/// Возникает при сокрытии <see cref="Balloon"/> с экрана.
		/// </summary>
		public event RoutedEventHandler Closed
		{
			add
			{
				this.AddHandler(Balloon.ClosedEvent, value);
			}
			remove
			{
				this.RemoveHandler(Balloon.ClosedEvent, value);
			}
		}

		#endregion
	}
}
