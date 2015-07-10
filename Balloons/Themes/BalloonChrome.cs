using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using TST.Phoenix.Arm.Controls;
using TST.Phoenix.Arm.Utility;

namespace TST.Phoenix.Arm.Themes
{
	/// <summary>
	/// Декоратор всплывающего элемента, отображающий его связь с заданным элементом пользовательского интерфейса.
	/// </summary>
	public class BalloonChrome : Decorator
	{
		/// <summary>
		/// Интервал включения рендеринга коннектора после показа всплывающего элемента.
		/// </summary>
		private static readonly TimeSpan _DelayRenderInterval = TimeSpan.FromMilliseconds(30);
		
		/// <summary>
		/// Интервал проверок изменения положения всплывающего элемента.
		/// </summary>
		private static readonly TimeSpan _CheckPositionInterval = TimeSpan.FromMilliseconds(20);

		/// <summary>
		/// Радиус скругления углов области содержимого декоратора.
		/// </summary>
		private const double CornerRadius = 2;

		/// <summary>
		/// Указывает, что всплывающий элемент связан с другим элементом пользовательского интерфейса.
		/// </summary>
		private bool _isConnected;

		/// <summary>
		/// Указывает, что декоратор должен рендерить коннектор связи всплывающего элемента.
		/// </summary>
		private bool _renderConnector;

		/// <summary>
		/// Таймер для корректировки декоратора в соответствии с измененями позиции всплывающего элемента.
		/// </summary>
		private readonly DispatcherTimer _popupWatchTimer;

		/// <summary>
		/// Инициализирует новый объект класса <see cref="BalloonChrome"/>.
		/// </summary>
		public BalloonChrome()
		{
			this._popupWatchTimer = new DispatcherTimer(DispatcherPriority.Render);
			this._popupWatchTimer.Tick += this.PopupWatchTimer_Tick;
		}

		/// <summary>
		/// Вызывается, когда требуется определить размер <see cref="BalloonChrome"/>.
		/// </summary>
		/// <param name="constraintSize">Максимальный размер, которым ограничен декоратор.</param>
		/// <returns>Размер требуемый декоратору для компоновки.</returns>
		protected override Size MeasureOverride(Size constraintSize)
		{
			var padding = this.GetTotalPadding();

			// Уменьшаем размер с учетом отступов.
			constraintSize.Width = Math.Max(0, constraintSize.Width - padding.Left - padding.Right);
			constraintSize.Height = Math.Max(0, constraintSize.Height - padding.Top - padding.Bottom);

			// Измеряем декоратор базовым методом.
			var result = base.MeasureOverride(constraintSize);

			// Добавляем отступы к результату.
			result.Width += padding.Left + padding.Right;
			result.Height += padding.Top + padding.Bottom;

			return result;
		}

		/// <summary>
		/// Компонует содержимое <see cref="BalloonChrome"/>.
		/// </summary>
		/// <param name="arrangeSize">Размер, выделенный родительским элементом для размещения декоратора.</param>
		/// <returns>Размер, использованный для размещения декоратора.</returns>
		protected override Size ArrangeOverride(Size arrangeSize)
		{
			var child = this.Child;

			// Выравниваем содержимое с учетом отступов.
			if (child != null)
			{
				child.Arrange(Helper.DeflateRect(new Rect(arrangeSize), this.GetTotalPadding()));
			}

			return arrangeSize;
		}

