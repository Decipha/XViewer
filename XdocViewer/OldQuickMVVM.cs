using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Quick.MVVM
{
	/// <summary>
	/// quick and dirty implementation of the relay command pattern
	/// </summary>
	public class RelayCommand : ICommand
	{
		Func<bool>     _canExecute;
		Action<object> _execute;

		/// <summary>
		/// construct an always can execute relay command with the given action
		/// </summary>
		/// <param name="execute"></param>
		public RelayCommand(Action<object> execute)
		{
			this._canExecute = () => true;
			this._execute    = execute;
		}

		public RelayCommand(Func<bool> canExecute, Action<object> execute)
		{
			this._canExecute = canExecute;
			this._execute    = execute;

			// pass through the requery event:
			CommandManager.RequerySuggested += (s, e) => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter)
		{
			return _canExecute();
		}

		public void Execute(object parameter)
		{
			_execute(parameter);
		}
	}

	/// <summary>
	/// very basic view-model base
	/// </summary>
	public class SimpleViewModel : INotifyPropertyChanged
	{
		/// <summary>
		/// stores the values for the view-model;
		/// </summary>
		protected Dictionary<string, object> _values = new Dictionary<string, object>();

		/// <summary>
		/// strongly typed get method
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		protected T Get<T>(string name)
		{
			return (T)this[name];
		}

		/// <summary>
		/// fires the PropertyChanged event for all values.
		/// </summary>
		protected void RefreshAll()
		{
			foreach (var k in _values.Keys)
			{
				OnPropertyChanged(k);
			}
		}

		/// <summary>
		/// gets or sets a value from the dictionary; setting a value fires the <see cref="PropertyChanged"/> event
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected object this[string name]
		{
			get
			{

				object tmp = null;
				if (_values.TryGetValue(name, out tmp))
					return tmp;
				else
					return null;
			}
			set { _values[name] = value; OnPropertyChanged(name); }
		}


		/// <summary>
		/// bind the window cursor property to this to get a wait-cursor
		/// </summary>
		public Cursor Cursor
		{
			get { return Get<Cursor>(nameof(Cursor)); }
			set { this[nameof(Cursor)] = value; }
		}

		/// <summary>
		/// used to set the cursor to Wait and back.
		/// </summary>
		public bool IsBusy
		{
			get { return (bool)this[nameof(IsBusy)]; }
			set {

				// set the value
				this[nameof(IsBusy)] = value;

				// set the cursor property:
				if (value)
				{
					this.Cursor = Cursors.Wait;
				}
				else
				{
					this.Cursor = Cursors.Arrow;
				}
			}
		}

		/// <summary>
		/// status text for the form
		/// </summary>
		public string Status
		{
			get { return this[nameof(Status)] as string; }
			set { this[nameof(Status)] = value; }
		}

		/// <summary>
		/// returns true if the application is in design-mode.
		/// </summary>
		public bool InDesignMode
		{
			get
			{
				return (bool)DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty, typeof(FrameworkElement)).Metadata.DefaultValue;
			}
		}

		#region Property Changed

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		#endregion

		#region Safe Invokation

		/// <summary>
		/// invokes an action on the UI thread
		/// </summary>
		/// <param name="action"></param>
		public void SafeInvoke(Action action)
		{
			System.Windows.Application.Current.Dispatcher.Invoke(action);
		}

		/// <summary>
		/// invokes an action on the UI thread and returns a result.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="callback"></param>
		/// <returns></returns>
		public T SafeInvoke<T>(Func<T> callback)
		{
			return System.Windows.Application.Current.Dispatcher.Invoke<T>(callback);
		}

		#endregion
	}

	public class TreeItemBase : SimpleViewModel
	{
		public TreeItemBase()
		{
			this.IsSelected = false;
			this.IsExpanded = false;
			this.IsBusy = false;
		}

		public TreeItemBase(TreeItemBase parent) : this()
		{
			this.OnSelected = parent.OnSelected;
			this.OnExpanded = parent.OnExpanded;
		}

		public Action<TreeItemBase> OnSelected { get; set; }
		public Action<TreeItemBase> OnExpanded { get; set; }

		public bool IsSelected
		{
			get { return (bool)this[nameof(IsSelected)]; }
			set {
				this[nameof(IsSelected)] = value;
				if (value)
				{
					if (OnSelected != null)
						OnSelected.Invoke(this);
				}
			}
		}

		public bool IsExpanded
		{
			get { return (bool)this[nameof(IsExpanded)]; }
			set {

				this[nameof(IsExpanded)] = value;
				if (value)
				{
					if (OnExpanded != null)
						OnExpanded.Invoke(this);
				}
			}
		}
	}

}
