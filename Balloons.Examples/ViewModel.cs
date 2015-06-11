using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Balloons.Examples
{
	public sealed class ViewModel : INotifyPropertyChanged
	{
		private string _left = "100";

		public string Left
		{
			get
			{
				return this._left;
			}
			set
			{
				if (this._left != value)
				{
					this._left = value;
					this.OnPropertyChanged("Left");
					this.OnPropertyChanged("FlowAround");
				}
			}
		}

		private string _top = "100";

		public string Top
		{
			get
			{
				return this._top;
			}
			set
			{
				if (this._top!= value)
				{
					this._top = value;
					this.OnPropertyChanged("Top");
					this.OnPropertyChanged("FlowAround");
				}
			}
		}

		private string _width = "25";

		public string Width
		{
			get
			{
				return this._width;
			}
			set
			{
				if (this._width != value)
				{
					this._width = value;
					this.OnPropertyChanged("Width");
					this.OnPropertyChanged("FlowAround");
				}
			}
		}

		private string _height = "25";

		public string Height
		{
			get
			{
				return this._height;
			}
			set
			{
				if (this._height != value)
				{
					this._height = value;
					this.OnPropertyChanged("Height");
					this.OnPropertyChanged("FlowAround");
				}
			}
		}

		public Rect FlowAround
		{
			get
			{
				double left = 0;
				double.TryParse(this.Left, out left);
				double top = 0;
				double.TryParse(this.Top, out top);
				double width = 0;
				double.TryParse(this.Width, out width);
				double height = 0;
				double.TryParse(this.Height, out height);

				return new Rect(left, top, width, height);
			}
		}

		private int _topDockPriority = 1;
		public int TopDockPriority
		{
			get
			{
				return this._topDockPriority; 
			}
			set
			{ 
				if(this._topDockPriority!=value)
				{
					this._topDockPriority = value;
					this.OnPropertyChanged("TopDockPriority");
				}
			}
		}

		private int _leftDockPriority = 4;
		public int LeftDockPriority
		{
			get
			{
				return this._leftDockPriority;
			}
			set
			{
				if (this._leftDockPriority != value)
				{
					this._leftDockPriority = value;
					this.OnPropertyChanged("LeftDockPriority");
				}
			}
		}

		private int _bottomDockPriority = 2;
		public int BottomDockPriority
		{
			get
			{
				return this._bottomDockPriority;
			}
			set
			{
				if (this._bottomDockPriority != value)
				{
					this._bottomDockPriority = value;
					this.OnPropertyChanged("BottomDockPriority");
				}
			}
		}

		private int _rightDockPriority = 3;
		public int RightDockPriority
		{
			get
			{
				return this._rightDockPriority;
			}
			set
			{
				if (this._rightDockPriority != value)
				{
					this._rightDockPriority = value;
					this.OnPropertyChanged("RightDockPriority");
				}
			}
		}

		private void OnPropertyChanged(string name)
		{
			var handlers = this.PropertyChanged;
			if(handlers!=null)
			{
				handlers(this, new PropertyChangedEventArgs(name));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