		/// <summary>
		/// Задает содержимое <see cref="DrawingContext"/> для рендеринга.
		/// </summary>
		/// <param name="dc"><see cref="DrawingContext"/> для рендеринга.</param>
		protected override void OnRender(DrawingContext dc)
		{
			// Если декоратор присоединен, но рендеринг коннектора не включен,
			// оставляем DrawingContext пустым.
			if (this._isConnected && !this._renderConnector)
			{
				return;
			}

			const double borderThickness = 1;

			var background = this.Background;
			var borderBrush = this.BorderBrush;
			var connectorSize = this.ConnectorSize;
			var connectorSide = this.ConnectorSide;

			// Если задана кисть окантовки, потребуется перо.
			Pen borderPen = null;
			if (borderBrush != null)
			{
				borderPen = new Pen(borderBrush, borderThickness);
				borderPen.Freeze();
			}

			// Определяем размер области с содержимым.
			var contentBounds = Helper.DeflateRect(new Rect(this.RenderSize), this.BalloonPadding);
			Point connectorPoint;
			if (this._isConnected && this.IsConnectorVisible)
			{
				//contentBounds.Inflate(-connectorSize, -connectorSize);
				var thickness = new Thickness(
					connectorSide == Dock.Left ? connectorSize : 0,
					connectorSide == Dock.Top ? connectorSize : 0d,
					connectorSide == Dock.Right ? connectorSize : 0d,
					connectorSide == Dock.Bottom ? connectorSize : 0);
				contentBounds = Helper.DeflateRect(contentBounds, thickness);
			}			

			// Учитываем выравнивание к пикселам.
			var snapsToDevicePixels = this.SnapsToDevicePixels;
			if (snapsToDevicePixels)
			{
				var offset = borderThickness / 2;
				var guidelineSet = new GuidelineSet(
					new double[] { contentBounds.Left + offset, contentBounds.Right - offset },
					new double[] { contentBounds.Top + offset, contentBounds.Bottom - offset });
				guidelineSet.Freeze();
				dc.PushGuidelineSet(guidelineSet);
			}
			
			// Задаем содержимое DrawingContext.
			if (this._isConnected && this.IsConnectorVisible && this.TryGetConnectorPoint(out connectorPoint))
			{
				var contiguitySize = connectorSize * 1.5;

				#region Функции для ограничения координат, чтобы они не попадали на скругление углов

				Func<double, double> alignX =
					dbl => Math.Max(
						contentBounds.Left + BalloonChrome.CornerRadius,
						Math.Min(contentBounds.Right - BalloonChrome.CornerRadius, dbl));
				Func<double, double> alignY =
					dbl => Math.Max(
						contentBounds.Top + BalloonChrome.CornerRadius,
						Math.Min(contentBounds.Bottom - BalloonChrome.CornerRadius, dbl));

				#endregion

				// Рисуем фигуру от точки вершины коннектора, добавляя стороны по часовой стрелке.
				var contentGeom = new StreamGeometry();
				using (var ctx = contentGeom.Open())
				{
					switch (connectorSide)
					{
						case Dock.Left:
							{
								ctx.BeginFigure(
									connectorPoint,
									true, true);

								ctx.LineTo(
									new Point(
										contentBounds.Left,
										alignY(connectorPoint.Y - contiguitySize / 2)),
									true, true);

								BalloonChrome.AddSidesToRenderClockwise(Dock.Left, ctx, contentBounds);

								ctx.LineTo(
									new Point(
										contentBounds.Left,
										alignY(connectorPoint.Y + contiguitySize / 2)),
									true, true);

								break;
							}
						case Dock.Top:
							{
								ctx.BeginFigure(
									connectorPoint,
									true, true);

								ctx.LineTo(
									new Point(
										alignX(connectorPoint.X + contiguitySize / 2),
										contentBounds.Top),
									true, true);

								BalloonChrome.AddSidesToRenderClockwise(Dock.Top, ctx, contentBounds);

								ctx.LineTo(
									new Point(
										alignX(connectorPoint.X - contiguitySize / 2),
										contentBounds.Top),
									true, true);

								break;
							}
						case Dock.Right:
							{
								ctx.BeginFigure(
									connectorPoint,
									true, true);

								ctx.LineTo(
									new Point(
										contentBounds.Right,
										alignY(connectorPoint.Y + contiguitySize / 2)),
									true, true);

								BalloonChrome.AddSidesToRenderClockwise(Dock.Right, ctx, contentBounds);

								ctx.LineTo(
									new Point(
										contentBounds.Right,
										alignY(connectorPoint.Y - contiguitySize / 2)),
									true, true);

								break;
							}
						case Dock.Bottom:
							{
								ctx.BeginFigure(
									connectorPoint,
									true, true);

								ctx.LineTo(
									new Point(
										alignX(connectorPoint.X - contiguitySize / 2),
										contentBounds.Bottom),
									true, true);

								BalloonChrome.AddSidesToRenderClockwise(Dock.Bottom, ctx, contentBounds);

								ctx.LineTo(
									new Point(
										alignX(connectorPoint.X + contiguitySize / 2),
										contentBounds.Bottom),
									true, true);

								break;
							}
					}
				}

				contentGeom.Freeze();
				dc.DrawGeometry(background, borderPen, contentGeom);
			}
			else if (background != null || borderPen != null)
			{
				dc.DrawRoundedRectangle(background, borderPen, contentBounds, BalloonChrome.CornerRadius, BalloonChrome.CornerRadius);
			}

			// Учитываем выравнивание к пикселам.
			if (snapsToDevicePixels)
			{
				dc.Pop();
			}
		}

		/// <summary>
		/// Добавляет в контекст потоковой геометрии инструкции рендеринга сторон декоратора,
		/// начиная с заданной, по часовой стрелке.
		/// </summary>
		/// <param name="begin">Сторона, с которой начать рендеринг граней.</param>
		/// <param name="ctx">Контекст потоковой геометрии для добавления инструкций.</param>
		/// <param name="contentBounds">Прямоугольник, грани которого требуется отрендерить.</param>
		private static void AddSidesToRenderClockwise(Dock begin, StreamGeometryContext ctx, Rect contentBounds)
		{
			// Задаем порядок обхода и определяем с какой стороны начать.
			var order = new Dock[] { Dock.Left, Dock.Top, Dock.Right, Dock.Bottom };
			var beginIndex = Math.Max(0, Array.IndexOf(order, begin));

			// Обходим стороны, начиная с заданной.
			for (int i = 0; i < order.Length; i++)
			{
				// Для каждой стороны рисуется грань и скругление от конца грани
				// до начала следующей по направлению часовой стрелки.
				switch (order[(i + beginIndex) % order.Length])
				{
					case Dock.Left:
						{
							ctx.LineTo(
								new Point(contentBounds.Left, contentBounds.Top + BalloonChrome.CornerRadius),
								true, true);
							ctx.BezierTo(
								contentBounds.TopLeft,
								new Point(contentBounds.Left + BalloonChrome.CornerRadius, contentBounds.Top),
								new Point(contentBounds.Left + BalloonChrome.CornerRadius, contentBounds.Top),
								true, true);							
							
							break;
						}
					case Dock.Top:
						{
							ctx.LineTo(
								new Point(contentBounds.Right - BalloonChrome.CornerRadius, contentBounds.Top),
								true, true);
							ctx.BezierTo(
								contentBounds.TopRight,
								new Point(contentBounds.Right, contentBounds.Top + BalloonChrome.CornerRadius),
								new Point(contentBounds.Right, contentBounds.Top + BalloonChrome.CornerRadius),
								true, true);							

							break;
						}
					case Dock.Right:
						{
							ctx.LineTo(
								new Point(contentBounds.Right, contentBounds.Bottom - BalloonChrome.CornerRadius),
								true, true);
							ctx.BezierTo(
								new Point(contentBounds.Right, contentBounds.Bottom - BalloonChrome.CornerRadius),
								contentBounds.BottomRight,
								new Point(contentBounds.Right - BalloonChrome.CornerRadius, contentBounds.Bottom),
								true, true);

							break;
						}
					case Dock.Bottom:
						{
							ctx.LineTo(
								new Point(contentBounds.Left + BalloonChrome.CornerRadius, contentBounds.Bottom),
								true, true);
							ctx.BezierTo(
								contentBounds.BottomLeft,
								new Point(contentBounds.Left, contentBounds.Bottom - BalloonChrome.CornerRadius),
								new Point(contentBounds.Left, contentBounds.Bottom - BalloonChrome.CornerRadius),
								true, true);

							break;

						}
				}
			}
		}

		/// <summary>
		/// Вызывается при изменении условий, влияющих на рендеринг связи.
		/// </summary>
		private void OnConnectionChanged()
		{
			// Выключаем таймер и отменяем связь.
			this._popupWatchTimer.Stop();
			this._renderConnector = false;
			this._isConnected = false;

			// Если декоратор видим, проверяем наличие связанного элемента.
			// Если он есть, сбрасываем размеры и включаем рендеринг коннектора по таймеру.
			if (this.IsOpen && this.ConnectionTarget != null)
			{
				this._isConnected = true;
				this.InvalidateMeasure();
				this._popupWatchTimer.Interval = BalloonChrome._DelayRenderInterval;
				this._popupWatchTimer.Start();
			}

			// Сбрасываем внешний вид элемента.
			this.InvalidateVisual();
		}

		/// <summary>
		/// Вызывается при возникновении события <see cref="DispatcherTimer.Tick"/> таймера,
		/// корректирующего декоратор в соответствии с измененями позиции всплывающего элемента.
		/// </summary>
		/// <param name="sender">Объект, к которому прикреплен обработчик события.</param>
		/// <param name="e">Объект, содержащий аргументы события.</param>
		private void PopupWatchTimer_Tick(object sender, EventArgs e)
		{
			// Потребуются экранные координаты. Чтобы получить их,
			// декоратор должен быть связан с PresentationSource.
			if (PresentationSource.FromVisual(this) == null)
			{
				this._popupWatchTimer.Stop();

				return;
			}

			var zero = this.PointToScreen(new Point(0, 0));

			// Если рендеринг коннектора еще не включен, включаем его и изменяем интервал
			// корректировки, переводя таймер в режим проверки положения всплывающего элемента.
			if (!this._renderConnector)
			{
				this._renderConnector = true;
				this._popupWatchTimer.Tag = zero;
				this._popupWatchTimer.Interval = BalloonChrome._CheckPositionInterval;

				// Сбрасываем рендеринг, чтобы отрисовать коннектор.
				this.InvalidateVisual();

				return;
			}

			// Сравниваем текущую и ранее сохраненную точку (0,0) в экранных координатах.
			var prev = (Point)this._popupWatchTimer.Tag;
			if (prev != zero)
			{
				this._popupWatchTimer.Tag = zero;
				this.InvalidateVisual();
			}
		}
		
		/// <summary>
		/// Определяет точку присоединения коннектора.
		/// </summary>
		/// <param name="chromeConnectionPoint">Расчитанная в координатах <see cref="BalloonChrome"/> точка присоединения коннектора.</param>
		/// <returns><langword>true</langword>, если точка определена; иначе <langword>false</langword>.</returns>
		private bool TryGetConnectorPoint(out Point chromeConnectionPoint)
		{
			// Потребуются экранные координаты. Чтобы получить их,
			// декоратор должен быть связан с PresentationSource.
			if (PresentationSource.FromVisual(this) != null)
			{
				var connectorSize = this.ConnectorSize;
				var connectionPoint = this.ConnectionTarget.PointToScreen(new Point(this.ConnectionX, this.ConnectionY));

				// Для определения положения декоратора, задаем две области:
				// 1. Область декоратора, сжатая на размер коннектора.
				// 2. Окрестность точки присоединения элемента, взятая как пустой прямоугольник,
				//    расширенный на половину размера декоратора.
				var chromeBounds = new Rect(this.PointToScreen(new Point(0, 0)), this.RenderSize);
				chromeBounds.Inflate(-connectorSize, -connectorSize);
				var targetBounds = new Rect(connectionPoint, new Size());
				targetBounds.Inflate(connectorSize  / 2, connectorSize / 2);

				// Определяем положенние коннектора, исходя из расположения границ
				// декоратора относительно окрестности точки присоединения элемента.
				switch(this.ConnectorSide)
				{ 
					case Dock.Top:
						{
							var x = this.IsConnectorCentered 
								? connectorSize + chromeBounds.Width / 2
 								: connectorSize + connectionPoint.X - chromeBounds.Left;

							chromeConnectionPoint = new Point(x, 0);
							return true;
						}
					case Dock.Bottom:
						{
							var x = this.IsConnectorCentered
								? connectorSize + chromeBounds.Width / 2
								: connectorSize + connectionPoint.X - chromeBounds.Left;

							chromeConnectionPoint = new Point(x, chromeBounds.Height + connectorSize * 2);
							return true;
						}
					case Dock.Right:
						{
							var y = this.IsConnectorCentered
								? connectorSize + chromeBounds.Height / 2
								: connectorSize + connectionPoint.Y - chromeBounds.Top;

							chromeConnectionPoint = new Point(chromeBounds.Width + connectorSize * 2, y);
							return true;
						}
					case Dock.Left:
						{
							var y = this.IsConnectorCentered
								? connectorSize + chromeBounds.Height / 2
								: connectorSize + connectionPoint.Y - chromeBounds.Top;
					
							chromeConnectionPoint = new Point(0, y);
							return true;
						}
				}
			}

			chromeConnectionPoint = new Point(this.ConnectionX, this.ConnectionY);
			return false;
		}

		/// <summary>
		/// Возвращает результирующий отступ содержимого от границ декоратора.
		/// </summary>
		/// <returns><see cref="Thickness"/> с результирующим значением.</returns>
		private Thickness GetTotalPadding()
		{
			var result = new Thickness();

			// Суммируем значения свойств, задающих отступы.
			var padding = this.Padding;
			var balloonPadding = this.BalloonPadding;
			result.Left = padding.Left + balloonPadding.Left;
			result.Top = padding.Top + balloonPadding.Top;
			result.Right = padding.Right + balloonPadding.Right;
			result.Bottom = padding.Bottom + balloonPadding.Bottom;

			// Если элемент связан, добавляем отступ для коннектора.
			if (this._isConnected)
			{
				switch (this.ConnectorSide)
				{
					case Dock.Left:
						{
							result.Left += this.ConnectorSize;
							break;
						}
					case Dock.Top:
						{
							result.Top += this.ConnectorSize;
							break;
						}
					case Dock.Right:
						{
							result.Right += this.ConnectorSize;
							break;
						}
					case Dock.Bottom:
						{
							result.Bottom += this.ConnectorSize;
							break;
						}
				}
			}

			return result;
		}

		#region Background Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="Background"/>.
		/// </summary>
		public static readonly DependencyProperty BackgroundProperty = 
			Border.BackgroundProperty.AddOwner(
			    typeof(BalloonChrome),				
			    new FrameworkPropertyMetadata(
			        Border.BackgroundProperty.DefaultMetadata.DefaultValue,
					FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

		/// <summary>
		/// Возвращает или присваивает кисть фона декоратора.
		/// </summary>
		public Brush Background
		{
			get
			{
				return (Brush)this.GetValue(BalloonChrome.BackgroundProperty);
			}
			set
			{
				this.SetValue(BalloonChrome.BackgroundProperty, value);
			}
		}

		#endregion

		#region BorderBrush Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="BorderBrush"/>.
		/// </summary>
		public static readonly DependencyProperty BorderBrushProperty =
			Border.BorderBrushProperty.AddOwner(
				typeof(BalloonChrome),
				new FrameworkPropertyMetadata(
					Border.BorderBrushProperty.DefaultMetadata.DefaultValue,
					FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));

		/// <summary>
		/// Возвращает или присваивает кисть окантовки декоратора.
		/// </summary>
		public Brush BorderBrush
		{
			get
			{
				return (Brush)this.GetValue(BalloonChrome.BorderBrushProperty);
			}
			set
			{
				this.SetValue(BalloonChrome.BorderBrushProperty, value);
			}
		}

		#endregion

		#region BalloonPadding Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="BalloonPadding"/>.
		/// </summary>
		public static readonly DependencyProperty BalloonPaddingProperty = DependencyProperty.Register(
			"BalloonPadding", typeof(Thickness), typeof(BalloonChrome),
			new FrameworkPropertyMetadata(
				Helper.ThicknessZero,
				FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

		/// <summary>
		/// Возвращает или присваивает значение, задающее отступ области рендеринга декоратора от его границ.
		/// </summary>
		public Thickness BalloonPadding
		{
			get
			{
				return (Thickness)this.GetValue(BalloonChrome.BalloonPaddingProperty);
			}
			set
			{
				this.SetValue(BalloonChrome.BalloonPaddingProperty, value);
			}
		}

		#endregion

		#region Padding Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="Padding"/>.
		/// </summary>
		public static readonly DependencyProperty PaddingProperty = 
			Border.PaddingProperty.AddOwner(
				typeof(BalloonChrome),
				new FrameworkPropertyMetadata(
					Border.PaddingProperty.DefaultMetadata.DefaultValue,
					FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

		/// <summary>
		/// Возвращает или присваивает значение, задающее отступ содержимого декоратора.
		/// </summary>
		public Thickness Padding
		{
			get
			{
				return (Thickness)this.GetValue(BalloonChrome.PaddingProperty);
			}
			set
			{
				this.SetValue(BalloonChrome.PaddingProperty, value);
			}
		}

		#endregion
				
		#region IsOpen Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="IsOpen"/>.
		/// </summary>
		public static readonly DependencyProperty IsOpenProperty = Balloon.IsOpenProperty.AddOwner(
			typeof(BalloonChrome),
			new FrameworkPropertyMetadata((d, e) => ((BalloonChrome)d).OnConnectionChanged()));

		/// <summary>
		/// Возвращает или присваивает значение, указывающее, является ли всплывающий элемент,
		/// декорируемый <see cref="BalloonChrome"/>, видимым.
		/// </summary>
		public bool IsOpen
		{
			get
			{
				return (bool)this.GetValue(BalloonChrome.IsOpenProperty);
			}
			set
			{
				this.SetValue(BalloonChrome.IsOpenProperty, value);
			}
		}

		#endregion

		#region ConnectionX Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="ConnectionX"/>.
		/// </summary>
		public static readonly DependencyProperty ConnectionXProperty = DependencyProperty.Register(
			"ConnectionX", typeof(double), typeof(BalloonChrome),
			new FrameworkPropertyMetadata(Helper.DoubleZero, FrameworkPropertyMetadataOptions.AffectsRender));

		/// <summary>
		/// Возвращает или присваивает горизонтальную координату точки присоединения относительно
		/// левого верхнего угла элемента, с которым рисуется связь.
		/// </summary>
		public double ConnectionX
		{
			get
			{
				return (double)this.GetValue(BalloonChrome.ConnectionXProperty);
			}
			set
			{
				this.SetValue(BalloonChrome.ConnectionXProperty, value);
			}
		}

		#endregion

		#region ConnectionY Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="ConnectionY"/>.
		/// </summary>
		public static readonly DependencyProperty ConnectionYProperty = DependencyProperty.Register(
			"ConnectionY", typeof(double), typeof(BalloonChrome),
			new FrameworkPropertyMetadata(Helper.DoubleZero, FrameworkPropertyMetadataOptions.AffectsRender));

		/// <summary>
		/// Возвращает или присваивает вертикальную координату точки присоединения относительно
		/// левого верхнего угла элемента, с которым рисуется связь.
		/// </summary>
		public double ConnectionY
		{
			get
			{
				return (double)this.GetValue(BalloonChrome.ConnectionYProperty);
			}
			set
			{
				this.SetValue(BalloonChrome.ConnectionYProperty, value);
			}
		}

		#endregion

		#region ConnectionTarget Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="ConnectionTarget"/>.
		/// </summary>
		public static readonly DependencyProperty ConnectionTargetProperty = DependencyProperty.Register(
			"ConnectionTarget", typeof(UIElement), typeof(BalloonChrome),
			new FrameworkPropertyMetadata((d, e) => ((BalloonChrome)d).OnConnectionChanged()));

		/// <summary>
		/// Возвращает или присваивает элемент, с которым рисуется связь.
		/// </summary>
		public UIElement ConnectionTarget
		{
			get
			{
				return (UIElement)this.GetValue(BalloonChrome.ConnectionTargetProperty);
			}
			set
			{
				this.SetValue(BalloonChrome.ConnectionTargetProperty, value);
			}
		}

		#endregion

		#region IsConnectorVisible Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="IsConnectorVisible"/>.
		/// </summary>
		public static readonly DependencyProperty IsConnectorVisibleProperty = DependencyProperty.Register(
			"IsConnectorVisible", typeof(bool), typeof(BalloonChrome),
			new FrameworkPropertyMetadata(
				true,
				FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsArrange,
				(d, e) => d.CoerceValue(BalloonChrome.ConnectorSizeProperty)));

		/// <summary>
		/// Возвращает или присваивает значение, указывающее, показывает ли <see cref="Balloon"/>
		/// связь с элементом, заданным в <see cref="ConnectionTarget"/>.
		/// </summary>
		public bool IsConnectorVisible
		{
			get
			{
				return (bool)this.GetValue(BalloonChrome.IsConnectorVisibleProperty);
			}
			set
			{
				this.SetValue(BalloonChrome.IsConnectorVisibleProperty, value);
			}
		}

		#endregion

		#region ConnectorSize Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="ConnectorSize"/>.
		/// </summary>
		public static readonly DependencyProperty ConnectorSizeProperty = 
			Balloon.ConnectorSizeProperty.AddOwner(typeof(BalloonChrome));

		/// <summary>
		/// Возвращает или присваивает значение, определяющее размер, выделяемый для рендеринга связи.
		/// </summary>
		public double ConnectorSize
		{
			get
			{
				return (double)this.GetValue(BalloonChrome.ConnectorSizeProperty);
			}
			set
			{
				this.SetValue(BalloonChrome.ConnectorSizeProperty, value);
			}
		}

		#endregion

		#region IsConnectorCentered Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="IsConnectorCentered"/>.
		/// </summary>
		public static readonly DependencyProperty IsConnectorCenteredProperty =
			DependencyProperty.Register(
				"IsConnectorCentered",
				typeof(bool),
				typeof(BalloonChrome),
				new FrameworkPropertyMetadata(false));

		/// <summary>
		/// Возвращает или присваивает признак того, что коннектор находится по середине стыкуемой стороны.
		/// Это свойство зависимостей.
		/// </summary>
		public bool IsConnectorCentered
		{
			get
			{
				return (bool)this.GetValue(BalloonChrome.IsConnectorCenteredProperty);
			}
			set
			{
				this.SetValue(BalloonChrome.IsConnectorCenteredProperty, value);
			}
		}

		#endregion

		#region ConnectorSide Dependency Property

		/// <summary>
		/// Идентификатор свойства зависимостей <see cref="ConnectorSide"/>.
		/// </summary>
		public static readonly DependencyProperty ConnectorSideProperty = 
			DependencyProperty.Register(
				"ConnectorSide", 
				typeof(Dock),
				typeof(BalloonChrome), 
				new FrameworkPropertyMetadata(default(Dock), FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		/// Возвращает или присваивает сторону, с которой отображается коннектор.
		/// Это свойство зависимостей.
		/// </summary>
		public Dock ConnectorSide
		{
			get
			{
				return (Dock)this.GetValue(BalloonChrome.ConnectorSideProperty);
			}
			set
			{
				this.SetValue(BalloonChrome.ConnectorSideProperty, value);
			}
		}

		#endregion
	}
}